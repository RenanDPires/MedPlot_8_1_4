using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using DotNumerics;
using DotNumerics.LinearAlgebra;
using System.Windows.Forms.DataVisualization.Charting;
using MedFasee.Structure;
using MedFasee.Equipment;

namespace MedPlot
{
    public partial class GraficoEventos : Form
    {
        private JanelaPrincipal pai; //form pai
        Color[] cores; //cores do gráfico

        private int min, max; //índices máximo e mínimo dos pontos mostrados no gráfico
        private DateTime dataIni; //data inicial da consulta
        private double taxa; //taxa de amostragem
        private double[][] freqOriginal; //vetor com a frequência original
        private double[][] sinalDetecção; //vetor com a frequência filtrada
        private DateTime[] vetTempo; //vetor de tempo
        private Eventos EventosObj; //objeto que armazena os eventos
        private string dirConsulta;
        private ToolTip tip = new ToolTip();

        public Query Query { get; internal set; }
        Measurement[] SelectedMeasurements { get; }

        // caso seja frequência filtrada
        public GraficoEventos(JanelaPrincipal frm, Query query, Measurement[] measurements, int minInd, int maxInd, Color[] coresAtuais, parametrosFreqFiltrada parametros, string dir, double vMin)
        {

            InitializeComponent();
            dirConsulta = dir;
            chart1.ChartAreas[0].AxisY.Title = "Hz";
            chart1.ChartAreas[0].AxisY.LabelStyle.Format = "0.#####";
            chart1.Titles[0].Text = "Frequência Original";

            chart2.ChartAreas[0].AxisY.Title = "Hz";
            chart2.ChartAreas[0].AxisY.LabelStyle.Format = "0.#####";
            chart2.Titles[0].Text = "Frequência Filtrada pelo Filtro Passa-Baixa";
            toolStripTextBox1.Text = Convert.ToString(0.05); //pra garantir que vai escrever com . ou , dependendo da região
            //passa algumas variáveis na inicialização
            pai = frm;
            Query = query;
            SelectedMeasurements = measurements;
            min = minInd;
            max = maxInd;
            int ordemF = parametros.ordemDoFiltro;
            cores = new Color[coresAtuais.Length];
            cores = coresAtuais;

            int tlEvento = (max - min) + 1; //comprimento da janela (em pontos)

            dataIni = SelectedMeasurements[0].Start;
            taxa = SelectedMeasurements[0].FramesPerSecond;

            freqOriginal = new double[SelectedMeasurements.Length][];
            sinalDetecção = new double[SelectedMeasurements.Length][];

            vetTempo = new DateTime[tlEvento];

            long inc = 0; //incremento a ser adicionado a partir da data inicial, para criar o vetor de tempo

            for (int k = 0; k < SelectedMeasurements.Length; k++)
            {
                freqOriginal[k] = new double[tlEvento];
                sinalDetecção[k] = new double[tlEvento];

                for (int i = 0; i < tlEvento; i++)
                {
                    //passa algumas variáveis para os vetores
                    inc = Convert.ToInt64(Math.Floor((1 / taxa) * 1000 * (i + min)) * 10000);
                    vetTempo[i] = dataIni.AddTicks(inc);

                        // Entra se o terminal de índice 'm' tem medição de tensão de SP
                        if (SelectedMeasurements[k].Series.ContainsKey(Channel.VOLTAGE_POS_MOD))
                        {
                            if (i != 0 && SelectedMeasurements[k].Series[Channel.VOLTAGE_POS_MOD].Reading(i) < vMin * SelectedMeasurements[k].Terminal.VoltageLevel)
                            {
                                freqOriginal[k][i] = freqOriginal[k][i - 1];
                            }
                            else
                            {
                                freqOriginal[k][i] = SelectedMeasurements[k].Series[Channel.FREQ].Reading(i + min);
                            }
                        }
                        else
                        {
                            freqOriginal[k][i] = SelectedMeasurements[k].Series[Channel.FREQ].Reading(i + min);
                        }

                    if (i < ordemF)
                        sinalDetecção[k][i] = freqOriginal[k][i] - Query.System.NominalFrequency; //ignora os primeiros "ordemF" pontos
                    else
                        sinalDetecção[k][i] = (sinalDetecção[k][i - 1] + (freqOriginal[k][i] - freqOriginal[k][i - ordemF]) / ordemF); // filtra o sinal
                }
            }
        }

