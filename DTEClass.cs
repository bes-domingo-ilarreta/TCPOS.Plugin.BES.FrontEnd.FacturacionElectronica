using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TCPOS.FrontEnd.BusinessLogic.Plugins;
using TCPOS.FrontEnd.BusinessLogic;
using TCPOS.Utilities;
using TCPOS.Debug;
using Plugin.BES.FrontEnd.FacturacionElectronica.BESdteLOCAL;
using TCPOS.FrontEnd.DataClasses;
using TCPOS.DbHelper;
using System.Xml;
using System.Data;
using System.Windows.Forms;
using TCPOS.FrontEnd.UserInterface.Interfaces;

namespace Plugin.BES.FrontEnd.FacturacionElectronica
{
    public class DTEClass : IPlugin 
    {
        //DECLARACIONES
        public BLogic BL;
        private DbCustomer nuevoCustomer;
        public DbCustomer datosReceptor;
        private string webAddress,rut; //Getted from DB or INI file
        private bool enableWebService;//Getted from DB or INI file
        private DTELocal webservice;
        private DbCustomer customer;
        private SolicitarFolio solicitarFolio;
        private ProcesarTXT resultProcesarTXTDTE;
        private ProcesarTXTBoleta resultProcesarTXTBoleta;
        //FIN DECLARACIONES

        public void Register(BLogic BL, PluginManager PM)
        {
            //throw new NotImplementedException();
            this.BL = BL;

            //EVENTOS
            //Read from TCPOS.FRONTEND INI file
            PM.OnProcessUnknownIniParameter += new IniParameterEvent(PM_OnProcessUnknownIniParameter);
            PM.OnDataAvailable += new NotificationEvent(PM_OnDataAvailable);
            PM.OnBeforeCloseTransaction += new BeforeCloseTransactionEvent(PM_OnBeforeCloseTransaction);
            PM.OnSerializeToDbTransItemInElement += new SerializeTransItemEvent(PM_OnSerializeToDbTransItemInElement);
            PM.OnBeforeKeyPress += new KeyPressEvent(PM_OnBeforeKeyPress);
            PM.OnSerializeToPrinterTransItemInElement += new SerializeTransItemEvent(PM_OnSerializeToPrinterTransItemInElement);
            PM.OnAfterCloseTransaction += new SimpleTransactionNotificationEvent(PM_OnAfterCloseTransaction);
        }

        void PM_OnAfterCloseTransaction(Transaction transaction)
        {
            //throw new NotImplementedException();
            customer = new DbCustomer();
        }

        void PM_OnSerializeToPrinterTransItemInElement(XmlStringWriter writer, TransItem item)
        {
            //throw new NotImplementedException();
            if (item is Transaction)
            {
                Transaction transaction = item as Transaction;
                string foliosii = transaction.GetCustomField("bes_folio_num").ToString();
                writer.WriteField("folionum", foliosii);

                string webserviceChileSignature = transaction.GetCustomField("webservice_bes_signature").ToString();
                writer.WriteField("dte_firma", webserviceChileSignature);
            }
        }

        void PM_OnBeforeKeyPress(KeyData key, ref bool processed)
        {
            //throw new NotImplementedException();
            if (key.FunctionCode == 10003)
            {
                consultaCliente();               
            }
        }

        //GRABA EN DB
        void PM_OnSerializeToDbTransItemInElement(XmlStringWriter writer, TransItem item)
        {
            //throw new NotImplementedException();
            if (item is Transaction)
            {
                Transaction transaction = item as Transaction;
                string webserviceChileSignature = transaction.GetCustomField("webservice_bes_signature").ToString();
                writer.WriteField("dte_firma", webserviceChileSignature);

                //webservice_bes_folionum
                string webserviceChileFolioNUM = transaction.GetCustomField("webservice_bes_folionum").ToString();
                writer.WriteField("bes_folio_num", webserviceChileFolioNUM);

                string doctypeBES = transaction.GetCustomField("dte_doc_type").ToString();
                writer.WriteField("doc_type", doctypeBES);
            }
        }//FINISH PM_OnSerializeToDbTransItemInElement

