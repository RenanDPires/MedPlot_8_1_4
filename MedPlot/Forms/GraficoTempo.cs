using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Windows.Forms.DataVisualization.Charting;
using MedFasee.Structure;
using MedFasee.Equipment;

namespace MedPlot
{
    public partial class GraficoTempo : Form
    {
        private JanelaPrincipal pai;
        private int opGraf;  // opção do gráfico original que terá seu espectro traçado
        private Measurement reference; // referência para gráficos de diferenças angulares
        private Measurement[] selectedMeasurements; // vetor com os índices dos selecionados
        private int numSele;
        private string dirConsulta;

        private double taxa;

        // Data inicial da consulta
        private DateTime dataIni;

        ToolTip tip = new ToolTip();

        Point pto;

        // Flag para marcação ou não da opção "automático" do eixo Y no form de configurações
        public bool yAuto = true;
        public bool xAuto = true;

        // Separador decimal
        char decSep;

        // Tensão mínima de validação das medidas de frequência
        double vMin;

        // Tempo T1 anterior
        DateTime previousT1;

        public Query Query { get; internal set; }

        public GraficoTempo(JanelaPrincipal frm1, Query query, int op, Measurement reference, Measurement[] measurements, string diretorio, double vMinFreq)
        {
            InitializeComponent();

            // Repassando para as variáveis internas
            pai = frm1;
            Query = query;
            opGraf = op;
            selectedMeasurements = measurements;
            this.reference = reference;
            dirConsulta = diretorio;
            vMin = vMinFreq;
        }