        //caso seja taxa de variação
        public GraficoEventos(JanelaPrincipal frm, Query query, Measurement[] measurements, int minInd, int maxInd, Color[] coresAtuais, parametrosFMMeTX parametros, string dir, double vMin)
        {

            InitializeComponent();
            dirConsulta = dir;
            toolStripTextBox1.Text = Convert.ToString(0.02); //pra garantir que vai escrever com . ou , dependendo da região
            chart1.ChartAreas[0].AxisY.Title = "Hz";
            chart1.ChartAreas[0].AxisY.LabelStyle.Format = "0.#####";
            chart1.Titles[0].Text = "Frequência Original";

            chart2.ChartAreas[0].AxisY.Title = "Hz/s";
            chart2.ChartAreas[0].AxisY.LabelStyle.Format = "0.#####";
            chart2.Titles[0].Text = "Taxa de Variação";
            //passa algumas variáveis na inicialização
            pai = frm;
            Query = query;
            SelectedMeasurements = measurements;
            min = minInd;
            max = maxInd;
            int ordemF = parametros.ordemDoFiltro;
            int numPontos = parametros.numeroDePontos;
            cores = new Color[coresAtuais.Length];
            cores = coresAtuais;

            int tlEvento = (max - min) + 1; //comprimento da janela (em pontos)

            dataIni = SelectedMeasurements[0].Start;
            taxa = SelectedMeasurements[0].FramesPerSecond;

            freqOriginal = new double[SelectedMeasurements.Length][];
            sinalDetecção = new double[SelectedMeasurements.Length][];
            double[][] freqFiltrada = new double[SelectedMeasurements.Length][];

            vetTempo = new DateTime[tlEvento];

            long inc = 0; //incremento a ser adicionado a partir da data inicial, para criar o vetor de tempo

            for (int k = 0; k < SelectedMeasurements.Length; k++)
            {
                freqOriginal[k] = new double[tlEvento];
                freqFiltrada[k] = new double[tlEvento];
                sinalDetecção[k] = new double[tlEvento];

                for (int i = 0; i < tlEvento; i++)
                {
                    //passa algumas variáveis para os vetores
                    inc = Convert.ToInt64(Math.Floor((1 / taxa) * 1000 * (i + min)) * 10000);
                    vetTempo[i] = dataIni.AddTicks(inc);

                    // Entra se pelo menos um terminal tem medição de tensão de SP
                    if (SelectedMeasurements[k].Series.ContainsKey(Channel.VOLTAGE_POS_MOD))
                    {
                        if (i != 0 && SelectedMeasurements[k].Series[Channel.VOLTAGE_POS_MOD].Reading(i) < vMin * SelectedMeasurements[k].Terminal.VoltageLevel)
                        {
                            freqOriginal[k][i] = freqOriginal[k][i - 1];
                        }
                        else
                        {
                            freqOriginal[k][i] = SelectedMeasurements[k].Series[Channel.FREQ].Reading(i + min);
                        }
                    }
                    else
                    {
                        freqOriginal[k][i] = SelectedMeasurements[k].Series[Channel.FREQ].Reading(i + min);
                    }

                    if (i < ordemF) freqFiltrada[k][i] = freqOriginal[k][i]; //ignora os primeiros "ordemF" pontos
                    else freqFiltrada[k][i] = freqFiltrada[k][i - 1] + (freqOriginal[k][i] - freqOriginal[k][i - ordemF]) / ordemF; // filtra o sinal
                }

                for (int i = ordemF + numPontos; i < tlEvento; i++)
                {
                    sinalDetecção[k][i] = ((freqFiltrada[k][i] - freqFiltrada[k][i - numPontos]) / numPontos) * taxa;
                }
            }

        }

