using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Reflection;

namespace MedPlot
{
    public partial class AboutMe : Form
    {
        public AboutMe()
        {
            InitializeComponent();

            //string[] lines = richTextBox1.Lines;
            //lines[3] = "teste";
            //richTextBox1.Lines = lines;

            // Versão do assembly
            Version assemblyVersion = typeof(AboutMe).Assembly.GetName().Version;

            // Data do assembly: terceiro número do 'AssemblyVersion', corresponde ao total de dias desde 01/01/2000
            DateTime assemblyDate = new DateTime(2000, 1, 1);
            assemblyDate = assemblyDate.AddDays(assemblyVersion.Build);

            // Adiciona à linha 3 a versão que está assinalada no "AssemblyInfo.cs" 
            // (na realidade, somente os dois primeiros identificadores da versão - major e minor version numbers)
            richTextBox1.Select(richTextBox1.GetFirstCharIndexFromLine(3), richTextBox1.Lines[3].Length);
            richTextBox1.SelectedText = "Versão " + assemblyVersion.Major.ToString()
                + "." + assemblyVersion.Minor.ToString() + "." + Program.Build + " - " + assemblyDate.ToString("dd/MM/yyyy"); //assemblyDate.ToString("MMM yyyy");

            richTextBox1.SelectAll();
            richTextBox1.SelectionColor = Color.Black;
            richTextBox1.SelectionAlignment = HorizontalAlignment.Center;
            //richTextBox1.DeselectAll();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            // Abre o site do MedFasee, com o navegador padrão
            System.Diagnostics.Process.Start("www.medfasee.ufsc.br");
        }

    }
}
