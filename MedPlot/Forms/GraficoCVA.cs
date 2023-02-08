using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DotNumerics.LinearAlgebra;
using DotNumerics.LinearAlgebra.CSLapack;
using System.Windows.Forms.DataVisualization.Charting;
using System.IO;
using MathNet.Numerics.IntegralTransforms;
using MathNet.Filtering.FIR;
using Excel = Microsoft.Office.Interop.Excel;
using MedFasee.Structure;
using MedFasee.Equipment;
using System.Collections.Generic;
using System.Reflection;

namespace MedPlot
{
    public partial class GraficoCVA : Form
    {
        private JanelaPrincipal pai;
        private int opGraf;  // opção do gráfico original que terá seu espectro traçado
        private int min, max; // índices de início e término da visualização atual do gráfico base
        private int pu; // contém a indicação de gráfico em PU

        // Data inicial da consulta
        private DateTime dataIni;
        private DateTime dataEsc;

        Color[] cores;
        ToolTip tip = new ToolTip();

        // Nomes para o gráfico das energias
        string[] nomesTitulo;

        // Matriz de entrada para o algoritmo CVA
        Matrix y;

        int dim; // dim(x) - sugestão -> 20
        int nbr; // number of block rows - sugestão -> 80

        int wl, ws; // window length ; window step
        int ne; // number of executions

        // Faixa de frequências escolhida para a análise
        double fmin, fmax;

        Matrix Y, Yp, Yf, H, Htr, L_aux, L, Ltr, L11, L21, L22, Rff, Rfp, Rpp;
        private Matrix T, Tinv, Minv, OC, S, Ok;

        double sr; // sampling rate

        // tempo de execução do algoritmo
        DateTime inicio, fim;

        bool flagFechar;

        // Variáveis para o pré-processamento
        int movAveOrder = 20;
        double fDown = 5.0;

        // Pasta de trabalho
        Excel._Workbook excelBook = null;
        // Planilha de trabalho
        Excel._Worksheet excelSheet = null;
        // Excel application
        Excel.Application excelApp = null;

        // Listas para armazenamento no arquivo
        //List<double> freqEnergy = new List<double>();
        //List<double> freqIdm = new List<double>();
        //List<double> dampEnergy = new List<double>();
        //List<double> dampIdm = new List<double>();
        //List<string> timeEnergy = new List<string>();
        //List<string> timeIdm = new List<string>();
        List<double> energy = new List<double>();
        List<double> idm = new List<double>();
        //List<List<double>> msEnergy = new List<List<double>>();
        //List<List<double>> msIdm = new List<List<double>>();

        // Flag para marcação ou não da opção "automático" do eixo Y no form de configurações
        public bool xAuto = true;
        public bool freqYAuto = false;
        public bool dampYAuto = false;

        public Query Query { get; internal set; }
        Measurement[] SelectedMeasurements { get; }
        Measurement Reference { get; }

        public GraficoCVA(JanelaPrincipal frm1, Query query, int op, Measurement reference, Measurement[] measurements, int minInd, int maxInd, int opPu, Color[] coresAtuais, int dimensao, int numBlocos, int tamJanela, int pasJanela, double freqMin, double freqMax)
        {
            InitializeComponent();

            // Habilita as funções de atualização do progresso e cancelamento do processo
            backgroundWorker1.WorkerReportsProgress = true;
            backgroundWorker1.WorkerSupportsCancellation = true;

            // Repassando para as variáveis internas
            pai = frm1;
            Query = query;
            opGraf = op;

            SelectedMeasurements = measurements;
            Reference = reference;

            min = minInd;
            max = maxInd;

            pu = opPu;

            cores = new Color[coresAtuais.Length];
            cores = coresAtuais;

            // Parâmetros para o algoritmo da RBE
            dim = dimensao;
            nbr = numBlocos;
            wl = tamJanela;
            ws = pasJanela;
            fmin = freqMin;
            fmax = freqMax;

        }

        private void Form15_Shown(object sender, EventArgs e)
        {
            inicio = DateTime.UtcNow;

            #region Configurações

            // Taxa com a qual a consulta foi realizada
            sr = SelectedMeasurements[0].FramesPerSecond;

            // Total de linhas da janela do gráfico base
            int tlRbe = (max - min) + 1;
            // Total de linhas da janela deslizante
            int tlJanDes = wl * Convert.ToInt16(sr) * 60; // min * (fas/s) * (s/min) = nº fasores(dados)
            // Número de execuções do algoritmo
            ne = Convert.ToInt16(Math.Floor((tlRbe - tlJanDes) / (ws * sr))) + 1;

            double vb = 1;
            double ib = 1;


            dataIni = Query.Measurements[0].Start;

            DateTime data = new DateTime();

            // Variável de incremento para as datas do eixo X
            long inc = 0;

            // Inicialização da data para o eixo X
            inc = Convert.ToInt64(Math.Floor((1 / Convert.ToDouble(sr)) * 1000 * (min)) * 10000);
            data = dataIni.AddTicks(inc);
            dataEsc = data;

            #region Seleção do sinal de entrada

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
                if (sr == 1)
                {
                    chartModes.Titles[0].Text = "Sinal de entrada: Módulo da Tensão - Fase " + phase.Phase + " - "
                        + Convert.ToString(sr) + " fasor/s";
                }
                else
                {
                    chartModes.Titles[0].Text = "Sinal de entrada: Módulo da Tensão - Fase " + phase.Phase + " - "
                        + Convert.ToString(sr) + " fasores/s";
                }

                // Inicialização do vetor que recebe o sinal de entrada para o Prony
                y = new Matrix(SelectedMeasurements.Length, tlRbe);
                // Nomes para o gráfico das energias
                nomesTitulo = new string[SelectedMeasurements.Length];

                for (int k = 0; k < SelectedMeasurements.Length; k++)
                {
                    nomesTitulo[k] = SelectedMeasurements[k].Terminal.DisplayName;

                    if (pu == 1)
                        vb = SelectedMeasurements[k].Terminal.VoltageLevel / Math.Sqrt(3);



                    for (int i = 0; i < tlRbe; i++)
                    {
                        y[k, i] = SelectedMeasurements[k].Series[phase].Reading(i+min) / vb;
                    }
                }
            }

            // Gráfico do módulo da corrente - FASE ÚNICA
            if ((opGraf == 12) || (opGraf == 13) || (opGraf == 14))
            {
                // Define qual das séries foi selecionada
                Channel phase = null;
                switch (opGraf)
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
                if (sr == 1)
                {
                    chartModes.Titles[0].Text = "Sinal de entrada: Módulo da Corrente - Fase " + phase.Phase + " - "
                        + Convert.ToString(sr) + " fasor/s";
                }
                else
                {
                    chartModes.Titles[0].Text = "Sinal de entrada: Módulo da Corrente - Fase " + phase.Phase + " - "
                        + Convert.ToString(sr) + " fasores/s";
                }

                // Inicialização do vetor que recebe o sinal de entrada para o Prony
                y = new Matrix(SelectedMeasurements.Length, tlRbe);
                // Nomes para o gráfico das energias
                nomesTitulo = new string[SelectedMeasurements.Length];

                for (int k = 0; k < SelectedMeasurements.Length; k++)
                {
                    nomesTitulo[k] = SelectedMeasurements[k].Terminal.DisplayName;
                    

                    for (int i = 0; i < tlRbe; i++)
                    {
                        y[k, i] = SelectedMeasurements[k].Series[phase].Reading(i+min) / ib;
                    }
                }
            }
            // Gráfico do módulo da tensão - Trifásico
            if (opGraf == 4)
            {
                Channel[] phases = new Channel[3];

                phases[0] = Channel.VOLTAGE_A_MOD;
                phases[1] = Channel.VOLTAGE_B_MOD;
                phases[2] = Channel.VOLTAGE_C_MOD;

                // Nomes para o gráfico das energias
                nomesTitulo = new string[1];
                nomesTitulo[0] = SelectedMeasurements[0].Terminal.DisplayName;

                // Título do gráfico
                if (sr== 1)
                {
                    chartModes.Titles[0].Text = "Sinal de entrada: Módulo da Tensão - " + SelectedMeasurements[0].Terminal.DisplayName + " - "
                        + Convert.ToString(sr) + " fasor/s";
                }
                else
                {
                    chartModes.Titles[0].Text = "Sinal de entrada: Módulo da Tensão - " + SelectedMeasurements[0].Terminal.DisplayName + " - "
                        + Convert.ToString(sr) + " fasores/s";
                }


                // Inicialização do vetor que recebe o sinal de entrada para o Prony
                y = new Matrix(3, tlRbe);

                for (int k = 0; k < 3; k++)
                {

                    if (pu == 1)
                        vb = SelectedMeasurements[0].Terminal.VoltageLevel / Math.Sqrt(3);

                    for (int i = 0; i < tlRbe; i++)
                    {
                        y[k, i] = SelectedMeasurements[0].Series[phases[k]].Reading(i+min) / vb;
                    }
                }
            }
            // Gráfico do módulo da corrente - Trifásico
            if (opGraf == 15)
            {
                Channel[] phases = new Channel[3];

                phases[0] = Channel.CURRENT_A_MOD;
                phases[1] = Channel.CURRENT_B_MOD;
                phases[2] = Channel.CURRENT_C_MOD;

                // Nomes para o gráfico das energias
                nomesTitulo = new string[1];
                nomesTitulo[0] = SelectedMeasurements[0].Terminal.DisplayName;

                // Título do gráfico
                if (sr == 1)
                {
                    chartModes.Titles[0].Text = "Sinal de entrada: Módulo da Corrente - " + SelectedMeasurements[0].Terminal.DisplayName + " - "
                        + Convert.ToString(sr) + " fasor/s";
                }
                else
                {
                    chartModes.Titles[0].Text = "Sinal de entrada: Módulo da Corrente - " + SelectedMeasurements[0].Terminal.DisplayName + " - "
                        + Convert.ToString(sr) + " fasores/s";
                }


                // Inicialização do vetor que recebe o sinal de entrada para o Prony
                y = new Matrix(3, tlRbe);


                for (int k = 0; k < 3; k++)
                {

                    for (int i = 0; i < tlRbe; i++)
                    {
                        y[k, i] = SelectedMeasurements[0].Series[phases[k]].Reading(i + min) / ib;
                    }

                }
            }
            // Gráfico do módulo da tensão - Seq. Positiva
            if (opGraf == 5)
            {
                // Título do gráfico
                if (sr == 1)
                {
                    chartModes.Titles[0].Text = "Sinal de entrada: Módulo da Tensão - Sequência Positiva" + " - "
                        + Convert.ToString(sr) + " fasor/s";
                }
                else
                {
                    chartModes.Titles[0].Text = "Sinal de entrada: Módulo da Tensão - Sequência Positiva" + " - "
                        + Convert.ToString(sr) + " fasores/s";
                }

                // Inicialização do vetor que recebe o sinal de entrada para o Prony
                y = new Matrix(SelectedMeasurements.Length, tlRbe);
                // Nomes para o gráfico das energias
                nomesTitulo = new string[SelectedMeasurements.Length];

                for (int k = 0; k < SelectedMeasurements.Length; k++)
                {
                    nomesTitulo[k] = SelectedMeasurements[k].Terminal.DisplayName;

                    if (pu == 1)
                        vb = SelectedMeasurements[k].Terminal.VoltageLevel / Math.Sqrt(3);
                    

                    for (int i = 0; i < tlRbe; i++)
                    {
                        y[k, i] = SelectedMeasurements[k].Series[Channel.VOLTAGE_POS_MOD].Reading(i+min) / vb;
                    }
                }
            }
            // Gráfico do módulo da corrente - Seq. Positiva
            if (opGraf == 16)
            {
                // Título do gráfico
                if (sr == 1)
                {
                    chartModes.Titles[0].Text = "Sinal de entrada: Módulo da Corrente - Sequência Positiva" + " - "
                        + Convert.ToString(sr) + " fasor/s";
                    chartModes.Titles[0].Text = "Sinal de entrada: Módulo da Corrente - Sequência Positiva" + " - "
                        + Convert.ToString(sr) + " fasor/s";
                }
                else
                {
                    chartModes.Titles[0].Text = "Sinal de entrada: Módulo da Corrente - Sequência Positiva" + " - "
                        + Convert.ToString(sr) + " fasores/s";
                    chartModes.Titles[0].Text = "Sinal de entrada: Módulo da Corrente - Sequência Positiva" + " - "
                        + Convert.ToString(sr) + " fasores/s";
                }

                // Inicialização do vetor que recebe o sinal de entrada para o Prony
                y = new Matrix(SelectedMeasurements.Length, tlRbe);
                // Nomes para o gráfico das energias
                nomesTitulo = new string[SelectedMeasurements.Length];

                for (int k = 0; k < SelectedMeasurements.Length; k++)
                {
                    nomesTitulo[k] = SelectedMeasurements[k].Terminal.DisplayName;


                    for (int i = 0; i < tlRbe; i++)
                    {
                        y[k, i] = SelectedMeasurements[k].Series[Channel.CURRENT_POS_MOD].Reading(i+min) / ib;
                    }
                }
            }
            // Gráfico da Frequência
            if (opGraf == 6)
            {

            }