        private void Form3_Shown(object sender, EventArgs e)
        {
            // Habilita as funções de zoom e pan no gráfico
            //chart1.EnableZoomAndPanControls(ChartCursorSelected, ChartCursorMoved);

            /* As informações que vão para o listbox servem para, na hora de requisitar o gráfico de DFT, 
             o programa saiba quais dados deve usar, ou seja a qual gráfico original a DFT está ssociada*/

            //escreve valores padrões para os textbox do método de detecção por filtro de Kalman, colocando . ou , dependendo do sistema usado
            toolStripTextBox18.Text = Convert.ToString(0.005);
            toolStripTextBox19.Text = Convert.ToString(0.01);
            toolStripTextBox20.Text = Convert.ToString(1);

            taxa = selectedMeasurements[0].FramesPerSecond;

            // Identificação do separador decimal segundo a cultura
            decSep = Convert.ToChar(System.Globalization.CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator);

            // Parâmetro copiado do Form6          
            //textBox1.Text = Convert.ToString(indCons);
            // Opção do gráfico base traçado, para saber qual sinal utilizar no cálculo da DFT (freq, tensão..)
            textBox2.Text = Convert.ToString(OpcaoGrafico.Opcao);
            // Referência se for gráfico de diferença angular
            if(reference != null)
                textBox3.Text = Convert.ToString(reference.Terminal.Id);
            // Opção de pu selecionada
            textBox4.Text = Convert.ToString(OpcaoGrafico.Pu);
            
            // Número de terminais selecionados
            numSele = selectedMeasurements.Length;


            // Ordem considerando a janela completa da consulta
            //toolStripTextBox1.Text = Convert.ToString(Objetos.ObjMat[indCons].tl / 4);
            ///////////////////////////////////////////////

            // Taxa com a qual a consulta foi realizada

            // se a taxa da consulta for diferente de 10, desabilita o botão do RBE
            if (taxa == 1.0)
                rBEToolStripMenuItem.Enabled = false;

            // Variável de incremento para as datas do eixo X
            long inc = 0;

            // define o mínimo e o máximo para o eixo X, aonde vão os valores de tempo
            //chart1.ChartAreas[0].AxisX.Minimum = Objetos.ObjMat[indCons].matTempo[0];
            //chart1.ChartAreas[0].AxisX.Maximum = Objetos.ObjMat[indCons].matTempo[Objetos.ObjMat[indCons].tl - 1];

            // limpa as séries do gráfico
            chart1.Series.Clear();

            dataIni = Query.Measurements[0].Start;

            DateTime data = new DateTime();

            // Define o título do eixo dos tempos
            chart1.ChartAreas[0].AxisX.Title = "Tempo (UTC) - Dia: " + dataIni.Day + "/" + dataIni.Month
                + "/" + dataIni.Year;
            //chart1.ChartAreas[0].AxisX.LabelStyle.Format = "t";//"0.###";

            chart1.ChartAreas[0].AxisX.IsMarginVisible = false;

            // Formato das labels no eixo X
            if (taxa == 1)
            {
                chart1.ChartAreas[0].AxisX.LabelStyle.Format = "HH:mm:ss"; // HH maiúsculo para que a hora fique de 0 a 24h
            }
            else if (taxa == 10)
            {
                chart1.ChartAreas[0].AxisX.LabelStyle.Format = "HH:mm:ss.f";
            }
            else
            {
                chart1.ChartAreas[0].AxisX.LabelStyle.Format = "HH:mm:ss.fff";
            }
            chart1.ChartAreas[0].AxisX.LabelStyle.IsEndLabelVisible = true;

            // Número de casas decimais no eixo Y
            chart1.ChartAreas[0].AxisY.LabelStyle.Format = "0.#####";

            // Paleta de cores para as séries 
            Color[] cores = new Color[80];

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
            cores[25] = Color.Aquamarine;
            cores[26] = Color.Bisque;
            cores[27] = Color.Brown;
            cores[28] = Color.CadetBlue;
            cores[29] = Color.Coral;
            cores[30] = Color.CornflowerBlue;
            cores[31] = Color.Cyan;
            cores[32] = Color.DarkSeaGreen;
            cores[33] = Color.DarkKhaki;
            cores[34] = Color.DarkOrange;
            cores[35] = Color.DarkOrchid;
            cores[36] = Color.DeepPink;
            cores[37] = Color.DimGray;
            cores[38] = Color.Firebrick;
            cores[39] = Color.ForestGreen;
            cores[40] = Color.Fuchsia;
            cores[41] = Color.Gold;
            cores[42] = Color.GreenYellow;
            cores[43] = Color.Honeydew;
            cores[44] = Color.Indigo;
            cores[45] = Color.Ivory;
            cores[46] = Color.Lavender;
            cores[47] = Color.LemonChiffon;
            cores[48] = Color.Linen;
            cores[49] = Color.OldLace;
            cores[50] = Color.OrangeRed;
            cores[51] = Color.Orchid;
            cores[52] = Color.PaleGoldenrod;
            cores[53] = Color.PaleGreen;
            cores[54] = Color.PaleTurquoise;
            cores[55] = Color.PaleVioletRed;
            cores[56] = Color.PapayaWhip;
            cores[57] = Color.PeachPuff;
            cores[58] = Color.Peru;
            cores[59] = Color.Pink;
            cores[60] = Color.Plum;
            cores[61] = Color.PowderBlue;
            cores[62] = Color.RosyBrown;
            cores[63] = Color.RoyalBlue;
            cores[64] = Color.SaddleBrown;
            cores[65] = Color.SandyBrown;
            cores[66] = Color.SeaGreen;
            cores[67] = Color.SeaShell;
            cores[68] = Color.Sienna;
            cores[69] = Color.Silver;
            cores[70] = Color.SkyBlue;
            cores[71] = Color.SlateBlue;
            cores[72] = Color.SlateGray;
            cores[73] = Color.SpringGreen;
            cores[74] = Color.SteelBlue;
            cores[75] = Color.Tan;
            cores[76] = Color.Thistle;
            cores[77] = Color.Tomato;
            cores[78] = Color.Wheat;
            cores[79] = Color.YellowGreen;

            //chart1.PaletteCustomColors = cores;

            // Número aleatório para gerar cores
            Random r = new Random();

            // Gráfico do módulo da tensão - FASE ÚNICA
            if ((OpcaoGrafico.Opcao == 1) || (OpcaoGrafico.Opcao == 2) || (OpcaoGrafico.Opcao == 3))
            {
                // Define qual das séries foi selecionada
                MedFasee.Equipment.Channel phase = null;
                switch (OpcaoGrafico.Opcao)
                {
                    case 1:
                        phase = MedFasee.Equipment.Channel.VOLTAGE_A_MOD;
                        break;
                    case 2:
                        phase = MedFasee.Equipment.Channel.VOLTAGE_B_MOD;
                        break;
                    case 3:
                        phase = MedFasee.Equipment.Channel.VOLTAGE_C_MOD;
                        break;
                    default:
                        break;
                }

                // Desabilita o RBE se mais de um sinal foi selecionado
                if (numSele > 3)
                {
                    rBEToolStripMenuItem.Enabled = false;
                }

                // Título do gráfico
                if (taxa == 1)
                {
                    chart1.Titles[0].Text = "Módulo da Tensão - Fase " +
                        phase.Phase.ToString().Substring(phase.Phase.ToString().Length - 1) + " - "
                        + Convert.ToString(taxa) + " fasor/s";
                }
                else
                {
                    chart1.Titles[0].Text = "Módulo da Tensão - Fase " +
                        phase.Phase.ToString().Substring(phase.Phase.ToString().Length - 1) + " - "
                        + Convert.ToString(taxa) + " fasores/s";
                }


                // Título do eixo Y
                if (OpcaoGrafico.Pu == 1)
                {
                    // Define o título do eixo Y
                    chart1.ChartAreas[0].AxisY.Title = "Tensão (pu)";
                }
                else if (OpcaoGrafico.Pu == 0)
                {
                    // Define o título do eixo Y
                    chart1.ChartAreas[0].AxisY.Title = "Tensão (V)";
                }

                foreach(Measurement measurement in selectedMeasurements)
                {

                    // Inicialização da data para o eixo X
                    data = dataIni;
                    // Inicializa o incremento
                    inc = 0;

                    // Adiciona quantas séries forem necessárias. O seu nome corresponde ao terminal.

                    Series series = new Series(measurement.Terminal.DisplayName);

                    if (chart1.Series.Count < 80)
                        series.Color = cores[chart1.Series.Count];
                    else
                        series.Color = Color.FromArgb(r.Next(0, 255), r.Next(0, 255), r.Next(0, 255));
                    // Necessário para conhecer o índice de cada dado no eixo X, serve para moldar a janela
                    // aparente, utilizada para DFT e Prony
                    series.IsXValueIndexed = true;
                    // Define o tipo da série recém-criada, aqui usa-se o tipo 'FastLine'
                    series.ChartType = SeriesChartType.FastLine;



                    double vb = 1; // a base para gráficos em pu 

                    if (OpcaoGrafico.Pu == 1)
                        vb = (measurement.Terminal.VoltageLevel / Math.Sqrt(3)) * 1000;

                    for (int i = 0; i < measurement.Series[phase].Count; i++)
                    {
                        // Traça a série
                        series.Points.AddXY(data, measurement.Series[phase].Reading(i) / vb);
                        // Objetos.ObjMat[indCons].matTempo[i]

                        // Incremento em 'ticks' no valor da data, de acordo com a taxa da consulta
                        // 10 mil ticks = 1 ms
                        inc = Convert.ToInt64(Math.Floor((1 / taxa) * 1000 * (i + 1)) * 10000);
                        data = dataIni.AddTicks(inc);
                    }

                    chart1.Series.Add(series);


                    //chart1.Series[k].XValueType = ChartValueType.DateTime;
                }


            }

            // Gráfico do módulo da corrente - FASE ÚNICA
            if ((OpcaoGrafico.Opcao == 12) || (OpcaoGrafico.Opcao == 13) || (OpcaoGrafico.Opcao == 14))
            {
                // Define qual das séries foi selecionada
                Channel phase = null;
                switch (OpcaoGrafico.Opcao)
                {
                    case 12:
                        phase = Channel.CURRENT_A_MOD;
                        break;
                    case 13:
                        phase = Channel.CURRENT_B_MOD;
                        break;
                    case 14:
                        phase = Channel.CURRENT_C_MOD;
                        break;
                    default:
                        break;
                }

                // Desabilita o RBE se mais de um sinal foi selecionado
                if (numSele > 3)
                {
                    rBEToolStripMenuItem.Enabled = false;
                }

                // Título do gráfico
                if (taxa == 1)
                {
                    chart1.Titles[0].Text = "Módulo da Corrente - Fase " +
                        phase.Phase.ToString().Substring(phase.Phase.ToString().Length - 1) + " - "
                        + Convert.ToString(taxa) + " fasor/s";
                }
                else
                {
                    chart1.Titles[0].Text = "Módulo da Corrente - Fase " +
                        phase.Phase.ToString().Substring(phase.Phase.ToString().Length - 1) + " - "
                        + Convert.ToString(taxa) + " fasores/s";
                }

                // Título do eixo Y
                if (OpcaoGrafico.Pu == 1)
                {
                    // Define o título do eixo Y
                    chart1.ChartAreas[0].AxisY.Title = "Corrente (pu)";
                }
                else if (OpcaoGrafico.Pu == 0)
                {
                    // Define o título do eixo Y
                    chart1.ChartAreas[0].AxisY.Title = "Corrente (A)";
                }

                foreach(Measurement measurement in selectedMeasurements)
                {
                    // Inicialização da data para o eixo X
                    data = dataIni;
                    // Inicializa o incremento
                    inc = 0;

                    // Adiciona quantas séries forem necessárias. O seu nome corresponde ao terminal.

                    Series series = new Series(measurement.Terminal.DisplayName);

                    if (chart1.Series.Count < 80)
                        series.Color = cores[chart1.Series.Count];
                    else
                        series.Color = Color.FromArgb(r.Next(0, 255), r.Next(0, 255), r.Next(0, 255));
                    // Necessário para conhecer o índice de cada dado no eixo X, serve para moldar a janela
                    // aparente, utilizada para DFT e Prony
                    series.IsXValueIndexed = true;
                    // Define o tipo da série recém-criada, aqui usa-se o tipo 'FastLine'
                    series.ChartType = SeriesChartType.FastLine;

                    double ib = 1; // a base para gráficos em pu 

                    for (int i = 0; i < measurement.Series[phase].Count; i++)
                    {
                        // Traça a série
                        series.Points.AddXY(data, measurement.Series[phase].Reading(i) / ib);
                        // Objetos.ObjMat[indCons].matTempo[i]

                        // Incremento em 'ticks' no valor da data, de acordo com a taxa da consulta
                        // 10 mil ticks = 1 ms
                        inc = Convert.ToInt64(Math.Floor((1 / taxa) * 1000 * (i + 1)) * 10000);
                        data = dataIni.AddTicks(inc);
                    }

                    chart1.Series.Add(series);

                    //chart1.Series[k].XValueType = ChartValueType.DateTime;
                }

            }
            // Gráfico do módulo da tensão - Trifásico
            if (OpcaoGrafico.Opcao == 4)
            {
                // Desabilita o Prony
                //toolStripSplitButton1.Enabled = false;

                Channel[] fases;
                fases = new Channel[3];

                fases[0] = Channel.VOLTAGE_A_MOD;
                fases[1] = Channel.VOLTAGE_B_MOD;
                fases[2] = Channel.VOLTAGE_C_MOD;

                double vb; // a base para gráficos em pu 

                if (OpcaoGrafico.Pu == 1)
                {
                    // Define o título do eixo Y
                    chart1.ChartAreas[0].AxisY.Title = "Tensão (pu)";
                }
                else if (OpcaoGrafico.Pu == 0)
                {
                    // Define o título do eixo Y
                    chart1.ChartAreas[0].AxisY.Title = "Tensão (V)";
                }

                // Título do gráfico
                if (taxa == 1)
                {
                    chart1.Titles[0].Text = "Módulo da Tensão - " + selectedMeasurements[0].Terminal.DisplayName + " - "
                        + Convert.ToString(taxa) + " fasor/s";
                }
                else
                {
                    chart1.Titles[0].Text = "Módulo da Tensão - " + selectedMeasurements[0].Terminal.DisplayName + " - "
                        + Convert.ToString(taxa) + " fasores/s";
                }

                for (int k = 0; k < 3; k++)
                {
                    // Inicialização da data para o eixo X
                    data = dataIni;
                    // Inicializa o incremento
                    inc = 0;

                    // Adiciona quantas séries forem necessárias. O seu nome corresponde ao terminal.
                    chart1.Series.Add("Fase " + fases[k].Phase.ToString().Substring(fases[k].Phase.ToString().Length - 1));
                    chart1.Series[k].Color = cores[k];
                    // Necessário para conhecer o índice de cada dado no eixo X, serve para moldar a janela
                    // aparente, utilizada para DFT e Prony
                    chart1.Series[k].IsXValueIndexed = true;
                    // Define o tipo da série recém-criada, aqui usa-se o tipo 'FastLine'
                    chart1.Series[k].ChartType = SeriesChartType.FastLine;

                    if (OpcaoGrafico.Pu == 1)
                    {
                        vb = (selectedMeasurements[0].Terminal.VoltageLevel / Math.Sqrt(3)) * 1000;
                    }
                    else
                    {
                        vb = 1;
                    }

                    for (int i = 0; i < selectedMeasurements[0].Series[fases[k]].Count; i++)
                    {
                        // Traça a série
                        chart1.Series[k].Points.AddXY(data, selectedMeasurements[0].Series[fases[k]].Reading(i) / vb);

                        // Incremento em 'ticks' no valor da data, de acordo com a taxa da consulta
                        // 10 mil ticks = 1 ms
                        inc = Convert.ToInt64(Math.Floor((1 / taxa) * 1000 * (i + 1)) * 10000);
                        data = dataIni.AddTicks(inc);
                    }

                }
            }
            // Gráfico do módulo da corrente - Trifásico
            if (OpcaoGrafico.Opcao == 15)
            {
                double ib = 1; // a base para gráficos em pu 

                // Desabilita o Prony
                //toolStripSplitButton1.Enabled = false;

                Channel[] fases = new Channel[3];

                fases[0] = Channel.CURRENT_A_MOD;
                fases[1] = Channel.CURRENT_B_MOD;
                fases[2] = Channel.CURRENT_C_MOD;

                if (OpcaoGrafico.Pu == 1)
                {
                    // Define o título do eixo Y
                    chart1.ChartAreas[0].AxisY.Title = "Corrente (pu)";
                }
                else if (OpcaoGrafico.Pu == 0)
                {
                    // Define o título do eixo Y
                    chart1.ChartAreas[0].AxisY.Title = "Corrente (A)";
                }

                // Título do gráfico
                if (taxa == 1)
                {
                    chart1.Titles[0].Text = "Módulo da Corrente - " + selectedMeasurements[0].Terminal.DisplayName + " - "
                        + Convert.ToString(taxa) + " fasor/s";
                }
                else
                {
                    chart1.Titles[0].Text = "Módulo da Corrente - " + selectedMeasurements[0].Terminal.DisplayName + " - "
                        + Convert.ToString(taxa) + " fasores/s";
                }

                for (int k = 0; k < 3; k++)
                {
                    // Inicialização da data para o eixo X
                    data = dataIni;
                    // Inicializa o incremento
                    inc = 0;

                    // Adiciona quantas séries forem necessárias. O seu nome corresponde ao terminal.
                    chart1.Series.Add("Fase " + fases[k].Phase.ToString().Substring(fases[k].Phase.ToString().Length - 1));
                    chart1.Series[k].Color = cores[k];
                    // Necessário para conhecer o índice de cada dado no eixo X, serve para moldar a janela
                    // aparente, utilizada para DFT e Prony
                    chart1.Series[k].IsXValueIndexed = true;
                    // Define o tipo da série recém-criada, aqui usa-se o tipo 'FastLine'
                    chart1.Series[k].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastLine;


                    for (int i = 0; i < selectedMeasurements[0].Series[fases[k]].Count; i++)
                    {
                        // Traça a série
                        chart1.Series[k].Points.AddXY(data, selectedMeasurements[0].Series[fases[k]].Reading(i) / ib);

                        // Incremento em 'ticks' no valor da data, de acordo com a taxa da consulta
                        // 10 mil ticks = 1 ms
                        inc = Convert.ToInt64(Math.Floor((1 / taxa) * 1000 * (i + 1)) * 10000);
                        data = dataIni.AddTicks(inc);
                    }

                }
            }
            // Gráfico do módulo da tensão - Seq. Positiva
            if (OpcaoGrafico.Opcao == 5)
            {
                double vb; // a base para gráficos em pu 

                if (OpcaoGrafico.Pu == 1)
                {
                    // Define o título do eixo Y
                    chart1.ChartAreas[0].AxisY.Title = "Tensão (pu)";
                }
                else if (OpcaoGrafico.Pu == 0)
                {
                    // Define o título do eixo Y
                    chart1.ChartAreas[0].AxisY.Title = "Tensão (V)";
                }
                // Desabilita o RBE se mais de um sinal foi selecionado
                if (numSele > 3)
                {
                    rBEToolStripMenuItem.Enabled = false;
                }
                // Título do gráfico
                if (taxa == 1)
                {
                    chart1.Titles[0].Text = "Módulo da Tensão - Sequência Positiva" + " - "
                        + Convert.ToString(taxa) + " fasor/s";
                }
                else
                {
                    chart1.Titles[0].Text = "Módulo da Tensão - Sequência Positiva" + " - "
                        + Convert.ToString(taxa) + " fasores/s";
                }

                foreach(Measurement measurement in selectedMeasurements)
                {
                    // Inicialização da data para o eixo X
                    data = dataIni;
                    // Inicializa o incremento
                    inc = 0;

                    // Adiciona quantas séries forem necessárias. O seu nome corresponde ao terminal.
                    Series series = new Series(measurement.Terminal.DisplayName);
                    if (chart1.Series.Count < 80)
                        series.Color = cores[chart1.Series.Count];
                    else
                        series.Color = Color.FromArgb(r.Next(0, 255), r.Next(0, 255), r.Next(0, 255));
                    // Necessário para conhecer o índice de cada dado no eixo X, serve para moldar a janela
                    // aparente, utilizada para DFT e Prony
                    series.IsXValueIndexed = true;
                    // Define o tipo da série recém-criada, aqui usa-se o tipo 'FastLine'
                    series.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastLine;
                    if (OpcaoGrafico.Pu == 1)
                    {
                        vb = (measurement.Terminal.VoltageLevel / Math.Sqrt(3)) * 1000;
                    }
                    else
                    {
                        vb = 1;
                    }
                    for (int i = 0; i < measurement.Series[Channel.VOLTAGE_POS_MOD].Count; i++)
                    {
                        // Traça a série
                        series.Points.AddXY(data, measurement.Series[Channel.VOLTAGE_POS_MOD].Reading(i) / vb);

                        // Incremento em 'ticks' no valor da data, de acordo com a taxa da consulta
                        // 10 mil ticks = 1 ms
                        inc = Convert.ToInt64(Math.Floor((1 / taxa) * 1000 * (i + 1)) * 10000);
                        data = dataIni.AddTicks(inc);
                    }
                    chart1.Series.Add(series);

                }

                chart1.ChartAreas[0].AxisX.LabelStyle.IsEndLabelVisible = true;

            }
            // Gráfico do módulo da corrente - Seq. Positiva
            if (OpcaoGrafico.Opcao == 16)
            {
                double ib = 1; // a base para gráficos em pu 

                if (OpcaoGrafico.Pu == 1)
                {
                    // Define o título do eixo Y
                    chart1.ChartAreas[0].AxisY.Title = "Corrente (pu)";
                }
                else if (OpcaoGrafico.Pu == 0)
                {
                    // Define o título do eixo Y
                    chart1.ChartAreas[0].AxisY.Title = "Corrente (A)";
                }
                // Desabilita o RBE se mais de um sinal foi selecionado
                if (numSele > 3)
                {
                    rBEToolStripMenuItem.Enabled = false;
                }
                // Título do gráfico
                if (taxa == 1)
                {
                    chart1.Titles[0].Text = "Módulo da Corrente - Sequência Positiva" + " - "
                        + Convert.ToString(taxa) + " fasor/s";
                }
                else
                {
                    chart1.Titles[0].Text = "Módulo da Corrente - Sequência Positiva" + " - "
                        + Convert.ToString(taxa) + " fasores/s";
                }

                foreach(Measurement measurement in selectedMeasurements)
                {
                    // Inicialização da data para o eixo X
                    data = dataIni;
                    // Inicializa o incremento
                    inc = 0;

                    Series series = new Series(measurement.Terminal.DisplayName);
                    if (chart1.Series.Count < 80)
                        series.Color = cores[chart1.Series.Count];
                    else
                        series.Color = Color.FromArgb(r.Next(0, 255), r.Next(0, 255), r.Next(0, 255));
                    // Necessário para conhecer o índice de cada dado no eixo X, serve para moldar a janela
                    // aparente, utilizada para DFT e Prony
                    series.IsXValueIndexed = true;
                    // Define o tipo da série recém-criada, aqui usa-se o tipo 'FastLine'
                    series.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastLine;

                    for (int i = 0; i < measurement.Series[Channel.CURRENT_POS_MOD].Count; i++)
                    {
                        // Traça a série
                        series.Points.AddXY(data, measurement.Series[Channel.CURRENT_POS_MOD].Reading(i) / ib);

                        // Incremento em 'ticks' no valor da data, de acordo com a taxa da consulta
                        // 10 mil ticks = 1 ms
                        inc = Convert.ToInt64(Math.Floor((1 / taxa) * 1000 * (i + 1)) * 10000);
                        data = dataIni.AddTicks(inc);
                    }
                    chart1.Series.Add(series);

                }
            }

            // Gráfico da diferença angular da tensão - FASE ÚNICA
            if ((OpcaoGrafico.Opcao == 7) || (OpcaoGrafico.Opcao == 8) || (OpcaoGrafico.Opcao == 9))
            {
                // Define qual das séries foi selecionada
                Channel phase = null;
                switch (OpcaoGrafico.Opcao)
                {
                    case 7:
                        phase = Channel.VOLTAGE_A_ANG;
                        break;
                    case 8:
                        phase = Channel.VOLTAGE_B_ANG;
                        break;
                    case 9:
                        phase = Channel.VOLTAGE_C_ANG;
                        break;
                    default:
                        break;
                }

                // Define o título do eixo Y
                chart1.ChartAreas[0].AxisY.Title = "Diferença Angular (Graus)";

                // Desabilita o RBE se mais de um sinal foi selecionado
                if (numSele > 4)
                {
                    rBEToolStripMenuItem.Enabled = false;
                }
                // Título do gráfico
                if (taxa == 1)
                {
                    chart1.Titles[0].Text = "Diferença Angular da Tensão - Fase " + 
                        phase.Phase.ToString().Substring(phase.Phase.ToString().Length - 1) + " - Ref.: " +
                    reference.Terminal.DisplayName + " - "
                        + Convert.ToString(taxa) + " fasor/s";
                }
                else
                {
                    chart1.Titles[0].Text = "Diferença Angular da Tensão - Fase " + 
                        phase.Phase.ToString().Substring(phase.Phase.ToString().Length - 1) + " - Ref.: " +
                    reference.Terminal.DisplayName + " - "
                        + Convert.ToString(taxa) + " fasores/s";
                }

                // Contador para as séries
                // Plota o número de selecionados menos um, já que um dos terminais é a referência
                foreach(Measurement measurement in selectedMeasurements)
                {

                    // Inicialização da data para o eixo X
                    data = dataIni;
                    // Inicializa o incremento
                    inc = 0;

                    if (measurement != reference)
                    {
                        // Adiciona quantas séries forem necessárias. O seu nome corresponde ao terminal.
                        Series series = new Series(measurement.Terminal.DisplayName);
                        // Define a cor da série
                        if (chart1.Series.Count < 80)
                            series.Color = cores[chart1.Series.Count];
                        else
                            series.Color = Color.FromArgb(r.Next(0, 255), r.Next(0, 255), r.Next(0, 255));
                        // Necessário para conhecer o índice de cada dado no eixo X, serve para moldar a janela
                        // aparente, utilizada para DFT e Prony
                        series.IsXValueIndexed = true;
                        // Define o tipo da série recém-criada, aqui usa-se o tipo 'FastLine'
                        series.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastLine;

                        double dif, difback; // diferença angular e o backup deste valor (para o caso de perda de dados)
                        difback = 0;
                        int cwh = 0; // contador para o while

                        for (int i = 0; i < measurement.Series[phase].Count; i++)
                        {
                            // Diferença angular
                            dif = measurement.Series[phase].Reading(i) -
                                reference.Series[phase].Reading(i);

                            // Dados faltantes nos ângulos das PMUs
                            // Dado faltante não é no início da série de dados
                            if ((measurement.Series[Channel.MISSING].Reading(i) == 1) || (reference.Series[Channel.MISSING].Reading(i) == 1))
                            {
                                if(i != 0)
                                {
                                    dif = difback;

                                }
                                else
                                {
                                    while ((measurement.Series[Channel.MISSING].Reading(cwh) == 1) || (reference.Series[Channel.MISSING].Reading(cwh) == 1))
                                    {
                                        // Se chegou ao fim e não há nenhum dado 
                                        if (cwh == (measurement.Series[phase].Count - 1))
                                            break;
                                        else
                                            cwh++; // atualiza o contador
                                    }
                                    dif = measurement.Series[phase].Reading(cwh) -
                                reference.Series[phase].Reading(cwh);
                                }
                            }


                            // Trata a diferença
                            /////////////////////
                            if (dif > 180.0)
                            {
                                dif = dif - 360;
                            }
                            else if (dif < -180.0)
                            {
                                dif = dif + 360;
                            }

                            // Backup da variável dif
                            difback = dif;

                            /////////////////////                    
                            // Traça a série
                            series.Points.AddXY(data, dif);

                            // Incremento em 'ticks' no valor da data, de acordo com a taxa da consulta
                            // 10 mil ticks = 1 ms
                            inc = Convert.ToInt64(Math.Floor((1 / taxa) * 1000 * (i + 1)) * 10000);
                            data = dataIni.AddTicks(inc);
                        }
                        chart1.Series.Add(series);
                    }
                }

            }
            // Gráfico da diferença angular da corrente - FASE ÚNICA
            if ((OpcaoGrafico.Opcao == 17) || (OpcaoGrafico.Opcao == 18) || (OpcaoGrafico.Opcao == 19))
            {
                // Define qual das séries foi selecionada
                Channel phase = null;
                switch (OpcaoGrafico.Opcao)
                {
                    case 7:
                        phase = Channel.CURRENT_A_ANG;
                        break;
                    case 8:
                        phase = Channel.CURRENT_B_ANG;
                        break;
                    case 9:
                        phase = Channel.CURRENT_C_ANG;
                        break;
                    default:
                        break;
                }

                // Define o título do eixo Y
                chart1.ChartAreas[0].AxisY.Title = "Diferença Angular (Graus)";
                // Desabilita o RBE se mais de um sinal foi selecionado
                if (numSele > 4)
                {
                    rBEToolStripMenuItem.Enabled = false;
                }
                // Título do gráfico
                if (taxa == 1)
                {
                    chart1.Titles[0].Text = "Diferença Angular da Corrente - Fase " +
                        phase.Phase.ToString().Substring(phase.Phase.ToString().Length - 1) + " - Ref.: " +
                    reference.Terminal.DisplayName + " - "
                        + Convert.ToString(taxa) + " fasor/s";
                }
                else
                {
                    chart1.Titles[0].Text = "Diferença Angular da Corrente - Fase " +
                        phase.Phase.ToString().Substring(phase.Phase.ToString().Length - 1) + " - Ref.: " +
                    reference.Terminal.DisplayName + " - "
                        + Convert.ToString(taxa) + " fasores/s";
                }

                // Contador para as séries
                // Plota o número de selecionados menos um, já que um dos terminais é a referência
                foreach(Measurement measurement in selectedMeasurements)
                {
                    // Inicialização da data para o eixo X
                    data = dataIni;
                    // Inicializa o incremento
                    inc = 0;

                    if (measurement != reference)
                    {
                        // Adiciona quantas séries forem necessárias. O seu nome corresponde ao terminal.
                        Series series = new Series(measurement.Terminal.DisplayName);
                        // Define a cor da série
                        if (chart1.Series.Count < 80)
                            series.Color = cores[chart1.Series.Count];
                        else
                            series.Color = Color.FromArgb(r.Next(0, 255), r.Next(0, 255), r.Next(0, 255));
                        // Necessário para conhecer o índice de cada dado no eixo X, serve para moldar a janela
                        // aparente, utilizada para DFT e Prony
                        series.IsXValueIndexed = true;
                        // Define o tipo da série recém-criada, aqui usa-se o tipo 'FastLine'
                        series.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastLine;

                        double dif, difback; // diferença angular e o backup deste valor (para o caso de perda de dados)
                        difback = 0;
                        int cwh = 0; // contador para o while

                        for (int i = 0; i < measurement.Series[phase].Count; i++)
                        {

                            // Diferença angular
                            dif = measurement.Series[phase].Reading(i) -
                                reference.Series[phase].Reading(i);

                            if ((measurement.Series[Channel.MISSING].Reading(i) == 1) || (reference.Series[Channel.MISSING].Reading(i) == 1))
                            {
                                if (i != 0)
                                {
                                    dif = difback;
                                }
                                else
                                {
                                    while ((measurement.Series[Channel.MISSING].Reading(cwh) == 1) || (reference.Series[Channel.MISSING].Reading(cwh) == 1))
                                    {
                                        // Se chegou ao fim e não há nenhum dado 
                                        if (cwh == (measurement.Series[phase].Count - 1))
                                            break;
                                        else
                                            cwh++; // atualiza o contador
                                    }
                                    dif = measurement.Series[phase].Reading(cwh) -
                                reference.Series[phase].Reading(cwh);
                                }
                            }

                            // Trata a diferença
                            /////////////////////
                            if (dif > 180.0)
                            {
                                dif = dif - 360;
                            }
                            else if (dif < -180.0)
                            {
                                dif = dif + 360;
                            }

                            // Backup da variável dif
                            difback = dif;

                            /////////////////////                    
                            // Traça a série
                            series.Points.AddXY(data, dif);

                            // Incremento em 'ticks' no valor da data, de acordo com a taxa da consulta
                            // 10 mil ticks = 1 ms
                            inc = Convert.ToInt64(Math.Floor((1 / taxa) * 1000 * (i + 1)) * 10000);
                            data = dataIni.AddTicks(inc);
                        }
                        chart1.Series.Add(series);
                    }
                }
            }
            // Gráfico da diferença angular da tensão - Sequência Positiva
            if (OpcaoGrafico.Opcao == 10)
            {
                // Define o título do eixo Y
                chart1.ChartAreas[0].AxisY.Title = "Diferença Angular (Graus)";
                // Desabilita o RBE se mais de um sinal foi selecionado
                if (numSele > 4)
                {
                    rBEToolStripMenuItem.Enabled = false;
                }
                // Título do gráfico
                if (taxa == 1)
                {
                    chart1.Titles[0].Text = "Diferença Angular da Tensão - Sequência Positiva - Ref.: " +
                    reference.Terminal.DisplayName + " - "
                        + Convert.ToString(taxa) + " fasor/s";
                }
                else
                {
                    chart1.Titles[0].Text = "Diferença Angular da Tensão - Sequência Positiva - Ref.: " +
                    reference.Terminal.DisplayName + " - "
                        + Convert.ToString(taxa) + " fasores/s";
                }
                // Plota o número de selecionados menos um, já que um dos terminais é a referência
                foreach(Measurement measurement in selectedMeasurements)
                {
                    // Inicialização da data para o eixo X
                    data = dataIni;
                    // Inicializa o incremento
                    inc = 0;

                    if (measurement != reference)
                    {
                        // Adiciona quantas séries forem necessárias. O seu nome corresponde ao terminal.
                        Series series = new Series(measurement.Terminal.DisplayName);
                        // Define a cor da série
                        if (chart1.Series.Count < 80)
                            series.Color = cores[chart1.Series.Count];
                        else
                            series.Color = Color.FromArgb(r.Next(0, 255), r.Next(0, 255), r.Next(0, 255));
                        // Necessário para conhecer o índice de cada dado no eixo X, serve para moldar a janela
                        // aparente, utilizada para DFT e Prony
                        series.IsXValueIndexed = true;
                        // Define o tipo da série recém-criada, aqui usa-se o tipo 'FastLine'
                        series.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastLine;

                        double dif, difback; // diferença angular e o backup deste valor (para o caso de perda de dados)
                        difback = 0;
                        int cwh = 0; // contador para o while

                        for (int i = 0; i < measurement.Series[Channel.VOLTAGE_POS_ANG].Count; i++)
                        {
                            // Diferença angular
                            dif = measurement.Series[Channel.VOLTAGE_POS_ANG].Reading(i) -
                                reference.Series[Channel.VOLTAGE_POS_ANG].Reading(i);

                            // Dados faltantes nos ângulos das PMUs
                            // Dado faltante não é no início da série de dados

                            if ((measurement.Series[Channel.MISSING].Reading(i) == 1) || (reference.Series[Channel.MISSING].Reading(i) == 1))
                            {
                                if (i != 0)
                                {
                                    dif = difback;
                                }
                                else
                                {
                                    while ((measurement.Series[Channel.MISSING].Reading(cwh) == 1) || (reference.Series[Channel.MISSING].Reading(cwh) == 1))
                                    {
                                        // Se chegou ao fim e não há nenhum dado 
                                        if (cwh == (measurement.Series[Channel.VOLTAGE_POS_ANG].Count - 1))
                                            break;
                                        else
                                            cwh++; // atualiza o contador
                                    }
                                    dif = measurement.Series[Channel.VOLTAGE_POS_ANG].Reading(cwh) -
                                reference.Series[Channel.VOLTAGE_POS_ANG].Reading(cwh);
                                }
                            }

                            // Trata a diferença
                            /////////////////////
                            if (dif > 180.0)
                            {
                                dif = dif - 360;
                            }
                            else if (dif < -180.0)
                            {
                                dif = dif + 360;
                            }
                            /////////////////////

                            // Backup da variável dif
                            difback = dif;

                            // Traça a série
                            series.Points.AddXY(data, dif);

                            // Incremento em 'ticks' no valor da data, de acordo com a taxa da consulta
                            // 10 mil ticks = 1 ms
                            inc = Convert.ToInt64(Math.Floor((1 / taxa) * 1000 * (i + 1)) * 10000);
                            data = dataIni.AddTicks(inc);
                        }
                        chart1.Series.Add(series);
                    }
                }
            }
            // Gráfico da diferença angular da corrente - Sequência Positiva
            if (OpcaoGrafico.Opcao == 20)
            {
                // Define o título do eixo Y
                chart1.ChartAreas[0].AxisY.Title = "Diferença Angular (Graus)";
                // Desabilita o RBE se mais de um sinal foi selecionado
                if (numSele > 4)
                {
                    rBEToolStripMenuItem.Enabled = false;
                }
                // Título do gráfico
                if (taxa == 1)
                {
                    chart1.Titles[0].Text = "Diferença Angular da Corrente - Sequência Positiva - Ref.: " +
                    reference.Terminal.DisplayName + " - "
                        + Convert.ToString(taxa) + " fasor/s";
                }
                else
                {
                    chart1.Titles[0].Text = "Diferença Angular da Corrente - Sequência Positiva - Ref.: " +
                    reference.Terminal.DisplayName + " - "
                        + Convert.ToString(taxa) + " fasores/s";
                }

                // Plota o número de selecionados menos um, já que um dos terminais é a referência
                foreach(Measurement measurement in selectedMeasurements)
                {
                    // Inicialização da data para o eixo X
                    data = dataIni;
                    // Inicializa o incremento
                    inc = 0;

                    if (measurement != reference)
                    {
                        // Adiciona quantas séries forem necessárias. O seu nome corresponde ao terminal.
                        Series series = new Series(measurement.Terminal.DisplayName);
                        // Define a cor da série
                        if (chart1.Series.Count < 80)
                            series.Color = cores[chart1.Series.Count];
                        else
                            series.Color = Color.FromArgb(r.Next(0, 255), r.Next(0, 255), r.Next(0, 255));
                        // Necessário para conhecer o índice de cada dado no eixo X, serve para moldar a janela
                        // aparente, utilizada para DFT e Prony
                        series.IsXValueIndexed = true;
                        // Define o tipo da série recém-criada, aqui usa-se o tipo 'FastLine'
                        series.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastLine;

                        double dif, difback; // diferença angular e o backup deste valor (para o caso de perda de dados)
                        difback = 0;
                        int cwh = 0; // contador para o while

                        for (int i = 0; i < measurement.Series[Channel.CURRENT_POS_ANG].Count; i++)
                        {
                            // Diferença angular
                            dif = measurement.Series[Channel.CURRENT_POS_ANG].Reading(i) -
                                reference.Series[Channel.CURRENT_POS_ANG].Reading(i);

                            if ((measurement.Series[Channel.MISSING].Reading(i) == 1) || (reference.Series[Channel.MISSING].Reading(i) == 1))
                            {
                                if (i != 0)
                                {
                                    dif = difback;
                                }
                                else
                                {
                                    while ((measurement.Series[Channel.MISSING].Reading(cwh) == 1) || (reference.Series[Channel.MISSING].Reading(cwh) == 1))
                                    {
                                        if (cwh == (measurement.Series[Channel.VOLTAGE_POS_ANG].Count - 1))
                                            break;
                                        else
                                            cwh++; // atualiza o contador
                                    }
                                    dif = measurement.Series[Channel.VOLTAGE_POS_ANG].Reading(cwh) - reference.Series[Channel.VOLTAGE_POS_ANG].Reading(cwh);
                                }
                            }

                            // Trata a diferença
                            /////////////////////
                            if (dif > 180.0)
                            {
                                dif = dif - 360;
                            }
                            else if (dif < -180.0)
                            {
                                dif = dif + 360;
                            }
                            /////////////////////

                            // Backup da variável dif
                            difback = dif;

                            // Traça a série
                            series.Points.AddXY(data, dif);

                            // Incremento em 'ticks' no valor da data, de acordo com a taxa da consulta
                            // 10 mil ticks = 1 ms
                            inc = Convert.ToInt64(Math.Floor((1 / taxa) * 1000 * (i + 1)) * 10000);
                            data = dataIni.AddTicks(inc);
                        }
                        chart1.Series.Add(series);
                    }
                }
            }
            // Gráfico da potência ativa trifásica
            if (OpcaoGrafico.Opcao == 21)
            {
                chart1.ChartAreas[0].AxisY.Title = "Potência (MW)";

                // Desabilita o RBE se mais de um sinal foi selecionado
                if (numSele > 3)
                {
                    rBEToolStripMenuItem.Enabled = false;
                }
                // Título do gráfico
                if (taxa == 1)
                {
                    chart1.Titles[0].Text = "Potência Ativa" + " - "
                        + Convert.ToString(taxa) + " fasor/s";
                }
                else
                {
                    chart1.Titles[0].Text = "Potência Ativa" + " - "
                        + Convert.ToString(taxa) + " fasores/s";
                }

                foreach(Measurement measurement in selectedMeasurements)
                {
                    // Inicialização da data para o eixo X
                    data = dataIni;
                    // Inicializa o incremento
                    inc = 0;

                    Series series = new Series(measurement.Terminal.DisplayName);
                    if (chart1.Series.Count < 80)
                        series.Color = cores[chart1.Series.Count];
                    else
                        series.Color = Color.FromArgb(r.Next(0, 255), r.Next(0, 255), r.Next(0, 255));
                    series.IsXValueIndexed = true;
                    series.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastLine;
                    for (int i = 0; i < measurement.Series[Channel.ACTIVE_POWER].Count; i++)
                    {
                        // Traça a série
                        series.Points.AddXY(data, measurement.Series[Channel.ACTIVE_POWER].Reading(i) / 1e6);

                        // Incremento em 'ticks' no valor da data, de acordo com a taxa da consulta
                        // 10 mil ticks = 1 ms
                        inc = Convert.ToInt64(Math.Floor((1 / taxa) * 1000 * (i + 1)) * 10000);
                        data = dataIni.AddTicks(inc);
                    }
                    chart1.Series.Add(series);

                }

            }
            // Gráfico da potência reativa trifásica
            if (OpcaoGrafico.Opcao == 22)
            {
                chart1.ChartAreas[0].AxisY.Title = "Potência (Mvar)";

                // Desabilita o RBE se mais de um sinal foi selecionado
                if (numSele > 3)
                {
                    rBEToolStripMenuItem.Enabled = false;
                }
                // Título do gráfico
                if (taxa == 1)
                {
                    chart1.Titles[0].Text = "Potência Reativa" + " - "
                        + Convert.ToString(taxa) + " fasor/s";
                }
                else
                {
                    chart1.Titles[0].Text = "Potência Reativa" + " - "
                        + Convert.ToString(taxa) + " fasores/s";
                }

                foreach(Measurement measurement in selectedMeasurements)
                {
                    // Inicialização da data para o eixo X
                    data = dataIni;
                    // Inicializa o incremento
                    inc = 0;

                    Series series = new Series(measurement.Terminal.DisplayName);
                    if (chart1.Series.Count < 80)
                        series.Color = cores[chart1.Series.Count];
                    else
                        series.Color = Color.FromArgb(r.Next(0, 255), r.Next(0, 255), r.Next(0, 255));

                    series.IsXValueIndexed = true;
                    series.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastLine;

                    for (int i = 0; i < measurement.Series[Channel.REACTIVE_POWER].Count; i++)
                    {
                        // Traça a série
                        series.Points.AddXY(data, measurement.Series[Channel.REACTIVE_POWER].Reading(i) / 1e6);

                        // Incremento em 'ticks' no valor da data, de acordo com a taxa da consulta
                        // 10 mil ticks = 1 ms
                        inc = Convert.ToInt64(Math.Floor((1 / taxa) * 1000 * (i + 1)) * 10000);
                        data = dataIni.AddTicks(inc);
                    }
                    chart1.Series.Add(series);
                }

            }
            // Gráfico da frequência
            if (OpcaoGrafico.Opcao == 11)
            {
                //habilita o botão eventos
                if (taxa == 60 || taxa == 50)
                {
                    toolStripButton1.Visible = true;
                    toolStripLabel2.Visible = true;
                    toolStripSeparator7.Visible = true;
                }
                // Define o título do eixo Y
                chart1.ChartAreas[0].AxisY.Title = "Frequência (Hz)";
                // Desabilita o RBE se mais de um sinal foi selecionado
                if (numSele > 3)
                {
                    rBEToolStripMenuItem.Enabled = false;
                }
                // Título do gráfico
                if (taxa == 1)
                {
                    chart1.Titles[0].Text = "Frequência" + " - "
                        + Convert.ToString(taxa) + " fasor/s";
                }
                else
                {
                    chart1.Titles[0].Text = "Frequência" + " - "
                        + Convert.ToString(taxa) + " fasores/s";
                }

                foreach(Measurement measurement in selectedMeasurements)
                {
                    // Inicialização da data para o eixo X
                    data = dataIni;
                    // Inicializa o incremento
                    inc = 0;
                    Series series = new Series(measurement.Terminal.DisplayName);

                    // Adiciona quantas séries forem necessárias. O seu nome corresponde ao terminal.
                    // Define a cor da série
                    if (chart1.Series.Count < 80)
                        series.Color = cores[chart1.Series.Count];
                    else
                        series.Color = Color.FromArgb(r.Next(0, 255), r.Next(0, 255), r.Next(0, 255));
                    // Necessário para conhecer o índice de cada dado no eixo X, serve para moldar a janela
                    // aparente, utilizada para DFT e Prony
                    series.IsXValueIndexed = true;
                    // Define o tipo da série recém-criada, aqui usa-se o tipo 'FastLine'
                    series.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastLine;


                    for (int i = 0; i < measurement.Series[Channel.FREQ].Count; i++)
                    {
                        if (measurement.Series.ContainsKey(Channel.VOLTAGE_POS_MOD))
                        {
                            // Se a tensão de SP está abaixo do limiar definido, repete-se a última frequência                        
                            if (i != 0 && measurement.Series[Channel.VOLTAGE_POS_MOD].Reading(i) < vMin * (1000*measurement.Terminal.VoltageLevel / Math.Sqrt(3)))
                            {
                                if (series.Points.Count > 0)
                                    series.Points.AddXY(data, series.Points.Last().YValues[0]);
                            }
                            else
                            {
                                series.Points.AddXY(data, measurement.Series[Channel.FREQ].Reading(i));
                            }
                        }
                        else
                        {
                            series.Points.AddXY(data, measurement.Series[Channel.FREQ].Reading(i));
                        }
                        
                        // Incremento em 'ticks' no valor da data, de acordo com a taxa da consulta
                        // 10 mil ticks = 1 ms
                        inc = Convert.ToInt64(Math.Floor((1 / taxa) * 1000 * (i + 1)) * 10000);
                        data = dataIni.AddTicks(inc);
                    }
                    chart1.Series.Add(series);
                }
            }

            // Gráfico da variação frequência
            if (OpcaoGrafico.Opcao == 23)
            {
                // Define o título do eixo Y
                chart1.ChartAreas[0].AxisY.Title = "Variação de Frequência (Hz/s)";
                // Desabilita o RBE se mais de 3 sinais forem selecionados
                if (numSele > 3)
                {
                    rBEToolStripMenuItem.Enabled = false;
                }
                // Título do gráfico
                if (taxa == 1)
                {
                    chart1.Titles[0].Text = "Variação de Frequência" + " - "
                        + Convert.ToString(taxa) + " fasor/s";
                }
                else
                {
                    chart1.Titles[0].Text = "Variação de Frequência" + " - "
                        + Convert.ToString(taxa) + " fasores/s";
                }

                foreach(Measurement measurement in selectedMeasurements)
                {
                    // Inicialização da data para o eixo X
                    data = dataIni;
                    // Inicializa o incremento
                    inc = 0;

                    Series series = new Series(measurement.Terminal.DisplayName);

                    // Adiciona quantas séries forem necessárias. O seu nome corresponde ao terminal.
                    // Define a cor da série
                    if (chart1.Series.Count < 80)
                        series.Color = cores[chart1.Series.Count];
                    else
                        series.Color = Color.FromArgb(r.Next(0, 255), r.Next(0, 255), r.Next(0, 255));
                    // Necessário para conhecer o índice de cada dado no eixo X, serve para moldar a janela
                    // aparente, utilizada para DFT e Prony
                    series.IsXValueIndexed = true;
                    // Define o tipo da série recém-criada, aqui usa-se o tipo 'FastLine'
                    series.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastLine;

                    for (int i = 0; i < measurement.Series[Channel.DFREQ].Count; i++)
                    {
                        if (measurement.Series.ContainsKey(Channel.VOLTAGE_POS_MOD))
                        {
                            // Se a tensão de SP está abaixo do limiar definido, repete-se a última frequência                        
                            if (i != 0 && measurement.Series[Channel.VOLTAGE_POS_MOD].Reading(i) < vMin * (1000 * measurement.Terminal.VoltageLevel / Math.Sqrt(3)))
                            {
                                if (series.Points.Count > 0)
                                    series.Points.AddXY(data, series.Points.Last().YValues[0]);
                            }
                            else
                            {
                                series.Points.AddXY(data, measurement.Series[Channel.DFREQ].Reading(i));
                            }
                        }
                        else
                        {
                            series.Points.AddXY(data, measurement.Series[Channel.DFREQ].Reading(i));
                        }

                        // Incremento em 'ticks' no valor da data, de acordo com a taxa da consulta
                        // 10 mil ticks = 1 ms
                        inc = Convert.ToInt64(Math.Floor((1 / taxa) * 1000 * (i + 1)) * 10000);
                        data = dataIni.AddTicks(inc);
                    }
                    chart1.Series.Add(series);
                }
            }

            // Gráfico da desbalanço de tensão
            if (OpcaoGrafico.Opcao == 24)
            {
                // Define o título do eixo Y
                chart1.ChartAreas[0].AxisY.Title = "Desbalanço de Tensão";
                // Desabilita o RBE se mais de 3 sinais forem selecionados
                if (numSele > 3)
                {
                    rBEToolStripMenuItem.Enabled = false;
                }
                // Título do gráfico
                if (taxa == 1)
                {
                    chart1.Titles[0].Text = "Desbalanço de Tensão" + " - "
                        + Convert.ToString(taxa) + " fasor/s";
                }
                else
                {
                    chart1.Titles[0].Text = "Desbalanço de Tensão" + " - "
                        + Convert.ToString(taxa) + " fasores/s";
                }

                foreach (Measurement measurement in selectedMeasurements)
                {
                    // Inicialização da data para o eixo X
                    data = dataIni;
                    // Inicializa o incremento
                    inc = 0;

                    Series series = new Series(measurement.Terminal.DisplayName);

                    // Adiciona quantas séries forem necessárias. O seu nome corresponde ao terminal.
                    // Define a cor da série
                    if (chart1.Series.Count < 80)
                        series.Color = cores[chart1.Series.Count];
                    else
                        series.Color = Color.FromArgb(r.Next(0, 255), r.Next(0, 255), r.Next(0, 255));
                    // Necessário para conhecer o índice de cada dado no eixo X, serve para moldar a janela
                    // aparente, utilizada para DFT e Prony
                    series.IsXValueIndexed = true;
                    // Define o tipo da série recém-criada, aqui usa-se o tipo 'FastLine'
                    series.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastLine;

                    for (int i = 0; i < measurement.Series[Channel.DFREQ].Count; i++)
                    {
                        var phase = new Channel(0, "", ChannelPhase.NONE, ChannelValueType.NONE, ChannelQuantity.VIMB);
                        series.Points.AddXY(data, measurement.Series[phase].Reading(i));

                        // Incremento em 'ticks' no valor da data, de acordo com a taxa da consulta
                        // 10 mil ticks = 1 ms
                        inc = Convert.ToInt64(Math.Floor((1 / taxa) * 1000 * (i + 1)) * 10000);
                        data = dataIni.AddTicks(inc);
                    }
                    chart1.Series.Add(series);
                }
            }

            if ((OpcaoGrafico.Opcao == 25) || (OpcaoGrafico.Opcao == 26) || (OpcaoGrafico.Opcao == 27))
            {
                // Define qual das séries foi selecionada
                MedFasee.Equipment.Channel phase = null;
                switch (OpcaoGrafico.Opcao)
                {
                    case 25:
                        phase = new Channel(0, "", ChannelPhase.PHASE_A, ChannelValueType.NONE, ChannelQuantity.THD);
                        break;
                    case 26:
                        phase = new Channel(0, "", ChannelPhase.PHASE_B, ChannelValueType.NONE, ChannelQuantity.THD);
                        break;
                    case 27:
                        phase = new Channel(0, "", ChannelPhase.PHASE_C, ChannelValueType.NONE, ChannelQuantity.THD);
                        break;
                    default:
                        break;
                }

                // Desabilita o RBE se mais de um sinal foi selecionado
                if (numSele > 3)
                {
                    rBEToolStripMenuItem.Enabled = false;
                }

                // Título do gráfico
                if (taxa == 1)
                {
                    chart1.Titles[0].Text = "Distorção Harmônica Total - Fase " +
                        phase.Phase.ToString().Substring(phase.Phase.ToString().Length - 1) + " - "
                        + Convert.ToString(taxa) + " fasor/s";
                }
                else
                {
                    chart1.Titles[0].Text = "Distorção Harmônica Total - Fase " +
                        phase.Phase.ToString().Substring(phase.Phase.ToString().Length - 1) + " - "
                        + Convert.ToString(taxa) + " fasores/s";
                }

                chart1.ChartAreas[0].AxisY.Title = "Distorção harmônica (%)";

                foreach (Measurement measurement in selectedMeasurements)
                {

                    // Inicialização da data para o eixo X
                    data = dataIni;
                    // Inicializa o incremento
                    inc = 0;

                    // Adiciona quantas séries forem necessárias. O seu nome corresponde ao terminal.

                    Series series = new Series(measurement.Terminal.DisplayName);

                    if (chart1.Series.Count < 80)
                        series.Color = cores[chart1.Series.Count];
                    else
                        series.Color = Color.FromArgb(r.Next(0, 255), r.Next(0, 255), r.Next(0, 255));
                    // Necessário para conhecer o índice de cada dado no eixo X, serve para moldar a janela
                    // aparente, utilizada para DFT e Prony
                    series.IsXValueIndexed = true;
                    // Define o tipo da série recém-criada, aqui usa-se o tipo 'FastLine'
                    series.ChartType = SeriesChartType.FastLine;



                    double vb = 1; // a base para gráficos em pu 

                    if (OpcaoGrafico.Pu == 1)
                        vb = (measurement.Terminal.VoltageLevel / Math.Sqrt(3)) * 1000;

                    for (int i = 0; i < measurement.Series[phase].Count; i++)
                    {
                        series.Points.AddXY(data, measurement.Series[phase].Reading(i));
                        inc = Convert.ToInt64(Math.Floor((1 / taxa) * 1000 * (i + 1)) * 10000);
                        data = dataIni.AddTicks(inc);
                    }

                    chart1.Series.Add(series);
                }


            }

            // Gráfico da distorção harmonica - Trifásico
            if (OpcaoGrafico.Opcao == 28)
            {
                // Desabilita o bloco de análise
                toolStrip1.Enabled = false;

                // Apaga o bloco de análise
                //toolStrip1.Visible = false;
                

                Channel[] fases;
                fases = new Channel[3];

                fases[0] = new Channel(0, "", ChannelPhase.PHASE_A, ChannelValueType.NONE, ChannelQuantity.THD);
                fases[1] = new Channel(0, "", ChannelPhase.PHASE_B, ChannelValueType.NONE, ChannelQuantity.THD);
                fases[2] = new Channel(0, "", ChannelPhase.PHASE_C, ChannelValueType.NONE, ChannelQuantity.THD);

                chart1.ChartAreas[0].AxisY.Title = "Distorção Harmonica (%)";

                // Título do gráfico
                if (taxa == 1)
                {
                    chart1.Titles[0].Text = "Distorção Harmônica Total de Tensão - " + selectedMeasurements[0].Terminal.DisplayName + " - "
                        + Convert.ToString(taxa) + " fasor/s";
                }
                else
                {
                    chart1.Titles[0].Text = "Distorção Harmônica Total de Tensão - " + selectedMeasurements[0].Terminal.DisplayName + " - "
                        + Convert.ToString(taxa) + " fasores/s";
                }

                for (int k = 0; k < 3; k++)
                {
                    // Inicialização da data para o eixo X
                    data = dataIni;
                    // Inicializa o incremento
                    inc = 0;

                    // Adiciona quantas séries forem necessárias. O seu nome corresponde ao terminal.
                    chart1.Series.Add("Fase " + fases[k].Phase.ToString().Substring(fases[k].Phase.ToString().Length - 1));
                    chart1.Series[k].Color = cores[k];
                    // Necessário para conhecer o índice de cada dado no eixo X, serve para moldar a janela
                    // aparente, utilizada para DFT e Prony
                    chart1.Series[k].IsXValueIndexed = true;
                    // Define o tipo da série recém-criada, aqui usa-se o tipo 'FastLine'
                    chart1.Series[k].ChartType = SeriesChartType.FastLine;

                    for (int i = 0; i < selectedMeasurements[0].Series[fases[k]].Count; i++)
                    {
                        // Traça a série
                        chart1.Series[k].Points.AddXY(data, selectedMeasurements[0].Series[fases[k]].Reading(i));

                        // Incremento em 'ticks' no valor da data, de acordo com a taxa da consulta
                        // 10 mil ticks = 1 ms
                        inc = Convert.ToInt64(Math.Floor((1 / taxa) * 1000 * (i + 1)) * 10000);
                        data = dataIni.AddTicks(inc);
                    }

                }
            }

            // Se alguma das séries não possui nenhum ponto, desabilita todas as opções de análise
            for (int i = 0; i < chart1.Series.Count; i++)
            {
                if (chart1.Series[i].Points.Count == 0)
                {
                    toolStripButton1.Enabled = false;
                    toolStripSplitButton1.Enabled = false;
                    toolStripSplitButton3.Enabled = false;
                }
            }
            // Ou se não há séries
            if (chart1.Series.Count == 0)
            {
                toolStripButton1.Enabled = false;
                toolStripSplitButton1.Enabled = false;
                toolStripSplitButton3.Enabled = false;
            }
        }