        //caso seja filtro média móvel (passa faixa)
        public GraficoEventos(JanelaPrincipal frm, Query query, Measurement[] measurements, int minInd, int maxInd, Color[] coresAtuais, parametrosPassaFaixa parametros, string dir, double vMin)
        {

            InitializeComponent();
            dirConsulta = dir;
            toolStripTextBox1.Text = Convert.ToString(0.02); //pra garantir que vai escrever com . ou , dependendo da região
            chart1.ChartAreas[0].AxisY.Title = "Hz";
            chart1.ChartAreas[0].AxisY.LabelStyle.Format = "0.#####";
            chart1.Titles[0].Text = "Frequência Original";

            chart2.ChartAreas[0].AxisY.Title = "Hz";
            chart2.ChartAreas[0].AxisY.LabelStyle.Format = "0.#####";
            chart2.Titles[0].Text = "Frequência Filtrada pelo Filtro Passa-Faixa";
            //passa algumas variáveis na inicialização
            pai = frm;
            Query = query;
            SelectedMeasurements = measurements;
            min = minInd;
            max = maxInd;
            int ordemF1 = parametros.ordemDoFiltro1;
            int ordemF2 = parametros.ordemDoFiltro2;
            cores = new Color[coresAtuais.Length];
            cores = coresAtuais;

            int tlEvento = (max - min) + 1; //comprimento da janela (em pontos)

            dataIni = SelectedMeasurements[0].Start;
            taxa = SelectedMeasurements[0].FramesPerSecond;

            freqOriginal = new double[SelectedMeasurements.Length][];
            sinalDetecção = new double[SelectedMeasurements.Length][];
            double[][] freqFiltrada = new double[SelectedMeasurements.Length][];
            double[][] tendencia = new double[SelectedMeasurements.Length][];

            vetTempo = new DateTime[tlEvento];

            long inc = 0; //incremento a ser adicionado a partir da data inicial, para criar o vetor de tempo

            for (int k = 0; k < SelectedMeasurements.Length; k++)
            {
                freqOriginal[k] = new double[tlEvento];
                freqFiltrada[k] = new double[tlEvento];
                tendencia[k] = new double[tlEvento];
                sinalDetecção[k] = new double[tlEvento];

                for (int i = 0; i < tlEvento; i++)
                {
                    //passa algumas variáveis para os vetores
                    inc = Convert.ToInt64(Math.Floor((1 / taxa) * 1000 * (i + min)) * 10000);
                    vetTempo[i] = dataIni.AddTicks(inc);

                    // Entra se pelo menos um terminal tem medição de tensão de SP
                    if (SelectedMeasurements[k].Series.ContainsKey(Channel.VOLTAGE_POS_MOD))
                    {
                        if (i != 0 && SelectedMeasurements[k].Series[Channel.VOLTAGE_POS_MOD].Reading(i) < vMin * SelectedMeasurements[k].Terminal.VoltageLevel)
                        {
                            freqOriginal[k][i] = freqOriginal[k][i - 1];
                        }
                        else
                        {
                            freqOriginal[k][i] = SelectedMeasurements[k].Series[Channel.FREQ].Reading(i + min);
                        }
                    }
                    else
                    {
                        freqOriginal[k][i] = SelectedMeasurements[k].Series[Channel.FREQ].Reading(i + min);
                    }

                    //filtra o ruído
                    if (i < ordemF1) freqFiltrada[k][i] = freqOriginal[k][i]; //ignora os primeiros "ordemF" pontos
                    else freqFiltrada[k][i] = freqFiltrada[k][i - 1] + (freqOriginal[k][i] - freqOriginal[k][i - ordemF1]) / ordemF1; // filtra o sinal

                    //filtra as oscilações
                    if (i < ordemF2) tendencia[k][i] = freqOriginal[k][i]; //ignora os primeiros "ordemF" pontos
                    else tendencia[k][i] = tendencia[k][i - 1] + (freqOriginal[k][i] - freqOriginal[k][i - ordemF2]) / ordemF2; // filtra o sinal

                    //calcula o sinal de detecção = freq filtrada - tendencia
                    sinalDetecção[k][i] = freqFiltrada[k][i] - tendencia[k][i];

                }
            }
        }