        //ANTES DEL CIERRE DE LA TRANSACCION
        void PM_OnBeforeCloseTransaction(Transaction transaction, ref bool abort)
        {
            //throw new NotImplementedException();
            // ARROJA ERROR SI EL SERVICIO NO ESTA DISPONIBLE


            if (webservice == null)
            {
                abort = true;
                BL.MsgError("WEBSERVICE NO COMUNICATION");
                return;
            }
            else
            {
                string printoutType = "";
                if (transaction.ManualPrintoutSelected == DbPayment.PrintoutType.KeepDefault || transaction.ManualPrintoutSelected == DbPayment.PrintoutType.NoPrintout)
                    printoutType = "39";
                if (transaction.ManualPrintoutSelected == DbPayment.PrintoutType.Invoice)
                    printoutType = "33";
                if (transaction.ManualPrintoutSelected == DbPayment.PrintoutType.Bill)
                    printoutType = "39";
                if (transaction.ManualPrintoutSelected == DbPayment.PrintoutType.Invoice && transaction.Customer == null)
                {
                    BL.MsgError("WE NEED A CUSTOMER");
                    abort = true;
                }
                string xmlData = "";
                string documentType = "";

                DbCustomer receptorDTE = new DbCustomer();
                receptorDTE = customer;

                XmlStringWriter writer = new XmlStringWriter(BL.DB);
                writer.WriteRaw(@"<?xml version=""1.0"" encoding=""ISO-8859-1""?>");

                if (printoutType == "33")//FACTURA
                {
                    //INICIO DTE
                    writer.WriteStartElement("DTE");
                    writer.WriteAttribute("version", "1.0");
                    writer.WriteAttribute("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");
                    writer.WriteAttribute("xmlns:xsd", "http://www.w3.org/2001/XMLSchema");
                    writer.WriteAttribute("xmlns", "http://www.sii.cl/SiiDte");
                    //FIN DTE
                }
                else//BOLETA
                {
                    //INICIO EnvioBOLETA
                    writer.WriteStartElement("EnvioBOLETA");
                    writer.WriteAttribute("version", "1.0");
                    writer.WriteAttribute("xmlns", "http://www.sii.cl/SiiDte");
                    writer.WriteAttribute("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");
                    writer.WriteAttribute("xsi:schemaLocation", "http://www.sii.cl/SiiDte EnvioBOLETA_v11.xsd");
                    //FIN EnvioBoleta
                }

                //RUT RECEPTOR
                string rrecep;
                rrecep = getRutReceptorDTE();                
                //RUT RECEPTOR

                if (printoutType == "39")//BOLETA
                {
                    //ONLY BOLETA
                    writer.WriteStartElement("SetDTE");
                    writer.WriteAttribute("ID", string.Format("ENVBOL-{0}", transaction.TransDate.ToString("yyyyMMddHHmmss")));
                    writer.WriteStartElement("Caratula");
                    writer.WriteAttribute("version", "1.0");
                    //string shopRut = BL.DB.Shop["rut"].ToString();
                    string shopRut = "76328464-6"; //MANAGE THIS FROM DB ON FRONTEND
                    writer.WriteElement("RutEmisor", shopRut);
                    //string RutEnvia = BL.DB.Shop["RutEnvia"].ToString();
                    string RutEnvia = "8833649-6";//MANAGE THIS FROM DB ON FRONTEND
                    writer.WriteElement("RutEnvia", RutEnvia);
                    //string RutReceptor = BL.DB.Shop["RutReceptor"].ToString();
                    //string RutReceptor = "66666666-6";
                    writer.WriteElement("RutReceptor", rrecep);
                    //DateTime FchResol = Convert.ToDateTime(BL.DB.Shop["FchResol"]);
                    DateTime FchResol = new DateTime(2014, 04, 22);
                    writer.WriteElement("FchResol", FchResol.ToString("yyyy-MM-dd"));//MANAGE THIS FROM DB ON FRONTEND
                    writer.WriteElement("NroResol", 0);//MANAGE THIS FROM DB ON FRONTEND
                    writer.WriteElement("TmstFirmaEnv", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"));
                    writer.WriteStartElement("SubTotDTE");
                    writer.WriteElement("TpoDTE", printoutType);
                    writer.WriteElement("NroDTE", 1);
                    writer.WriteEndElement();
                    writer.WriteEndElement();

                    writer.WriteStartElement("DTE");
                    writer.WriteAttribute("version", "1.0");
                    //END ONLY BOLETA
                }
                //string besRut = "76054473-6"; //MANAGE THIS FROM DB ON FRONTEND dataEmisor.Rut
                //
                string besRut = dataEmisor.Rut;
                if (printoutType == "39")
                {
                    //ONLY BOLETA
                    writer.WriteStartElement("Documento");
                    //string besRut = myClassPAramData["besRUT"].ToString();
                    string besRutBoletaE = "76328464-6";
                    solicitarFolio = webservice.Solicitar_Folio(besRutBoletaE, 39);
                    writer.WriteAttribute("ID", string.Format("R{0}T{1}F{2}", besRutBoletaE, printoutType, solicitarFolio.Folio));
                    //END ONLY BOLETA
                }
                else
                {
                    solicitarFolio = webservice.Solicitar_Folio(besRut, 33);
                    writer.WriteStartElement("Documento");
                }

                writer.WriteStartElement("Encabezado");
                writer.WriteStartElement("IdDoc");
                writer.WriteElement("TipoDTE", printoutType);
                writer.WriteElement("Folio", solicitarFolio.Folio);
                writer.WriteElement("FchEmis", DateTime.Now.ToString("yyyy-MM-dd"));

                if (printoutType == "39")
                {
                    //SOLO BOLETA
                    writer.WriteElement("IndServicio", 3);
                    //SOLO BOLETA
                }
                else
                {
                    //TermPagoGlosa
                    //FchVenc
                    writer.WriteElement("TermPagoGlosa", "30 dias Precio Contado");///////////////////////////////////////////ASK 4 THIS FIELD
                    writer.WriteElement("FchVenc", "2016-06-30");/////////////////////////////////////////////////////////////ASK 4 THIS FIELD
                }

                writer.WriteEndElement(); //IdDoc

                //OBTENER DATOS EMISOR FROM ADMIN
                //DataTable dtbemisor = BL.DB.ExecuteDataTable("SELECT * FROM bes_emisor");
                //object aaa = dtbemisor.Rows[0]["rut_emisor"];


                //FIN DATOS EMISOR

                if (printoutType == "39")
                {
                    writer.WriteStartElement("Emisor");
                    writer.WriteElement("RUTEmisor", dataEmisor.Rut);
                    writer.WriteElement("RznSocEmisor", dataEmisor.RazonEmisor);
                    writer.WriteElement("GiroEmisor", dataEmisor.GiroEmisor);
                    writer.WriteElement("CdgSIISucur", dataEmisor.CodigoSII);
                    writer.WriteElement("DirOrigen", dataEmisor.DireccionEm);
                    writer.WriteElement("CmnaOrigen", dataEmisor.ComunaEm);
                    writer.WriteElement("CiudadOrigen", dataEmisor.CiudadEm);
                    writer.WriteEndElement(); //Emisor
                }
                else
                {
                    writer.WriteStartElement("Emisor");
                    //<Acteco>602300</Acteco>
                    writer.WriteElement("Acteco", "602300");///////////////////////////////////////////ASK 4 THIS FIELD
                    writer.WriteElement("RUTEmisor", dataEmisor.Rut);
                    writer.WriteElement("RznSoc", dataEmisor.RazonEmisor);
                    writer.WriteElement("GiroEmis", dataEmisor.GiroEmisor);
                    writer.WriteElement("DirOrigen", dataEmisor.DireccionEm);
                    writer.WriteElement("CmnaOrigen", dataEmisor.ComunaEm);
                    writer.WriteElement("CiudadOrigen", dataEmisor.CiudadEm);
                    //<CdgVendedor>-Ningún empleado del departamento de ventas-</CdgVendedor>
                    writer.WriteElement("CdgVendedor", BL.CurrentOperator.Code);
                    writer.WriteEndElement(); //Emisor
                }

                //OBJETO RECEPTOR
                DbCustomer receptorC = BL.DB.GetCustomerByCardNum((rrecep.Replace("-","")), false);

                //OBJETO RECEPTOR

                writer.WriteStartElement("Receptor");
                //writer.WriteElement("RUTRecep", "66666666-6");//THIS NOT
                writer.WriteElement("RUTRecep", rrecep);//THIS YEP
                                

                if (printoutType == "33")//FACTURA
                {
                    //<CdgIntRecep>76124037C</CdgIntRecep>
                    writer.WriteElement("CdgIntRecep", getCodeIntRecep(rrecep));//ASK = RUT - DV
                    writer.WriteElement("RznSocRecep", receptorC.C_o);
                    //<GiroRecep>VENTA AL POR MAYOR DE OTROS PRODUCTOS N.</GiroRecep>
                    writer.WriteElement("GiroRecep", receptorC.C_o);
                    writer.WriteElement("DirRecep", receptorC.Street);
                    writer.WriteElement("CmnaRecep", receptorC.Notes1);
                    writer.WriteElement("CiudadRecep", receptorC.City);
                    writer.WriteEndElement(); //Receptor
                }
                else//BOLETA
                {                    
                    writer.WriteElement("RznSocRecep", null);
                    writer.WriteElement("Contacto", null);//ASK NUM TELEFONO
                    writer.WriteElement("DirRecep", null);
                    writer.WriteElement("CmnaRecep", null);
                    writer.WriteElement("CiudadRecep", null);
                    writer.WriteElement("DirPostal", null);
                    writer.WriteElement("CmnaPostal", null);
                    writer.WriteElement("CiudadPostal", null);
                    writer.WriteEndElement(); //Receptor
                }

                writer.WriteStartElement("Totales");

                if (printoutType == "33")//FACTURA
                {
                    //<TpoMoneda>PESO CL</TpoMoneda>
                    //<MntNeto>4744746</MntNeto>
                    //<TasaIVA>19.00</TasaIVA>
                    //<IVA>901502</IVA>
                    double monto,neto,iva;
                    string netoS, ivaS;
                    monto = SafeConvert.ToInt32(transaction.Total);
                    neto = monto * 0.81;
                    iva = monto - neto;
                    netoS = SafeConvert.ToString(neto);
                    ivaS = SafeConvert.ToString(iva);
                    //neto = SafeConvert.ToInt32(neto);
                    //iva = SafeConvert.ToInt32(iva);

                    writer.WriteElement("TpoMoneda", "PESO CL");
                    writer.WriteElement("MntNeto", netoS);
                    writer.WriteElement("TasaIVA", "19.00");
                    writer.WriteElement("IVA", ivaS);
                }
                writer.WriteElement("MntTotal", SafeConvert.ToInt32(transaction.Total));
                writer.WriteEndElement(); //Totales

                writer.WriteEndElement(); //Encabezado

                int position = 1;
                foreach (TransArticle art in transaction.GetItems<TransArticle>())
                {
                    writer.WriteStartElement("Detalle");
                    writer.WriteElement("NroLinDet", position);
                    writer.WriteStartElement("CdgItem");
                    writer.WriteElement("TpoCodigo", "Interna");
                    writer.WriteElement("VlrCodigo", art.Data.Code);
                    writer.WriteEndElement(); //CdgItem
                    writer.WriteElement("NmbItem", art.Data.Description);

                    if (printoutType == "33")
                    {
                        writer.WriteElement("DscItem", art.Data.Description);
                    }

                    writer.WriteElement("QtyItem", art.Data.MeasureUnit == DbArticle.Units.Pieces ? art.Quantity : 1);
                    writer.WriteElement("UnmdItem", null);
                    writer.WriteElement("PrcItem", art.UnitPrice);
                    writer.WriteElement("MontoItem", art.TotalPrice);
                    writer.WriteEndElement(); //Detalle
                    position++;
                }

                writer.WriteEndElement(); //Documento
                writer.WriteEndElement(); //DTE

                if (printoutType == "39")
                {
                    writer.WriteEndElement(); //SetDTE                
                    writer.WriteEndElement(); //EnvioBOLETA || DTE
                }

                string xmlString = writer.ToString();
                

                if (printoutType == "33")
                {
                     resultProcesarTXTDTE = webservice.Carga_TXTDTE(xmlString, "XML");
                }
                else
                {
                     resultProcesarTXTBoleta = webservice.Carga_TXTBoleta(xmlString, "XML");
                }
                string fob;
                if (printoutType == "33") { fob = "Factura"; } else { fob = "Boleta"; }

                if (dataEmisor.SaveXML == 1)
                {
                    //SAVE TO FILE XML
                    System.IO.File.WriteAllText(@"C:\xmlTCPOS\" + fob + "" + DateTime.Now.ToString("yyyyMMddHHmm") + ".xml", xmlString);
                    //END SAVE TO FILE XML
                }

                if (printoutType == "33")
                {
                    if (resultProcesarTXTDTE.XML != "")
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(resultProcesarTXTDTE.XML);
                        XmlElement root = doc.DocumentElement;
                        if (root == null)
                        {
                            abort = true;
                            throw new Exception("XML BAD FORMAT");
                        }
                        XmlNodeList SignatureValue = root.GetElementsByTagName("TED");
                        string signature = "";
                        foreach (XmlNode tableNode in SignatureValue)
                        {
                            signature = tableNode.InnerXml;
                        }
                        if (signature != "")
                            signature = string.Format("<TED version=\"1.0\">" + signature + "</TED>");
                        transaction.SetCustomField("webservice_bes_signature", signature);
                        transaction.SetCustomField("dte_doc_type", printoutType);

                        XmlNodeList folioNUMValue = root.GetElementsByTagName("F");
                        string folioNUM = "";
                        foreach (XmlNode tableNode in folioNUMValue)
                        {
                            folioNUM = tableNode.InnerXml;
                        }

                        if (folioNUM != "")
                            folioNUM = string.Format("" + folioNUM + "");


                        transaction.SetCustomField("webservice_bes_folionum", folioNUM);
                        //GUARDA FOLIO 2 PRINT
                        if (folioNUM != "")
                        {
                            transaction.SetCustomField("bes_folio_num", folioNUM);
                        }
                        //END GUARDA FOLIO 2 PRINT
                    }
                    else
                    {
                        abort = true;
                        BL.MsgError("No answer from webservice,\nelectronic invoice cannot be accepted\nPlease use another type of document");
                    }
                }
                else
                {
                    //if response from webservice read xml response and get the signature, save it into transaction and print signature
                    if (resultProcesarTXTBoleta.XML != "")
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(resultProcesarTXTBoleta.XML);
                        XmlElement root = doc.DocumentElement;
                        if (root == null)
                        {
                            abort = true;
                            throw new Exception("XML BAD FORMAT");
                        }

                        //OBTENER TED Y GUARDAR
                        XmlNodeList SignatureValue = root.GetElementsByTagName("TED");
                        string signature = "";
                        foreach (XmlNode tableNode in SignatureValue)
                        {
                            signature = tableNode.InnerXml;
                        }

                        if (signature != "")
                            signature = string.Format("<TED version=\"1.0\">" + signature + "</TED>");

                        transaction.SetCustomField("webservice_bes_signature", signature);
                        transaction.SetCustomField("dte_doc_type", printoutType);
                        //FIN OBTENER TED Y GUARDAR
                        
                        //OBTENER FOLIONUM Y GUARDAR
                        XmlNodeList folioNUMValue = root.GetElementsByTagName("F");
                        string folioNUM = "";
                        foreach (XmlNode tableNode in folioNUMValue)
                        {
                            folioNUM = tableNode.InnerXml;
                        }

                        if (folioNUM != "")
                            folioNUM = string.Format("" + folioNUM + "");


                        transaction.SetCustomField("webservice_bes_folionum", folioNUM);
                        //FIN FOLIONUM Y GUARDAR
                        //GUARDA FOLIO 2 PRINT
                        if (folioNUM != "")
                        {
                            transaction.SetCustomField("bes_folio_num", folioNUM);
                        }
                        //END GUARDA FOLIO 2 PRINT
                    }
                    else
                    {
                        abort = true;
                        BL.MsgError("No answer from webservice,\nelectronic invoice cannot be accepted\nPlease use another type of document");
                    }
                }
                

            }

        }//FINISH PM_OnBeforeCloseTransaction
        private DataEmisorClass dataEmisor;
        void PM_OnDataAvailable()
        {
            //OBTENER OBJETO FROM DB | DataEmisorClass | XML config file
            if(BL.DB.TableExists("bes_emisor"))
            {
                dataEmisor = BL.DB.SelectObject<DataEmisorClass>("SELECT * FROM bes_emisor");                
            }
            //throw new NotImplementedException();
            webservice = null;
            if (webservice == null)
            {
                webservice = new DTELocal();
                try
                {
                    EstadoDocto result = webservice.Estado_DTE("76328464-6", 39, 1);
                    //if (result.Estatus == 2)
                    //    //BL.MsgInfo("WEBSERVICE OK!");
                    //else
                    //    //BL.MsgInfo("WEBSERVICE NOT OK!");
                }
                catch (Exception e)
                {
                    Trace.LogException(e);
                    webservice = null;
                    //BL.MsgError("WEBSERVICE NO COMUNICATION");
                }
            }

        }//FINISH PM_OnDataAvailable

