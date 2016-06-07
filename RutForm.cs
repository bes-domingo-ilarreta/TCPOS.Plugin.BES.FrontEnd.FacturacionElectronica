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

namespace Plugin.BES.FrontEnd.FacturacionElectronica
{
    public partial class RutForm : Form, IObjectResult
    {
        private BLogic BL;
        public RutForm()
        {
            InitializeComponent();
        }
        public void SetupForm(BLogic BL)
        {
            this.BL = BL;
            UserInterfaceHelper.CenterWindow(this);
            UserInterfaceHelper.VisibleForms.Add(this);
        }

        private void txtRut_Click(object sender, EventArgs e)
        {
            KeypadParameters param = new KeypadParameters("RUT");
            param.DefaultValue = txtRut.Text;
            param.MaxLength = 10;

            KeypadResult result;
            if (BL.AlphaKeypad(param, out result))
                txtRut.Text = result.StringValue;
        }

        private void btnCancelar_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            UserInterfaceHelper.VisibleForms.Remove(this);
        }

        private void btnAceptar_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            UserInterfaceHelper.VisibleForms.Remove(this);
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
    }
}
