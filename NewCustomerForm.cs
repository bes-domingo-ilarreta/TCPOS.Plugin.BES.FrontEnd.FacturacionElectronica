using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using TCPOS.FrontEnd.UserInterface.Interfaces;
using TCPOS.FrontEnd.BusinessLogic;
using TCPOS.FrontEnd.UserInterface;
using TCPOS.FrontEnd.DataClasses;
using TCPOS.Debug;

namespace Plugin.BES.FrontEnd.FacturacionElectronica
{
    public partial class NewCustomerForm : Form, IObjectResult 
    {
        private BLogic BL;
        //private DbCustomer customer;
        private string cardNum;
        public NewCustomerForm()
        {
            InitializeComponent();
        }

        public void SetupForm(BLogic BL, string cardNum)
        {
            this.BL = BL;
            this.cardNum = cardNum;
            UserInterfaceHelper.CenterWindow(this);
            UserInterfaceHelper.VisibleForms.Add(this);
            
        }

        public bool AcceptObject(object o)
        {
            throw new NotImplementedException();
        }

        public object ObjectResult
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        private void NewCustomerForm_Load(object sender, EventArgs e)
        {
        }

        private void btnCancelar_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            UserInterfaceHelper.VisibleForms.Remove(this);
        }

        private void btnAceptar_Click(object sender, EventArgs e)
        {
            try
            {
                if (ingresoCliente())
                {
                    this.DialogResult = DialogResult.OK;
                    UserInterfaceHelper.VisibleForms.Remove(this);
                }
                else
                {
                    this.DialogResult = DialogResult.Cancel;
                    UserInterfaceHelper.VisibleForms.Remove(this);
                }
            }
            catch (Exception ex)
            {
                Trace.LogException(ex);
            }

            
        }
        public bool ingresoCliente()
        {
            DbCustomer customer = new DbCustomer();

            customer.CardNum = cardNum;
            customer.Description = txtRazonSocial.Text;
            customer.Street = txtDireccion.Text;
            customer.Notes1 = txtComuna.Text;
            customer.City = txtCiudad.Text;
            customer.Notes2 = txtDirecPostal.Text;
            customer.Notes3 = txtComPostal.Text;
            customer.Notes4 = txtCiudadPostal.Text;
            customer.Type = DbCustomer.Types.Identification;
            customer.VisibilityCriteriaID = 1;
            customer.PrepayPaymentID = 4;
            customer.PrepayPricelevelID = 1;
            customer.CreditPaymentID = 4;
            customer.CreditPricelevelID = 1;
            customer.IdentificationPricelevelID = 1;
            customer["passwd"] = "*";

            BL.DB.InsertCustomer(customer);
            

            if (BL.AddCustomer(null, customer.CardNum, true) == BLogic.AddCustomerResult.Ok)
            {
                BL.RefreshTransactionItems();
                return true;
            }
            return false;
        }
    }


    
}