        //caso seja filtro de Kalman
        public GraficoEventos(JanelaPrincipal frm, Query query, Measurement[] measurements, int minInd, int maxInd, Color[] coresAtuais, parametrosFiltroDeKalman parametros, string dir, double vMin)
        {
            InitializeComponent();
            dirConsulta = dir;
            toolStripTextBox1.Text = Convert.ToString(0.006); //pra garantir que vai escrever com . ou , dependendo da região
            chart1.ChartAreas[0].AxisY.Title = "Hz";
            chart1.ChartAreas[0].AxisY.LabelStyle.Format = "0.#####";
            chart1.Titles[0].Text = "Frequência Original";

            chart2.ChartAreas[0].AxisY.Title = "Hz/s";
            chart2.ChartAreas[0].AxisY.LabelStyle.Format = "0.#####";
            chart2.Titles[0].Text = "Aceleração Angular";

            //passa algumas variáveis na inicialização
            pai = frm;
            SelectedMeasurements = measurements;
            Query = query;
            cores = new Color[coresAtuais.Length];
            cores = coresAtuais;
            min = minInd;
            max = maxInd;
            int tlEvento = (max - min) + 1;
            dataIni = SelectedMeasurements[0].Start;
            taxa = SelectedMeasurements[0].FramesPerSecond;

            double T = 1 / taxa;
            double tauv = parametros.desvioPadrao;
            double alfa = parametros.CaracteristicaDinamica;
            double amax = parametros.LimiteDeAceleração;

            freqOriginal = new double[SelectedMeasurements.Length][];
            sinalDetecção = new double[SelectedMeasurements.Length][];

            vetTempo = new DateTime[tlEvento];

            //matriz de transição de estados
            Matrix F = new Matrix(3, 3);
            F[0, 0] = 1; F[0, 1] = T; F[0, 2] = (-1 + (alfa * T) + (Math.Exp(-alfa * T))) / (Math.Pow(alfa, 2));
            F[1, 0] = 0; F[1, 1] = 1; F[1, 2] = (1 - Math.Exp(-alfa * T)) / alfa;
            F[2, 0] = 0; F[2, 1] = 0; F[2, 2] = Math.Exp(-alfa * T);

            // matriz de entrada 
            Vector U = new Vector(VectorType.Column, 3);
            U[0] = (-T + (alfa * (Math.Pow(T, 2))) / 2 + (1 - Math.Exp(-alfa * T)) / alfa) / alfa;
            U[1] = T - (1 - Math.Exp(-alfa * T)) / alfa;
            U[2] = 1 - Math.Exp(-alfa * T);

            //matriz de medição
            Vector H = new Vector(VectorType.Row, 3);
            H[0] = 0; H[1] = 1; H[2] = 0;

            //covariância do ruído
            double q11 = (1 / (2 * Math.Pow(alfa, 5))) * (1 - Math.Exp(-2 * alfa * T) + 2 * alfa * T + ((2 * Math.Pow(alfa, 3) * Math.Pow(T, 3)) / 3) - 2 * Math.Pow(alfa, 2) * Math.Pow(T, 2) - 4 * alfa * T * Math.Exp(-alfa * T));
            double q12 = (1 / (2 * Math.Pow(alfa, 4))) * (Math.Exp(-2 * alfa * T) + 1 - 2 * Math.Exp(-alfa * T) + 2 * alfa * T * Math.Exp(-alfa * T) - 2 * alfa * T + Math.Pow(alfa, 2) * Math.Pow(T, 2));
            double q13 = (1 / (2 * Math.Pow(alfa, 3))) * (1 - Math.Exp(-2 * alfa * T) - 2 * alfa * T * Math.Exp(-alfa * T));
            double q22 = (1 / (2 * Math.Pow(alfa, 3))) * (4 * Math.Exp(-alfa * T) - 3 - Math.Exp(-2 * alfa * T) + 2 * alfa * T);
            double q23 = (1 / (2 * Math.Pow(alfa, 2))) * (Math.Exp(-2 * alfa * T) + 1 - 2 * Math.Exp(-alfa * T));
            double q33 = (1 / (2 * alfa)) * (1 - Math.Exp(-2 * alfa * T));

            Matrix Q = new Matrix(3, 3);
            Q[0, 0] = q11; Q[0, 1] = q12; Q[0, 2] = q13;
            Q[1, 0] = q12; Q[1, 1] = q22; Q[1, 2] = q23;
            Q[2, 0] = q13; Q[2, 1] = q23; Q[2, 2] = q33;

            //matriz para o cálculo da covariância
            Matrix P1 = new Matrix(3, 3);
            P1[0, 0] = 1; P1[0, 1] = T; P1[0, 2] = Math.Pow(T, 2) / 2;
            P1[1, 0] = 0; P1[1, 1] = 1; P1[1, 2] = T;
            P1[2, 0] = 0; P1[2, 1] = 0; P1[2, 2] = 1;

            long inc = 0; //incremento a ser adicionado a partir da data inicial, para criar o vetor de tempo

            for (int i = 0; i < SelectedMeasurements.Length; i++)
            {
                freqOriginal[i] = new double[tlEvento];
                sinalDetecção[i] = new double[tlEvento];
                double R = Math.Pow(tauv, 2);
                double a = 0;
                Vector xe = new Vector(VectorType.Column, 3);
                Vector x = new Vector(VectorType.Column, 3);
                Matrix P = new Matrix(3, 3);

                xe[0] = 0; xe[1] = SelectedMeasurements[i].Series[Channel.FREQ].Reading(min); xe[2] = 0;

                for (int ii = 0; ii < tlEvento; ii++)
                {
                    // Entra se pelo menos um terminal tem medição de tensão de SP
                    if (SelectedMeasurements[i].Series.ContainsKey(Channel.VOLTAGE_POS_MOD))
                    {
                        if (ii != 0 && SelectedMeasurements[i].Series[Channel.VOLTAGE_POS_MOD].Reading(ii) < vMin * SelectedMeasurements[i].Terminal.VoltageLevel)
                        {
                            freqOriginal[i][ii] = freqOriginal[i][ii - 1];
                        }
                        else
                        {
                            freqOriginal[i][ii] = SelectedMeasurements[i].Series[Channel.FREQ].Reading(ii + min);
                        }
                    }
                    else
                    {
                        freqOriginal[i][ii] = SelectedMeasurements[i].Series[Channel.FREQ].Reading(ii + min);
                    }

                    inc = Convert.ToInt64(Math.Floor((1 / taxa) * 1000 * (ii + min)) * 10000);
                    vetTempo[ii] = dataIni.AddTicks(inc);
                    //densidade de probabilidade
                    double Ta = ((4 - Math.PI) / Math.PI) * Math.Pow(amax - Math.Abs(x[2]), 2);

                    //atualização de tempo
                    // 1 - projeção do estado a priori
                    xe = (F * xe + U * a).GetColumnVector(0);

                    // 2 - projeção da covariância do erro a priori
                    P = F * P * F.Transpose() + (H * (Q * 2 * alfa * Ta) * H.Transpose()).ToScalar();

                    //atualização da medida
                    // 1 - ganho de kalman                     
                    Vector K = (P * H.Transpose() / (H * P * H.Transpose() + R).ToScalar()).GetColumnVector(0);

                    // 2 - atualiza a estimativa com a medida                    
                    Vector temp = new Vector(VectorType.Column, 3);
                    temp[0] = 0;
                    temp[1] = freqOriginal[i][ii];
                    temp[2] = 0;
                    double y = (H * temp).ToScalar(); //sinal medido + ruído
                    xe = (xe + K * (y - H * xe)).GetColumnVector(0);

                    sinalDetecção[i][ii] = xe[2];

                    // 3 - atualização da covariância do erro

                    P = (Matrix.Identity(H.Length, H.Length) - K * H) * P;

                    //entrada para a próxima iteração
                    x = (P1 * xe).GetColumnVector(0);
                    a = x[2];

                }
            }
        }

