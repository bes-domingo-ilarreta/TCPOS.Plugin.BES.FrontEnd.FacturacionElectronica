using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TCPOS.FrontEnd.DataClasses;

namespace Plugin.BES.FrontEnd.FacturacionElectronica
{
    public class DataEmisorClass : LocalDBClassBase
    {
        [DbField("rut_emisor")]
        public string Rut;
        [DbField("rzns_emisor")]
        public string RazonEmisor;
        [DbField("giro_emisor")]
        public string GiroEmisor;
        [DbField("cdg_sii_sucur")]
        public string CodigoSII;
        [DbField("dir_orig")]
        public string DireccionEm;
        [DbField("cmna_origen")]
        public string ComunaEm;
        [DbField("ciudad_origen")]
        public string CiudadEm;
        [DbField("save_xml")]
        public int SaveXML;
        [DbField("rut_emisor_boleta")]
        public string RutBoleta;
        [DbField("rut_agent")]
        public string RutRepLegal;
        [DbField("num_res")]
        public int NumResol;
        [DbField("res_date")]
        public DateTime ResolDate;
        [DbField("act_eco")]
        public int ActEco;
    }
}