            // Gráfico da diferença angular da tensão - FASE ÚNICA
            if ((opGraf == 7) || (opGraf == 8) || (opGraf == 9))
            {
                Channel phase = null;

                switch (opGraf)
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

                // Título do gráfico
                if (sr == 1)
                {
                    chartModes.Titles[0].Text = "Sinal de entrada: Diferença Angular da Tensão - Fase " + phase.Phase + " - Ref.: " +
                    Reference.Terminal.DisplayName + " - "
                        + Convert.ToString(sr) + " fasor/s";
                }
                else
                {
                    chartModes.Titles[0].Text = "Sinal de entrada: Diferença Angular da Tensão - Fase " + phase.Phase + " - Ref.: " +
                    Reference.Terminal.DisplayName + " - "
                        + Convert.ToString(sr) + " fasores/s";
                }

                // Inicialização do vetor que recebe o sinal de entrada para o Prony
                y = new Matrix(SelectedMeasurements.Length-1, tlRbe);
                // Nomes para o gráfico das energias
                nomesTitulo = new string[SelectedMeasurements.Length - 1];

                // Vetor que acumula as diferenças, utilizado para traçar o gráfico
                double[] vetorDifs = new double[Reference.Series[Channel.MISSING].Count];

                // Contador para as séries
                int n = 0;
                // Plota o número de selecionados menos um, já que um dos terminais é a referência
                for (int k = 0; k < SelectedMeasurements.Length; k++)
                {
                    if (SelectedMeasurements[k] != Reference)
                    {
                        nomesTitulo[n] = SelectedMeasurements[k].Terminal.DisplayName;

                        double dif, difback; // diferença angular e o backup deste valor (para o caso de perda de dados)
                        difback = 0;
                        int cwh = 0; // contador para o while

                        for (int i = 0; i < Reference.Series[Channel.MISSING].Count; i++)
                        {

                            // Diferença angular
                            dif = SelectedMeasurements[k].Series[phase].Reading(i) -
                                Reference.Series[phase].Reading(i);

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
                                    dif = SelectedMeasurements[k].Series[phase].Reading(cwh) - Reference.Series[phase].Reading(cwh);
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

                        // Preenche a matriz com as diferenças angulares
                        for (int i = 0; i < tlRbe; i++)
                        {
                            y[n, i] = vetorDifs[i + min];
                        }
                        n++;
                    }
                }
            }
            // Gráfico da diferença angular da corrente - FASE ÚNICA
            if ((opGraf == 17) || (opGraf == 18) || (opGraf == 19))
            {
                Channel phase = null;

                switch (opGraf)
                {
                    case 17:
                        phase = Channel.CURRENT_A_ANG;
                        break;
                    case 18:
                        phase = Channel.CURRENT_B_ANG;
                        break;
                    case 19:
                        phase = Channel.CURRENT_C_ANG;
                        break;
                    default:
                        break;
                }

                // Título do gráfico
                if (sr == 1)
                {
                    chartModes.Titles[0].Text = "Sinal de entrada: Diferença Angular da Corrente - Fase " + phase.Phase + " - Ref.: " +
                    Reference.Terminal.DisplayName + " - "
                        + Convert.ToString(sr) + " fasor/s";
                }
                else
                {
                    chartModes.Titles[0].Text = "Sinal de entrada: Diferença Angular da Corrente - Fase " + phase.Phase + " - Ref.: " +
                    Reference.Terminal.DisplayName + " - "
                        + Convert.ToString(sr) + " fasores/s";
                }

                // Inicialização do vetor que recebe o sinal de entrada para o Prony
                y = new Matrix(SelectedMeasurements.Length-1, tlRbe);
                // Nomes para o gráfico das energias
                nomesTitulo = new string[SelectedMeasurements.Length - 1];

                // Vetor que acumula as diferenças, utilizado para traçar o gráfico
                double[] vetorDifs = new double[Reference.Series[Channel.MISSING].Count];

                // Contador para as séries
                int n = 0;
                // Plota o número de selecionados menos um, já que um dos terminais é a referência
                for (int k = 0; k < Reference.Series[Channel.MISSING].Count; k++)
                {
                    if (SelectedMeasurements[k] != Reference)
                    {
                        nomesTitulo[n] = SelectedMeasurements[k].Terminal.DisplayName;

                        double dif, difback; // diferença angular e o backup deste valor (para o caso de perda de dados)
                        difback = 0;
                        int cwh = 0; // contador para o while

                        for (int i = 0; i < Reference.Series[Channel.MISSING].Count; i++) // tamanho dos sinais que serão traçados
                        {

                            ///////////////////////////////////////////////////////////////////////////////////////////
                            // Cálculo da diferença angular

                            // Diferença angular
                            dif = SelectedMeasurements[k].Series[phase].Reading(i) -
                                Reference.Series[phase].Reading(i);


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
                                    dif = SelectedMeasurements[k].Series[phase].Reading(cwh) - Reference.Series[phase].Reading(cwh);
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
                        for (int i = 0; i < tlRbe; i++)
                        {
                            y[n, i] = vetorDifs[i + min];
                        }
                        n++;
                    }
                }
            }
            // Gráfico da diferença angular da tensão - Sequência Positiva
            if (opGraf == 10)
            {
                // Título do gráfico
                if (sr == 1)
                {
                    chartModes.Titles[0].Text = "Sinal de entrada: Diferença Angular da Tensão - Sequência Positiva - Ref.: " +
                    Reference.Terminal.DisplayName + " - "
                        + Convert.ToString(sr) + " fasor/s";
                }
                else
                {
                    chartModes.Titles[0].Text = "Sinal de entrada: Diferença Angular da Tensão - Sequência Positiva - Ref.: " +
                    Reference.Terminal.DisplayName + " - "
                        + Convert.ToString(sr) + " fasores/s";

                }

                // Inicialização do vetor que recebe o sinal de entrada para o Prony
                y = new Matrix(SelectedMeasurements.Length-1, tlRbe);
                // Nomes para o gráfico das energias
                nomesTitulo = new string[SelectedMeasurements.Length - 1];

                // Vetor que acumula as diferenças, utilizado para traçar o gráfico
                double[] vetorDifs = new double[Reference.Series[Channel.MISSING].Count];

                // Contador para as séries
                int n = 0;
                // Plota o número de selecionados menos um, já que um dos terminais é a referência
                for (int k = 0; k < SelectedMeasurements.Length; k++)
                {
                    if (SelectedMeasurements[k] != Reference)
                    {
                        nomesTitulo[n] = SelectedMeasurements[k].Terminal.DisplayName;

                        double dif, difback; // diferença angular e o backup deste valor (para o caso de perda de dados)
                        difback = 0;
                        int cwh = 0; // contador para o while

                        for (int i = 0; i < Reference.Series[Channel.MISSING].Count; i++) // tamanho dos sinais que serão traçados
                        {

                            ///////////////////////////////////////////////////////////////////////////////////////////
                            // Cálculo da diferença angular

                            // Diferença angular
                            dif = SelectedMeasurements[k].Series[Channel.VOLTAGE_POS_ANG].Reading(i) -
                                Reference.Series[Channel.VOLTAGE_POS_ANG].Reading(i);


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
                                    dif = SelectedMeasurements[k].Series[Channel.VOLTAGE_POS_ANG].Reading(cwh) - Reference.Series[Channel.VOLTAGE_POS_ANG].Reading(cwh);
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

                        // Preenche a matriz com as diferenças angulares
                        for (int i = 0; i < tlRbe; i++)
                        {
                            y[n, i] = vetorDifs[i + min];
                        }
                        n++;
                    }
                }
            }
            // Gráfico da diferença angular da corrente - Sequência Positiva
            if (opGraf == 20)
            {
                // Título do gráfico
                if (sr == 1)
                {
                    chartModes.Titles[0].Text = "Sinal de entrada: Diferença Angular da Corrente - Sequência Positiva - Ref.: " +
                    Reference.Terminal.DisplayName + " - "
                        + Convert.ToString(sr) + " fasor/s";
                }
                else
                {
                    chartModes.Titles[0].Text = "Sinal de entrada: Diferença Angular da Corrente - Sequência Positiva - Ref.: " +
                    Reference.Terminal.DisplayName + " - "
                        + Convert.ToString(sr) + " fasores/s";
                }

                // Inicialização do vetor que recebe o sinal de entrada para o Prony
                y = new Matrix(SelectedMeasurements.Length-1, tlRbe);
                // Nomes para o gráfico das energias
                nomesTitulo = new string[SelectedMeasurements.Length - 1];

                // Vetor que acumula as diferenças, utilizado para traçar o gráfico
                double[] vetorDifs = new double[Reference.Series[Channel.MISSING].Count];

                // Contador para as séries
                int n = 0;
                // Plota o número de selecionados menos um, já que um dos terminais é a referência
                for (int k = 0; k < SelectedMeasurements.Length; k++)
                {
                    if (SelectedMeasurements[k] != Reference)
                    {
                        nomesTitulo[n] = SelectedMeasurements[k].Terminal.DisplayName;

                        double dif, difback; // diferença angular e o backup deste valor (para o caso de perda de dados)
                        difback = 0;
                        int cwh = 0; // contador para o while

                        for (int i = 0; i < Reference.Series[Channel.MISSING].Count; i++) // tamanho dos sinais que serão traçados
                        {

                            ///////////////////////////////////////////////////////////////////////////////////////////
                            // Cálculo da diferença angular

                            // Diferença angular
                            dif = SelectedMeasurements[k].Series[Channel.CURRENT_POS_ANG].Reading(i) -
                                Reference.Series[Channel.CURRENT_POS_ANG].Reading(i);


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
                                    dif = SelectedMeasurements[k].Series[Channel.CURRENT_POS_ANG].Reading(cwh) - Reference.Series[Channel.CURRENT_POS_ANG].Reading(cwh);
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

                        // Preenche a matriz com as diferenças angulares
                        for (int i = 0; i < tlRbe; i++)
                        {
                            y[n, i] = vetorDifs[i + min];
                        }
                        n++;
                    }
                }
            }
            // Gráfico da potência ativa trifásica
            if (opGraf == 21)
            {
                // Título do gráfico
                if (sr == 1)
                {
                    chartModes.Titles[0].Text = "Sinal de entrada: Potência Ativa" + " - "
                        + Convert.ToString(sr) + " fasor/s";
                }
                else
                {
                    chartModes.Titles[0].Text = "Sinal de entrada: Potência Ativa" + " - "
                        + Convert.ToString(sr) + " fasores/s";
                }

                // Inicialização do vetor que recebe o sinal de entrada para o Prony
                y = new Matrix(SelectedMeasurements.Length, tlRbe);
                // Nomes para o gráfico das energias
                nomesTitulo = new string[SelectedMeasurements.Length];

                for (int k = 0; k < SelectedMeasurements.Length; k++)
                {
                    nomesTitulo[k] = SelectedMeasurements[k].Terminal.DisplayName;

                    for (int i = 0; i < tlRbe; i++)
                    {
                        y[k, i] = SelectedMeasurements[k].Series[Channel.ACTIVE_POWER].Reading(i + min) / 1e6;
                    }
                }

            }
            // Gráfico da potência reativa trifásica
            if (opGraf == 22)
            {
                // Título do gráfico
                if (sr == 1)
                {
                    chartModes.Titles[0].Text = "Sinal de entrada: Potência Reativa" + " - "
                        + Convert.ToString(sr) + " fasor/s";
                }
                else
                {
                    chartModes.Titles[0].Text = "Sinal de entrada: Potência Reativa" + " - "
                        + Convert.ToString(sr) + " fasores/s";
                }

                // Inicialização do vetor que recebe o sinal de entrada para o Prony
                y = new Matrix(SelectedMeasurements.Length, tlRbe);
                // Nomes para o gráfico das energias
                nomesTitulo = new string[SelectedMeasurements.Length];

                for (int k = 0; k < SelectedMeasurements.Length; k++)
                {
                    nomesTitulo[k] = SelectedMeasurements[k].Terminal.DisplayName;

                    for (int i = 0; i < tlRbe; i++)
                    {
                        y[k, i] = SelectedMeasurements[k].Series[Channel.REACTIVE_POWER].Reading(i + min) / 1e6;
                    }
                }

            }
            // Gráfico da frequência
            if (opGraf == 11)
            {
                // Título do gráfico
                if (sr == 1)
                {
                    chartModes.Titles[0].Text = "Sinal de entrada: Frequência" + " - "
                        + Convert.ToString(sr) + " fasor/s";
                }
                else
                {
                    chartModes.Titles[0].Text = "Sinal de entrada: Frequência" + " - "
                        + Convert.ToString(sr) + " fasores/s";
                    //chartModes.Titles[0].Text = "Input signal: Frequency" + " - "
                    //    + Convert.ToString(sr) + " phasors/s";
                }

                // Inicialização do vetor que recebe o sinal de entrada para o Prony
                y = new Matrix(SelectedMeasurements.Length, tlRbe);
                // Nomes para o gráfico das energias
                nomesTitulo = new string[SelectedMeasurements.Length];

                for (int k = 0; k < SelectedMeasurements.Length; k++)
                {
                    nomesTitulo[k] = SelectedMeasurements[k].Terminal.DisplayName;

                    for (int i = 0; i < tlRbe; i++)
                    {
                        y[k, i] = SelectedMeasurements[k].Series[Channel.FREQ].Reading(i + min);
                    }
                }
            }
            // Gráfico da variação de frequência
            if (opGraf == 23)
            {
                // Título do gráfico
                if (sr == 1)
                {
                    chartModes.Titles[0].Text = "Sinal de entrada: Variação de Frequência" + " - "
                        + Convert.ToString(sr) + " fasor/s";
                }
                else
                {
                    chartModes.Titles[0].Text = "Sinal de entrada: Variação de Frequência" + " - "
                        + Convert.ToString(sr) + " fasores/s";
                }

                // Inicialização do vetor que recebe o sinal de entrada para o Prony
                y = new Matrix(SelectedMeasurements.Length, tlRbe);
                // Nomes para o gráfico das energias
                nomesTitulo = new string[SelectedMeasurements.Length];

                for (int k = 0; k < SelectedMeasurements.Length; k++)
                {
                    nomesTitulo[k] = SelectedMeasurements[k].Terminal.DisplayName;

                    for (int i = 0; i < tlRbe; i++)
                    {
                        y[k, i] = SelectedMeasurements[k].Series[Channel.DFREQ].Reading(i+min);
                    }
                }
            }

            #endregion

            #region Configurações dos gráficos

            // Limpa os gráficos
            chartModes.Series.Clear();

            chartModes.Titles[1].Text = "Teste";

            // Vetor com os tipos de marcadores possíveis para as séries
            MarkerStyle[] markers = new MarkerStyle[9];
            markers[0] = MarkerStyle.Square;
            markers[1] = MarkerStyle.Star4;
            markers[2] = MarkerStyle.Diamond;
            markers[3] = MarkerStyle.Square;
            markers[4] = MarkerStyle.Star10;
            markers[5] = MarkerStyle.Star4;
            markers[6] = MarkerStyle.Star5;
            markers[7] = MarkerStyle.Star6;
            markers[8] = MarkerStyle.Triangle;

            Color[] colors = new Color[3];
            colors[0] = Color.Red;
            colors[1] = Color.Green;
            colors[2] = Color.Blue;

            #region Séries

            // Séries de frequência
            chartModes.Series.Add("Energia");
            chartModes.Series["Energia"].ChartType = SeriesChartType.FastPoint;
            chartModes.Series["Energia"].XValueType = ChartValueType.DateTime;
            chartModes.Series["Energia"].Color = colors[0]; //Color.FromArgb(2 * i, 2 * (1 - i), 0);
            chartModes.Series["Energia"].MarkerStyle = markers[0];
            chartModes.Series["Energia"].MarkerSize = 6;

            // IDM
            chartModes.Series.Add("IDM");
            chartModes.Series["IDM"].ChartType = SeriesChartType.FastPoint;
            chartModes.Series["IDM"].XValueType = ChartValueType.DateTime;
            chartModes.Series["IDM"].Color = colors[1]; //Color.FromArgb(2 * i, 2 * (1 - i), 0);
            chartModes.Series["IDM"].MarkerStyle = markers[1];
            chartModes.Series["IDM"].MarkerSize = 7;

            // Séries de taxa de amortecimento
            chartModes.Series.Add("Energia - DR");
            chartModes.Series["Energia - DR"].ChartType = SeriesChartType.FastPoint;
            chartModes.Series["Energia - DR"].XValueType = ChartValueType.DateTime;
            chartModes.Series["Energia - DR"].Color = colors[0]; //Color.FromArgb(2 * i, 2 * (1 - i), 0);
            chartModes.Series["Energia - DR"].MarkerStyle = markers[0];
            chartModes.Series["Energia - DR"].MarkerSize = 6;
            chartModes.Series["Energia - DR"].IsVisibleInLegend = false;
            chartModes.Series["Energia - DR"].ChartArea = "ChartArea2";

            chartModes.Series.Add("IDM - DR");
            chartModes.Series["IDM - DR"].ChartType = SeriesChartType.FastPoint;
            chartModes.Series["IDM - DR"].XValueType = ChartValueType.DateTime;
            chartModes.Series["IDM - DR"].Color = colors[1]; //Color.FromArgb(2 * i, 2 * (1 - i), 0);
            chartModes.Series["IDM - DR"].MarkerStyle = markers[1];
            chartModes.Series["IDM - DR"].MarkerSize = 7;
            chartModes.Series["IDM - DR"].IsVisibleInLegend = false;
            chartModes.Series["IDM - DR"].ChartArea = "ChartArea2";

            #endregion

            // Início e fim dos eixos temporais dos gráficos
            chartModes.ChartAreas[0].AxisX.Minimum = dataEsc.ToOADate();
            chartModes.ChartAreas[0].AxisX.Maximum = dataIni.AddTicks(Convert.ToInt64(Math.Floor((1 / Convert.ToDouble(sr)) * 1000 * (max)) * 10000)).ToOADate();
            chartModes.ChartAreas[1].AxisX.Minimum = dataEsc.ToOADate();
            chartModes.ChartAreas[1].AxisX.Maximum = dataIni.AddTicks(Convert.ToInt64(Math.Floor((1 / Convert.ToDouble(sr)) * 1000 * (max)) * 10000)).ToOADate();

            // Título 2 do gráfico, nomes dos terminais
            chartModes.Titles[1].Text = "Terminais: " + string.Join(", ", nomesTitulo);

            // Define o título do eixo Y
            chartModes.ChartAreas[0].AxisY.Title = "Frequência (Hz)";
            // Define o título do eixo Y
            chartModes.ChartAreas[1].AxisY.Title = "Taxa de amort. (%)";
            
            // Define o título do eixo dos tempos
            chartModes.ChartAreas[0].AxisX.Title = "Tempo (UTC) - Data: " + dataIni.Day + "/" + dataIni.Month
                + "/" + dataIni.Year;
            // Define o título do eixo dos tempos
            chartModes.ChartAreas[1].AxisX.Title = "Tempo (UTC) - Data: " + dataIni.Day + "/" + dataIni.Month
                + "/" + dataIni.Year;

            chartModes.ChartAreas[0].AxisX.IsMarginVisible = false;
            chartModes.ChartAreas[1].AxisX.IsMarginVisible = false;

            // Intervalo do eixo Y
            chartModes.ChartAreas[0].AxisY.Minimum = fmin;
            chartModes.ChartAreas[0].AxisY.Maximum = fmax;

            //chartModes.ChartAreas[1].AxisY.Minimum = 0;
            chartModes.ChartAreas[1].AxisY.Maximum = 30;


            // Formato das labels no eixo X
            if (sr == 1)
            {
                chartModes.ChartAreas[0].AxisX.LabelStyle.Format = "HH:mm:ss"; // HH maiúculo para que a hora fique de 0 a 24h
                chartModes.ChartAreas[1].AxisX.LabelStyle.Format = "HH:mm:ss"; // HH maiúculo para que a hora fique de 0 a 24h                
            }
            else if (sr == 10)
            {
                chartModes.ChartAreas[0].AxisX.LabelStyle.Format = "HH:mm:ss.f";
                chartModes.ChartAreas[1].AxisX.LabelStyle.Format = "HH:mm:ss.f";
            }
            else
            {
                chartModes.ChartAreas[0].AxisX.LabelStyle.Format = "HH:mm:ss.fff";
                chartModes.ChartAreas[1].AxisX.LabelStyle.Format = "HH:mm:ss.fff";
            }
            chartModes.ChartAreas[0].AxisX.LabelStyle.IsEndLabelVisible = true;
            chartModes.ChartAreas[1].AxisX.LabelStyle.IsEndLabelVisible = true;
            chartModes.ChartAreas[0].AxisY.LabelStyle.Format = "0.#####";
            chartModes.ChartAreas[1].AxisY.LabelStyle.Format = "0.#####";

            #endregion

            #endregion

            // Escrever arquivo com as informações do sinal de entrada
            // Arquivo relativo à ordenação por energia
            //try
            //{
            //    // Objeto de escrita para o arquivo especificado
            //    StreamWriter writer = new StreamWriter(Path.GetDirectoryName(System.Reflection.
            //        Assembly.GetExecutingAssembly().Location) + "\\teste.txt");

            //    // Variáveis para a escrita, linha por linha
            //    StringWriter sw = new StringWriter();

            //    #region Dados
            //    for (int i = 0; i < y.ColumnCount; i++)
            //    {
            //        writer.WriteLine(String.Format(CultureInfo.InvariantCulture, "{0:0.000000}", y[0, i]) + String.Format(CultureInfo.InvariantCulture, "{0:0.000000}", y[1, i]).PadLeft(20));
            //    }
            //    #endregion

            //    writer.Close();
            //}
            //catch (Exception ex)
            //{

            //}


            if (backgroundWorker1.IsBusy != true)
            {
                object[] argumentos = { tlJanDes, data };

                // Start the asynchronous operation.
                backgroundWorker1.RunWorkerAsync(argumentos);
            }         

        }
        
        private void chartModes_DoubleClick(object sender, EventArgs e)
        {
            chartModes.ChartAreas[0].AxisX.ScaleView.ZoomReset();
            chartModes.ChartAreas[0].AxisY.ScaleView.ZoomReset();
        }

        private void chartModes_KeyDown(object sender, KeyEventArgs e)
        {
            // Código para copiar o gráfico para a área de trabalho

            // Verifica se o usuário pressionou as teclas "Ctrl + C" com o form em questão ativo 
            if (e.Control && e.KeyCode == Keys.C)
            {
                // Realça o contorno do gráfico
                chartModes.BorderlineDashStyle = ChartDashStyle.Dash;

                MemoryStream ms = new MemoryStream();

                chartModes.SaveImage(ms, ChartImageFormat.Bmp);
                Bitmap bm = new Bitmap(ms);
                bm.SetResolution(1024, 1024);
                // Disponibiliza o bitmap na área de trabalho
                Clipboard.SetImage(bm);
            }
        }

        private bool StartsReport()
        {
            try
            {

                //(Re)inicializa o contador de intervalos para escrita na linha correta no Excel
                var energyCounter = 0;
                var idmCounter = 0;

                excelApp = new Excel.Application();

                //Get a new workbook.
                excelBook = excelApp.Workbooks.Add(Missing.Value);
                // Adiciona uma nova planilha ao documento
                excelBook.Worksheets.Add();

                #region Energia Sheet
                // Seleciona a primeira planilha na lista
                excelSheet = excelBook.Worksheets[1];
                // Renomeia a planilha
                excelSheet.Name = "Energia";

                Excel.Range range;

                // 1ª linha
                excelSheet.Cells[1, 1] = chartModes.Titles[0].Text;
                excelSheet.get_Range("a1", "d1").Merge();
                excelSheet.Cells[1, 1].Font.Bold = true;
                excelSheet.Cells[2, 1] = "Terminais: " + string.Join(", ", nomesTitulo);
                excelSheet.get_Range("a2", "d2").Merge();
                excelSheet.Cells[2, 1].Font.Bold = true;

                excelSheet.Cells[1, 6] = "Ordem do modelo";
                excelSheet.Cells[1, 6].Font.Bold = true;
                excelSheet.Cells[2, 6] = dim;

                excelSheet.Cells[1, 7] = "Numero de Blocos";
                excelSheet.Cells[1, 7].Font.Bold = true;
                excelSheet.Cells[2, 7] = nbr;

                excelSheet.Cells[1, 8] = "Tamanho da Janela (min)";
                excelSheet.Cells[1, 8].Font.Bold = true;
                excelSheet.Cells[2, 8] = wl;

                excelSheet.Cells[1, 9] = "Passo da Janela (seg)";
                excelSheet.Cells[1, 9].Font.Bold = true;
                excelSheet.Cells[2, 9] = ws;

                excelSheet.Cells[1, 10] = "Frequência mínima (Hz)";
                excelSheet.Cells[1, 10].Font.Bold = true;
                excelSheet.Cells[2, 10] = fmin;


                excelSheet.Cells[1, 11] = "Frequência máxima (Hz)";
                excelSheet.Cells[1, 11].Font.Bold = true;
                excelSheet.Cells[2, 11] = fmax;

                // 1ª coluna
                excelSheet.Cells[4, 1] = "Tempo (UTC)";
                excelSheet.Cells[4, 1].Font.Bold = true;
                // Seleciona a célula a1
                range = excelSheet.get_Range("a2");
                // Formata a 1ª coluna inteira como data
                range.EntireColumn.NumberFormat = "@";
                // 2ª coluna
                excelSheet.Cells[4, 2] = "Frequência (Hz)";
                excelSheet.Cells[4, 2].Font.Bold = true;
                range = excelSheet.get_Range("b2");
                range.EntireColumn.NumberFormat = "0.0000";
                // 3ª coluna
                excelSheet.Cells[4, 3] = "Amortecimento (%)";
                excelSheet.Cells[4, 3].Font.Bold = true;
                range = excelSheet.get_Range("c2");
                range.EntireColumn.NumberFormat = "0.00";
                // 4ª coluna
                excelSheet.Cells[4, 4] = "Energia";
                excelSheet.Cells[4, 4].Font.Bold = true;
                range = excelSheet.get_Range("d2");
                range.EntireColumn.NumberFormat = "0.000000";



                //////////////////////////////////////////////////////////////
                ///chartModes.Series["Energia"]
                for (int i = 0; i < chartModes.Series["Energia"].Points.Count; i++)
                {
                    excelSheet.Cells[energyCounter + 5, 1] = DateTime.FromOADate(chartModes.Series["Energia"].Points[i].XValue).ToString("dd-MM-yyyy HH:mm:ss.fff");
                    excelSheet.Cells[energyCounter + 5, 2] = chartModes.Series["Energia"].Points[i].YValues[0];
                    excelSheet.Cells[energyCounter + 5, 3] = chartModes.Series["Energia - DR"].Points[i].YValues[0];
                    excelSheet.Cells[energyCounter + 5, 4] = energy[i];
                    energyCounter++;
                }
                //////////////////////////////////////////////////////////////

                // Alterar largura da coluna de acordo com o conteúdo
                excelSheet.Columns.AutoFit();
                #endregion



                //UnixTimeStampToDateTime(SPMS.Instance.AnalysisStartTimestamp);


                #region IDM Sheet
                // Seleciona a segunda planilha na lista
                excelSheet = excelBook.Worksheets[2];
                // Renomeia a planilha
                excelSheet.Name = "IDM";

                //1ª linha
                excelSheet.Cells[1, 1] = chartModes.Titles[0].Text;
                excelSheet.get_Range("a1", "h1").Merge();
                excelSheet.Cells[1, 1].Font.Bold = true;
                excelSheet.Cells[2, 1] = "Terminais: " + string.Join(", ", nomesTitulo);
                excelSheet.get_Range("a2", "h2").Merge();
                excelSheet.Cells[2, 1].Font.Bold = true;
                //excelSheet.get_Range("c1", (Excel.Range)excelSheet.Cells[1, 2 + SPMS.Instance.StnName.Count()]).Merge();
                //excelSheet.get_Range("c1", "g1").Merge();
                //excelSheet.get_Range("a1").EntireRow.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;

                // 1ª coluna
                excelSheet.Cells[4, 1] = "Tempo (UTC)";
                excelSheet.Cells[4, 1].Font.Bold = true;
                // Seleciona a célula a1
                range = excelSheet.get_Range("a2");
                // Formata a 1ª coluna inteira como data
                range.EntireColumn.NumberFormat = "@";
                // 2ª coluna
                excelSheet.Cells[4, 2] = "Frequência (Hz)";
                excelSheet.Cells[4, 2].Font.Bold = true;
                range = excelSheet.get_Range("b2");
                range.EntireColumn.NumberFormat = "0.0000";
                // 3ª coluna
                excelSheet.Cells[4, 3] = "Amortecimento (%)";
                excelSheet.Cells[4, 3].Font.Bold = true;
                range = excelSheet.get_Range("c2");
                range.EntireColumn.NumberFormat = "0.00";
                // 4ª coluna
                excelSheet.Cells[4, 4] = "IDM";
                excelSheet.Cells[4, 4].Font.Bold = true;
                range = excelSheet.get_Range("d2");
                range.EntireColumn.NumberFormat = "0.000000";

                ////////////////////////////////////////////////////////////////
                for (int i = 0; i < chartModes.Series["IDM"].Points.Count; i++)
                {
                    excelSheet.Cells[idmCounter + 5, 1] = DateTime.FromOADate(chartModes.Series["IDM"].Points[i].XValue).ToString("dd-MM-yyyy HH:mm:ss.fff");
                    excelSheet.Cells[idmCounter + 5, 2] = chartModes.Series["IDM"].Points[i].YValues[0];
                    excelSheet.Cells[idmCounter + 5, 3] = chartModes.Series["IDM - DR"].Points[i].YValues[0];
                    excelSheet.Cells[idmCounter + 5, 4] = idm[i];
                    idmCounter++;
                }
                ////////////////////////////////////////////////////////////////

                // Alterar largura da coluna de acordo com o conteúdo
                excelSheet.Columns.AutoFit();
                #endregion


                Directory.CreateDirectory(Path.Combine(Properties.Settings.Default.OscillationFolder, Query.System.Name));
                // Salva o workbook
                excelBook.SaveAs(Path.Combine(Properties.Settings.Default.OscillationFolder, Query.System.Name, SelectedMeasurements[0].Start.ToString("yyyyMMdd_HHmmss_") + SelectedMeasurements[0].Finish.ToString("yyyyMMdd_HHmmss_") + getFileName()));

                excelBook.Close();
                excelApp.Quit();


                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);

                //MessageBox.Show("Não foi possível criar o relatório no Microsoft Excel. A análise foi interrompida.", "MedPlot RT", MessageBoxButtons.OK);
                excelBook = null;
                excelSheet = null;

                return false;
            }
        }

        private string getFileName()
        {
            string tmp = "";
            foreach (string s in nomesTitulo)
                tmp += s + "_";

            if (chartModes.Titles[0].Text.Contains("Frequência"))
                tmp += "F";
            else if (chartModes.Titles[0].Text.Contains("Angular"))
            {
                string tmp2 = "";

                tmp2 = chartModes.Titles[0].Text.Substring(chartModes.Titles[0].Text.IndexOf(':') + 2);
                tmp2 = tmp2.Split(' ')[0];
                tmp += "DA_REF_" + tmp2;
            }

            return tmp;

        }

        private void chartModes_MouseDown(object sender, MouseEventArgs e)
        {
            chartModes.Focus();

            tip.IsBalloon = true;

            if (e.Button == MouseButtons.Right)
            {
                // Call Hit Test Method
                HitTestResult result = chartModes.HitTest(e.X, e.Y);

                if (result.ChartElementType == ChartElementType.DataPoint)
                {
                    // index of the clicked point in its series
                    tip.ToolTipTitle = result.Series.Name;

                    // Frequência
                    if (result.ChartArea.Name == "ChartArea1")
                    {
                        tip.Show("X = " + DateTime.FromOADate(Math.Round(chartModes.ChartAreas[0].AxisX.PixelPositionToValue(e.Location.X), 5)).ToString("HH:mm:ss")
                            + "\nY = " + Math.Round(chartModes.ChartAreas[0].AxisY.PixelPositionToValue(e.Location.Y), 5), chartModes, e.X, e.Y);
                        tip.Show("X = " + DateTime.FromOADate(Math.Round(chartModes.ChartAreas[0].AxisX.PixelPositionToValue(e.Location.X), 5)).ToString("HH:mm:ss")
                            + "\nY = " + Math.Round(chartModes.ChartAreas[0].AxisY.PixelPositionToValue(e.Location.Y), 5), chartModes, e.X, e.Y, 3000);
                    }
                    // Taxa de amortecimento
                    if (result.ChartArea.Name == "ChartArea2")
                    {
                        tip.Show("X = " + DateTime.FromOADate(Math.Round(chartModes.ChartAreas[1].AxisX.PixelPositionToValue(e.Location.X), 5)).ToString("HH:mm:ss")
                            + "\nY = " + Math.Round(chartModes.ChartAreas[1].AxisY.PixelPositionToValue(e.Location.Y), 5), chartModes, e.X, e.Y);
                        tip.Show("X = " + DateTime.FromOADate(Math.Round(chartModes.ChartAreas[1].AxisX.PixelPositionToValue(e.Location.X), 5)).ToString("HH:mm:ss")
                            + "\nY = " + Math.Round(chartModes.ChartAreas[1].AxisY.PixelPositionToValue(e.Location.Y), 5), chartModes, e.X, e.Y, 3000);
                    }
                }
                else if (result.ChartElementType == ChartElementType.LegendItem)
                {
                    tip.Hide(chartModes);

                    LegendItem leg = (LegendItem)result.Object;
                    string nomeLeg = leg.SeriesName;

                    // Se a cor foi escolhida
                    if (colorDialog1.ShowDialog() == DialogResult.OK)
                    {
                        chartModes.Series[nomeLeg].Color = colorDialog1.Color;
                        chartModes.Series[nomeLeg + " - DR"].Color = colorDialog1.Color;
                    }
                }
                else if (result.ChartElementType == ChartElementType.AxisLabels)
                {
                    // Área clicada
                    string area = result.ChartArea.Name;
                    string axis = result.Axis.AxisName.ToString();

                    AjustaCVA f = new AjustaCVA(this, chartModes, area, axis);
                    // Mostra sobreposto
                    f.ShowDialog(); 
                }
                else
                {
                    tip.Hide(chartModes);
                }
            }
            else
            {
                tip.Hide(chartModes);
            }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            // Objeto worker
            BackgroundWorker worker = sender as BackgroundWorker;

            // Recebendo as variáveis do obejto de argumentos
            object[] arg = (object[])e.Argument;

            int tlJanDes = (int)arg[0];
            DateTime data = (DateTime)arg[1];

            // Número de execuções do algoritmo RBE
            for (int count = 0; count < ne; count++)
            {
                if (!backgroundWorker1.CancellationPending)
                {
                    try
                    {
                        #region Algoritmo RBE

                        #region Pré-processamento dos dados de entrada

                        int p = y.RowCount;
                        
                        // Matriz auxiliar que corresponde a janela de dados
                        Matrix yJan = new Matrix(y.RowCount, tlJanDes);

                        for (int i = 0; i < p; i++)
                        {
                            for (int j = 0; j < tlJanDes; j++)
                            {
                                yJan[i, j] = y[i, j + count * ws * Convert.ToInt16(sr)];
                            }
                        }

                        // Pré-processa os dados de entrada
                        Matrix yProcessed = PreProcessesData(tlJanDes, yJan);

                        #endregion

                        #region Formação da matriz Y

                        //int Ndat = tlRbe;
                        //int Ndat = tlJanDes;
                        int Ndat = yProcessed.ColumnCount;

                        int N = Ndat - 2 * nbr;

                        Y = new Matrix(2 * nbr * p, N);

                        for (int i = 0; i < 2 * nbr; i++)
                        {
                            for (int j = 0; j < N; j++)
                            {
                                for (int t = 0; t < p; t++)
                                {
                                    //Y[i * p + t, j] = y[t, i + j];
                                    //Y[i * p + t, j] = y[t, i + j + count * ws * Convert.ToInt16(sr)];
                                    Y[i * p + t, j] = yProcessed[t, i + j];
                                }
                            }
                        }

                        Yp = new Matrix(nbr * p, N);
                        Yf = new Matrix(nbr * p, N);

                        for (int i = 0; i < nbr * p; i++)
                        {
                            for (int j = 0; j < N; j++)
                            {
                                Yp[i, j] = Y[i, j];
                                Yf[i, j] = Y[nbr * p + i, j];
                            }
                        }
                        #endregion

                        #region Decomposição QR

                        H = new Matrix(2 * nbr * p, N);

                        // 1ª metade de H
                        for (int i = 0; i < nbr * p; i++)
                        {
                            for (int j = 0; j < N; j++)
                            {
                                H[i, j] = Yp[i, j];
                            }
                        }
                        // 2ª metade de H
                        for (int i = 0; i < nbr * p; i++)
                        {
                            for (int j = 0; j < N; j++)
                            {
                                H[nbr * p + i, j] = Yf[i, j];
                            }
                        }

                        // Transposta de H
                        Htr = H.Transpose();

                        // Transformação da matriz em um vetor
                        double[] clonHtr = new double[Htr.RowCount * Htr.ColumnCount];
                        int mm = 0;

                        for (int i = 0; i < Htr.ColumnCount; i++)
                        {
                            for (int j = 0; j < Htr.RowCount; j++)
                            {
                                clonHtr[mm] = Htr[j, i];
                                mm++;
                            }
                        }

                        DGELS _dgels = new DGELS();
                        DGELQF _dgelqf = new DGELQF();
                        DGEQRF _dgeqrf = new DGEQRF();

                        int Info = 0; // se info é não negativa, houve sucesso na operação
                        //int LWork
                        double[] Work = new double[1];
                        double[] Tau = new double[Math.Min(Htr.RowCount, Htr.ColumnCount)];
                        int LWork = -1;

                        //_dgelqf.Run(2, 3, ref A, 0, 2, ref Tau, 0, ref Work, 0, LWork, ref Info);
                        _dgeqrf.Run(Htr.RowCount, Htr.ColumnCount, ref clonHtr, 0, Htr.RowCount, ref Tau, 0, ref Work, 0, LWork, ref Info);
                        ///////////////////////////////////////////////
                        LWork = Convert.ToInt32(Work[0]);
                        Work = new double[LWork];
                        //_dgelqf.Run(2, 3, ref A, 0, 2, ref Tau, 0, ref Work, 0, LWork, ref Info);
                        _dgeqrf.Run(Htr.RowCount, Htr.ColumnCount, ref clonHtr, 0, Htr.RowCount, ref Tau, 0, ref Work, 0, LWork, ref Info);

                        // Matriz L
                        L_aux = new Matrix(Htr.RowCount, Htr.ColumnCount);

                        mm = 0;
                        for (int i = 0; i < L_aux.ColumnCount; i++)
                        {
                            for (int j = 0; j < L_aux.RowCount; j++)
                            {
                                if (j > i)
                                    L_aux[j, i] = 0;
                                else
                                {
                                    L_aux[j, i] = clonHtr[mm];
                                }
                                mm++;
                            }
                        }

                        // Formar a matriz conforme a "economy-size decomposition" do Matlab
                        // [Q,L] = qr(H',0)
                        // If m > n  only the first n columns of Q and the first n rows of R are computed.
                        if (Htr.RowCount > Htr.ColumnCount)
                        {
                            L = new Matrix(Htr.ColumnCount, Htr.ColumnCount);
                            for (int i = 0; i < L.ColumnCount; i++)
                            {
                                for (int j = 0; j < L.RowCount; j++)
                                {
                                    L[j, i] = L_aux[j, i];
                                }
                            }
                        }
                        else
                        {
                            L = L_aux;
                        }

                        Ltr = L.Transpose();

                        for (int i = 0; i < Ltr.RowCount; i++)
                        {
                            for (int j = 0; j < Ltr.ColumnCount; j++)
                            {
                                Ltr[i, j] = Ltr[i, j] / Math.Sqrt(N);
                            }
                        }

                        L11 = new Matrix(nbr * p, nbr * p);
                        L21 = new Matrix(nbr * p, nbr * p);
                        L22 = new Matrix(nbr * p, nbr * p);

                        for (int i = 0; i < nbr * p; i++)
                        {
                            for (int j = 0; j < nbr * p; j++)
                            {
                                L11[i, j] = Ltr[i, j];
                                L21[i, j] = Ltr[nbr * p + i, j];
                                L22[i, j] = Ltr[nbr * p + i, nbr * p + j];
                            }
                        }

                        #endregion

                        #region Matrizes de co-variância

                        Rff = new Matrix(nbr, nbr);
                        Rfp = new Matrix(nbr, nbr);
                        Rpp = new Matrix(nbr, nbr);

                        Rff = L21 * L21.Transpose() + L22 * L22.Transpose();
                        Rfp = L21 * L11.Transpose();
                        Rpp = L11 * L11.Transpose();

                        #endregion

                        #region Raízes quadradas e inversas das matrizes

                        SingularValueDecomposition svd = new SingularValueDecomposition();
                        Matrix Uf, Sf, Vf;
                        svd.ComputeSVD(Rff, out Sf, out Uf, out Vf);
                        Matrix Up, Sp, Vp;
                        svd.ComputeSVD(Rpp, out Sp, out Up, out Vp);

                        Vector teste1 = new Vector(Sf.RowCount);
                        for (int i = 0; i < Sf.RowCount; i++)
                        {
                            teste1[i] = Sf[i, i];
                        }
                        Vector teste2 = new Vector(Sp.RowCount);
                        for (int i = 0; i < Sp.RowCount; i++)
                        {
                            teste2[i] = Sp[i, i];
                        }

                        // As matrizes Sf e Sp são diagonais, basta tirar a raiz quadrada dos números das diagonais
                        for (int i = 0; i < Sf.RowCount; i++)
                        {
                            Sf[i, i] = Math.Sqrt(Sf[i, i]);
                            Sp[i, i] = Math.Sqrt(Sp[i, i]);
                        }

                        T = Uf * Sf * Vf;

                        // ATENÇÃO: retirada, não era utilizada em lugar algum
                        //M = Up * Sp * Vp;

                        Matrix Sfi, Spi;

                        Sfi = Sf.Inverse();
                        Spi = Sp.Inverse();

                        Tinv = Vf.Transpose() * Sfi * Uf.Transpose();

                        Minv = Vp.Transpose() * Spi * Up.Transpose();

                        OC = Tinv * Rfp * Minv.Transpose();

                        Matrix UU, SS, VV;
                        svd.ComputeSVD(OC, out SS, out UU, out VV);

                        // ATENÇÃO: retirada, não era utilizada em lugar algum
                        //Lambda = new Matrix(p, p);
                        //for (int i = 0; i < p; i++)
                        //{
                        //    for (int j = 0; j < p; j++)
                        //    {
                        //        Lambda[i, j] = Rpp[i, j];
                        //    }
                        //}

                        Vector teste3 = new Vector(SS.RowCount);
                        for (int i = 0; i < SS.RowCount; i++)
                        {
                            //teste3[i] = SS[i, i];
                            teste3[i] = Math.Acos(SS[i, i])*180/Math.PI;
                        }                        

                        S = new Matrix(dim, dim);
                        for (int i = 0; i < dim; i++)
                        {
                            for (int j = 0; j < dim; j++)
                            {
                                S[i, j] = SS[i, j];
                            }
                        }

                        Matrix UU_aux = new Matrix(UU.RowCount, dim);

                        for (int i = 0; i < UU.RowCount; i++)
                        {
                            for (int j = 0; j < dim; j++)
                            {
                                UU_aux[i, j] = UU[i, j];
                            }
                        }

                        Ok = T * UU_aux * S.Sqrtm();

                        // Pseudoinversa: Hp = inv(H' * H) * H'
                        Matrix Ok_inv = (Ok.Transpose() * Ok) * Ok.Transpose();

                        Matrix X_est = Ok_inv * OC;

                        // ATENÇÃO: retirada, não era utilizada em lugar algum
                        //VV = VV.Transpose();

                        Matrix Ok_num = new Matrix(nbr * p - p, dim);
                        Matrix Ok_den = new Matrix(nbr * p - p, dim);

                        for (int i = 0; i < nbr * p - p; i++)
                        {
                            for (int j = 0; j < dim; j++)
                            {
                                Ok_num[i, j] = Ok[i, j];
                                Ok_den[i, j] = Ok[p + i, j];
                            }
                        }

                        #endregion

                        #region Cálculos dos modos de oscilação

                        LinearLeastSquares lls = new LinearLeastSquares();
                        Matrix A = lls.COFSolve(Ok_num, Ok_den);

                        // Matriz C, primeiras "p" linhas de Ok
                        ComplexMatrix C = new ComplexMatrix(p, Ok.ColumnCount);
                        for (int i = 0; i < p; i++)
                        {
                            for (int j = 0; j < Ok.ColumnCount; j++)
                            {
                                C[i, j] = new DotNumerics.Complex(Ok[i, j], 0);
                            }
                        }

                        EigenSystem es = new EigenSystem();
                        ComplexMatrix eigenVectors;
                        // Autovalores de A
                        ComplexMatrix eigenA_disc = es.GetEigenvalues(A, out eigenVectors);

                        // Autovetores à esquerda de "A"
                        EigenSystem es2 = new EigenSystem();
                        ComplexMatrix eigenVectors2;
                        // Autovalores de A
                        ComplexMatrix eigenA_disc2 = es2.GetEigenvalues(A.Transpose(), out eigenVectors2);

                        // Raízes são convertidas do modelo discreto para o modelo contínuo
                        ComplexMatrix eigenA_cont = new ComplexMatrix(dim, 1); // raízes no plano s
                        for (int i = 0; i < dim; i++)
                        {
                            // Log de um número complexo é obtido da seguinte forma
                            // Log(z) = ln(|z|) + iArg(z)
                            //eigenA_cont[i, 0] = new Complex((Math.Log(eigenA_disc[i, 0].Modulus) * sr),
                            //    ((eigenA_disc[i, 0].Argument) * sr));
                            eigenA_cont[i, 0] = new DotNumerics.Complex((Math.Log(eigenA_disc[i, 0].Modulus) * fDown),
                                ((eigenA_disc[i, 0].Argument) * fDown));
                        }

                        // Fator de amortecimento
                        Matrix dampFac = eigenA_cont.GetReal();

                        // Taxa de amortecimento
                        Vector dampRate = new Vector(VectorType.Column, dim);
                        //Vector dampRate2 = new Vector(VectorType.Column, dim);
                        for (int i = 0; i < dim; i++)
                        {
                            dampRate[i] = (-dampFac[i, 0] / eigenA_cont[i, 0].Modulus) * 100;
                            //dampRate2[i] = (-eigenA_cont[i, 0].Real / (Math.Sqrt(Math.Pow(-eigenA_cont[i, 0].Real, 2) + 
                            //    Math.Pow(eigenA_cont[i, 0].Imaginary, 2)))) * 100;
                        }

                        // cálculo das frequências para cada modo de oscilação
                        Vector freq = new Vector(VectorType.Column, dim);

                        for (int i = 0; i < dim; i++)
                        {
                            //freq[i] = eigenA_disc[i, 0].Argument / (2 * Math.PI * (1 / sr));
                            freq[i] = eigenA_disc[i, 0].Argument / (2 * Math.PI * (1 / fDown));
                        }

                        ComplexMatrix z = new ComplexMatrix(Ndat, dim);

                        for (int i = 0; i < Ndat; i++)
                        {
                            for (int j = 0; j < dim; j++)
                            {
                                // De Moivre's formula
                                DotNumerics.Complex aux = new DotNumerics.Complex(Math.Cos(i * eigenA_disc[j, 0].Argument), Math.Sin(i * eigenA_disc[j, 0].Argument));
                                z[i, j] = Math.Pow(eigenA_disc[j, 0].Modulus, i) * aux;
                            }
                        }
                        
                        #region Inversão da matriz complexa "z"

                        // Pseudoinversa: Hp = inv(H' * H) * H'

                        ComplexVector[] c = z.GetRowVectors();
                        // zt é a transposta de z
                        ComplexMatrix zt = new ComplexMatrix(dim, Ndat);

                        for (int i = 0; i < Ndat; i++)
                        {
                            ComplexVector temp = c[i];
                            for (int j = 0; j < dim; j++)
                            {
                                //zt[j, i] = temp[j];
                                zt[j, i] = temp[j].Conjugate;
                            }
                        }

                        ComplexMatrix P = zt * z;

                        Matrix Pre = P.GetReal();
                        Matrix Pim = P.GetImag();

                        Matrix Q = new Matrix(dim * 2, dim * 2);

                        for (int i = 0; i < dim; i++)
                        {
                            for (int j = 0; j < dim; j++)
                            {
                                Q[i, j] = Pre[i, j];
                                Q[i + dim, j] = -Pim[i, j];
                                Q[i, j + dim] = Pim[i, j];
                                Q[i + dim, j + dim] = Pre[i, j];
                            }
                        }

                        Matrix Q_inv = Q.Inverse();
                        // Matriz Q_cp é a reordenação de Q_inv na forma complexa
                        ComplexMatrix Q_cp = new ComplexMatrix(dim, dim);

                        for (int i = 0; i < dim; i++)
                        {
                            for (int j = 0; j < dim; j++)
                            {
                                Q_cp[i, j] = new DotNumerics.Complex(Q_inv[i, j], Q_inv[i, j + dim]);
                            }
                        }

                        // Pseudoinversa: Hp = inv(H' * H) * H'
                        // Neste algoritmo corresponde a: Z_inv = Q_cp*zt

                        ComplexMatrix z_inv = Q_cp * zt;

                        #endregion

                        #region Cálculo dos resíduos complexos

                        ComplexMatrix X = new ComplexMatrix(Ndat, p);

                        for (int j = 0; j < p; j++)
                        {
                            for (int i = 0; i < Ndat; i++)
                            {
                                //X[i, j] = new Complex(y[j][i], 0);
                                //X[i, j] = new Complex(y[j, i + count * ws * Convert.ToInt16(sr)], 0);
                                X[i, j] = new DotNumerics.Complex(yProcessed[j, i], 0);
                            }
                        }

                        // PASSO 3: encontrar os resíduos complexos
                        ComplexMatrix R = z_inv * X;

                        Matrix amps = new Matrix(dim, p);
                        Matrix ang = new Matrix(dim, p);
                        Vector sig_aux = new Vector(Ndat);
                        ComplexVector sig_aux_2 = new ComplexVector(Ndat);

                        Matrix energia = new Matrix(dim, p);

                        Vector w = new Vector(VectorType.Column, Ndat);

                        for (int i = 0; i < Ndat; i++)
                        {
                            //w[i] = (1 / sr) * (i + 1);
                            w[i] = (1 / fDown) * (i + 1);
                        }

                        for (int l = 0; l < p; l++)
                        {
                            for (int i = 0; i < dim; i++)
                            {
                                // amplitude de cada modo
                                amps[i, l] = R[i, l].Modulus;
                                // fase de cada modo
                                ang[i, l] = R[i, l].Argument;
                            }
                        }

                        // Cálculo da pseudo energia associada a cada modo de oscilação
                        Vector pseudoEnergy = new Vector(dim);
                        for (int i = 0; i < dim; i++)
                        {
                            DotNumerics.Complex somatorio = new DotNumerics.Complex();
                            for (int j = 0; j < Ndat; j++)
                            {
                                DotNumerics.Complex aux = new DotNumerics.Complex(Math.Cos(j * eigenA_disc[i, 0].Argument), Math.Sin(j * eigenA_disc[i, 0].Argument));
                                DotNumerics.Complex aux2 = Math.Pow(eigenA_disc[i, 0].Modulus, j) * aux;
                                somatorio += aux2.Conjugate * aux2; // SEGUNDA PARTE DA EQUAÇÃO
                            }
                            // para cada sinal
                            for (int ns = 0; ns < p; ns++)
                            {
                                DotNumerics.Complex aux = R[i, ns].Conjugate * R[i, ns];
                                pseudoEnergy[i] += (aux * somatorio).Real;
                            }
                        }

                        #endregion

                        #endregion

                        #region Índice de dominância modal

                        // Declaração da matriz de índices de dominância modal
                        Matrix idm = new Matrix(dim, p);
                        double[] idmMax = new double[dim];

                        for (int i = 0; i < dim; i++)
                        {
                            for (int j = 0; j < p; j++)
                            {
                                // neste caso o sinal pode indicar a direção, ver pag. 172 da tese do Prioste
                                //idm[i] = -(R[i, 0] * eigenA_cont[i, 0].Conjugate).Real / (eigenA_cont[i, 0] * eigenA_cont[i, 0].Conjugate).Real;
                                idm[i, j] = Math.Abs(-(R[i, j] * eigenA_cont[i, 0].Conjugate).Real / (eigenA_cont[i, 0] * eigenA_cont[i, 0].Conjugate).Real);

                                idmMax[i] += idm[i, j];
                            }
                            idmMax[i] = idmMax[i] / p;
                        }

                        //var indIdmMax = idmMax.ToList().IndexOf(idmMax.Max());

                        #endregion

                        #region Modal-energy (Aguinaldo)

                        //Matrix modalEnergy = X_est * eigenVectors2 * eigenVectors2.

                        //Vector modalEnergy = new Vector(dim);                
                        //for (int i = 0; i < dim; i++)
                        //{
                        //    modalEnergy += 
                        //}

                        #endregion

                        #region Limpeza de modos insignificantes
                        // Zerar energias e IDMs de modos com frequências negativas e taxas de amortecimento muito altas
                        for (int i = 0; i < dim; i++)
                        {
                            //if (freq[i] <= 0.1)
                            //{
                            //    pseudoEnergy[i] = 0;
                            //    idmMax[i] = 0;
                            //}
                            if (freq[i] < fmin || freq[i] > fmax)
                            {
                                pseudoEnergy[i] = 0;
                                idmMax[i] = 0;
                            }
                            if (dampRate[i] > 30.0)
                            {
                                pseudoEnergy[i] = 0;
                                idmMax[i] = 0;
                            }
                        }
                        #endregion

                        #region Mode-shapes

                        int maxEnergyIndex = pseudoEnergy.ToArray().ToList().IndexOf(pseudoEnergy.ToArray().Max());
                        int maxIdmIndex = idmMax.ToArray().ToList().IndexOf(idmMax.ToArray().Max());

                        ComplexMatrix modeShapesEnergy = C * eigenVectors.GetColumnVectors()[maxEnergyIndex];
                        ComplexMatrix modeShapesIdm = C * eigenVectors.GetColumnVectors()[maxIdmIndex];
                        // primeiro, para verificar
                        //ComplexVector MS1 = C.GetRowVector(0) * eigenVectors.GetColumnVectors()[0];

                        #endregion

                        #endregion

                        #region Algoritmo Welch

                        // Executa a função que calcula o Periodograma
                        //Welch(yProcessed, 5, out freqWelch, out psdWelch);

                        #endregion

                        //object[] saida = { count, dim, p, freq, dampRate, pseudoEnergy, idmMax, modeShapesEnergy, modeShapesIdm, freqWelch, psdWelch};
                        object[] saida = { count, dim, p, freq, dampRate, pseudoEnergy, idmMax, modeShapesEnergy, modeShapesIdm};

                        worker.ReportProgress(0, saida);
                    }
                    catch (Exception ex)
                    {
                        //MessageBox.Show(ex.Message);
                    }
                }
            }

            e.Cancel = true;
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            object[] entrada = (object[])e.UserState;

            int count = (int)entrada[0];
            int dim = (int)entrada[1];
            int p = (int)entrada[2];
            Vector freq = (Vector)entrada[3];
            Vector dampRate = (Vector)entrada[4];
            Vector pseudoEnergy = (Vector)entrada[5];
            double[] idmMax = (double[])entrada[6];            
            ComplexMatrix modeShapesEnergy = (ComplexMatrix)entrada[7];
            ComplexMatrix modeShapesIdm = (ComplexMatrix)entrada[8];
            //Vector freqWelch = (Vector)entrada[9];
            //Matrix psdWelch = (Matrix)entrada[10];

            #region Atualização dos gráficos

            // Somente atualiza os gráficos se os vetores de freq., taxa de amort. e a matriz de energias não forem null
            //if (freqCCA != null && dampRateCCA != null && pseudoEnergyCCA != null && freqWelch != null && psdWelch != null && idmMax != null)
            if (freq != null && dampRate != null && pseudoEnergy != null && idmMax != null)
            {
                // Data para o gráfico dos modos no tempo
                DateTime data = dataEsc.AddSeconds(count * ws + wl * 60);

                // Resolução de frequência do periodograma
                //double deltaFreqWelch = freqWelch[1];

                // Máxima energia da lista
                int maxEnergyIndex = pseudoEnergy.ToArray().ToList().IndexOf(pseudoEnergy.ToArray().Max());
                int maxIdmIndex = idmMax.ToList().IndexOf(idmMax.Max());
                //int maxPSDIndex = psdWelch.GetColumnArray(0).ToList().IndexOf(psdWelch.GetColumnArray(0).Max());

                // Mode-shapes do modo dominante
                //ComplexVector MS_dominantMode = MS.GetColumnVectors()[maxEnergyIndex];

                Vector ampsMSEnergy = new Vector(modeShapesEnergy.RowCount);
                Vector angsMSEnergy = new Vector(modeShapesEnergy.RowCount);
                Vector ampsMSIdm = new Vector(modeShapesIdm.RowCount);
                Vector angsMSIdm = new Vector(modeShapesIdm.RowCount);

                for (int i = 0; i < modeShapesEnergy.RowCount; i++)
                {
                    ampsMSEnergy[i] = modeShapesEnergy[i, 0].Modulus;
                    angsMSEnergy[i] = Math.Round(modeShapesEnergy[i, 0].Argument * 180 / Math.PI, 2);
                }
                for (int i = 0; i < modeShapesIdm.RowCount; i++)
                {
                    ampsMSIdm[i] = modeShapesIdm[i, 0].Modulus;
                    angsMSIdm[i] = Math.Round(modeShapesIdm[i, 0].Argument * 180 / Math.PI, 2);
                }
                // Vetor com as amplitudes dos PSDs normalizadas
                //Vector psdAverages = new Vector(psdWelch.RowCount);

                //// Soma das amplitudes normalizadas do Periodograma de Welch
                //for (int i = 0; i < psdWelch.ColumnCount; i++)
                //{
                //    //int maxPSDIndex = psdWelch.GetColumnArray(i).ToList().IndexOf(psdWelch.GetColumnArray(i).Max());
                //    for (int j = 0; j < psdWelch.RowCount; j++)
                //    {
                //        //psdAverages[j] += psdWelch[j, i] / psdWelch[maxPSDIndex, i];
                //        psdAverages[j] += psdWelch[j, i];
                //    }
                //}

                //int maxPSDAveragesIndex = psdAverages.ToArray().ToList().IndexOf(psdAverages.ToArray().Max());

                // ENERGIA
                // Se as frequências são similares
                //if ((freq[maxEnergyIndex] > freqWelch[maxPSDAveragesIndex] - deltaFreqWelch) && (freq[maxEnergyIndex] < freqWelch[maxPSDAveragesIndex] + deltaFreqWelch) && freq[maxEnergyIndex] != 0.0)
                if (pseudoEnergy[maxEnergyIndex] != 0)
                {
                    // Frequência
                    chartModes.Series["Energia"].Points.AddXY(data, freq[maxEnergyIndex]);
                    // Taxa de amortecimento
                    chartModes.Series["Energia - DR"].Points.AddXY(data, dampRate[maxEnergyIndex]);

                    // Adiciona valores às listas que serão armazenadas nos arquivos ao fim do dia
                    //freqEnergy.Add(freq[maxEnergyIndex]);
                    //dampEnergy.Add(dampRate[maxEnergyIndex]);
                    energy.Add(pseudoEnergy[maxEnergyIndex]);
                    ////timeEnergy.Add(data.ToString("HH:mm:ss.fff"));
                    //timeEnergy.Add(data.ToString("HH:mm:ss"));

                    //List<double> aux = new List<double>(angsMSEnergy.ToArray());
                    //msEnergy.Add(aux);
                }

                // IDM
                // Se as frequências são similares
                //if ((freq[maxIdmIndex] > freqWelch[maxPSDAveragesIndex] - deltaFreqWelch) && (freq[maxIdmIndex] < freqWelch[maxPSDAveragesIndex] + deltaFreqWelch) && freq[maxIdmIndex] != 0.0)
                if (pseudoEnergy[maxIdmIndex] != 0)
                {
                    // Frequência
                    chartModes.Series["IDM"].Points.AddXY(data, freq[maxIdmIndex]);
                    // Taxa de amortecimento
                    chartModes.Series["IDM - DR"].Points.AddXY(data, dampRate[maxIdmIndex]);

                    // Adiciona valores às listas que serão armazenadas nos arquivos ao fim do dia
                    //freqIdm.Add(freq[maxIdmIndex]);
                    //dampIdm.Add(dampRate[maxIdmIndex]);
                    idm.Add(idmMax[maxIdmIndex]);
                    ////timeIdm.Add(data.ToString("HH:mm:ss.fff"));
                    //timeIdm.Add(data.ToString("HH:mm:ss"));

                    //List<double> aux2 = new List<double>(angsMSIdm.ToArray());
                    //msIdm.Add(aux2);

                    //// Formas modais (gráfico somente é atualizado quando IDM concorda com resultado do periodograma de Welch)
                    //for (int i = 0; i < chartModeShapes.Series.Count; i++)
                    //{
                    //    // Limpa a série
                    //    chartModeShapes.Series[i].Points.Clear();
                    //    // Ponto central da seta
                    //    chartModeShapes.Series[i].Points.AddXY(0, 0);
                    //    // Ponta da seta
                    //    chartModeShapes.Series[i].Points.AddXY(360 - angsMSIdm[i], 1.0);
                    //}
                }
            }

            //// Welch
            //if (psdWelch != null && freqWelch != null)
            //{
            //    for (int j = 0; j < chartWelch.Series.Count; j++)
            //    {
            //        chartWelch.Series[j].Points.Clear();
            //        for (int i = 0; i < freqWelch.Length; i++)
            //        {
            //            // Tentar evitar o problema do X na plotagem!!!
            //            if (psdWelch[i, j] > 0.0 && !Double.IsNaN(psdWelch[i, j]) && !Double.IsInfinity(psdWelch[i, j]))
            //                chartWelch.Series[j].Points.AddXY(freqWelch[i], 10 * Math.Log10(psdWelch[i, j]));
            //            //chartWelch.Series[0].Points.AddXY(freqWelch[i], psdWelch[i, 0]);
            //        }
            //    }
            //}

            // Atualiza os gráficos a cada ponto traçado
            chartModes.Update();
            //chart3.Update();

            #endregion
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //WritesFile();
            //ClearsLists();

            if (flagFechar)
                Close();

            fim = DateTime.UtcNow;

            TimeSpan duracao = fim - inicio;

            string outReportState;

            if (energy.Count == 0 && idm.Count == 0)
            {
                outReportState = "Não há dados para salvar.";
            }
            else
            {
                var wroteReport = StartsReport();

                if (wroteReport)
                {
                    outReportState = "Arquivo de saída salvo com sucesso.";
                }
                else
                {
                    outReportState = "Houve um erro ao salvar o arquivo de saída.";
                }
            }

            MessageBox.Show("Tempo de execução do algoritmo: " + duracao + ".\r\n\r\n" + outReportState,
                    "MedFasee", MessageBoxButtons.OK);
        }

        private void Form15_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (backgroundWorker1.IsBusy)
            {
                flagFechar = true;
                backgroundWorker1.CancelAsync();
                e.Cancel = true;
            }
        }

        private void chartModes_Leave(object sender, EventArgs e)
        {
            tip.Hide(chartModes);
        }

        private double LinearInterpolation(double x, double xa, double xb, double ya, double yb)
        {
            try
            {
                return ya + (yb - ya) * ((x - xa) / (xb - xa));
            }
            catch (Exception)
            {
                return ya;
            }
        }

        private double[] MovingAverage(double[] aux)
        {
            // Moving sum and moving average
            double movSum = 0.0;
            double[] movAve = new double[aux.Length];

            try
            {
                // Pontos de um sinal
                for (int j = 0; j < aux.Length; j++)
                {
                    if (j == 0)
                    {
                        movAve[j] = aux[j];
                    }
                    else if (j > 0 && j < movAveOrder / 2)
                    {
                        movSum = 0.0;

                        for (int k = 1; k < j; k++)
                        {
                            movSum += aux[j - k];
                        }
                        for (int k = 0; k < movAveOrder / 2; k++)
                        {
                            movSum += aux[j + k];
                        }

                        movAve[j] = movSum / (j + (movAveOrder / 2) - 1);
                    }
                    else if (j >= movAveOrder / 2 && j < aux.Length - movAveOrder / 2)
                    {
                        movSum = 0.0;

                        for (int k = 1; k <= movAveOrder / 2; k++)
                        {
                            movSum += aux[j - k];
                            movSum += aux[j + k];
                        }
                        movSum += aux[j]; // ponto do meio

                        movAve[j] = movSum / (movAveOrder + 1);
                    }
                    else if (j >= aux.Length - movAveOrder / 2)
                    {
                        movSum = 0.0;

                        for (int k = 1; k < aux.Length - j; k++)
                        {
                            movSum += aux[j + k];
                        }
                        for (int k = 0; k < movAveOrder / 2; k++)
                        {
                            movSum += aux[j - k];
                        }

                        movAve[j] = movSum / (aux.Length - j + (movAveOrder / 2) - 1);
                    }
                }
                return movAve;
            }
            catch (Exception)
            {
                return movAve;
            }
        }

        private double StandardDeviation(double[] aux) // COMPARADO COM O MATLAB - OK!
        {
            // standard deviation
            double sd = 0.0;
            try
            {
                // se o vetor tiver comprimento não-nulo
                if (aux.Length > 0)
                {
                    double average = aux.Average();
                    double sum = aux.Sum(value => Math.Pow(value - average, 2));
                    // Equação do desvio padrão - corrected sample standard deviation (por isso é dividido por ""aux.Length - 1" e não por "aux.Length")
                    sd = Math.Sqrt(sum / (aux.Length - 1));
                }
                return sd;
            }
            catch (Exception)
            {
                return sd;
            }
        }

        private double[] IdentificatesOutliers(double[] aux, double[] movAve, double stdDev, double outThreshold)
        {
            try
            {
                for (int j = 0; j < aux.Length; j++)
                {
                    if ((Math.Abs(aux[j]) > Math.Abs(movAve[j]) + outThreshold * stdDev)
                        || (Math.Abs(aux[j]) < Math.Abs(movAve[j]) - outThreshold * stdDev))
                    {
                        // Atribui um NaN ao valor que for outlier para posterior identificação
                        aux[j] = double.NaN;
                    }
                }

                return aux;
            }
            catch (Exception)
            {
                return aux;
            }
        }

        private double[] RemovesAverage(double[] aux)
        {
            try
            {
                // Calcula a média do vetor de dados
                double average = aux.Average(); // média do sinal
                for (int j = 0; j < aux.Length; j++)
                {
                    aux[j] -= average; // diminui a média do vetor de cada item
                }
                return aux;
            }
            catch (Exception)
            {
                return aux;
            }
        }

        private double[] DownSampling(double[] aux, int d)
        {
            double[] auxDown = new double[aux.Length / d];

            try
            {
                for (int i = 0; i < auxDown.Length; i++)
                {
                    auxDown[i] = aux[d * i];
                }

                return auxDown;
            }
            catch (Exception)
            {
                return auxDown;
            }
        }

        private Matrix PreProcessesData(int N, Matrix yOriginal)
        {
            // Fator de dizimação
            int d = Convert.ToInt16(sr / fDown);

            Matrix yProcessed = new Matrix(yOriginal.RowCount, yOriginal.ColumnCount / d);

            try
            {
                double outThreshold = 5.0; // ouliers thresold

                // Sinais
                for (int i = 0; i < yOriginal.RowCount; i++)
                {
                    double[] aux = yOriginal.GetRowArray(i);

                    //movAve = new double[aux.Length];

                    // Utiliza a função criada para o cálculo da média móvel de um vetor
                    double[] movAve = MovingAverage(aux);

                    // Desvio padrão do sinal (standard deviation)
                    double stdDev = StandardDeviation(aux);

                    // Identificação de outliers
                    aux = IdentificatesOutliers(aux, movAve, stdDev, outThreshold);

                    #region Interpolação dos outliers
                    int lastPtIndex = 0; //  last existing point index
                    for (int j = 0; j < aux.Length; j++)
                    {
                        // Se a medida apresenta valor NaN significa que é um outlier 
                        if (Double.IsNaN(aux[j]))
                        {
                            int firstPtIndex = aux.ToList().FindIndex(j, item => !Double.IsNaN(item));
                            // SE A LISTA FICAR MUITO LENTA FAZER UM WHILE QUE ENCONTRA O PRÓXIMO PONTO DIFERENTE DE NaN
                            // Se encontrou um ponto válido
                            if (firstPtIndex > 0)
                                aux[j] = LinearInterpolation(j, lastPtIndex, firstPtIndex, aux[lastPtIndex], aux[firstPtIndex]);
                            else
                                aux[j] = aux[lastPtIndex];
                        }
                        else
                        {
                            lastPtIndex = j;
                        }
                    }
                    #endregion

                    #region Remoção da média do sinal

                    aux = RemovesAverage(aux);

                    #endregion

                    #region Filtro passa-banda FIR (MathNet)

                    double[] coefNovo = FirCoefficients.BandPass(sr, 0.15, 2, 10); // último parâmetro é igual a: ordem_filtro / 2;                 

                    OnlineFirFilter firFilter = new OnlineFirFilter(coefNovo);

                    aux = firFilter.ProcessSamples(aux);

                    #endregion

                    #region Downsampling
                    // retorna aux downsampled
                    double[] auxDown = DownSampling(aux, d);
                    //double[] auxDown = aux;
                    #endregion

                    // repasse à variável de saída da função
                    for (int j = 0; j < auxDown.Length; j++)
                    {
                        yProcessed[i, j] = auxDown[j];
                    }
                }

                return yProcessed;
            }
            catch (Exception)
            {
                return yProcessed;
            }
        }
        
        public void Welch(Matrix y, double sr, out Vector freq, out Matrix psd)
        {
            try
            {
                // Número de sinais
                int p = y.RowCount;
                // Número de amostras em cada sinal
                int Ndat = y.ColumnCount;

                // Number of segments
                int ns = Convert.ToInt16(Math.Floor(Ndat / 150.0));
                // Segment length
                int L = Ndat / ns;
                // Overlap
                int D = L / 2;

                psd = new Matrix((L / 2) + 1, p);
                freq = new Vector(VectorType.Column, (L / 2) + 1);

                // Vetor de frequências
                for (int i = 0; i < (L / 2) + 1; i++)
                {
                    freq[i] = Convert.ToDouble(i * sr) / L;
                }

                // Janela
                Vector w = Hann(L);
                double wPower = w.DotProduct(w); // window power

                // Laço de sinais
                for (int i = 0; i < p; i++)
                {
                    // Laço de segmentos de cada sinal
                    for (int j = 0; j < ns; j++)
                    {
                        // Vetor complexo com o sinal
                        System.Numerics.Complex[] segCpx = new System.Numerics.Complex[L];
                        // Preenchimento do vetor
                        for (int k = 0; k < L; k++)
                        {
                            segCpx[k] = new System.Numerics.Complex((y[i, j * D + k] * w[k]), 0);
                        }
                        Fourier.BluesteinForward(segCpx, FourierOptions.NoScaling);

                        double[] amps = new double[(L / 2) + 1];

                        for (int k = 0; k < (L / 2) + 1; k++)
                        {
                            //amps[k] = (float)Math.Pow(Math.Sqrt(Math.Pow(Convert.ToDouble(segSpec[2 * k]), 2) + Math.Pow(Convert.ToDouble(segSpec[2 * k + 1]), 2)), 2);
                            amps[k] = (float)Math.Pow(Math.Sqrt(Math.Pow(segCpx[k].Real, 2) + Math.Pow(segCpx[k].Imaginary, 2)), 2);

                            // Normalize by window power
                            amps[k] = amps[k] / wPower;
                            // Multiply by 2 (except DC & Nyquist) to calculate one-sided spectrum.
                            // Nyquist é a última, mas como essa FFT sempre fornece uma a menos... (DÚVIDA? VER CÓDIGO MATLAB: teste_welch.m)
                            if (k != 0 && k != L / 2)
                            {
                                amps[k] = amps[k] * 2;
                            }
                            // Divide by sampling rate to calculate spectral  density.
                            psd[k, i] += amps[k] / sr;
                        }

                        // destrói o plano e libera a memória alocada para as operações
                        //fftwf.destroy_plan(fourierPlan);
                        //handleSeg.Free();
                        //handleSegSpec.Free();
                    }

                    // Dividir a PSD pelo número de segmentos
                    for (int k = 0; k < (L / 2) + 1; k++)
                    {
                        if (k > 2)
                            psd[k, i] /= ns;
                        else
                            psd[k, i] = 0;
                    }
                }
            }
            catch (Exception)
            {
                freq = null;
                psd = null;
            }
        }

        public Vector Hann(int L)
        {
            // Janela retornada
            Vector window = new Vector(L);

            try
            {
                int half;

                // se o comprimento é par
                if (L % 2 == 0)
                {
                    half = L / 2;
                }
                else // se é ímpar
                {
                    half = (L + 1) / 2;
                }

                for (int i = 0; i < half; i++)
                {
                    window[i] = 0.5 * (1 - Math.Cos(2 * Math.PI * (Convert.ToDouble(i + 1) / (L + 1))));
                    window[L - i - 1] = window[i];
                }

                return window;
            }
            catch (Exception)
            {
                return window;
            }
        }

        private void WritesFile()
        {
            // Arquivo relativo à ordenação por energia
            try
            {
                //DateTime chartTime = (DateTime)this.Tag;
                DateTime chartTime = DateTime.UtcNow;

                // Objeto de escrita para o arquivo especificado
                StreamWriter writer = new StreamWriter(Path.GetDirectoryName(System.Reflection.
                    Assembly.GetExecutingAssembly().Location) + "\\Dados\\" +
                    DateTime.FromOADate(chartModes.ChartAreas[0].AxisX.Minimum).Date.ToString("yyyyMMdd_") + chartTime.ToString("HHmmss") + "_energia.txt");

                // Variáveis para a escrita, linha por linha
                StringWriter sw = new StringWriter();

                #region Cabeçalho
                //writer.WriteLine("Data: " + DateTime.FromOADate(chartModes.ChartAreas[0].AxisX.Minimum).Date.ToShortDateString());
                //writer.WriteLine(chartModes.Titles[0].Text);

                //sw.Write("Terminais: ");
                ////for (int i = 0; i < formChart.chart1.Series.Count; i++)
                ////{
                ////    sw.Write(formChart.chart1.Series[i].Name);
                ////    if (i < formChart.chart1.Series.Count - 1)
                ////        sw.Write(", ");
                ////}
                //for (int i = 0; i < chartModes.Series.Count; i++)
                //{
                //    sw.Write(chartModes.Series[i].Name);
                //    if (i < chartModes.Series.Count - 1)
                //        sw.Write(", ");
                //}
                //writer.WriteLine(sw);
                //sw.Close();
                //writer.WriteLine();

                writer.WriteLine("Parâmetros");
                writer.WriteLine("Ordem: " + dim);
                writer.WriteLine("Nº de linhas de blocos: " + nbr);
                writer.WriteLine("Tam. janela:" + wl + " ( min.)");
                writer.WriteLine("Passo janela:" + ws + " ( s)");
                writer.WriteLine("Freq. mín." + fmin + " ( Hz)");
                writer.WriteLine("Freq. máx." + fmax + " ( Hz)");
                writer.WriteLine();

                // Títulos das colunas
                //writer.WriteLine("Tempo" + "Frequência (Hz)".PadLeft(25) + "Amortecimento (%)".PadLeft(19) + "Energia".PadLeft(13) + "Ângs. MS".PadLeft(20));
                writer.WriteLine("Tempo \t Frequência (Hz) \t Amortecimento (%) \t Energia \t Ângs. MS");
                #endregion

                #region Dados
                //for (int i = 0; i < timeEnergy.Count; i++)
                //{
                //    // String com os valores de ângulos das mode-shapes
                //    string auxMS = string.Join("\t", Array.ConvertAll<double, string>(msEnergy[i].ToArray(), Convert.ToString));

                //    //writer.WriteLine(timeEnergy[i] + Math.Round(freqEnergy[i], 4).ToString().PadLeft(13) +
                //    //    Math.Round(dampEnergy[i], 4).ToString().PadLeft(19) + Math.Round(energy[i], 4).ToString().PadLeft(17) +
                //    //    auxMS.PadLeft(20));
                //    writer.WriteLine(timeEnergy[i] + "\t" + Math.Round(freqEnergy[i], 4).ToString() + "\t" +
                //        Math.Round(dampEnergy[i], 4).ToString() + "\t" + Math.Round(energy[i], 4).ToString() + "\t" +
                //        auxMS);
                //}
                #endregion

                writer.Close();
            }
            catch (Exception)
            {

            }

            // Arquivo relativo à ordenação por IDM
            try
            {
                //DateTime chartTime = (DateTime)this.Tag;
                DateTime chartTime = DateTime.UtcNow;

                // Objeto de escrita para o arquivo especificado
                StreamWriter writer = new StreamWriter(Path.GetDirectoryName(System.Reflection.
                    Assembly.GetExecutingAssembly().Location) + "\\Dados\\" +
                    DateTime.FromOADate(chartModes.ChartAreas[0].AxisX.Minimum).Date.ToString("yyyyMMdd_") + chartTime.ToString("HHmmss") + "_idm.txt");

                // Variáveis para a escrita, linha por linha
                StringWriter sw = new StringWriter();

                #region Cabeçalho
                //writer.WriteLine("Data: " + DateTime.FromOADate(chartModes.ChartAreas[0].AxisX.Minimum).Date.ToShortDateString());
                //writer.WriteLine(chartModes.Titles[0].Text);

                //sw.Write("Terminais: ");
                ////for (int i = 0; i < formChart.chart1.Series.Count; i++)
                ////{
                ////    sw.Write(formChart.chart1.Series[i].Name);
                ////    if (i < formChart.chart1.Series.Count - 1)
                ////        sw.Write(", ");
                ////}
                //for (int i = 0; i < chartModes.Series.Count; i++)
                //{
                //    sw.Write(chartModes.Series[i].Name);
                //    if (i < chartModes.Series.Count - 1)
                //        sw.Write(", ");
                //}
                //writer.WriteLine(sw);
                //sw.Close();
                //writer.WriteLine();

                writer.WriteLine("Parâmetros");
                writer.WriteLine("Ordem: " + dim);
                writer.WriteLine("Nº de linhas de blocos: " + nbr);
                writer.WriteLine("Tam. janela: " + wl + " min.");
                writer.WriteLine("Passo janela: " + ws + " s");
                writer.WriteLine("Freq. mín.: " + fmin + " Hz");
                writer.WriteLine("Freq. máx.: " + fmax + " Hz");
                writer.WriteLine();

                // Títulos das colunas
                //writer.WriteLine("Tempo" + "Frequência (Hz)".PadLeft(25) + "Amortecimento (%)".PadLeft(19) + "IDM".PadLeft(9));
                writer.WriteLine("Tempo \t Frequência (Hz) \t Amortecimento (%)");// \t Energia \t Ângs. MS");
                #endregion

                #region Dados
                //for (int i = 0; i < timeIdm.Count; i++)
                //{
                //    // String com os valores de ângulos das mode-shapes
                //    string auxMS = string.Join("\t", Array.ConvertAll<double, string>(msIdm[i].ToArray(), Convert.ToString));

                //    //writer.WriteLine(timeIdm[i] + Math.Round(freqIdm[i], 4).ToString().PadLeft(13) +
                //    //    Math.Round(dampIdm[i], 4).ToString().PadLeft(19) + Math.Round(idm[i], 4).ToString().PadLeft(17));
                //    writer.WriteLine(timeIdm[i] + "\t" + Math.Round(freqIdm[i], 4).ToString() + "\t" +
                //          Math.Round(dampIdm[i], 4).ToString() + "\t" + Math.Round(idm[i], 4).ToString() + "\t" +
                //          auxMS);
                //}
                #endregion

                writer.Close();
            }
            catch (Exception)
            {

            }
        }

        private void ClearsLists()
        {
            try
            {
                //timeEnergy.Clear();
                //freqEnergy.Clear();
                //dampEnergy.Clear();
                //timeIdm.Clear();
                //freqIdm.Clear();
                //dampIdm.Clear();
            }
            catch (Exception)
            {

            }
        }

        private void chartModes_DoubleClick_1(object sender, EventArgs e)
        {
            // Intervalo do eixo Y
            chartModes.ChartAreas[0].AxisY.Minimum = fmin;
            chartModes.ChartAreas[0].AxisY.Maximum = fmax;
            // reset dos intervalos e intervalos de label
            chartModes.ChartAreas[0].AxisX.LabelStyle.Interval = 0;
            chartModes.ChartAreas[0].AxisX.Interval = 0;
            chartModes.ChartAreas[0].AxisY.LabelStyle.Interval = 0;
            chartModes.ChartAreas[0].AxisY.Interval = 0;
            // Desfaz o zoom
            //chartModes.ChartAreas[0].AxisX.ScaleView.ZoomReset();
            //chartModes.ChartAreas[0].AxisY.ScaleView.ZoomReset();
            
            // Intervalo do eixo Y (amortecimento)
            //chartModes.ChartAreas[1].AxisY.Minimum = 0;
            chartModes.ChartAreas[1].AxisY.Maximum = 30;
            // reset dos intervalos e intervalos de label
            chartModes.ChartAreas[1].AxisX.LabelStyle.Interval = 0;
            chartModes.ChartAreas[1].AxisX.Interval = 0;
            chartModes.ChartAreas[1].AxisY.LabelStyle.Interval = 0;
            chartModes.ChartAreas[1].AxisY.Interval = 0;

            // Desfaz o zoom
            chartModes.ChartAreas[0].AxisX.ScaleView.ZoomReset();
            chartModes.ChartAreas[0].AxisY.ScaleView.ZoomReset();
            // Desfaz o zoom
            chartModes.ChartAreas[1].AxisX.ScaleView.ZoomReset();
            chartModes.ChartAreas[1].AxisY.ScaleView.ZoomReset();
        }

        private void chartModes_Leave_1(object sender, EventArgs e)
        {
            // Realça o contorno do gráfico
            chartModes.BorderlineDashStyle = ChartDashStyle.Solid;
            // Esconde o toolTip
            tip.Hide(chartModes);
        }

        private void chartModes_KeyDown_1(object sender, KeyEventArgs e)
        {
            try
            {
                // Código para copiar o gráfico para a área de trabalho

                // Verifica se o usuário pressionou as teclas "Ctrl + C" com o form em questão ativo 
                if (e.Control && e.KeyCode == Keys.C)
                {
                    // Realça o contorno do gráfico
                    chartModes.BorderlineDashStyle = ChartDashStyle.Dash;

                    MemoryStream ms = new MemoryStream();

                    chartModes.SaveImage(ms, ChartImageFormat.Bmp);
                    Bitmap bm = new Bitmap(ms);
                    bm.SetResolution(4000, 4000);
                    // Disponibiliza o bitmap na área de trabalho
                    Clipboard.SetImage(bm);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void chartModes_MouseDown_1(object sender, MouseEventArgs e)
        {
            chartModes.Focus();

            tip.IsBalloon = true;

            // Botão direito do mouse foi pressionado
            if (e.Button == MouseButtons.Right)
            {
                // Aponta em que lugar do gráfico aconteceu o clique
                HitTestResult result = chartModes.HitTest(e.X, e.Y);

                // Clique em dado de uma série
                if (result.ChartElementType == ChartElementType.DataPoint)
                {
                    // index of the clicked point in its series
                    tip.ToolTipTitle = result.Series.Name;

                    // Frequência
                    if (result.ChartArea.Name == "ChartArea1")
                    {
                        tip.Show("X = " + DateTime.FromOADate(Math.Round(chartModes.ChartAreas[0].AxisX.PixelPositionToValue(e.Location.X), 5)).ToString("HH:mm:ss")
                            + "\nY = " + Math.Round(chartModes.ChartAreas[0].AxisY.PixelPositionToValue(e.Location.Y), 5), chartModes, e.X, e.Y);
                        tip.Show("X = " + DateTime.FromOADate(Math.Round(chartModes.ChartAreas[0].AxisX.PixelPositionToValue(e.Location.X), 5)).ToString("HH:mm:ss")
                            + "\nY = " + Math.Round(chartModes.ChartAreas[0].AxisY.PixelPositionToValue(e.Location.Y), 5), chartModes, e.X, e.Y, 3000);
                    }
                    // Taxa de amortecimento
                    if (result.ChartArea.Name == "ChartArea2")
                    {
                        tip.Show("X = " + DateTime.FromOADate(Math.Round(chartModes.ChartAreas[1].AxisX.PixelPositionToValue(e.Location.X), 5)).ToString("HH:mm:ss")
                            + "\nY = " + Math.Round(chartModes.ChartAreas[1].AxisY.PixelPositionToValue(e.Location.Y), 5), chartModes, e.X, e.Y);
                        tip.Show("X = " + DateTime.FromOADate(Math.Round(chartModes.ChartAreas[1].AxisX.PixelPositionToValue(e.Location.X), 5)).ToString("HH:mm:ss")
                            + "\nY = " + Math.Round(chartModes.ChartAreas[1].AxisY.PixelPositionToValue(e.Location.Y), 5), chartModes, e.X, e.Y, 3000);
                    }
                }
                // Clique na legenda
                else if (result.ChartElementType == ChartElementType.LegendItem)
                {
                    tip.Hide(chartModes);

                    LegendItem leg = (LegendItem)result.Object;
                    string nomeLeg = leg.SeriesName;

                    // Se a cor foi escolhida
                    if (colorDialog1.ShowDialog() == DialogResult.OK)
                    {
                        chartModes.Series[nomeLeg].Color = colorDialog1.Color;
                        chartModes.Series[nomeLeg + " - DR"].Color = colorDialog1.Color;
                    }
                }
                // Caso o clique tenha sido sobre as legendas dos eixos
                else if (result.ChartElementType == ChartElementType.AxisLabels)
                {
                    // Área clicada
                    string area = result.ChartArea.Name;
                    string axis = result.Axis.AxisName.ToString();

                    AjustaCVA f = new AjustaCVA(this, chartModes, area, axis);
                    // Mostra sobreposto
                    f.ShowDialog();
                }
                else
                {
                    tip.Hide(chartModes);
                }
            }
            else
            {
                tip.Hide(chartModes);
            }
        }        
    }
}