        private void Form19_Shown(object sender, EventArgs e)
        {
            chart1.Series.Clear();//limpa os gráficos
            chart2.Series.Clear();
            int tlEvento = (max - min) + 1; //comprimento da janela (em pontos)

            //ajusta os gráficos
            if (taxa == 1)
            {
                chart1.ChartAreas[0].AxisX.LabelStyle.Format = "HH:mm:ss"; // HH maiúculo para que a hora fique de 0 a 24h
                chart2.ChartAreas[0].AxisX.LabelStyle.Format = "HH:mm:ss";
            }
            else if (taxa == 10)
            {
                chart1.ChartAreas[0].AxisX.LabelStyle.Format = "HH:mm:ss.f";
                chart2.ChartAreas[0].AxisX.LabelStyle.Format = "HH:mm:ss.f";
            }
            else
            {
                chart1.ChartAreas[0].AxisX.LabelStyle.Format = "HH:mm:ss.fff";
                chart2.ChartAreas[0].AxisX.LabelStyle.Format = "HH:mm:ss.fff";
            }

            foreach(Measurement measurement in SelectedMeasurements)
            {
                Series series1 = new Series(measurement.Terminal.DisplayName)
                {
                    Color = cores[chart1.Series.Count],
                    ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastLine
                };
                Series series2 = new Series(measurement.Terminal.DisplayName)
                {
                    Color = cores[chart2.Series.Count],
                    ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastLine
                };

                for (int i = 0; i < tlEvento; i++)
                {
                    series1.Points.AddXY(vetTempo[i], freqOriginal[chart1.Series.Count][i]);
                    series2.Points.AddXY(vetTempo[i], sinalDetecção[chart2.Series.Count][i]);
                }

                chart1.Series.Add(series1);
                chart2.Series.Add(series2);
            }
        }

