using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ModulesTesting
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            txtResultado.Text = returnCode();
        }
        private string returnCode()
        {
            string resultado;
            resultado = "";

            resultado = txtRUT.Text;
            resultado = resultado.Remove(resultado.Length - 1);

            return resultado;
        }
    }
}
