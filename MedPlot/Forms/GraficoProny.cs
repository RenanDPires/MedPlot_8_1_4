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
using DotNumerics;
using DotNumerics.LinearAlgebra;
using DotNumerics.LinearAlgebra.CSLapack;
using DotNumerics.FortranLibrary;
using MedFasee.Structure;
using MedFasee.Equipment;

namespace MedPlot
{
    public partial class GraficoProny : Form
    {
        private JanelaPrincipal pai;
        private int opGraf;  // opção do gráfico original que terá seu espectro traçado
        private double taxa; // taxa (fasores/s) com a qual a consulta foi realizada
        private int min, max; // índices de início e término da visualização atual do gráfico base
        private int pu; // contém a indicação de gráfico em PU

        int ordem = 0;            

        private int ordemDef; // definida pelo usuário

        // Data inicial da consulta
        private DateTime dataIni;

        // Data escolhida como inicial para calcular o Prony
        private DateTime dataEsc;

        // Largura do gráfico no tamanho padrão
        private int largIni;
        // Altura inicial
        private int altIni;

        // Dados do Prony, são declarados aqui porque são utilizados depois nas mode shapes
        Vector freq;
        Matrix amps, ang;

        // Vetor que armazena os modos de oscilação para as mode shapes
        double[] modos;

        ToolTip tip = new ToolTip();

        Color[] cores;

        // Tempo T1 anterior
        DateTime previousT1;

        // Flag para marcação ou não da opção "automático" do eixo Y no form de configurações
        public bool yAuto = true;
        public bool xAuto = true;

        public Query Query { get; internal set; }
        Measurement[] SelectedMeasurements { get; }
        Measurement Reference { get; }

        public GraficoProny(JanelaPrincipal frm1, Query query, int op, Measurement reference, Measurement[] measurements, int minInd, int maxInd, int opPu, int ordemEsc, Color[] coresAtuais)
        {
            InitializeComponent();

            // Repassando para as variáveis internas
            pai = frm1;
            Query = query;
            opGraf = op;
            SelectedMeasurements = measurements;
            Reference = reference;

            min = minInd;
            max = maxInd;

            pu = opPu;

            ordemDef = ordemEsc;

            cores = new Color[coresAtuais.Length];
            cores = coresAtuais;

        }

