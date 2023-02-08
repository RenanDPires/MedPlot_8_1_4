using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.IO;
using MedFasee.Structure;

namespace MedPlot
{
    public partial class GraficoModes : Form
    {
        private JanelaPrincipal pai;
        public int indCons;
        double modo;
        string[] nomes;
        double[] amps, angs;
        DateTime di, df;
        string t;

        Color[] cores;

        // Balão que mostra dados de um ponto no gráfico
        ToolTip tip = new ToolTip();

        public Query Query { get; internal set; }

        public GraficoModes(JanelaPrincipal frm1, double modoMS, string[] nomesMS, double[] ampMS, double[] angMS, DateTime dataIniMS, DateTime dataFinMS, string titulo, Color[] coresAtuais, Query query)
        {
            InitializeComponent();
            // Repassnado para aas variáveis internas
            pai = frm1;
            modo = modoMS;
            nomes = nomesMS;
            amps = ampMS;
            angs = angMS;
            di = dataIniMS;
            df = dataFinMS;
            t = titulo;

            Query = query;

            cores = new Color[coresAtuais.Length];
            cores = coresAtuais;
        }

        private void chart1_MouseDown(object sender, MouseEventArgs e)
        {
            tip.IsBalloon = true;

            // Botão direito do mouse foi pressionado
            if (e.Button == MouseButtons.Right)
            {
                // Aponta em que lugar do gráfico aconteceu o clic
                HitTestResult result = chart1.HitTest(e.X, e.Y);

                if (result.ChartElementType == ChartElementType.LegendItem) // caso o clic foi sobre as legendas das séries
                {
                    LegendItem leg = (LegendItem)result.Object;
                    string nomeLeg = leg.SeriesName;

                    // Se a cor foi escolhida
                    if (colorDialog1.ShowDialog() == DialogResult.OK)
                    {
                        for (int k = 0; k < chart1.Series.Count; k++)
                        {
                            string nomeSerie = chart1.Series[k].Name;
                            if (nomeLeg == nomeSerie)
                            {
                                chart1.Series[k].Color = colorDialog1.Color;
                            }
                        }
                    }
                }
                else if (result.ChartElementType == ChartElementType.DataPoint)
                {
                    tip.ToolTipTitle = result.Series.Name;

                    //tip.Show(Math.Round(chart1.ChartAreas[0].AxisX.PixelPositionToValue(e.Location.X), 2) + "°", chart1, e.X, e.Y);
                    //tip.Show(Math.Round(chart1.ChartAreas[0].AxisX.PixelPositionToValue(e.Location.X), 2) + "°", chart1, e.X, e.Y, 3000);
                    double angle = 360 - result.Series.Points.Last().XValue;
                    if (angle < -180.0)
                        angle = angle + 360.0;

                    // String para verificar se é gráfico de tensão ou corrente
                    string chartTitle = chart1.Titles[0].Text.Substring(0, 19);

                    double modulus = result.Series.Points.Last().YValues[0];
                    tip.Show("Ampl. = " + Math.Round(modulus, 2) + 
                        "\n Fase = " + Math.Round(angle, 2) + "°", chart1, e.X, e.Y, 3000);
                    tip.Show("Ampl. = " + Math.Round(modulus, 2) +
                        "\n Fase = " + Math.Round(angle, 2) + "°", chart1, e.X, e.Y, 3000);

                }
                else
                    tip.Hide(chart1);
            }
            else
                tip.Hide(chart1);
        }