        private void chart1_DoubleClick(object sender, EventArgs e)
        {
            //retira o zoom
            ((Chart)sender).ChartAreas[0].AxisX.ScaleView.ZoomReset();
            ((Chart)sender).ChartAreas[0].AxisY.ScaleView.ZoomReset();
            //e faz a mesma coisa pro outro chart
            chart1_AxisViewChanged(sender, null);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //detecta os eventos
            button2.Enabled = false;
            ListaDeLimites limitesdeevento;
            limitesdeevento.MaxMagnitude = Convert.ToDouble(toolStripTextBox1.Text);
            limitesdeevento.MaxTempoSalto = new TimeSpan(0, 0, 0, 0, Convert.ToInt32(toolStripTextBox2.Text));
            limitesdeevento.TempoAbaixoFimdoEvento = new TimeSpan(0, 0, 0, 0, Convert.ToInt32(toolStripTextBox3.Text));
            limitesdeevento.MaxDiferencaMesmoEventodeSalto = new TimeSpan(0, 0, 0, 0, Convert.ToInt32(toolStripTextBox4.Text));
            limitesdeevento.MaxDiferencaMesmoEventodeDesbalanço = new TimeSpan(0, 0, 0, 0, Convert.ToInt32(toolStripTextBox5.Text));
            EventosObj = new Eventos(SelectedMeasurements.Length, (max - min) + 1, sinalDetecção, vetTempo, limitesdeevento);

            if (EventosObj.Items.Count > 0)
            {
                //adiciona os eventos detectados ao treeview
                treeView1.Nodes.Clear();
                for (int i = 0; i < EventosObj.Items.Count; i++)
                {
                    treeView1.Nodes.Add("Evento " + Convert.ToString(i + 1));
                    treeView1.Nodes[i].Nodes.Add("Tipo: " + EventosObj.Items[i].Tipo);
                    treeView1.Nodes[i].Nodes.Add("Instante de detecção: " + EventosObj.Items[i].InstanteDeDetecção.TimeOfDay);
                    treeView1.Nodes[i].Nodes.Add("Nº de terminais sensibilizados: " + Convert.ToString(EventosObj.Items[i].TerminaisAfetados.Count));
                    treeView1.Nodes[i].Nodes.Add("Sequência temporal: (");                                        

                    for (int ii = 0; ii < EventosObj.Items[i].TerminaisAfetados.Count; ii++)
                    {
                        if (ii < 3)
                            treeView1.Nodes[i].Nodes[3].Text = treeView1.Nodes[i].Nodes[3].Text + SelectedMeasurements[EventosObj.Items[i].TerminaisAfetados[ii].Index].Terminal.DisplayName + ", ";
                        treeView1.Nodes[i].Nodes[3].Nodes.Add(SelectedMeasurements[EventosObj.Items[i].TerminaisAfetados[ii].Index].Terminal.DisplayName);
                        treeView1.Nodes[i].Nodes[3].Nodes[ii].Nodes.Add("Instante de detecção: " + EventosObj.Items[i].TerminaisAfetados[ii].Início.TimeOfDay);
                        treeView1.Nodes[i].Nodes[3].Nodes[ii].Nodes.Add("Tempo de atraso: " + EventosObj.Items[i].TerminaisAfetados[ii].TempoDeAtraso.TotalSeconds);
                        treeView1.Nodes[i].Nodes[3].Nodes[ii].Nodes.Add("Duração: " + EventosObj.Items[i].TerminaisAfetados[ii].Duração.ToString());
                    }
                    treeView1.Nodes[i].Nodes[3].Text = treeView1.Nodes[i].Nodes[3].Text.Remove(treeView1.Nodes[i].Nodes[3].Text.Length - 2);
                    treeView1.Nodes[i].Nodes[3].Text = treeView1.Nodes[i].Nodes[3].Text + ")";
                }
                if (EventosObj.Items.Count > 0) treeView1.SelectedNode = treeView1.Nodes[0];
                treeView1.Focus();
            }
            else
            {
                treeView1.Nodes.Clear();
                MessageBox.Show("Nenhum evento detectado.", "Atenção!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            //mostra as "bolinhas no gráfico" do evento selecionado no treeview
            if (treeView1.SelectedNode.Level == 0) //se for um nó de primeiro nível (evento)
            {
                //habilita o botão de localização, caso seja um evento de desbalanço de carga/geração
                if (EventosObj.Items[e.Node.Index].Tipo == EventosDetectados.TipoDeEvento.Desvio_de_Frequência || EventosObj.Items[e.Node.Index].Tipo == EventosDetectados.TipoDeEvento.Desvio_de_Frequência) button2.Enabled = true;
                else button2.Enabled = false;
                //remove as bolinhas que já estão no gráfico
                int seriesCount = chart2.Series.Count;
                for (int i = 0; i < (seriesCount - SelectedMeasurements.Length); i++)
                {
                    chart2.Series.RemoveAt(chart2.Series.Count - 1);
                }


                for (int i = 0; i < EventosObj.Items[treeView1.SelectedNode.Index].TerminaisAfetados.Count; i++) //procura a magnitude do sinal no instante de tempo de detecção
                {
                    double mag = 0;
                    for (int ii = 0; ii < vetTempo.Count(); ii++)
                    {
                        if (EventosObj.Items[treeView1.SelectedNode.Index].TerminaisAfetados[i].Início == vetTempo[ii])
                        {
                            mag = sinalDetecção[EventosObj.Items[treeView1.SelectedNode.Index].TerminaisAfetados[i].Index][ii];
                            break;
                        }
                    }
                    //plota as bolinhas do evento no chart2   
                    //chart2.Series.Add(Objetos.ObjMat[indCons].matNomes[indSelec[EventosObj.Items[treeView1.SelectedNode.Index].TerminaisAfetados[i].Índice]] + " ");
                    chart2.Series.Add(chart2.Series[EventosObj.Items[treeView1.SelectedNode.Index].TerminaisAfetados[i].Index].Name + " ");
                    chart2.Series[chart2.Series.Count - 1].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Point;
                    chart2.Series[chart2.Series.Count - 1].Color = chart2.Series[EventosObj.Items[treeView1.SelectedNode.Index].TerminaisAfetados[i].Index].Color;
                    chart2.Series[chart2.Series.Count - 1].MarkerStyle = System.Windows.Forms.DataVisualization.Charting.MarkerStyle.Circle;
                    chart2.Series[chart2.Series.Count - 1].MarkerSize = 7;
                    chart2.Series[chart2.Series.Count - 1].BorderColor = Color.Black;
                    chart2.Series[chart2.Series.Count - 1].IsVisibleInLegend = false;
                    chart2.Series[chart2.Series.Count - 1].Points.AddXY(EventosObj.Items[treeView1.SelectedNode.Index].TerminaisAfetados[i].Início, mag);
                }
            }
            else
            {
                button2.Enabled = false;
            }
        }

        private void toolStripTextBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            //só permite que sejam inseridos números, backspace e separador decimal (. ou , dependendo da região)
            char separadorDecimal = Convert.ToChar(System.Globalization.CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator);
            if (!Char.IsDigit(e.KeyChar) && e.KeyChar != '\b' && (e.KeyChar != separadorDecimal || toolStripTextBox1.Text.Contains(separadorDecimal)))
            {
                e.Handled = true;
            }
        }

        private void toolStripTextBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            //só permite números e backspace
            if (!Char.IsDigit(e.KeyChar) && e.KeyChar != '\b')
            {
                e.Handled = true;
            }
        }

