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
    public partial class MapaTerminais : Form
    {
        public MapaTerminais(JanelaPrincipal frm1)
        {
            InitializeComponent();
        }

        private void Form22_Shown(object sender, EventArgs e)
        {
            try
            {
                pictureBox1.Image = Image.FromFile(@"pmus.jpg");
            }
            catch
            {
                MessageBox.Show("A figura com a localização das PMUs não foi encontrada. A figura deve estar no diretório de instalação do aplicativo com o nome \"pmus.jpg\".", "ATENÇÃO!", MessageBoxButtons.OK);
                this.Close();
            }
        }
    }
}