        private void Form10_Shown(object sender, EventArgs e)
        {
            /*
            // Paleta de cores para as séries 
            Color[] cores = new Color[25];

            // Cores selecionadas
            cores[0] = Color.Red;
            cores[1] = Color.Green;
            cores[2] = Color.Blue;
            cores[3] = Color.DarkGoldenrod;
            cores[4] = Color.Orange;
            cores[5] = Color.Lime;
            cores[6] = Color.Purple;
            cores[7] = Color.Gold;
            cores[8] = Color.YellowGreen;
            cores[9] = Color.BlueViolet;
            cores[10] = Color.Turquoise;
            cores[11] = Color.Maroon;
            cores[12] = Color.Salmon;
            cores[13] = Color.DarkSeaGreen;
            cores[14] = Color.DeepSkyBlue;
            cores[15] = Color.LightSeaGreen;
            cores[16] = Color.Magenta;
            cores[17] = Color.Yellow;
            cores[18] = Color.Violet;
            cores[19] = Color.Gray;
            cores[20] = Color.DarkKhaki;
            cores[21] = Color.Aqua;
            cores[22] = Color.Teal;
            cores[23] = Color.Black;
            cores[24] = Color.OliveDrab;
            */
            chart1.PaletteCustomColors = cores;            

            // Número de séries
            int ns = amps.Length;

            /////////////////////
            // Títulos do gráfico
            // Tipo do gráfico base (grandeza traçada, taxa de fasores/s)
            chart1.Titles[0].Text = t;

            string minIni, segIni, horaIni, minFin, segFin, horaFin = "";

            if (di.Hour < 10)
                horaIni = "0" + di.Hour;
            else
                horaIni = Convert.ToString(di.Hour);
            if (di.Minute < 10)
                minIni = "0" + di.Minute;
            else
                minIni = Convert.ToString(di.Minute);
            if (di.Second < 10)
                segIni = "0" + di.Second;
            else
                segIni = Convert.ToString(di.Second);
            if (df.Hour < 10)
                horaFin = "0" + df.Hour;
            else
                horaFin = Convert.ToString(df.Hour);
            if (df.Minute < 10)
                minFin = "0" + df.Minute;
            else
                minFin = Convert.ToString(df.Minute);
            if (df.Second < 10)
                segFin = "0" + df.Second;
            else
                segFin = Convert.ToString(df.Second);

            // Data e horário da janela escolhida para o Prony traçado
            chart1.Titles[1].Text = "Data: " + di.Day + "/" + di.Month + "/" + di.Year
                + " - Horário: " + horaIni + ":" + minIni + ":" + segIni + " - "
                + horaFin + ":" + minFin + ":" + segFin;
            // Modo escolhido para traçar as mode shapes
            chart1.Titles[2].Text = "Modo: " + Math.Round(modo, 3) + " Hz";

            // Limpa o gráfico
            chart1.Series.Clear();

            chart1.ChartAreas[0].AxisX.Crossing = 90;

            double[] angulos = new double[ns];
            // Traça as mode shapes
            for (int k = 0; k < ns; k++)
            {
                // Adiciona quantas séries forem necessárias. O seu nome corresponde ao terminal.
                chart1.Series.Add(nomes[k]);
                // Define o tipo da série recém-criada, aqui usa-se o tipo 'FastLine'
                chart1.Series[k].ChartType = System.Windows.Forms.DataVisualization.
                    Charting.SeriesChartType.Polar;
                angulos[k] = angs[k] * 180 / (Math.PI);
                chart1.Series[k].Points.AddXY(0, 0);
                chart1.Series[k].Points.AddXY(360 - (angs[k]*180/(Math.PI)), amps[k]);                
                chart1.Series[k].BorderWidth = 2;
            }

            chart1.ChartAreas[0].AxisY.LabelStyle.Angle = -45;
            
            // Reversão das etiquetas de ângulo, para ficar no sentido anti-horário
            int c = 0;
            for (int i = 0; i < 12; i++)
            {
                if (i == 0) // põe zero no primeiro
                    c = 0;
                else
                    c = 360 - 30 * i;
                // reversão das etiquetas
                chart1.ChartAreas[0].AxisX.CustomLabels.Add(30 * i - 0.5, 30 * i + 0.5, Convert.ToString(c));            
            }
            
        }

        private void chart1_KeyDown(object sender, KeyEventArgs e)
        {
            // Código para copiar o gráfico para a área de trabalho

            // Verifica se o usuário pressionou as teclas "Ctrl + C" com o form em questão ativo 
            if (e.Control && e.KeyCode == Keys.C)
            {
                // Realça o contorno do gráfico
                chart1.BorderlineDashStyle = ChartDashStyle.Dash;

                MemoryStream ms = new MemoryStream();

                chart1.SaveImage(ms, ChartImageFormat.Bmp);
                Bitmap bm = new Bitmap(ms);
                bm.SetResolution(1024, 1024);
                // Disponibiliza o bitmap na área de trabalho
                Clipboard.SetImage(bm);
            }
        }

    }
}