        void PM_OnProcessUnknownIniParameter(IniValue param, ref bool processed)
        {
            //throw new NotImplementedException();
            //GET PARAMETERS FROM INI file
            webAddress = "";
            enableWebService = false;

            if (param.Name.ToUpper() == "BESCONSULTING")
            {
                try
                {
                    webAddress = param.ValueOf("webserviceaddress", "");
                    enableWebService = param.ValueOf("enablewebservice", "").ToUpper() == "YES";
                }
                catch (Exception ex)
                {
                    param.ErrorDescription = ex.Message;
                    throw (ex);
                }
                processed = true;
            }

        }//FINISH PM_OnProcessUnknownIniParameter

        private void consultaCliente()
        {
            //if (true) 
            //    return;
            if(BL.CurrentTransaction != null && BL.CurrentTransaction.Customer == null)
            {
                //USING FORM
                using (RutForm rutForm = new RutForm())
                {
                    rutForm.SetupForm(BL);
                    rutForm.Focus();
                    if (rutForm.ShowDialog() == DialogResult.OK)
                    {
                        rut = rutForm.txtRut.Text;
                        if (validaRut(rut))
                        {
                            //BL.MsgInfo("RUT VALIDO");
                        }
                        else
                        {
                            BL.MsgInfo("RUT NO VALIDO");
                            return;
                        }
                    }
                    else
                    {
                        customer = new DbCustomer();
                    }
                }
                //END USING FOMR

                KeypadParameters param = new KeypadParameters("");
                KeypadResult result;

                if (rut == null)
                {
                    return;
                }
                rut = parseRUT(rut);
                string myCustomer = SafeConvert.ToString(BL.DB.CentralDbExecuteScalar("SELECT card_num FROM customers WHERE card_num = " + SqlHelper.Quote(rut)));
                

                ////ONLY TESTING
                //if (myCustomer != "")
                //{
                //    //return true; //CLIENTE EXISTE
                //    BL.MsgInfo("CLIENTE EXISTE");
                //    BL.RefreshTransactionItems();

                //}
                //else
                //{
                //    //return false;//CLIENTE NO EXISTE
                //    BL.MsgInfo("CLIENTE NO EXISTE");
                //}
                ////ONLY TESTING
                //DbCustomer customer = null;
                customer = null;
                if (myCustomer != "")
                    customer = BL.DB.GetCustomerByCardNum(myCustomer, false);
                if (customer == null)
                    customer = BL.DB.GetCustomerByCardNum(rut, false);

                if (customer != null)
                {
                    BL.AddCustomer(null, customer.CardNum, true);
                    BL.RefreshTransactionItems();
                }
                else
                {
                    if (BL.MsgQuestion("Cliente no exite, deseas crearlo?", 5) == true)
                    {
                        //INICIO FORMULARIO NUEVO CLIENTE
                        BL.MsgWarning("Has Decidido Guardar al Cliente");
                        using (NewCustomerForm customerForm = new NewCustomerForm())
                        {
                            customerForm.SetupForm(BL, rut);
                            customerForm.Focus();
                            if (customerForm.ShowDialog() == DialogResult.OK)
                            {                       
                            }
                        }
                        //FIN INGRESO FORMULARIO NUEVO CLIENTE
                    }
                    else
                    {
                        BL.MsgInfo("Has Decidido NO guardar al Cliente");
                        return;
                    }
                }
            }
        }
        string parseRUT(string rut)
        {
            rut = rut.Replace(".", "");
            rut = rut.Replace(",", "");
            rut = rut.Replace("-", "");

            return rut;
        }
        public bool validaRut(string rut)
        {
            bool validacion = false;
            try
            {
                rut = rut.ToUpper();
                rut = rut.Replace(".", "");
                rut = rut.Replace("-", "");
                int rutAux = int.Parse(rut.Substring(0, rut.Length - 1));

                char dv = char.Parse(rut.Substring(rut.Length - 1, 1));

                int m = 0, s = 1;
                for (; rutAux != 0; rutAux /= 10)
                {
                    s = (s + rutAux % 10 * (9 - m++ % 6)) % 11;
                }
                if (dv == (char)(s != 0 ? s + 47 : 75))
                {
                    validacion = true; //RUT VALIDO
                }
            }
            catch (Exception ex)
            {
                Trace.LogException(ex);
            }
            return validacion;
        }
        public string getRutReceptorDTE()
        {
            string resultado;
            int size;
            resultado = "";
            size = 0;

            if(rut != null)
            {
                resultado = rut;
                size = resultado.Length;
                resultado = resultado.Insert((size - 1), "-");
            }
            else{
                resultado = "66666666-6";
            }
            return resultado;
        }
        public string getCodeIntRecep(string rut)
        {            
            string resultado;
            resultado = "";

            resultado = rut;
            resultado = resultado.Replace("-", "");
            resultado = resultado.Remove(resultado.Length - 1);
            resultado = resultado + "C";

            return resultado;        
        }
    }
}