        private void chart1_AxisViewChanged(object sender, System.Windows.Forms.DataVisualization.Charting.ViewEventArgs e)
        {
            //faz o zoom no eixo X igual para os dois gráficos
            foreach (Control controle in this.Controls)
            {
                if (controle is Chart && controle.Name != sender.ToString())
                {
                    ((Chart)controle).ChartAreas[0].AxisX.ScaleView.Zoom(((Chart)sender).ChartAreas[0].AxisX.ScaleView.ViewMinimum, ((Chart)sender).ChartAreas[0].AxisX.ScaleView.ViewMaximum);
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string[] xml = Directory.GetFiles(dirConsulta, "*.XML");
            if (xml.Count() != 0)
            {
                pai.CriaLocalização(dirConsulta, Query, SelectedMeasurements, EventosObj.Items[treeView1.SelectedNode.Index]);
            }
            else
            {
                MessageBox.Show("Arquivo de configuração não disponível para esta consulta.", "Erro!", MessageBoxButtons.OK);
            }
        }

        private void chart1_KeyDown(object sender, KeyEventArgs e)
        {
            // Código para copiar o gráfico para a área de trabalho

            // Verifica se o usuário pressionou as teclas "Ctrl + C" com o form em questão ativo 
            if (e.Control && e.KeyCode == Keys.C)
            {
                // Realça o contorno do gráfico
                ((System.Windows.Forms.DataVisualization.Charting.Chart)sender).BorderlineDashStyle = ChartDashStyle.Dash;

                System.IO.MemoryStream ms = new System.IO.MemoryStream();

                ((System.Windows.Forms.DataVisualization.Charting.Chart)sender).SaveImage(ms, ChartImageFormat.Bmp);
                Bitmap bm = new Bitmap(ms);
                bm.SetResolution(4000, 4000);
                // Disponibiliza o bitmap na área de trabalho
                Clipboard.SetImage(bm);
            }
        }

        private void chart1_MouseDown(object sender, MouseEventArgs e)
        {
            //coloca o foco no chart, para que funcione o ctrl+C
            ((System.Windows.Forms.DataVisualization.Charting.Chart)sender).Focus();

            tip.IsBalloon = true;
            
            if (e.Button == MouseButtons.Right)
            {
                // Call Hit Test Method
                HitTestResult result = ((System.Windows.Forms.DataVisualization.Charting.Chart)sender).HitTest(e.X, e.Y);

                if (result.ChartElementType == ChartElementType.DataPoint)
                {
                    
                    double valorOA = ((System.Windows.Forms.DataVisualization.Charting.Chart)sender).ChartAreas[0].AxisX.PixelPositionToValue(e.Location.X);
                    DateTime data = DateTime.FromOADate(valorOA);
                  
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


                    tip.ToolTipTitle = result.Series.Name;
                    tip.Show("X = " + horario + "\nY = " + Math.Round(((System.Windows.Forms.DataVisualization.Charting.Chart)sender).ChartAreas[0].AxisY.PixelPositionToValue(e.Location.Y), 5), ((System.Windows.Forms.DataVisualization.Charting.Chart)sender), e.X, e.Y);
                    tip.Show("X = " + horario + "\nY = " + Math.Round(((System.Windows.Forms.DataVisualization.Charting.Chart)sender).ChartAreas[0].AxisY.PixelPositionToValue(e.Location.Y), 5), ((System.Windows.Forms.DataVisualization.Charting.Chart)sender), e.X, e.Y, 3000);
                }
                else if (result.ChartElementType == ChartElementType.LegendItem)
                {
                    tip.Hide(((System.Windows.Forms.DataVisualization.Charting.Chart)sender));

                    LegendItem leg = (LegendItem)result.Object;
                    string nomeLeg = leg.SeriesName;

                    // Se a cor foi escolhida
                    if (colorDialog1.ShowDialog() == DialogResult.OK)
                    {
                        for (int k = 0; k < ((System.Windows.Forms.DataVisualization.Charting.Chart)sender).Series.Count; k++)
                        {
                            string nomeSerie = ((System.Windows.Forms.DataVisualization.Charting.Chart)sender).Series[k].Name;
                            if (nomeLeg == nomeSerie)
                            {
                                ((System.Windows.Forms.DataVisualization.Charting.Chart)sender).Series[k].Color = colorDialog1.Color;
                            }
                        }
                    }
                }
                else
                {
                    tip.Hide(chart1);
                }
            }
            else
            {
                tip.Hide(chart1);
            }
        }

        private void chart1_MouseLeave(object sender, EventArgs e)
        {
            tip.Hide(this);
        }

        private void treeView1_AfterExpand(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Level == 1)
            {
                e.Node.Text = "Sequência temporal:";
            }
        }

        private void treeView1_AfterCollapse(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Level == 1)
            {
                e.Node.Text = "Sequência temporal: (";
                for (int i = 0; i < Math.Min(e.Node.Nodes.Count,3); i++)
                {
                    e.Node.Text = e.Node.Text + e.Node.Nodes[i].Text + ", ";
                }
                e.Node.Text = e.Node.Text.Remove(e.Node.Text.Length - 2);
                e.Node.Text = e.Node.Text + ")";
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {

        }
    }

    public struct parametrosFreqFiltrada
    {
        public int ordemDoFiltro;
        public parametrosFreqFiltrada(int x)
        {
            ordemDoFiltro = x;
        }
    }

    public struct parametrosFMMeTX
    {
        public int ordemDoFiltro;
        public int numeroDePontos;
        public parametrosFMMeTX(int x, int y)
        {
            ordemDoFiltro = x;
            numeroDePontos = y;
        }
    }

    public struct parametrosPassaFaixa
    {
        public int ordemDoFiltro1;
        public int ordemDoFiltro2;
        public parametrosPassaFaixa(int x, int y)
        {
            ordemDoFiltro1 = x;
            ordemDoFiltro2 = y;
        }
    }

    public struct parametrosFiltroDeKalman
    {
        public parametrosFiltroDeKalman(double x, double y, double z)
        {
            desvioPadrao = x;
            CaracteristicaDinamica = y;
            LimiteDeAceleração = z;
        }
        public double desvioPadrao;
        public double CaracteristicaDinamica;
        public double LimiteDeAceleração;
    }
}