        private void chart1_KeyDown_1(object sender, KeyEventArgs e)
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
                bm.SetResolution(4000, 4000);
                // Disponibiliza o bitmap na área de trabalho
                Clipboard.SetImage(bm);
            }
            else if (e.Control)
            {
                //flagPan = true;
            }
            //chart1.BorderlineDashStyle = ChartDashStyle.NotSet;
        }

        private void Form3_Leave(object sender, EventArgs e)
        {
            chart1.BorderlineDashStyle = ChartDashStyle.NotSet;
        }        

        private void chart1_AxisViewChanged(object sender, ViewEventArgs e)
        {
            int a, b, c;

            // Mínimo da janela atual
            a = Convert.ToInt32(chart1.ChartAreas[0].AxisX.ScaleView.ViewMinimum);
            // Máximo da janela atual
            b = Convert.ToInt32(chart1.ChartAreas[0].AxisX.ScaleView.ViewMaximum);

            // Ordem calculada considerando a janela atual
            c = Convert.ToInt32((b - a) / 4);

            if ((b - a) > 4)
            {
                toolStripTextBox1.Text = Convert.ToString(c);
            }
            else
            {
                toolStripTextBox1.Text = Convert.ToString(b - a);
            }
        }

        private void chart1_DoubleClick(object sender, EventArgs e)
        {
            //chart1.ChartAreas[0].AxisX.Minimum = Double.NaN;
            //chart1.ChartAreas[0].AxisX.Maximum = Double.NaN;
            chart1.ChartAreas[0].AxisY.Minimum = Double.NaN;
            chart1.ChartAreas[0].AxisY.Maximum = Double.NaN;

            // reset dos intervalos e intervalos de label
            chart1.ChartAreas[0].AxisX.LabelStyle.Interval = 0;
            chart1.ChartAreas[0].AxisX.Interval = 0;
            chart1.ChartAreas[0].AxisY.LabelStyle.Interval = 0;
            chart1.ChartAreas[0].AxisY.Interval = 0;
            // Desfaz o zoom
            chart1.ChartAreas[0].AxisX.ScaleView.ZoomReset();
            chart1.ChartAreas[0].AxisY.ScaleView.ZoomReset();
        }

        private void chart1_MouseDown(object sender, MouseEventArgs e)
        {
            tip.IsBalloon = true;

            // index of the clicked point in its series
            //int index = result.PointIndex;
            Int32 index = Convert.ToInt32(Math.Round(chart1.ChartAreas[0].AxisX.PixelPositionToValue(e.Location.X)) - 1);
            long inc = Convert.ToInt64(Math.Floor((1 / Convert.ToDouble(taxa)) * 1000 * (index)) * 10000);
            DateTime data = dataIni.AddTicks(inc);
            string horario = "";

            if (taxa == 1)
            {
                //horario = data.Hour + ":" + data.Minute + ":" + data.Second;
                horario = string.Format("{0:HH:mm:ss}", data);
            }
            else
            {
                //horario = data.Hour + ":" + data.Minute + ":" + data.Second + "." + data.Millisecond;
                horario = string.Format("{0:HH:mm:ss.fff}", data);
            }

            if (e.Button == MouseButtons.Right)
            {
                chart1.ChartAreas[0].CursorX.LineColor = Color.Transparent;

                // Call Hit Test Method
                HitTestResult result = chart1.HitTest(e.X, e.Y);

                if (result.ChartElementType == ChartElementType.DataPoint)
                {
                    tip.ToolTipTitle = result.Series.Name;
                    tip.Show("X = " + horario + "\nY = " + Math.Round(result.Series.Points[index].YValues[0], 5), chart1, e.X, e.Y);
                    tip.Show("X = " + horario + "\nY = " + Math.Round(result.Series.Points[index].YValues[0], 5), chart1, e.X, e.Y, 3000);
                }
                else if (result.ChartElementType == ChartElementType.LegendItem)
                {
                    tip.Hide(chart1);

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
                else if (result.ChartElementType == ChartElementType.AxisLabels)
                {
                    tip.Hide(chart1);

                    // Nome do eixo no qual houve o clic
                    string axis = result.Axis.AxisName.ToString();

                    AjustaTempo f = new AjustaTempo(this, chart1, dataIni, taxa, axis);
                    f.ShowDialog();
                }
                else
                {
                    tip.Hide(chart1);
                }
            }
            else
            {
                tip.Hide(chart1);
                if (Control.ModifierKeys == Keys.Control)
                {
                    // Define o que fazer de acordo com o número de striplines presentes no gráfico
                    if (chart1.ChartAreas[0].AxisX.StripLines[0].BackColor == Color.Transparent)
                    {
                        chart1.ChartAreas[0].AxisX.StripLines[0].IntervalOffset = index;
                        chart1.ChartAreas[0].AxisX.StripLines[0].BackColor = Color.Black;
                        chart1.ChartAreas[0].AxisX.StripLines[0].ForeColor = Color.Black;
                        chart1.ChartAreas[0].AxisX.StripLines[0].Text = "T1";

                        //labelX1.Text = "T1 = " + data.ToString("HH:mm:ss.fff");

                        chart1.Titles[1].Text = "T1 = " + data.ToString("HH:mm:ss.fff") + "   " + "T2 = 00:00:00.000";

                    }
                    else if (chart1.ChartAreas[0].AxisX.StripLines[0].BackColor != Color.Transparent &&
                        chart1.ChartAreas[0].AxisX.StripLines[1].BackColor == Color.Transparent)
                    {
                        chart1.ChartAreas[0].AxisX.StripLines[1].IntervalOffset = index;
                        chart1.ChartAreas[0].AxisX.StripLines[1].BackColor = Color.Black;
                        chart1.ChartAreas[0].AxisX.StripLines[1].ForeColor = Color.Black;
                        chart1.ChartAreas[0].AxisX.StripLines[1].Text = "T2";

                        TimeSpan dt = data - previousT1;
                        string dtStr = (dt < TimeSpan.Zero ? "\\-" : "") + "hh\\:mm\\:ss\\.fff";
                        chart1.Titles[1].Text = "T1 = " + previousT1.ToString("HH:mm:ss.fff") + "   " +
                            "T2 = " + data.ToString("HH:mm:ss.fff") + "   " +
                            "DT = " + dt.ToString(dtStr) + "   " + "|1/DT| = " + (Math.Abs(1 / dt.TotalSeconds)).ToString("0.000") + " Hz";

                    }
                    else if (chart1.ChartAreas[0].AxisX.StripLines[0].BackColor != Color.Transparent &&
                        chart1.ChartAreas[0].AxisX.StripLines[1].BackColor != Color.Transparent)
                    {
                        // Nome da linha T2 passa a ser T1
                        if (chart1.ChartAreas[0].AxisX.StripLines[0].Text == "T1")
                        {
                            chart1.ChartAreas[0].AxisX.StripLines[1].Text = "T1";
                            chart1.ChartAreas[0].AxisX.StripLines[0].Text = "T2";
                            chart1.ChartAreas[0].AxisX.StripLines[0].IntervalOffset = index;
                        }
                        else if (chart1.ChartAreas[0].AxisX.StripLines[0].Text == "T2")
                        {
                            chart1.ChartAreas[0].AxisX.StripLines[0].Text = "T1";
                            chart1.ChartAreas[0].AxisX.StripLines[1].Text = "T2";
                            chart1.ChartAreas[0].AxisX.StripLines[1].IntervalOffset = index;
                        }
         

                        //labelX2.Text = "T2 = " + data.ToString("HH:mm:ss.fff");
                        TimeSpan dt = data - previousT1;
                        string dtStr = (dt < TimeSpan.Zero ? "\\-" : "") + "hh\\:mm\\:ss\\.fff";
                        chart1.Titles[1].Text = "T1 = " + previousT1.ToString("HH:mm:ss.fff") + "   " +
                            "T2 = " + data.ToString("HH:mm:ss.fff") + "   " +
                            "DT = " + dt.ToString(dtStr) + "   " + "|1/DT| = " + (Math.Abs(1 / dt.TotalSeconds)).ToString("0.000") + " Hz";
                    }                
                } 
                else
                {
                    //chart1.ChartAreas[0].CursorX.LineColor = Color.Transparent;
                    chart1.Titles[1].Text = "T1 = 00:00:00.000   T2 = 00:00:00.000";
                    // Limpa as striplines do gráfico
                    //chart1.ChartAreas[0].AxisX.StripLines.Clear();
                    chart1.ChartAreas[0].AxisX.StripLines[0].BackColor = Color.Transparent;
                    chart1.ChartAreas[0].AxisX.StripLines[1].BackColor = Color.Transparent;
                    chart1.ChartAreas[0].AxisX.StripLines[0].ForeColor = Color.Transparent;
                    chart1.ChartAreas[0].AxisX.StripLines[1].ForeColor = Color.Transparent;
                }               
            }

            previousT1 = data;
        }

             
        private void panel2_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                pto = e.Location;
        }

        private void panel2_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                panel2.Left += e.X - pto.X;
                panel2.Top += e.Y - pto.Y;
            }
        }

        private void chart1_MouseLeave(object sender, EventArgs e)
        {
            tip.Hide(this);
        }

        private void toolStripSplitButton1_DropDownOpening(object sender, EventArgs e)
        {
            int a, b, c;

            // Mínimo da janela atual
            a = Convert.ToInt32(chart1.ChartAreas[0].AxisX.ScaleView.ViewMinimum);
            // Máximo da janela atual
            b = Convert.ToInt32(chart1.ChartAreas[0].AxisX.ScaleView.ViewMaximum);

            // Ordem calculada considerando a janela atual
            c = Convert.ToInt32((b - a) / 4);

            if ((b - a) > 4)
            {
                if (c < 300)
                    toolStripTextBox1.Text = Convert.ToString(c);
                else
                    toolStripTextBox1.Text = Convert.ToString(300);
            }
            else
            {
                toolStripTextBox1.Text = Convert.ToString(b - a);
            }
        }       

        private void toolStripSplitButton3_DropDownOpening(object sender, EventArgs e)
        {
            // Ordem do modelo
            toolStripTextBox7.Text = (8).ToString();
            // Número de linhas por bloco
            toolStripTextBox8.Text = (20).ToString();
            // Tamanho da janela deslizante (em minutos)
            toolStripTextBox9.Text = (10).ToString();
            // Passo com o qual a janela desliza (em segundos)
            toolStripTextBox10.Text = (60).ToString();
            // Frequência mínima
            toolStripTextBox11.Text = (0.3).ToString();
            // Frequência máxima
            toolStripTextBox12.Text = (0.4).ToString();
        }

        private void calcularToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            // TRAÇAR A DFT

            double f = chart1.ChartAreas[0].AxisX.Minimum;

            // Opção de gráfico escolhida
            int op = Convert.ToInt16(textBox2.Text);
            int opPu = Convert.ToInt16(textBox4.Text);

            // Índices inicial e final da janela visível no momento em que a DFT é requisitada
            int minInd = Convert.ToInt32(chart1.ChartAreas[0].AxisX.ScaleView.ViewMinimum - 1.0);
            int maxInd = Convert.ToInt32(chart1.ChartAreas[0].AxisX.ScaleView.ViewMaximum - 1.0);

            // Cores atuais das séries (pode ser que o usuário tenha alterado alguma cor original)
            Color[] coresAtuais = new Color[chart1.Series.Count];

            for (int i = 0; i < chart1.Series.Count; i++)
            {
                coresAtuais[i] = chart1.Series[i].Color;
            }

            pai.CriarDFT(Query, op, reference, selectedMeasurements, minInd, maxInd, opPu, coresAtuais);
        }

        private void calcularToolStripMenuItem1_Click_1(object sender, EventArgs e)
        {
            // CALCULAR O PRONY

            // Opção escolhida de gráfico
            int op = Convert.ToInt16(textBox2.Text);
            // Indica se o gráfico está em pu
            int opPu = Convert.ToInt16(textBox4.Text);
            // Ordem escolhida para o cálculo do Prony
            int ordemEsc = Convert.ToInt32(textBox5.Text);

            // Texto do título
            string titulo = chart1.ChartAreas[0].AxisX.Title.ToString();

            // Índices inicial e final da janela visível no momento em que o Prony é requisitado
            int minInd = Convert.ToInt32(chart1.ChartAreas[0].AxisX.ScaleView.ViewMinimum - 1.0);
            int maxInd = Convert.ToInt32(chart1.ChartAreas[0].AxisX.ScaleView.ViewMaximum - 1.0);

            // Cores atuais das séries (pode ser que o usuário tenha alterado alguma cor original)
            Color[] coresAtuais = new Color[chart1.Series.Count];

            for (int i = 0; i < chart1.Series.Count; i++)
            {
                coresAtuais[i] = chart1.Series[i].Color;
            }

            if ((((maxInd - minInd + 1) / taxa) > 60) ||
                ((maxInd - minInd + 1) < 4) ||
                (ordemEsc < 1) || (ordemEsc >= selectedMeasurements[0].Series[Channel.MISSING].Count)
                || (ordemEsc > 300) || (numSele > 25))
            {
                if (((maxInd - minInd + 1) / taxa) > 60)
                {
                    MessageBox.Show("Escolha um período de dados com duração menor que 1 minuto.", "ATENÇÃO", MessageBoxButtons.OK);
                }
                else if ((maxInd - minInd + 1) < 4)
                {
                    MessageBox.Show("Escolha um período de dados com mais do que 4 pontos.", "ATENÇÃO", MessageBoxButtons.OK);
                }
                else if (ordemEsc < 1)
                {
                    MessageBox.Show("A ordem escolhida deve ser maior que um.", "ATENÇÃO", MessageBoxButtons.OK);
                }
                else if (ordemEsc >= selectedMeasurements[0].Series[Channel.MISSING].Count)
                {
                    string s = "A ordem escolhida deve ser menor que o número de pontos do gráfico. Este gráfico possui " +
                        Convert.ToString(selectedMeasurements[0].Series[Channel.MISSING].Count) + " pontos.";
                    MessageBox.Show(s, "ATENÇÃO", MessageBoxButtons.OK);
                }
                else if (ordemEsc > 300)
                {
                    MessageBox.Show("A ordem escolhida não deve ser superior a 300.", "ATENÇÃO", MessageBoxButtons.OK);
                }
                else if (numSele > 25)
                {
                    MessageBox.Show("O número de sinais selecionados não deve ser superior a 25.", "ATENÇÃO", MessageBoxButtons.OK);
                }

            }
            else
            {
                pai.CriarProny(Query, op, reference, selectedMeasurements, minInd, maxInd, opPu, ordemEsc, coresAtuais);
            }
        }

        private void calcularToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            // Opção de gráfico escolhida
            int op = Convert.ToInt16(textBox2.Text);
            int opPu = Convert.ToInt16(textBox4.Text);

            int minInd = Convert.ToInt32(chart1.ChartAreas[0].AxisX.ScaleView.ViewMinimum - 1.0);
            int maxInd = Convert.ToInt32(chart1.ChartAreas[0].AxisX.ScaleView.ViewMaximum - 1.0);

            // Cores atuais das séries (pode ser que o usuário tenha alterado alguma cor original)
            Color[] coresAtuais = new Color[chart1.Series.Count];

            for (int i = 0; i < chart1.Series.Count; i++)
            {
                coresAtuais[i] = chart1.Series[i].Color;
            }

            pai.CriarDFT(Query, op, reference, selectedMeasurements, minInd, maxInd, opPu, coresAtuais);
        }

        private void calcularToolStripMenuItem3_Click(object sender, EventArgs e)
        {
            try
            {
                // CALCULAR A RBE

                // Opção escolhida de gráfico
                int op = Convert.ToInt16(textBox2.Text);
                // Indica se o gráfico está em pu
                int opPu = Convert.ToInt16(textBox4.Text);
                // Dimensão
                int dimensao = Convert.ToInt32(textBox7.Text);
                // Número de blocos
                int numBlocos = Convert.ToInt32(textBox8.Text);
                // Tamanho da janela
                int tamJanela = Convert.ToInt32(textBox9.Text);
                // Passo da janela
                int pasJanela = Convert.ToInt32(textBox10.Text);
                // Freq. mínima da faixa de interesse
                double freqMin = Convert.ToDouble(textBox11.Text);
                // Freq. mínima da faixa de interesse
                double freqMax = Convert.ToDouble(textBox12.Text);


                // executa o método se: Nº dados jan. des. > 2 * nbr
                //                      && Nº dados jan. des. <= Nº dados jan. total 
                if ((tamJanela * 60 * taxa > 2 * numBlocos) && (tamJanela * 60 * taxa <= selectedMeasurements[0].Series[Channel.MISSING].Count))
                {

                    // Texto do título
                    string titulo = chart1.ChartAreas[0].AxisX.Title.ToString();

                    // Índices mínimo e máximo do período representado na janela atual
                    int minInd, maxInd;

                    // Índices inicial e final da janela visível no momento em que o Prony é requisitado
                    if (chart1.ChartAreas[0].AxisX.ScaleView.IsZoomed == true)
                    {
                        minInd = Convert.ToInt32(chart1.ChartAreas[0].AxisX.ScaleView.ViewMinimum - 1.0);
                        maxInd = Convert.ToInt32(chart1.ChartAreas[0].AxisX.ScaleView.ViewMaximum - 1.0);
                    }
                    else
                    {
                        minInd = Convert.ToInt32(chart1.ChartAreas[0].AxisX.ScaleView.ViewMinimum - 1.0);
                        maxInd = Convert.ToInt32(chart1.ChartAreas[0].AxisX.ScaleView.ViewMaximum - 1.0);
                    }

                    // Cores atuais das séries (pode ser que o usuário tenha alterado alguma cor original)
                    Color[] coresAtuais = new Color[chart1.Series.Count];

                    for (int i = 0; i < chart1.Series.Count; i++)
                    {
                        coresAtuais[i] = chart1.Series[i].Color;
                    }

                    pai.CriarRBE(Query, op, reference, selectedMeasurements, minInd, maxInd, opPu, coresAtuais, dimensao, numBlocos, tamJanela, pasJanela, freqMin, freqMax);
                }
                else if (tamJanela * 60 * taxa < 2 * numBlocos)
                {
                    MessageBox.Show("O número de linhas por bloco deve ser inferior a metade do número de pontos da janela deslizante. Exemplo: uma janela deslizante de 1 min. com resolução de 1 fasor/s contém 60 pontos, assim o nº de linhas por bloco deve ser menor que 30.", "MedPlot", MessageBoxButtons.OK);
                }
                else if (tamJanela * 60 * taxa > selectedMeasurements[0].Series[Channel.MISSING].Count)
                {
                    MessageBox.Show("O tamanho da janela deslizante deve ser inferior ao tamanho do período total do sinal.", "MedPlot", MessageBoxButtons.OK);
                }

            }
            catch (Exception)
            {

            }
        }

        private void toolStripTextBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == Convert.ToChar("."))
                e.KeyChar = Convert.ToChar(",");
        }

        private void toolStripTextBox1_TextChanged(object sender, EventArgs e)
        {
            // copia a ordem digitada pelo usuário para a caixa
            textBox5.Text = toolStripTextBox1.Text;
        }

        private void toolStripTextBox7_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar)
                && !char.IsDigit(e.KeyChar))
                e.Handled = true;
        }

        private void toolStripTextBox8_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar)
                && !char.IsDigit(e.KeyChar))
                e.Handled = true;
        }

        private void toolStripTextBox9_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar)
                && !char.IsDigit(e.KeyChar))
                e.Handled = true;
        }

        private void toolStripTextBox10_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar)
                && !char.IsDigit(e.KeyChar))
                e.Handled = true;
        }

        private void toolStripTextBox11_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar)
                && !char.IsDigit(e.KeyChar)
                && e.KeyChar != decSep)
                e.Handled = true;
        }

        private void toolStripTextBox12_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar)
                && !char.IsDigit(e.KeyChar)
                && e.KeyChar != decSep)
                e.Handled = true;
        }

        private void toolStripTextBox7_TextChanged(object sender, EventArgs e)
        {
            textBox7.Text = toolStripTextBox7.Text;
        }

        private void toolStripTextBox8_TextChanged(object sender, EventArgs e)
        {
            textBox8.Text = toolStripTextBox8.Text;
        }

        private void toolStripTextBox9_TextChanged(object sender, EventArgs e)
        {
            textBox9.Text = toolStripTextBox9.Text;
        }

        private void toolStripTextBox10_TextChanged(object sender, EventArgs e)
        {
            textBox10.Text = toolStripTextBox10.Text;
        }

        private void toolStripTextBox11_TextChanged(object sender, EventArgs e)
        {
            textBox11.Text = toolStripTextBox11.Text;
        }

        private void toolStripTextBox12_TextChanged(object sender, EventArgs e)
        {
            textBox12.Text = toolStripTextBox12.Text;
        }

        private void toolStripTextBox13_KeyPress(object sender, KeyPressEventArgs e)
        {
            //só permite que sejam inseridos números e backspace
            if (!Char.IsDigit(e.KeyChar) && e.KeyChar != '\b')
            {
                e.Handled = true;
            }
        }

        private void filtroDeKalmanToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            //faz a lógica de "checkar" os menus do botão localização de eventos
            foreach (ToolStripMenuItem Item in ((ToolStripMenuItem)sender).GetCurrentParent().Items)
            {
                Item.Checked = false;
                foreach (ToolStripItem Subitem in Item.DropDownItems)
                {
                    Subitem.Visible = false;
                }
            }
            ((ToolStripMenuItem)sender).Checked = true;
            foreach (ToolStripItem Subitem in ((ToolStripMenuItem)sender).DropDownItems)
            {
                Subitem.Visible = true;
            }
            toolStripButton1.ShowDropDown();
            configurarToolStripMenuItem.ShowDropDown();
            ((ToolStripMenuItem)sender).ShowDropDown();


        }

        private void sinalIndicadorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //executa o método de pré processamento selecionado

            //verifica qual o método está selecionado
            int k = 0;
            foreach (ToolStripMenuItem Item in configurarToolStripMenuItem.DropDownItems)
            {
                if (Item.Checked) break;
                k++;
            }

            // Cores atuais das séries (pode ser que o usuário tenha alterado alguma cor original)
            Color[] coresAtuais = new Color[chart1.Series.Count];

            for (int i = 0; i < chart1.Series.Count; i++)
            {
                coresAtuais[i] = chart1.Series[i].Color;
            }
            int minInd = Convert.ToInt32(chart1.ChartAreas[0].AxisX.ScaleView.ViewMinimum - 1.0);
            int maxInd = Convert.ToInt32(chart1.ChartAreas[0].AxisX.ScaleView.ViewMaximum - 1.0);

            switch (k)
            {
                
                case 0:               
                    parametrosFreqFiltrada parametros1 = new parametrosFreqFiltrada(Convert.ToInt32(toolStripTextBox13.Text));
                    pai.CriarEvento(Query, selectedMeasurements, minInd, maxInd, coresAtuais, parametros1, dirConsulta, vMin);
                    break;
                
                case 1:
                    parametrosPassaFaixa parametros2 = new parametrosPassaFaixa(Convert.ToInt32(toolStripTextBox15.Text), Convert.ToInt32(toolStripTextBox16.Text));
                    pai.CriarEvento(Query, selectedMeasurements, minInd, maxInd, coresAtuais, parametros2, dirConsulta, vMin);
                    break;
                
                case 2:
                    parametrosFMMeTX parametros3 = new parametrosFMMeTX(Convert.ToInt32(toolStripTextBox21.Text), Convert.ToInt32(toolStripTextBox17.Text));
                    pai.CriarEvento(Query, selectedMeasurements, minInd, maxInd, coresAtuais, parametros3, dirConsulta, vMin);
                    break;

                case 3:
                    parametrosFiltroDeKalman parametros4 = new parametrosFiltroDeKalman(Convert.ToDouble(toolStripTextBox18.Text), Convert.ToDouble(toolStripTextBox19.Text), Convert.ToDouble(toolStripTextBox20.Text));
                    pai.CriarEvento(Query, selectedMeasurements, minInd, maxInd, coresAtuais, parametros4, dirConsulta, vMin);
                    break;
            }
        }

        private void toolStripTextBox18_KeyPress(object sender, KeyPressEventArgs e)
        {
            //só permite que sejam inseridos números, backspace e separador decimal (. ou , dependendo da região)
            char separadorDecimal = Convert.ToChar(System.Globalization.CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator);
            if (!Char.IsDigit(e.KeyChar) && e.KeyChar != '\b' && (e.KeyChar != separadorDecimal || ((ToolStripTextBox)sender).Text.Contains(separadorDecimal)))
            {
                e.Handled = true;
            }
        }

    }
}
