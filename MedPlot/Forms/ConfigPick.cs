using System;
using System.Drawing;
using System.Windows.Forms;
using System.Reflection;
using System.IO;

namespace MedPlot
{
    public partial class ConfigPick : Form
    {
        private JanelaPrincipal pai;
        string dirDestino;
        int n = 0;
        int[] tam;

        bool erroCopia = false;

        public ConfigPick(JanelaPrincipal frm1, string nomeCompleto)
        {
            InitializeComponent();

            // IMPORTANTE!!!
            pai = frm1;

            // Pasta da consulta escolhida
            dirDestino = nomeCompleto;
        }

        private void Form11_Shown(object sender, EventArgs e)
        {
            pai.HabilitaLista(false);

            // Preenche o form com 'radiobuttons' correspondentes aos arquivos de terminais nas pastas existentes em \Config\pdc
            try
            {
                // Pasta 'Config'
                string dirPdc = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\Config\\";

                // Arquivos XML no diretório de dados
                string[] files = Directory.GetFiles(dirPdc, "*.xml");

                // inicialização dos vetores de tamanho
                tam = new int[files.Length];

                foreach (string s in files)
                {
                    // Apenas o nome do arquivo
                    // Use static Path methods to extract only the file name from the path.
                    string name = Path.GetFileNameWithoutExtension(s);

                    RadioButton rb = new RadioButton();
                    // Necessário para se adequar ao tamanho do nome do SPMS
                    rb.AutoSize = true;
                    rb.Name = "RadioButton" + n;
                    rb.Text = name;
                    rb.Location = new Point(40, 40 * (n + 1));
                    // Marcar o primeiro 'radiobutton' adicionado
                    if (n == 0)
                    {
                        rb.Checked = true;
                    }
                    // Adiciona o 'radiobutton' ao 'form'
                    this.Controls.Add(rb);

                    tam[n] = rb.Text.Length;
                    n++;
                }         

                // Organiza os tamanhos dos textos dos radiobittons
                Array.Sort(tam);
                // Configura as dimensões do 'form'
                this.Size = new Size(80 + 8 *tam[tam.Length-1], 40 * (n + 1) + 80);
                // Bloqueia modificações no tamanho do 'form'
                this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
                this.MinimizeBox = false;
                this.MaximizeBox = false;
                // Posição do 'button1'
                button1.Left = (this.ClientSize.Width / 2 - button1.Width - 20); //((this.ClientSize.Width/4) - (button1.Width/4));
                button1.Top = (40 * (n + 1));
                // Posição do 'button2'
                button2.Left = (this.ClientSize.Width/2 + 20); //(3*(this.ClientSize.Width/4) - (button1.Width/4));
                button2.Top = (40 * (n + 1));
                // Posição do 'groupbox'
                groupBox1.Left = (this.ClientSize.Width / 2 - groupBox1.Width / 2);
                groupBox1.Top = (this.ClientSize.Height / 2 - groupBox1.Height / 2);

                //this.AutoSize = true;
            }
            catch(Exception)
            {

            }
        }

        private void Form11_FormClosed(object sender, FormClosedEventArgs e)
        {
            pai.HabilitaLista(true);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string dirOrigem = "";
            string cfgName = "";

            try
            {
                // Verificar qual das opções foi escolhida
                for (int i = 0; i < n; i++)
                {
                    // Encontra o controle
                    Control[] c = this.Controls.Find("RadioButton" + i, false);

                    // Se a opção está marcada
                    if (((RadioButton)c[0]).Checked == true)
                    {
                        cfgName = ((RadioButton)c[0]).Text + ".xml";
                        dirOrigem = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\Config\\" + cfgName;
                    }

                    // Deixar cinza as opções 
                    ((RadioButton)c[0]).Enabled = false;
                }

                // Inicia o 'timer'
                timer1.Start();

                #region Cópia do arquivo

                // Copiar o 'terminais.cfg' para a pasta de dados
                File.Copy(dirOrigem, dirDestino + "\\" + cfgName, true);

                #endregion

                groupBox1.Visible = true;

                progressBar1.Maximum = timer1.Interval;
                for (int i = 0; i < timer1.Interval; i++)
                {
                    progressBar1.Value = i;
                }
                
            }
            catch (Exception)
            {
                erroCopia = true;
            }            
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Stop();

            if (erroCopia == true)
            {
                MessageBox.Show("Erro ao copiar arquivo 'terminais.cfg'.", "Erro", MessageBoxButtons.OK);
            }

            this.Close();
        }

    }
}
