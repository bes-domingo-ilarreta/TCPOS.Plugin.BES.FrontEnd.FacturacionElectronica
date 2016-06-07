using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TestingModules
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
            int size;
            size = 0;

            resultado = txtRUT.Text;
            //resultado = resultado.Remove(resultado.Length - 2);
            size = resultado.Length;

            resultado = resultado.Insert((size - 1), "-");

            //resultado = resultado + "C";

            return resultado;
        }
        //private int returnResultado()
        //{
        //    double resultado;
        //    resultado = Convert.ToInt32(txtRUT.Text);

        //    resultado = Convert.ToInt32(resultado);
        //    return resultado;
        //}
    }
}