        private void Form8_Shown(object sender, EventArgs e)
        {
            // Habilita as funções de zoom e pan no gráfico
            //chart1.EnableZoomAndPanControls(ChartCursorSelected, ChartCursorMoved);

            // Taxa com a qual a consulta foi realizada
            taxa = SelectedMeasurements[0].FramesPerSecond;

            // Total de linhas para o cálculo do Prony                      
            int tlProny = (max - min) + 1;

            // limpa as séries do gráfico
            chart1.Series.Clear();

            dataIni = SelectedMeasurements[0].Start;

            DateTime data = new DateTime();

            // Define o título do eixo dos tempos
            chart1.ChartAreas[0].AxisX.Title = "Tempo (UTC) - Dia: " + dataIni.Day + "/" + dataIni.Month
                + "/" + dataIni.Year;

            chart1.ChartAreas[0].AxisX.IsMarginVisible = false;

            // Formato das labels no eixo X
            if (taxa == 1)
            {
                chart1.ChartAreas[0].AxisX.LabelStyle.Format = "HH:mm:ss"; // HH maiúculo para que a hora fique de 0 a 24h
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

            chart1.ChartAreas[0].AxisY.LabelStyle.Format = "0.#####";

            // largura do gráfico no tamanho default
            largIni = chart1.Width;
            // altura no tamanho default
            altIni = chart1.Height;

            // Variável de incremento para as datas do eixo X
            long inc = 0;

            // Inicialização da data para o eixo X
            inc = Convert.ToInt64(Math.Floor((1 / Convert.ToDouble(taxa)) * 1000 * (min)) * 10000);
            data = dataIni.AddTicks(inc);
            // data escolhida pelo usuário para o começo do cálculo do Prony            
            dataEsc = data;

            // Inicializa o incremento
            inc = 0;

            #region Entrada de dados

            // Matriz de entrada para o Prony, contém os valores dos sinais que serão estudados
            Matrix sinais = null;

            // Gráfico do módulo da tensão - FASE ÚNICA
            if ((opGraf == 1) || (opGraf == 2) || (opGraf == 3))
            {
                // Define qual das séries foi selecionada
                Channel phase = null;
                switch (opGraf)
                {
                    case 1:
                        phase = Channel.VOLTAGE_A_MOD;
                        break;
                    case 2:
                        phase = Channel.VOLTAGE_B_MOD;
                        break;
                    case 3:
                        phase = Channel.VOLTAGE_C_MOD;
                        break;
                    default:
                        break;
                }

                // Título do gráfico
                if (taxa == 1)
                {
                    chart1.Titles[0].Text = "Módulo da Tensão - Fase " + phase.Phase + " - "
                        + Convert.ToString(taxa) + " fasor/s";
                }
                else
                {
                    chart1.Titles[0].Text = "Módulo da Tensão - Fase " + phase.Phase + " - "
                        + Convert.ToString(taxa) + " fasores/s";
                }

                // Título do eixo Y
                if (pu == 1)
                {
                    // Define o título do eixo Y
                    chart1.ChartAreas[0].AxisY.Title = "Tensão (pu)";
                }
                else if (pu == 0)
                {
                    // Define o título do eixo Y
                    chart1.ChartAreas[0].AxisY.Title = "Tensão (V)";
                }

                // Inicialização do vetor que recebe o sinal de entrada para o Prony
                sinais = new Matrix(tlProny, SelectedMeasurements.Length);

                for (int k = 0; k < SelectedMeasurements.Length; k++)
                {
                    // Inicializa o incremento
                    inc = 0;
                    Series series = new Series(SelectedMeasurements[k].Terminal.DisplayName);
                    series.Color = cores[k];
                    series.IsXValueIndexed = true;
                    series.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastLine;

                    double vb = 1; // a base para gráficos em pu 

                    if (pu == 1)
                        vb = SelectedMeasurements[k].Terminal.VoltageLevel / Math.Sqrt(3);

                    // Inicialização da data para o eixo X
                    inc = Convert.ToInt64(Math.Floor((1 / Convert.ToDouble(taxa)) * 1000 * (min)) * 10000);
                    data = dataIni.AddTicks(inc);

                    for (int i = 0; i < tlProny; i++)
                    {
                        // Traça a série
                        series.Points.AddXY(data, SelectedMeasurements[k].Series[phase].Reading(i+min) / vb);
                        sinais[i, k] = SelectedMeasurements[k].Series[phase].Reading(i + min) / vb; 

                        // Incremento em 'ticks' no valor da data, de acordo com a taxa da consulta
                        // 10 mil ticks = 1 ms
                        inc = Convert.ToInt64(Math.Floor((1 / taxa) * 1000 * (i + 1 + min)) * 10000);
                        data = dataIni.AddTicks(inc);
                    }
                    chart1.Series.Add(series);
                }
            }

            // Gráfico do módulo da corrente - FASE ÚNICA
            if ((opGraf == 12) || (opGraf == 13) || (opGraf == 14))
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

                // Título do gráfico
                if (taxa == 1)
                {
                    chart1.Titles[0].Text = "Módulo da Corrente - Fase " + phase.Phase + " - "
                        + Convert.ToString(taxa) + " fasor/s";
                }
                else
                {
                    chart1.Titles[0].Text = "Módulo da Corrente - Fase " + phase.Phase + " - "
                        + Convert.ToString(taxa) + " fasores/s";
                }

                // Título do eixo Y
                if (pu == 1)
                {
                    // Define o título do eixo Y
                    chart1.ChartAreas[0].AxisY.Title = "Corrente (pu)";
                }
                else if (pu == 0)
                {
                    // Define o título do eixo Y
                    chart1.ChartAreas[0].AxisY.Title = "Corrente (A)";
                }

                // Inicialização do vetor que recebe o sinal de entrada para o Prony
                sinais = new Matrix(tlProny, SelectedMeasurements.Length);

                for (int k = 0; k < SelectedMeasurements.Length; k++)
                {
                    // Inicializa o incremento
                    inc = 0;

                    Series series = new Series(SelectedMeasurements[k].Terminal.DisplayName);
                    series.Color = cores[k];
                    series.IsXValueIndexed = true;
                    series.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastLine;

                    double ib = 1; // a base para gráficos em pu 

                    // Inicialização da data para o eixo X
                    inc = Convert.ToInt64(Math.Floor((1 / Convert.ToDouble(taxa)) * 1000 * (min)) * 10000);
                    data = dataIni.AddTicks(inc);

                    for (int i = 0; i < tlProny; i++)
                    {
                        // Traça a série
                        series.Points.AddXY(data, SelectedMeasurements[k].Series[phase].Reading(i + min) / ib);
                        sinais[i, k] = SelectedMeasurements[k].Series[phase].Reading(i + min) / ib;

                        // Incremento em 'ticks' no valor da data, de acordo com a taxa da consulta
                        // 10 mil ticks = 1 ms
                        inc = Convert.ToInt64(Math.Floor((1 / taxa) * 1000 * (i + 1 + min)) * 10000);
                        data = dataIni.AddTicks(inc);
                    }
                    chart1.Series.Add(series);
                }
            }
            // Gráfico do módulo da tensão - Trifásico
            if (opGraf == 4)
            {
                Channel[] fases;
                fases = new Channel[3];

                fases[0] = Channel.VOLTAGE_A_MOD;
                fases[1] = Channel.VOLTAGE_B_MOD;
                fases[2] = Channel.VOLTAGE_C_MOD;

                double vb; // a base para gráficos em pu 

                if (pu == 1)
                {
                    // Define o título do eixo Y
                    chart1.ChartAreas[0].AxisY.Title = "Tensão (pu)";
                }
                else if (pu == 0)
                {
                    // Define o título do eixo Y
                    chart1.ChartAreas[0].AxisY.Title = "Tensão (V)";
                }

                // Título do gráfico
                if (taxa == 1)
                {
                    chart1.Titles[0].Text = "Módulo da Tensão - " + SelectedMeasurements[0].Terminal.DisplayName + " - "
                        + Convert.ToString(taxa) + " fasor/s";
                }
                else
                {
                    chart1.Titles[0].Text = "Módulo da Tensão - " + SelectedMeasurements[0].Terminal.DisplayName + " - "
                        + Convert.ToString(taxa) + " fasores/s";
                }

                // Inicialização do vetor que recebe o sinal de entrada para o Prony
                sinais = new Matrix(tlProny, 3);

                for (int k = 0; k < 3; k++)
                {
                    // Inicializa o incremento
                    inc = 0;

                    // Adiciona quantas séries forem necessárias. O seu nome corresponde ao terminal.
                    chart1.Series.Add("Fase " + fases[k].Phase.ToString());
                    chart1.Series[k].Color = cores[k];
                    // Necessário para conhecer o índice de cada dado no eixo X, serve para moldar a janela
                    // aparente, utilizada para DFT e Prony
                    chart1.Series[k].IsXValueIndexed = true;
                    // Define o tipo da série recém-criada, aqui usa-se o tipo 'FastLine'
                    chart1.Series[k].ChartType = SeriesChartType.FastLine;

                    if (OpcaoGrafico.Pu == 1)
                    {
                        vb = (SelectedMeasurements[0].Terminal.VoltageLevel / Math.Sqrt(3));
                    }
                    else
                    {
                        vb = 1;
                    }

                    // Inicialização da data para o eixo X
                    inc = Convert.ToInt64(Math.Floor((1 / Convert.ToDouble(taxa)) * 1000 * (min)) * 10000);
                    data = dataIni.AddTicks(inc);

                    for (int i = 0; i < tlProny; i++)
                    {
                        // Traça a série
                        chart1.Series[k].Points.AddXY(data, SelectedMeasurements[0].Series[fases[k]].Reading(i+min) / vb);

                        // Incremento em 'ticks' no valor da data, de acordo com a taxa da consulta
                        // 10 mil ticks = 1 ms
                        inc = Convert.ToInt64(Math.Floor((1 / taxa) * 1000 * (i + 1 + min)) * 10000);
                        data = dataIni.AddTicks(inc);

                        sinais[i, k] = SelectedMeasurements[0].Series[fases[k]].Reading(i+min) / vb;
                    }

                }
            }
            // Gráfico do módulo da corrente - Trifásico
            if (opGraf == 15)
            {
                double ib=1; // a base para gráficos em pu 

                // Desabilita o Prony
                //toolStripSplitButton1.Enabled = false;

                Channel[] fases = new Channel[3];

                fases[0] = Channel.CURRENT_A_MOD;
                fases[1] = Channel.CURRENT_B_MOD;
                fases[2] = Channel.CURRENT_C_MOD;

                if (pu == 1)
                {
                    // Define o título do eixo Y
                    chart1.ChartAreas[0].AxisY.Title = "Corrente (pu)";
                }
                else if (pu == 0)
                {
                    // Define o título do eixo Y
                    chart1.ChartAreas[0].AxisY.Title = "Corrente (A)";
                }


                // Título do gráfico
                if (taxa == 1)
                {
                    chart1.Titles[0].Text = "Módulo da Corrente - " + SelectedMeasurements[0].Terminal.DisplayName + " - "
                        + Convert.ToString(taxa) + " fasor/s";
                }
                else
                {
                    chart1.Titles[0].Text = "Módulo da Corrente - " + SelectedMeasurements[0].Terminal.DisplayName + " - "
                        + Convert.ToString(taxa) + " fasores/s";
                }

                // Inicialização do vetor que recebe o sinal de entrada para o Prony
                sinais = new Matrix(tlProny, 3);

                for (int k = 0; k < 3; k++)
                {
                    // Inicializa o incremento
                    inc = 0;

                    // Adiciona quantas séries forem necessárias. O seu nome corresponde ao terminal.
                    chart1.Series.Add("Fase " + fases[k].Phase.ToString());
                    chart1.Series[k].Color = cores[k];
                    // Necessário para conhecer o índice de cada dado no eixo X, serve para moldar a janela
                    // aparente, utilizada para DFT e Prony
                    chart1.Series[k].IsXValueIndexed = true;
                    // Define o tipo da série recém-criada, aqui usa-se o tipo 'FastLine'
                    chart1.Series[k].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastLine;

                    // Inicialização da data para o eixo X
                    inc = Convert.ToInt64(Math.Floor((1 / Convert.ToDouble(taxa)) * 1000 * (min)) * 10000);
                    data = dataIni.AddTicks(inc);

                    for (int i = 0; i < tlProny; i++)
                    {
                        // Traça a série
                        chart1.Series[k].Points.AddXY(data, SelectedMeasurements[0].Series[fases[k]].Reading(i+min) / ib);
                        sinais[i, k] = SelectedMeasurements[0].Series[fases[k]].Reading(i + min) / ib;

                        // Incremento em 'ticks' no valor da data, de acordo com a taxa da consulta
                        // 10 mil ticks = 1 ms
                        inc = Convert.ToInt64(Math.Floor((1 / taxa) * 1000 * (i + 1 + min)) * 10000);
                        data = dataIni.AddTicks(inc);
                    }

                }
            }
            // Gráfico do módulo da tensão - Seq. Positiva
            if (opGraf == 5)
            {
                double vb=1; // a base para gráficos em pu 

                if (pu == 1)
                {
                    // Define o título do eixo Y
                    chart1.ChartAreas[0].AxisY.Title = "Tensão (pu)";
                }
                else if (pu == 0)
                {
                    // Define o título do eixo Y
                    chart1.ChartAreas[0].AxisY.Title = "Tensão (V)";
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

                // Inicialização do vetor que recebe o sinal de entrada para o Prony
                sinais = new Matrix(tlProny, SelectedMeasurements.Length);

                for (int k = 0; k < SelectedMeasurements.Length; k++)
                {
                    // Inicializa o incremento
                    inc = 0;

                    // Adiciona quantas séries forem necessárias. O seu nome corresponde ao terminal.
                    Series series = new Series(SelectedMeasurements[k].Terminal.DisplayName);
                    // Define a cor da série
                    series.Color = cores[k];
                    series.IsXValueIndexed = true;
                    // Define o tipo da série recém-criada, aqui usa-se o tipo 'FastLine'
                    series.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastLine;

                    if (pu == 1)
                        vb = SelectedMeasurements[k].Terminal.VoltageLevel / Math.Sqrt(3);

                    // Inicialização da data para o eixo X
                    inc = Convert.ToInt64(Math.Floor((1 / Convert.ToDouble(taxa)) * 1000 * (min)) * 10000);
                    data = dataIni.AddTicks(inc);

                    for (int i = 0; i < tlProny; i++)
                    {
                        // Traça a série
                        series.Points.AddXY(data, SelectedMeasurements[k].Series[Channel.VOLTAGE_POS_MOD].Reading(min+i)/ vb);
                        sinais[i, k] = SelectedMeasurements[k].Series[Channel.VOLTAGE_POS_MOD].Reading(min+i) / vb;

                        // Incremento em 'ticks' no valor da data, de acordo com a taxa da consulta
                        // 10 mil ticks = 1 ms
                        inc = Convert.ToInt64(Math.Floor((1 / taxa) * 1000 * (i + 1 + min)) * 10000);
                        data = dataIni.AddTicks(inc);
                    }
                    chart1.Series.Add(series);

                }

            }
            // Gráfico do módulo da corrente - Seq. Positiva
            if (opGraf == 16)
            {
                double ib = 1; // a base para gráficos em pu 

                if (pu == 1)
                {
                    // Define o título do eixo Y
                    chart1.ChartAreas[0].AxisY.Title = "Corrente (pu)";
                }
                else if (pu == 0)
                {
                    // Define o título do eixo Y
                    chart1.ChartAreas[0].AxisY.Title = "Corrente (A)";
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

                // Inicialização do vetor que recebe o sinal de entrada para o Prony
                sinais = new Matrix(tlProny, SelectedMeasurements.Length);

                for (int k = 0; k < SelectedMeasurements.Length; k++)
                {
                    // Inicializa o incremento
                    inc = 0;

                    // Adiciona quantas séries forem necessárias. O seu nome corresponde ao terminal.
                    Series series = new Series(SelectedMeasurements[k].Terminal.DisplayName);
                    // Define a cor da série
                    series.Color = cores[k];
                    series.IsXValueIndexed = true;
                    // Define o tipo da série recém-criada, aqui usa-se o tipo 'FastLine'
                    series.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastLine;

                    // Inicialização da data para o eixo X
                    inc = Convert.ToInt64(Math.Floor((1 / Convert.ToDouble(taxa)) * 1000 * (min)) * 10000);
                    data = dataIni.AddTicks(inc);

                    for (int i = 0; i < tlProny; i++)
                    {
                        // Traça a série
                        series.Points.AddXY(data, SelectedMeasurements[k].Series[Channel.CURRENT_POS_MOD].Reading(i+min) / ib);
                        sinais[i, k] = SelectedMeasurements[k].Series[Channel.CURRENT_POS_MOD].Reading(i + min) / ib;

                        // Incremento em 'ticks' no valor da data, de acordo com a taxa da consulta
                        // 10 mil ticks = 1 ms
                        inc = Convert.ToInt64(Math.Floor((1 / taxa) * 1000 * (i + 1 + min)) * 10000);
                        data = dataIni.AddTicks(inc);
                    }
                    chart1.Series.Add(series);

                }

            }

            // Gráfico da diferença angular da tensão - FASE ÚNICA
            if ((opGraf == 7) || (opGraf == 8) || (opGraf == 9))
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
                // Desabilita o Prony se mais de um sinal foi selecionado

                if (taxa == 1)
                {
                    chart1.Titles[0].Text = "Diferença Angular da Tensão - Fase " + phase.Phase + " - Ref.: " +
                    Reference.Terminal.DisplayName + " - "
                        + Convert.ToString(taxa) + " fasor/s";
                }
                else
                {
                    chart1.Titles[0].Text = "Diferença Angular da Tensão - Fase " + phase.Phase + " - Ref.: " +
                    Reference.Terminal.DisplayName + " - "
                        + Convert.ToString(taxa) + " fasores/s";
                }

                // Inicialização do vetor que recebe o sinal de entrada para o Prony
                sinais = new Matrix(tlProny, SelectedMeasurements.Length-1);

                // Vetor que acumula as diferenças, utilizado para traçar o gráfico
                double[] vetorDifs = new double[Reference.Series[Channel.MISSING].Count];

                // Contador para as séries
                int n = 0;
                // Plota o número de selecionados menos um, já que um dos terminais é a referência
                for (int k = 0; k < SelectedMeasurements.Length; k++)
                {
                    // Inicializa o incremento
                    inc = 0;

                    if (SelectedMeasurements[k] != Reference)
                    {
                        // Adiciona quantas séries forem necessárias. O seu nome corresponde ao terminal.
                        Series series = new Series(SelectedMeasurements[k].Terminal.DisplayName);
                        // Define a cor da série
                        series.Color = cores[n];
                        series.IsXValueIndexed = true;
                        // Define o tipo da série recém-criada, aqui usa-se o tipo 'FastLine'
                        series.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastLine;


                        double dif, difback; // diferença angular e o backup deste valor (para o caso de perda de dados)
                        difback = 0;
                        int cwh = 0; // contador para o while

                        for (int i = 0; i < Reference.Series[Channel.MISSING].Count; i++)
                        {

                            // Diferença angular
                            dif = SelectedMeasurements[k].Series[phase].Reading(i) -
                                Reference.Series[phase].Reading(i);

                            // Dados faltantes nos ângulos das PMUs
                            // Dado faltante não é no início da série de dados
                            if ((SelectedMeasurements[k].Series[Channel.MISSING].Reading(i) == 1) || (Reference.Series[Channel.MISSING].Reading(i) == 1))
                            {
                                if (i != 0)
                                {
                                    dif = difback;

                                }
                                else
                                {
                                    while ((SelectedMeasurements[k].Series[Channel.MISSING].Reading(cwh) == 1) || (Reference.Series[Channel.MISSING].Reading(cwh) == 1))
                                    {
                                        // Se chegou ao fim e não há nenhum dado 
                                        if (cwh == (SelectedMeasurements[k].Series[phase].Count - 1))
                                            break;
                                        else
                                            cwh++; // atualiza o contador
                                    }
                                    dif = SelectedMeasurements[k].Series[phase].Reading(cwh) -
                                Reference.Series[phase].Reading(cwh);
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
                            vetorDifs[i] = dif;

                        }

                        // Inicialização da data para o eixo X
                        inc = Convert.ToInt64(Math.Floor((1 / Convert.ToDouble(taxa)) * 1000 * (min)) * 10000);
                        data = dataIni.AddTicks(inc);

                        // Preenche a matriz com as diferenças angulares
                        for (int i = 0; i < tlProny; i++)
                        {
                            /////////////////////                    
                            // Traça a série
                            chart1.Series[n].Points.AddXY(data, vetorDifs[i + min]);

                            // Incremento em 'ticks' no valor da data, de acordo com a taxa da consulta
                            // 10 mil ticks = 1 ms
                            inc = Convert.ToInt64(Math.Floor((1 / taxa) * 1000 * (i + 1 + min)) * 10000);
                            data = dataIni.AddTicks(inc);

                            sinais[i, n] = vetorDifs[i + min];
                        }
                        chart1.Series.Add(series);
                        n++;
                    }
                }
            }
            // Gráfico da diferença angular da corrente - FASE ÚNICA
            if ((opGraf == 17) || (opGraf == 18) || (opGraf == 19))
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

                // Título do gráfico
                if (taxa == 1)
                {
                    chart1.Titles[0].Text = "Diferença Angular da Corrente - Fase " + phase.Phase + " - Ref.: " +
                    Reference.Terminal.DisplayName + " - "
                        + Convert.ToString(taxa) + " fasor/s";
                }
                else
                {
                    chart1.Titles[0].Text = "Diferença Angular da Corrente - Fase " + phase.Phase + " - Ref.: " +
                    Reference.Terminal.DisplayName + " - "
                        + Convert.ToString(taxa) + " fasores/s";
                }

                // Inicialização do vetor que recebe o sinal de entrada para o Prony
                sinais = new Matrix(tlProny, SelectedMeasurements.Length-1);

                // Vetor que acumula as diferenças, utilizado para traçar o gráfico
                double[] vetorDifs = new double[Reference.Series[Channel.MISSING].Count];

                // Contador para as séries
                int n = 0;
                // Plota o número de selecionados menos um, já que um dos terminais é a referência
                for (int k = 0; k < SelectedMeasurements.Length; k++)
                {
                    // Inicializa o incremento
                    inc = 0;

                    if (SelectedMeasurements[k] != Reference)
                    {
                        // Adiciona quantas séries forem necessárias. O seu nome corresponde ao terminal.
                        Series series = new Series(SelectedMeasurements[k].Terminal.DisplayName);
                        // Define a cor da série
                        series.Color = cores[n];
                        series.IsXValueIndexed = true;
                        // Define o tipo da série recém-criada, aqui usa-se o tipo 'FastLine'
                        series.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastLine;


                        double dif, difback; // diferença angular e o backup deste valor (para o caso de perda de dados)
                        difback = 0;
                        int cwh = 0; // contador para o while

                        for (int i = 0; i < Reference.Series[Channel.MISSING].Count; i++)
                        {

                            // Diferença angular
                            dif = SelectedMeasurements[k].Series[phase].Reading(i) -
                                Reference.Series[phase].Reading(i);

                            // Dados faltantes nos ângulos das PMUs
                            // Dado faltante não é no início da série de dados
                            if ((SelectedMeasurements[k].Series[Channel.MISSING].Reading(i) == 1) || (Reference.Series[Channel.MISSING].Reading(i) == 1))
                            {
                                if (i != 0)
                                {
                                    dif = difback;

                                }
                                else
                                {
                                    while ((SelectedMeasurements[k].Series[Channel.MISSING].Reading(cwh) == 1) || (Reference.Series[Channel.MISSING].Reading(cwh) == 1))
                                    {
                                        // Se chegou ao fim e não há nenhum dado 
                                        if (cwh == (SelectedMeasurements[k].Series[phase].Count - 1))
                                            break;
                                        else
                                            cwh++; // atualiza o contador
                                    }
                                    dif = SelectedMeasurements[k].Series[phase].Reading(cwh) -
                                Reference.Series[phase].Reading(cwh);
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
                            vetorDifs[i] = dif;

                        }

                        // Inicialização da data para o eixo X
                        inc = Convert.ToInt64(Math.Floor((1 / Convert.ToDouble(taxa)) * 1000 * (min)) * 10000);
                        data = dataIni.AddTicks(inc);

                        // Preenche a matriz com as diferenças angulares
                        for (int i = 0; i < tlProny; i++)
                        {
                            /////////////////////                    
                            // Traça a série
                            chart1.Series[n].Points.AddXY(data, vetorDifs[i + min]);

                            // Incremento em 'ticks' no valor da data, de acordo com a taxa da consulta
                            // 10 mil ticks = 1 ms
                            inc = Convert.ToInt64(Math.Floor((1 / taxa) * 1000 * (i + 1 + min)) * 10000);
                            data = dataIni.AddTicks(inc);

                            sinais[i, n] = vetorDifs[i + min];
                        }
                        chart1.Series.Add(series);
                        n++;
                    }
                }
            }
            // Gráfico da diferença angular da tensão - Sequência Positiva
            if (opGraf == 10)
            {
                // Define o título do eixo Y
                chart1.ChartAreas[0].AxisY.Title = "Diferença Angular (Graus)";
                // Desabilita o Prony se mais de um sinal foi selecionado

                // Título do gráfico
                if (taxa == 1)
                {
                    chart1.Titles[0].Text = "Diferença Angular da Tensão - Sequência Positiva - Ref.: " +
                    Reference.Terminal.DisplayName + " - "
                        + Convert.ToString(taxa) + " fasor/s";
                }
                else
                {
                    chart1.Titles[0].Text = "Diferença Angular da Tensão - Sequência Positiva - Ref.: " +
                    Reference.Terminal.DisplayName + " - "
                        + Convert.ToString(taxa) + " fasores/s";
                }

                // Inicialização do vetor que recebe o sinal de entrada para o Prony
                sinais = new Matrix(tlProny, SelectedMeasurements.Length-1);

                // Vetor que acumula as diferenças, utilizado para traçar o gráfico
                double[] vetorDifs = new double[Reference.Series[Channel.MISSING].Count];

                // Contador para as séries
                int n = 0;
                // Plota o número de selecionados menos um, já que um dos terminais é a referência
                for (int k = 0; k < SelectedMeasurements.Length; k++)
                {
                    // Inicializa o incremento
                    inc = 0;

                    if (SelectedMeasurements[k] != Reference)
                    {
                        // Adiciona quantas séries forem necessárias. O seu nome corresponde ao terminal.
                        Series series = new Series(SelectedMeasurements[k].Terminal.DisplayName);
                        // Define a cor da série
                        series.Color = cores[n];
                        series.IsXValueIndexed = true;
                        // Define o tipo da série recém-criada, aqui usa-se o tipo 'FastLine'
                        series.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastLine;

                        double dif, difback; // diferença angular e o backup deste valor (para o caso de perda de dados)
                        difback = 0;
                        int cwh = 0; // contador para o while

                        for (int i = 0; i < SelectedMeasurements[k].Series[Channel.VOLTAGE_POS_ANG].Count; i++)
                        {
                            // Diferença angular
                            dif = SelectedMeasurements[k].Series[Channel.VOLTAGE_POS_ANG].Reading(i) -
                                Reference.Series[Channel.VOLTAGE_POS_ANG].Reading(i);

                            // Dados faltantes nos ângulos das PMUs
                            // Dado faltante não é no início da série de dados

                            if ((SelectedMeasurements[k].Series[Channel.MISSING].Reading(i) == 1) || (Reference.Series[Channel.MISSING].Reading(i) == 1))
                            {
                                if (i != 0)
                                {
                                    dif = difback;
                                }
                                else
                                {
                                    while ((SelectedMeasurements[k].Series[Channel.MISSING].Reading(cwh) == 1) || (Reference.Series[Channel.MISSING].Reading(cwh) == 1))
                                    {
                                        // Se chegou ao fim e não há nenhum dado 
                                        if (cwh == (SelectedMeasurements[k].Series[Channel.VOLTAGE_POS_ANG].Count - 1))
                                            break;
                                        else
                                            cwh++; // atualiza o contador
                                    }
                                    dif = SelectedMeasurements[k].Series[Channel.VOLTAGE_POS_ANG].Reading(cwh) -
                                Reference.Series[Channel.VOLTAGE_POS_ANG].Reading(cwh);
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
                        n++;
                        chart1.Series.Add(series);
                    }
                }
            }
            // Gráfico da diferença angular da corrente - Sequência Positiva
            if (opGraf == 20)
            {
                // Define o título do eixo Y
                chart1.ChartAreas[0].AxisY.Title = "Diferença Angular (Graus)";

                // Título do gráfico
                if (taxa == 1)
                {
                    chart1.Titles[0].Text = "Diferença Angular da Corrente - Sequência Positiva - Ref.: " +
                    Reference.Terminal.DisplayName + " - "
                        + Convert.ToString(taxa) + " fasor/s";
                }
                else
                {
                    chart1.Titles[0].Text = "Diferença Angular da Corrente - Sequência Positiva - Ref.: " +
                    Reference.Terminal.DisplayName + " - "
                        + Convert.ToString(taxa) + " fasores/s";
                }

                // Inicialização do vetor que recebe o sinal de entrada para o Prony
                sinais = new Matrix(tlProny, SelectedMeasurements.Length-1);

                // Vetor que acumula as diferenças, utilizado para traçar o gráfico
                double[] vetorDifs = new double[Reference.Series[Channel.MISSING].Count];

                // Contador para as séries
                int n = 0;
                // Plota o número de selecionados menos um, já que um dos terminais é a referência
                for (int k = 0; k < SelectedMeasurements.Length; k++)
                {
                    // Inicializa o incremento
                    inc = 0;

                    if (SelectedMeasurements[k] != Reference)
                    {
                        // Adiciona quantas séries forem necessárias. O seu nome corresponde ao terminal.
                        Series series = new Series(SelectedMeasurements[k].Terminal.DisplayName);
                        // Define a cor da série
                        series.Color = cores[n];
                        series.IsXValueIndexed = true;
                        // Define o tipo da série recém-criada, aqui usa-se o tipo 'FastLine'
                        series.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastLine;

                        double dif, difback; // diferença angular e o backup deste valor (para o caso de perda de dados)
                        difback = 0;
                        int cwh = 0; // contador para o while

                        for (int i = 0; i < SelectedMeasurements[k].Series[Channel.CURRENT_POS_ANG].Count; i++)
                        {
                            // Diferença angular
                            dif = SelectedMeasurements[k].Series[Channel.CURRENT_POS_ANG].Reading(i) -
                                Reference.Series[Channel.CURRENT_POS_ANG].Reading(i);

                            // Dados faltantes nos ângulos das PMUs
                            // Dado faltante não é no início da série de dados

                            if ((SelectedMeasurements[k].Series[Channel.MISSING].Reading(i) == 1) || (Reference.Series[Channel.MISSING].Reading(i) == 1))
                            {
                                if (i != 0)
                                {
                                    dif = difback;
                                }
                                else
                                {
                                    while ((SelectedMeasurements[k].Series[Channel.MISSING].Reading(cwh) == 1) || (Reference.Series[Channel.MISSING].Reading(cwh) == 1))
                                    {
                                        // Se chegou ao fim e não há nenhum dado 
                                        if (cwh == (SelectedMeasurements[k].Series[Channel.CURRENT_POS_ANG].Count - 1))
                                            break;
                                        else
                                            cwh++; // atualiza o contador
                                    }
                                    dif = SelectedMeasurements[k].Series[Channel.CURRENT_POS_ANG].Reading(cwh) -
                                Reference.Series[Channel.CURRENT_POS_ANG].Reading(cwh);
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
                        n++;
                        chart1.Series.Add(series);
                    }
                }
            }
            // Gráfico da potência ativa trifásica
            if (opGraf == 21)
            {
                chart1.ChartAreas[0].AxisY.Title = "Potência (MW)";

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

                // Inicialização do vetor que recebe o sinal de entrada para o Prony
                sinais = new Matrix(tlProny, SelectedMeasurements.Length);

                for (int k = 0; k < SelectedMeasurements.Length; k++)
                {
                    // Inicializa o incremento
                    inc = 0;

                    Series series = new Series(SelectedMeasurements[k].Terminal.DisplayName);
                    series.Color = cores[k];
                    series.IsXValueIndexed = true;
                    series.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastLine;

                    // Inicialização da data para o eixo X
                    inc = Convert.ToInt64(Math.Floor((1 / Convert.ToDouble(taxa)) * 1000 * (min)) * 10000);
                    data = dataIni.AddTicks(inc);

                    for (int i = 0; i < tlProny; i++)
                    {
                        // Traça a série
                        chart1.Series[k].Points.AddXY(data, SelectedMeasurements[k].Series[Channel.ACTIVE_POWER].Reading(min+i) / 1e6);
                        sinais[i, k] = SelectedMeasurements[k].Series[Channel.ACTIVE_POWER].Reading(min + i) / 1e6;

                        // Incremento em 'ticks' no valor da data, de acordo com a taxa da consulta
                        // 10 mil ticks = 1 ms
                        inc = Convert.ToInt64(Math.Floor((1 / taxa) * 1000 * (i + 1 + min)) * 10000);
                        data = dataIni.AddTicks(inc);
                    }
                    chart1.Series.Add(series);
                }

            }
            // Gráfico da potência reativa trifásica
            if (opGraf == 22)
            {
                chart1.ChartAreas[0].AxisY.Title = "Potência (Mvar)";

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

                // Inicialização do vetor que recebe o sinal de entrada para o Prony
                sinais = new Matrix(tlProny, SelectedMeasurements.Length);

                for (int k = 0; k < SelectedMeasurements.Length; k++)
                {
                    // Inicializa o incremento
                    inc = 0;

                    Series series = new Series(SelectedMeasurements[k].Terminal.DisplayName);
                    series.Color = cores[k];
                    series.IsXValueIndexed = true;
                    series.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastLine;

                    // Inicialização da data para o eixo X
                    inc = Convert.ToInt64(Math.Floor((1 / Convert.ToDouble(taxa)) * 1000 * (min)) * 10000);
                    data = dataIni.AddTicks(inc);

                    for (int i = 0; i < tlProny; i++)
                    {
                        // Traça a série
                        chart1.Series[k].Points.AddXY(data, SelectedMeasurements[k].Series[Channel.REACTIVE_POWER].Reading(min + i) / 1e6);
                        sinais[i, k] = SelectedMeasurements[k].Series[Channel.REACTIVE_POWER].Reading(min + i) / 1e6;

                        // Incremento em 'ticks' no valor da data, de acordo com a taxa da consulta
                        // 10 mil ticks = 1 ms
                        inc = Convert.ToInt64(Math.Floor((1 / taxa) * 1000 * (i + 1 + min)) * 10000);
                        data = dataIni.AddTicks(inc);

                    }
                    chart1.Series.Add(series);
                }

            }
            // Gráfico da frequência
            if (opGraf == 11)
            {
                // Define o título do eixo Y
                chart1.ChartAreas[0].AxisY.Title = "Frequência (Hz)";
                // Desabilita o Prony se mais de um sinal foi selecionado

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

                // Inicialização do vetor que recebe o sinal de entrada para o Prony
                sinais = new Matrix(tlProny, SelectedMeasurements.Length);

                for (int k = 0; k < SelectedMeasurements.Length; k++)
                {
                    // Inicializa o incremento
                    inc = 0;

                    Series series = new Series(SelectedMeasurements[k].Terminal.DisplayName);
                    series.Color = cores[k];
                    series.IsXValueIndexed = true;
                    series.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastLine;

                    // Inicialização da data para o eixo X
                    inc = Convert.ToInt64(Math.Floor((1 / Convert.ToDouble(taxa)) * 1000 * (min)) * 10000);
                    data = dataIni.AddTicks(inc);

                    for (int i = 0; i < tlProny; i++)
                    {
                        // Traça a série
                        chart1.Series[k].Points.AddXY(data, SelectedMeasurements[k].Series[Channel.FREQ].Reading(min+i));
                        sinais[i, k] = SelectedMeasurements[k].Series[Channel.FREQ].Reading(min + i);

                        // Incremento em 'ticks' no valor da data, de acordo com a taxa da consulta
                        // 10 mil ticks = 1 ms
                        inc = Convert.ToInt64(Math.Floor((1 / taxa) * 1000 * (i + 1 + min)) * 10000);
                        data = dataIni.AddTicks(inc);
                    }
                    chart1.Series.Add(series);
                }
            }

            // Gráfico da DFREQ
            if (opGraf == 23)
            {
                // Define o título do eixo Y
                chart1.ChartAreas[0].AxisY.Title = "Variação de Frequência (Hz/s)";
                // Desabilita o Prony se mais de um sinal foi selecionado

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

                // Inicialização do vetor que recebe o sinal de entrada para o Prony
                sinais = new Matrix(tlProny, SelectedMeasurements.Length);

                for (int k = 0; k < SelectedMeasurements.Length; k++)
                {
                    // Inicializa o incremento
                    inc = 0;

                    Series series = new Series(SelectedMeasurements[k].Terminal.DisplayName);
                    series.Color = cores[k];
                    series.IsXValueIndexed = true;
                    series.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastLine;

                    // Inicialização da data para o eixo X
                    inc = Convert.ToInt64(Math.Floor((1 / Convert.ToDouble(taxa)) * 1000 * (min)) * 10000);
                    data = dataIni.AddTicks(inc);

                    for (int i = 0; i < tlProny; i++)
                    {
                        // Traça a série
                        chart1.Series[k].Points.AddXY(data, SelectedMeasurements[k].Series[Channel.DFREQ].Reading(min + i));
                        sinais[i, k] = SelectedMeasurements[k].Series[Channel.DFREQ].Reading(min + i);

                        // Incremento em 'ticks' no valor da data, de acordo com a taxa da consulta
                        // 10 mil ticks = 1 ms
                        inc = Convert.ToInt64(Math.Floor((1 / taxa) * 1000 * (i + 1 + min)) * 10000);
                        data = dataIni.AddTicks(inc);
                    }
                    chart1.Series.Add(series);
                }
            }

            #endregion

            try
            {
                int numSinais = SelectedMeasurements.Length;
                if (opGraf == 7 || opGraf == 8 || opGraf == 9 || opGraf == 17 || opGraf == 18 || opGraf == 19 || opGraf == 10 || opGraf == 20)
                    numSinais = numSinais - 1;

                #region Algoritmo de Prony

                double amos = 1.0 / Convert.ToDouble(taxa);

                // Comprimento dos sinais de entrada
                int N = sinais.RowCount;

                // definida aqui arbitrariamente, por enquanto
                //ordem = N / 4;
                ordem = ordemDef;

                Vector t = new Vector(VectorType.Column, N);

                for (int i = 0; i < N; i++)
                {
                    t[i] = amos * (i + 1);
                }

                ///////////////////////////////////
                // Matriz de Hankel
                Matrix H = new Matrix(numSinais * (N - ordem), ordem);

                // Deslocamento para a concatenação dos dados na matriz de Hankel e, logo a seguir,
                // no vetor de estimação
                int desl = 0;

                for (int k = 0; k < numSinais; k++) // número de sinais
                {
                    desl = k * (N - ordem);

                    for (int i = 0; i < (N - ordem); i++) // número de linhas (por sinal)
                    {
                        for (int j = 0; j < ordem; j++) // número de colunas
                        {
                            //H[i, j] = sinal[ordem - j + i - 1];
                            H[i + desl, j] = sinais[ordem - j + i - 1, k];
                        }
                    }
                }

                ///////////////////////////////////
                // Vetor de estimação
                Vector y = new Vector(VectorType.Column, numSinais * (N - ordem));

                for (int k = 0; k < numSinais; k++)
                {
                    desl = k * (N - ordem);

                    for (int i = 0; i < (N - ordem); i++)
                    {
                        y[i + desl] = sinais[ordem + i, k];
                    }
                }

                // Tem que usar este tipo de comando quando a matriz H, utilizada no comando 'solve'
                // não é quadrada.
                LinearLeastSquares lls = new LinearLeastSquares();

                // PASSO 1: cálculo dos coeficientes a's
                Matrix A = lls.COFSolve(H, y);

                ///////////////////////////////////

                ComplexMatrix C = new ComplexMatrix(ordem, ordem);

                for (int i = 0; i < ordem - 1; i++)
                {
                    // parte real são os valores que estão em A
                    C[i, ordem - 1] = new Complex(A[ordem - i - 1, 0], 0); // última coluna igual ao vetor A
                    C[i + 1, i] = new Complex(1, 0); // diagonal abaixo da principal
                }

                C[ordem - 1, ordem - 1] = new Complex(A[0, 0], 0);

                ComplexMatrix autovetores; // ordem, ordem
                EigenSystem es = new EigenSystem();

                // PASSO 2: encontrar as raízes do polinômio
                ComplexMatrix raizes = es.GetEigenvalues(C, out autovetores);

                // Raízes são convertidas do modelo discreto para o modelo contínuo
                ComplexMatrix raizes_s = new ComplexMatrix(ordem, 1); // raízes no plano s
                for (int i = 0; i < ordem; i++)
                {
                    // Log de um número complexo é obtido da seguinte forma
                    // Log(z) = ln(|z|) + iArg(z)
                    raizes_s[i, 0] = new Complex((Math.Log(raizes[i, 0].Modulus) / amos),
                        ((raizes[i, 0].Argument) / amos));
                }

                // Fator de amortecimento
                Matrix fat_amor = raizes_s.GetReal();

                // Taxa de amortecimento
                Vector taxa_amor = new Vector(VectorType.Column, ordem);
                for (int i = 0; i < ordem; i++)
                {
                    taxa_amor[i] = (-fat_amor[i, 0] / raizes_s[i, 0].Modulus) * 100;
                }

                // cálculo das frequências para cada modo de oscilação
                freq = new Vector(VectorType.Column, ordem);

                for (int i = 0; i < ordem; i++)
                {
                    freq[i] = raizes[i, 0].Argument / (2 * Math.PI * amos);
                }

                ComplexMatrix z = new ComplexMatrix(N, ordem);

                for (int i = 0; i < N; i++)
                {
                    for (int j = 0; j < ordem; j++)
                    {
                        Complexo aux = new Complexo(raizes[j, 0].Real, raizes[j, 0].Imaginary);
                        aux = aux ^ (i + 1);
                        Complex aux2 = new Complex(aux.re, aux.im);

                        z[i, j] = aux2;
                    }
                }

                //////////////////////////////////////////
                //////////////////////////////////////////
                // Inversão da matriz complexa

                // Transposta de z

                ComplexVector[] c = z.GetRowVectors();
                // zt é a transposta de z
                ComplexMatrix zt = new ComplexMatrix(ordem, N);

                for (int i = 0; i < N; i++)
                {
                    ComplexVector temp = c[i];
                    for (int j = 0; j < ordem; j++)
                    {
                        zt[j, i] = temp[j];
                    }
                }

                ComplexMatrix P = new ComplexMatrix(ordem, ordem);

                P = zt * z;

                Matrix Pre = P.GetReal();
                Matrix Pim = P.GetImag();

                Matrix Q = new Matrix(ordem * 2, ordem * 2);

                for (int i = 0; i < ordem; i++)
                {
                    for (int j = 0; j < ordem; j++)
                    {
                        Q[i, j] = Pre[i, j];
                        Q[i + ordem, j] = -Pim[i, j];
                        Q[i, j + ordem] = Pim[i, j];
                        Q[i + ordem, j + ordem] = Pre[i, j];
                    }
                }

                Matrix Q_inv = Q.Inverse();
                // Matriz Q_cp é a reordenação de Q_inv na forma complexa
                ComplexMatrix Q_cp = new ComplexMatrix(ordem, ordem);

                for (int i = 0; i < ordem; i++)
                {
                    for (int j = 0; j < ordem; j++)
                    {
                        Q_cp[i, j] = new Complex(Q_inv[i, j], Q_inv[i, j + ordem]);
                    }
                }

                // Pseudoinversa: Hp = inv(H' * H) * H'
                // Neste algoritmo corresponde a: Z_inv = Q_cp*zt

                ComplexMatrix Z_inv = new ComplexMatrix(ordem, N);

                Z_inv = Q_cp * zt;

                //////////////////////////////////////////
                //////////////////////////////////////////

                ComplexMatrix X = new ComplexMatrix(N, numSinais);

                for (int k = 0; k < numSinais; k++)
                {
                    for (int i = 0; i < N; i++)
                    {
                        X[i, k] = new Complex(sinais[i, k], 0);
                    }
                }

                // PASSO 3: encontrar os resíduos complexos
                ComplexMatrix R = Z_inv * X;

                amps = new Matrix(ordem, numSinais);
                ang = new Matrix(ordem, numSinais);
                Vector sig_aux = new Vector(N);
                Matrix energia = new Matrix(ordem, numSinais);
                double soma = 0;

                for (int k = 0; k < numSinais; k++)
                {
                    for (int i = 0; i < ordem; i++)
                    {
                        // amplitude de cada modo
                        amps[i, k] = R[i, k].Modulus;
                        // fase de cada modo
                        ang[i, k] = R[i, k].Argument;
                        for (int j = 0; j < N; j++)
                        {
                            sig_aux[j] = (amps[i, k] * Math.Exp(fat_amor[i, 0] * t[j])) * Math.Cos(2.0 * Math.PI * freq[i] * t[j] + ang[i, k]);
                            soma = soma + Math.Pow(sig_aux[j], 2);
                        }
                        // Cálculo da energia
                        energia[i, k] = soma / amos;
                        // Reinicializa a soma
                        soma = 0;
                    }
                }
                //////////////////////////////////////////
                // Reconstrução dos sinais
                Matrix Y_est = new Matrix(N, numSinais);

                for (int k = 0; k < numSinais; k++)
                {
                    for (int i = 0; i < N; i++)
                    {
                        double aux = 0;
                        for (int j = 0; j < ordem; j++)
                        {
                            aux = aux + amps[j, k] * Math.Exp(fat_amor[j, 0] * t[i]) * Math.Cos(2 * Math.PI * freq[j] * t[i] + ang[j, k]);
                        }
                        Y_est[i, k] = aux;
                    }
                }                 

            #endregion

                #region Resultados

                for (int k = 0; k < numSinais; k++)
                {
                    // GRÁFICO

                    // Cria a série do sinal estimado
                    chart1.Series.Add(chart1.Series[k].Name + " - Estimado");
                    chart1.Series[k + numSinais].IsXValueIndexed = true;
                    // Define o tipo da série recém-criada, aqui usa-se o tipo 'FastLine'
                    chart1.Series[k + numSinais].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastLine;
                    chart1.Series[k + numSinais].BorderDashStyle = ChartDashStyle.Dot;
                    chart1.Series[k + numSinais].Color = cores[k];

                    inc = Convert.ToInt64(Math.Floor((1 / Convert.ToDouble(taxa)) * 1000 * (min)) * 10000);
                    data = dataIni.AddTicks(inc);

                    for (int i = 0; i < tlProny; i++)
                    {
                        // Traça a série
                        chart1.Series[k + numSinais].Points.AddXY(data, Y_est[i, k]);
                        // Incremento em 'ticks' no valor da data, de acordo com a taxa da consulta
                        // 10 mil ticks = 1 ms
                        inc = Convert.ToInt64(Math.Floor((1 / Convert.ToDouble(taxa)) * 1000 * (i + 1 + min)) * 10000);
                        data = dataIni.AddTicks(inc);
                    }
                }

                //////////////////////////////////////////
                // TABELA

                // Montagem das abas do 'tab control'; são inseridas tantas abas quanto forem o número
                // de sinais originais no gráfico
                for (int k = 0; k < numSinais; k++)
                {
                    //tabControl1.TabPages.Add(Objetos.ObjMat[indCons].matNomes[indSelec[k]]);
                    tabControl1.TabPages.Add(chart1.Series[k].Name);

                    DataGridView tab = new DataGridView();

                    // Dimensões da tabela
                    tab.Height = 132;
                    tab.Width = 768;

                    // Para que não haja uma última linha em branco no datagrid
                    tab.AllowUserToAddRows = false;

                    // Adiciona o dataGridView à aba correspondente np tabControl
                    tabControl1.TabPages[k].Controls.Add(tab);

                    // Cria a tabela que será preenchida com o arquivo "terminais.cfg"
                    DataTable tabelaProny = new DataTable();
                    // Coluna para a tabela
                    DataColumn coluna = null;

                    // Colunas
                    coluna = new DataColumn("ener");
                    tabelaProny.Columns.Add(coluna);
                    tabelaProny.Columns[0].DataType = System.Type.GetType("System.Double");

                    coluna = new DataColumn("freq");
                    tabelaProny.Columns.Add(coluna);

                    coluna = new DataColumn("amort");
                    tabelaProny.Columns.Add(coluna);

                    coluna = new DataColumn("amp");
                    tabelaProny.Columns.Add(coluna);

                    coluna = new DataColumn("fase");
                    tabelaProny.Columns.Add(coluna);

                    coluna = new DataColumn("real");
                    tabelaProny.Columns.Add(coluna);

                    coluna = new DataColumn("imag");
                    tabelaProny.Columns.Add(coluna);

                    // Linhas

                    DataRow linha = null;

                    // Número de modos calculados pelo Prony
                    for (int i = 0; i < ordem; i++)
                    {
                        if ((freq[i] < 10) && (freq[i] > 1e-6) && (energia[i, k] > 1e-3))
                        {
                            linha = tabelaProny.NewRow();
                            linha["ener"] = Math.Round(energia[i, k], 3);
                            linha["freq"] = Math.Round(freq[i], 3);
                            linha["amort"] = Math.Round(taxa_amor[i], 3);
                            linha["amp"] = Math.Round(amps[i, k], 3);
                            linha["fase"] = Math.Round(ang[i, k], 3);
                            linha["real"] = Math.Round(raizes_s[i, 0].Real, 3);
                            linha["imag"] = Math.Round(raizes_s[i, 0].Imaginary, 3);
                            tabelaProny.Rows.Add(linha);
                        }
                    }

                    // Relaciona o DataGrid com a tabela
                    tab.DataSource = tabelaProny;

                    // Definir os títulos das colunas 
                    tab.Columns[0].HeaderText = "Energia";
                    tab.Columns[1].HeaderText = "Frequência (Hz)";
                    tab.Columns[1].Width = 107;
                    tab.Columns[2].HeaderText = "Amortecimento (%)";
                    tab.Columns[3].HeaderText = "Amplitude";
                    tab.Columns[4].HeaderText = "Fase (rad)";
                    tab.Columns[5].HeaderText = "Real";
                    tab.Columns[6].HeaderText = "Imaginário";
                    //////////////////////////////////////////////////////////

                    tab.Sort(tab.Columns[0], ListSortDirection.Descending);

                } // fim do for quantidade de sinais, igual à quantidade de abas e de tabelas

                // Preencher a lista para traçar as mode shapes
                toolStripComboBox1.Items.Clear();

                int nm = 0; // número de modos para as mode shapes
                // Descobre o número de modos dos quais pode-se traçar as mode shapes
                for (int i = 0; i < ordem; i++)
                {
                    if ((freq[i] < 10) && (freq[i] > 1e-6))
                    {
                        nm++;
                    }
                }

                if ((nm == 0) || (numSinais < 2))
                {
                    toolStripSplitButton2.Enabled = false;
                }
                else
                {
                    // Vetor com os modos que podem ser traçados nas mode shapes
                    modos = new double[nm];

                    nm = 0;
                    for (int i = 0; i < ordem; i++)
                    {
                        if ((freq[i] < 10) && (freq[i] > 1e-6))
                        {
                            modos[nm] = freq[i];
                            // Preenche o comBox com os modos de oscilação entre 5 e 1e-6 Hz 
                            toolStripComboBox1.Items.Add(Math.Round(modos[nm], 3));
                            nm++;
                        }
                    }

                    // Em ordem crescente
                    Array.Sort(modos);
                    toolStripComboBox1.Sorted = true;
                    // Mostra o primeiro valor da lista
                    toolStripComboBox1.SelectedIndex = 0;
                }

                #endregion
            }
            catch(Exception)
            {
                MessageBox.Show("Houve um problema na execução do algoritmo do método de Prony. Reduza o número de sinais utilizado ou o tamanho do período de dados e tente novamente.", "ATENÇÃO!", MessageBoxButtons.OK);
                this.Close();
            }
        }

        private void chart1_DoubleClick(object sender, EventArgs e)
        {
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

            //chart1.BorderlineDashStyle = ChartDashStyle.NotSet;
        }

        private void traçarToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            //////////////////////////////////////////////////////////////////
            // Argumentos que devem ser repassados ao método "CriarModeShapes"
            
            // Índice do modo escolhido
            int indModo = toolStripComboBox1.SelectedIndex;

            // O índice do comboBox é equivalente ao índice do modo no vetor de armazenamento
            double modoEsc = modos[indModo];

            int numSinais = SelectedMeasurements.Length;
            if (opGraf == 7 || opGraf == 8 || opGraf == 9 || opGraf == 17 || opGraf == 18 || opGraf == 19 || opGraf == 10 || opGraf == 20)
                numSinais = numSinais - 1;

            double[] ampMS, angMS;
            string[] nomesMS;
                
            ampMS = new double[numSinais];
            angMS = new double[numSinais];
            nomesMS = new string[numSinais];

            // Nomes, amplitudes e fases
            for (int k = 0; k < numSinais; k++)
            {
                nomesMS[k] = chart1.Series[k].Name;                
                // encontrar as amplitudes e as fases comparando o modo escolhido com os modos de cada sinal
                for (int i = 0; i < ordem; i++)
                {
                    if (freq[i] == modoEsc)
                    {
                        ampMS[k] = amps[i, k];
                        angMS[k] = ang[i, k];
                    }
                }
            }

            // Incremento para as datas
            long inc = 0;

            inc = Convert.ToInt64(Math.Floor((1 / Convert.ToDouble(taxa)) * 1000 * (min)) * 10000);
            DateTime dataIniMS = SelectedMeasurements[0].Start.AddTicks(inc); ;
            inc = Convert.ToInt64(Math.Floor((1 / Convert.ToDouble(taxa)) * 1000 * (max)) * 10000);
            DateTime dataFinMS = SelectedMeasurements[0].Start.AddTicks(inc);

            // Título do gráfico
            string titulo = chart1.Titles[0].Text;

            // Cores atuais das séries (pode ser que o usuário tenha alterado alguma cor original)
            Color[] coresAtuais = new Color[chart1.Series.Count];

            for (int i = 0; i < chart1.Series.Count; i++)
            {
                coresAtuais[i] = chart1.Series[i].Color;
            }
            
            //////////////////////////////////////////////////////////////////

            // Chama o método "CriarModeShapes" no form pai (Form1)
            pai.CriarModeShapes(modoEsc, nomesMS, ampMS, angMS, dataIniMS, dataFinMS, titulo, coresAtuais, Query);
        }

        private void chart1_MouseDown(object sender, MouseEventArgs e)
        {
            tip.IsBalloon = true;

            // index of the clicked point in its series
            Int32 index = Convert.ToInt32(Math.Round(chart1.ChartAreas[0].AxisX.PixelPositionToValue(e.Location.X)) - 1);//result.PointIndex;
            //Int32 index = Convert.ToInt32(Math.Round(chart1.ChartAreas[0].AxisX.PixelPositionToValue(e.Location.X)) - 1);

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

                    DateTime dataIniJanela = dataIni.AddTicks(inc);

                    // Nome do eixo no qual houve o clic
                    string axis = result.Axis.AxisName.ToString();

                    AjustaProny f = new AjustaProny(this, chart1, dataEsc, taxa, axis);
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
                            "DT = " + dt.ToString(dtStr) + "   " + "1/DT = " + (Math.Abs(1 / dt.TotalSeconds)).ToString("0.000") + " Hz";

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
                            "DT = " + dt.ToString(dtStr) + "   " + "1/DT = " + (Math.Abs(1 / dt.TotalSeconds)).ToString("0.000") + " Hz";
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

        private void Form8_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.Dispose();

            long mem = GC.GetTotalMemory(false);

            GC.Collect();
            mem = GC.GetTotalMemory(false);
            GC.WaitForPendingFinalizers();
            mem = GC.GetTotalMemory(false);
        }

    }
}
