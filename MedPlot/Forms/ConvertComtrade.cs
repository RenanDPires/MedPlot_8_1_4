using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MedPlot
{    
    public partial class ConvertComtrade : Form
    {
        public string dirDados;
        public string pastaCorrente;
        System.Threading.Thread comtradeThread;
        public bool threafinalizada; //flag que indica que o thread foi finalizado pelo código, e não pelo clique do usuário
        public ConvertComtrade()
        {
            InitializeComponent();
        }

        private void Form18_Shown(object sender, EventArgs e)
        {
            //iniciar o processo de conversão em um novo thread
            threafinalizada = false;
            comtradeThread = new System.Threading.Thread(() => COMTRADE.ExportaConsulta(this, Application.ProductName, dirDados, pastaCorrente));
            comtradeThread.Start();
        }

        private void Form18_FormClosing(object sender, FormClosingEventArgs e)
        {
            //impede que o usuário feche o form antes de terminar a conversão            
            if (!threafinalizada)
            {
                e.Cancel=true;
            }
        }
    }
}
