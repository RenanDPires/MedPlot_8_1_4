using System;
using System.Drawing;
using System.Windows.Forms;
//using fftwlib;
using System.Windows.Forms.DataVisualization.Charting;
using System.IO;
using MathNet.Numerics.IntegralTransforms;
using System.Numerics;
using MathNet.Numerics.LinearAlgebra.Complex;
using MedFasee.Structure;
using System.Collections.Generic;
using MedFasee.Equipment;

namespace MedPlot
{
    public partial class GraficoDFT : Form
    {

        private JanelaPrincipal pai;
        private int opGraf;  // opção do gráfico original que terá seu espectro traçado
        private int indRef; // referência para gráficos de diferenças angulares
        private Measurement[] selectedMeasurements; // vetor com os índices dos selecionados
        private double taxa; // taxa (fasores/s) com a qual a consulta foi realizada
        private int min, max; // índices de início e término da visualização atual do gráfico base
        private int pu; // contém a indicação de gráfico em PU

        DenseMatrix y, psd;
        DenseVector freq;

        // Mínimo e máximo para o eixo X
        private double minY, maxY, minX, maxX;

        ToolTip tip = new ToolTip();

        Color[] cores;

        double fMin;

        // Flag para marcação ou não da opção "automático" do eixo Y no form de configurações
        public bool yAuto = true;
        public bool xAuto = true;

        public Query Query { get; internal set; }
        Measurement Reference { get;  }

        public GraficoDFT(JanelaPrincipal frm1, Query query, int op, Measurement reference, Measurement[] measurements, int minInd, int maxInd, int opPu, Color[] coresAtuais)
        {
            InitializeComponent();

            // Repassando para as variáveis internas
            pai = frm1;
            Query = query;
            opGraf = op;
            selectedMeasurements = measurements;
            Reference = reference;

            min = minInd;
            max = maxInd;

            pu = opPu;

            cores = new Color[coresAtuais.Length];
            cores = coresAtuais;
        }
                
        private void Form7_Shown(object sender, EventArgs e)
        {
            // Habilita as funções de zoom e pan no gráfico
            //chart1.EnableZoomAndPanControls(ChartCursorSelected, ChartCursorMoved);

            // Aqui o gráfico DFT é de fato traçado

            // Total de linhas para o cálculo da DFT
            int Ndat = (max - min) + 1;

            double vb = 1;
            double ib = 1;

            // limpa as séries do gráfico
            chart1.Series.Clear();

            // Define o título do eixo dos tempos
            chart1.ChartAreas[0].AxisX.Title = "Frequência (Hz)";  // "Tempo (UTC) - Dia: " + d + "/" + mes + "/" + a;
            chart1.ChartAreas[0].AxisX.LabelStyle.Format = "0.###";

            // Define o título do eixo Y
            chart1.ChartAreas[0].AxisY.Title = "Magnitude";
            chart1.ChartAreas[0].AxisY.LabelStyle.Format = "0.#####";

            ///////////////////////////////////////////////////////////////////////////////////////////
            // FFT

            // Nomes das séries (para gráficos trifásicos)
            Channel[] phases = new Channel[3];
            taxa = selectedMeasurements[0].FramesPerSecond;

            // Índice para o sinal (diferenças angulares) 
            int m = 0;

            // Número de sinais que serão traçados, se não for trifásico é igual ao nº de sinais selecionados
            int p;
            // Se for gráfico trifásico de tensão ou de corrente
            if ((opGraf == 4) || (opGraf == 15))
                p = 3;
            else
                p = selectedMeasurements.Length;

            // Inicializa a matriz de dados
            // se for gráfico de diferença angular
            if (((opGraf == 7) || (opGraf == 8) || (opGraf == 9) || (opGraf == 10)
                || (opGraf == 17) || (opGraf == 18) || (opGraf == 19) || (opGraf == 20)))
                y = new DenseMatrix(p - 1, Ndat);
            else
                y = new DenseMatrix(p, Ndat);

            for (int k = 0; k < p; k++)
            {

                // Se for gráfico de difer. angular e for o índice da referência
                if (((opGraf == 7) || (opGraf == 8) || (opGraf == 9) || (opGraf == 10)
                    || (opGraf == 17) || (opGraf == 18) || (opGraf == 19) || (opGraf == 20))
                    && (selectedMeasurements[k] == Reference))
                {
                    int r = 0; // NÃO FAZ NADA!!!  -Era Rodolfo
                               // CONTINUA NÃO FAZENDO NADA!!!  -Era Rodrigo
                }
                else
                {
                    // Tensão e corrente base para o gráfico
                    if (pu == 1)
                    {
                        if ((opGraf != 4) && (opGraf != 15)) // se não é gráfico trifásico
                        {
                            
                            vb = selectedMeasurements[k].Terminal.VoltageLevel / Math.Sqrt(3);
                        }
                        else
                        {
                            vb = selectedMeasurements[0].Terminal.VoltageLevel / Math.Sqrt(3);
                        }
                    }
                    else if (pu == 0)
                    {
                        vb = 1;
                        ib = 1;
                    }
                    
                    //////////////////////////////////////////////////////////////////////////////////////////
                    // Seleção do vetor de dados de entrada para a FFT

                    // Gráfico do módulo da tensão - FASE ÚNICA
                    if ((opGraf == 1) || (opGraf == 2) || (opGraf == 3))
                    {
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
                            chart1.Titles[0].Text = "Espectro de Freq. do Módulo da Tensão - Fase " + phase.Phase + " - " +
                                Convert.ToString(taxa) + " fasor/s";
                        }
                        else
                        {
                            chart1.Titles[0].Text = "Espectro de Freq. do Módulo da Tensão - Fase " + phase.Phase + " - " +
                                Convert.ToString(taxa) + " fasores/s";
                        }

                        for (int i = 0; i < Ndat; i++) // tamanho dos sinais que serão traçados
                        {
                            // Vetor dos dados de entrada para a FFT
                            //fin[i] = (float)(Objetos.ObjMat[indCons].matModsTen[indSelec[k]][i + indIni + min] / vb);
                            y[k, i] = new Complex(selectedMeasurements[k].Series[phase].Reading(min + i) / vb, 0);
                        }
                    }
                    // Gráfico do módulo da corrente - FASE ÚNICA
                    if ((opGraf == 12) || (opGraf == 13) || (opGraf == 14))
                    {
                        Channel phase = null;

                        switch (opGraf)
                        {
                            case 1:
                                phase = Channel.CURRENT_A_MOD;
                                break;
                            case 2:
                                phase = Channel.CURRENT_B_MOD;
                                break;
                            case 3:
                                phase = Channel.CURRENT_C_MOD;
                                break;
                            default:
                                break;
                        }

                        // Título do gráfico
                        if (taxa == 1)
                        {
                            chart1.Titles[0].Text = "Espectro de Freq. do Módulo da Corrente - Fase " + phase.Phase + " - " +
                                Convert.ToString(taxa) + " fasor/s";
                        }
                        else
                        {
                            chart1.Titles[0].Text = "Espectro de Freq. do Módulo da Corrente - Fase " + phase.Phase + " - " +
                                Convert.ToString(taxa) + " fasores/s";
                        }

                        for (int i = 0; i < Ndat; i++) // tamanho dos sinais que serão traçados
                        {
                            // Vetor dos dados de entrada para a FFT
                            //fin[i] = (float)(Objetos.ObjMat[indCons].matModsCor[indSelec[k]][i + indIni + min] / ib);
                            y[k, i] = new Complex(selectedMeasurements[k].Series[phase].Reading(min + i) / ib, 0);
                        }
                    }
                    // Gráfico do módulo da tensão - Trifásico
                    else if (opGraf == 4)
                    {
                        phases[0] = Channel.VOLTAGE_A_MOD; //"Fase A";
                        phases[1] = Channel.VOLTAGE_B_MOD; //"Fase B";
                        phases[2] = Channel.VOLTAGE_C_MOD; //"Fase C";

                        // Título do gráfico
                        if (taxa == 1)
                        {
                            chart1.Titles[0].Text = "Espectro de Freq. do Módulo da Tensão - " + 
                                selectedMeasurements[0].Terminal.DisplayName + " - " +
                                Convert.ToString(taxa) + " fasor/s";
                        }
                        else
                        {
                            chart1.Titles[0].Text = "Espectro de Freq. do Módulo da Tensão - " +
                                selectedMeasurements[0].Terminal.DisplayName + " - " +
                                Convert.ToString(taxa) + " fasores/s";
                        }

                        for (int i = 0; i < Ndat; i++) // tamanho dos sinais que serão traçados
                        {
                            // Vetor dos dados de entrada para a FFT
                            //fin[i] = (float)(Objetos.ObjMat[indCons].matModsTen[indSelec[0]][i + indIni + min] / vb);
                            y[k, i] = new Complex(selectedMeasurements[0].Series[phases[k]].Reading(min+i) / vb, 0);
                        }
                    }
                    // Gráfico do módulo da corrente - Trifásico
                    else if (opGraf == 15)
                    {
                        phases[0] = Channel.CURRENT_A_MOD; //"Fase A";
                        phases[1] = Channel.CURRENT_B_MOD; //"Fase B";
                        phases[2] = Channel.CURRENT_C_MOD; //"Fase C";

                        // Título do gráfico
                        if (taxa == 1)
                        {
                            chart1.Titles[0].Text = "Espectro de Freq. do Módulo da Corrente - " +
                                selectedMeasurements[0].Terminal.DisplayName + " - " +
                                Convert.ToString(taxa) + " fasor/s";
                        }
                        else
                        {
                            chart1.Titles[0].Text = "Espectro de Freq. do Módulo da Corrente - " +
                                selectedMeasurements[0].Terminal.DisplayName + " - " +
                                Convert.ToString(taxa) + " fasores/s";
                        }

                        for (int i = 0; i < Ndat; i++) // tamanho dos sinais que serão traçados
                        {
                            // Vetor dos dados de entrada para a FFT
                            //fin[i] = (float)(Objetos.ObjMat[indCons].matModsCor[indSelec[0]][i + indIni + min] / ib);
                            y[k, i] = new Complex(selectedMeasurements[0].Series[phases[k]].Reading(min + i) / ib, 0);
                        }
                    }
                    // Gráfico do módulo da tensão - Seq. Positiva
                    if (opGraf == 5)
                    {
                        // Título do gráfico
                        if (taxa == 1)
                        {
                            chart1.Titles[0].Text = "Espectro de Freq. do Módulo da Tensão - Sequência Positiva - " +
                                Convert.ToString(taxa) + " fasor/s";
                        }
                        else
                        {
                            chart1.Titles[0].Text = "Espectro de Freq. do Módulo da Tensão - Sequência Positiva - " +
                                Convert.ToString(taxa) + " fasores/s";
                        }

                        for (int i = 0; i < Ndat; i++) // tamanho dos sinais que serão traçados
                        {
                            // Vetor dos dados de entrada para a FFT
                            //fin[i] = (float)(Objetos.ObjMat[indCons].matModsSP[indSelec[k]][i + indIni + min] / vb);
                            y[k, i] = new Complex(selectedMeasurements[k].Series[Channel.VOLTAGE_POS_MOD].Reading(min + i) / vb, 0);
                        }
                    }
                    // Gráfico do módulo da corrente - Seq. Positiva
                    if (opGraf == 16)
                    {
                        // Título do gráfico
                        if (taxa == 1)
                        {
                            chart1.Titles[0].Text = "Espectro de Freq. do Módulo da Corrente - Sequência Positiva - " +
                                Convert.ToString(taxa) + " fasor/s";
                        }
                        else
                        {
                            chart1.Titles[0].Text = "Espectro de Freq. do Módulo da Corrente - Sequência Positiva - " +
                                Convert.ToString(taxa) + " fasores/s";
                        }

                        for (int i = 0; i < Ndat; i++) // tamanho dos sinais que serão traçados
                        {
                            // Vetor dos dados de entrada para a FFT
                            //fin[i] = (float)(Objetos.ObjMat[indCons].matModsCorSP[indSelec[k]][i + indIni + min] / ib);
                            y[k, i] = new Complex(selectedMeasurements[k].Series[Channel.CURRENT_POS_MOD].Reading(min + i) / ib, 0);

                        }
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
                        if (taxa == 1)
                        {
                            chart1.Titles[0].Text = "Espectro de Freq. da Diferença Angular da Tensão - Fase " + phase.Phase + " - Ref.: " +
                                Reference.Terminal.DisplayName + " - " +
                                Convert.ToString(taxa) + " fasor/s";
                        }
                        else
                        {
                            chart1.Titles[0].Text = "Espectro de Freq. da Diferença Angular da Tensão - Fase " + phase.Phase + " - Ref.: " +
                                Reference.Terminal.DisplayName + " - " +
                                Convert.ToString(taxa) + " fasores/s";
                        }

                        ///////////////////////////////////////////////////////////////////////////////////////////

                        double dif, difback; // diferença angular e o backup deste valor (para o caso de perda de dados)
                        difback = 0;

                        // Vetor que acumula as diferenças, utilizado para traçar o gráfico
                        double[] vetorDifs = new double[Reference.Series[Channel.MISSING].Count];

                        int cwh = 0; // contador para o while

                        for (int i = 0; i < Reference.Series[Channel.MISSING].Count; i++) // tamanho dos sinais que serão traçados
                        {

                            ///////////////////////////////////////////////////////////////////////////////////////////
                            // Cálculo da diferença angular
                            
                            // Diferença angular
                            dif = selectedMeasurements[k].Series[phase].Reading(i) -
                                Reference.Series[phase].Reading(i);


                            if ((selectedMeasurements[k].Series[Channel.MISSING].Reading(i) == 1) || (Reference.Series[Channel.MISSING].Reading(i) == 1))
                            {
                                if (i != 0)
                                {
                                    dif = difback;
                                }
                                else
                                {
                                    while ((selectedMeasurements[k].Series[Channel.MISSING].Reading(cwh) == 1) || (Reference.Series[Channel.MISSING].Reading(cwh) == 1))
                                    {
                                        // Se chegou ao fim e não há nenhum dado 
                                        if (cwh == (selectedMeasurements[k].Series[phase].Count - 1))
                                            break;
                                        else
                                            cwh++; // atualiza o contador
                                    }
                                    dif = selectedMeasurements[k].Series[phase].Reading(cwh) - Reference.Series[phase].Reading(cwh);
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

                        for (int i = 0; i < Ndat; i++)
                        {
                            // Vetor dos dados de entrada para a FFT
                            //fin[i] = (float)vetorDifs[i + min];
                            y[m, i] = new Complex(vetorDifs[i + min], 0);
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
                        if (taxa == 1)
                        {
                            chart1.Titles[0].Text = "Espectro de Freq. da Diferença Angular da Corrente - Fase " + phase.Phase + " - Ref.: " +
                                Reference.Terminal.DisplayName + " - " +
                                Convert.ToString(taxa) + " fasor/s";
                        }
                        else
                        {
                            chart1.Titles[0].Text = "Espectro de Freq. da Diferença Angular da Corrente - Fase " + phase.Phase + " - Ref.: " +
                                Reference.Terminal.DisplayName + " - " +
                                Convert.ToString(taxa) + " fasores/s";
                        }

                        ///////////////////////////////////////////////////////////////////////////////////////////

                        double dif, difback; // diferença angular e o backup deste valor (para o caso de perda de dados)
                        difback = 0;

                        // Vetor que acumula as diferenças, utilizado para traçar o gráfico
                        double[] vetorDifs = new double[Reference.Series[Channel.MISSING].Count];

                        int cwh = 0; // contador para o while

                        for (int i = 0; i < Reference.Series[Channel.MISSING].Count; i++) // tamanho dos sinais que serão traçados
                        {

                            ///////////////////////////////////////////////////////////////////////////////////////////
                            // Cálculo da diferença angular

                            // Diferença angular
                            dif = selectedMeasurements[k].Series[phase].Reading(i) -
                                Reference.Series[phase].Reading(i);


                            if ((selectedMeasurements[k].Series[Channel.MISSING].Reading(i) == 1) || (Reference.Series[Channel.MISSING].Reading(i) == 1))
                            {
                                if (i != 0)
                                {
                                    dif = difback;
                                }
                                else
                                {
                                    while ((selectedMeasurements[k].Series[Channel.MISSING].Reading(cwh) == 1) || (Reference.Series[Channel.MISSING].Reading(cwh) == 1))
                                    {
                                        // Se chegou ao fim e não há nenhum dado 
                                        if (cwh == (selectedMeasurements[k].Series[phase].Count - 1))
                                            break;
                                        else
                                            cwh++; // atualiza o contador
                                    }
                                    dif = selectedMeasurements[k].Series[phase].Reading(cwh) - Reference.Series[phase].Reading(cwh);
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

                        for (int i = 0; i < Ndat; i++)
                        {
                            // Vetor dos dados de entrada para a FFT
                            //fin[i] = (float)vetorDifs[i + min];
                            y[m, i] = new Complex(vetorDifs[i + min], 0);
                        }

                    }
                    // Gráfico da diferença angular da tensão - Sequência Positiva
                    if (opGraf == 10)
                    {
                        // Título do gráfico
                        if (taxa == 1)
                        {
                            chart1.Titles[0].Text = "Espectro de Freq. da Diferença Angular da Tensão - Sequência Positiva - Ref.: " +
                                Reference.Terminal.DisplayName + " - " +
                                Convert.ToString(taxa) + " fasor/s";
                        }
                        else
                        {
                            chart1.Titles[0].Text = "Espectro de Freq. da Diferença Angular da Tensão - Sequência Positiva - Ref.: " +
                                Reference.Terminal.DisplayName + " - " +
                                Convert.ToString(taxa) + " fasores/s";
                        }

                        ///////////////////////////////////////////////////////////////////////////////////////////

                        double dif, difback; // diferença angular e o backup deste valor (para o caso de perda de dados)
                        difback = 0;

                        // Vetor que acumula as diferenças, utilizado para traçar o gráfico
                        double[] vetorDifs = new double[Reference.Series[Channel.MISSING].Count];

                        int cwh = 0; // contador para o while

                        for (int i = 0; i < Reference.Series[Channel.MISSING].Count; i++) // tamanho dos sinais que serão traçados
                        {

                            ///////////////////////////////////////////////////////////////////////////////////////////
                            // Cálculo da diferença angular

                            // Diferença angular
                            dif = selectedMeasurements[k].Series[Channel.VOLTAGE_POS_ANG].Reading(i) -
                                Reference.Series[Channel.VOLTAGE_POS_ANG].Reading(i);


                            if ((selectedMeasurements[k].Series[Channel.MISSING].Reading(i) == 1) || (Reference.Series[Channel.MISSING].Reading(i) == 1))
                            {
                                if (i != 0)
                                {
                                    dif = difback;
                                }
                                else
                                {
                                    while ((selectedMeasurements[k].Series[Channel.MISSING].Reading(cwh) == 1) || (Reference.Series[Channel.MISSING].Reading(cwh) == 1))
                                    {
                                        // Se chegou ao fim e não há nenhum dado 
                                        if (cwh == (selectedMeasurements[k].Series[Channel.VOLTAGE_POS_ANG].Count - 1))
                                            break;
                                        else
                                            cwh++; // atualiza o contador
                                    }
                                    dif = selectedMeasurements[k].Series[Channel.VOLTAGE_POS_ANG].Reading(cwh) - Reference.Series[Channel.VOLTAGE_POS_ANG].Reading(cwh);
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

                        // Gráfico da diferença angular de tensão de sequência positiva
                        for (int i = 0; i < Ndat; i++)
                        {
                            // Vetor dos dados de entrada para a FFT
                            //fin[i] = (float)vetorDifs[i + min];
                            y[m, i] = new Complex(vetorDifs[i + min], 0);
                        }

                    }
                    // Gráfico da diferença angular da corrente - Sequência Positiva
                    if (opGraf == 20)
                    {
                        // Título do gráfico
                        if (taxa == 1)
                        {
                            chart1.Titles[0].Text = "Espectro de Freq. da Diferença Angular da Corrente - Sequência Positiva - Ref.: " +
                                Reference.Terminal.DisplayName + " - " +
                                Convert.ToString(taxa) + " fasor/s";
                        }
                        else
                        {
                            chart1.Titles[0].Text = "Espectro de Freq. da Diferença Angular da Corrente - Sequência Positiva - Ref.: " +
                                Reference.Terminal.DisplayName + " - " +
                                Convert.ToString(taxa) + " fasores/s";
                        }

                        ///////////////////////////////////////////////////////////////////////////////////////////

                        double dif, difback; // diferença angular e o backup deste valor (para o caso de perda de dados)
                        difback = 0;
                        // Vetor que acumula as diferenças, utilizado para traçar o gráfico
                        double[] vetorDifs = new double[Reference.Series[Channel.MISSING].Count];

                        int cwh = 0; // contador para o while

                        for (int i = 0; i < Reference.Series[Channel.MISSING].Count; i++) // tamanho dos sinais que serão traçados
                        {

                            ///////////////////////////////////////////////////////////////////////////////////////////
                            // Cálculo da diferença angular

                            // Diferença angular
                            dif = selectedMeasurements[k].Series[Channel.CURRENT_POS_ANG].Reading(i) -
                                Reference.Series[Channel.CURRENT_POS_ANG].Reading(i);


                            if ((selectedMeasurements[k].Series[Channel.MISSING].Reading(i) == 1) || (Reference.Series[Channel.MISSING].Reading(i) == 1))
                            {
                                if (i != 0)
                                {
                                    dif = difback;
                                }
                                else
                                {
                                    while ((selectedMeasurements[k].Series[Channel.MISSING].Reading(cwh) == 1) || (Reference.Series[Channel.MISSING].Reading(cwh) == 1))
                                    {
                                        // Se chegou ao fim e não há nenhum dado 
                                        if (cwh == (selectedMeasurements[k].Series[Channel.CURRENT_POS_ANG].Count - 1))
                                            break;
                                        else
                                            cwh++; // atualiza o contador
                                    }
                                    dif = selectedMeasurements[k].Series[Channel.CURRENT_POS_ANG].Reading(cwh) - Reference.Series[Channel.CURRENT_POS_ANG].Reading(cwh);
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

                        // Gráfico da diferença angular de tensão de sequência positiva
                        for (int i = 0; i < Ndat; i++)
                        {
                            // Vetor dos dados de entrada para a FFT
                            //fin[i] = (float)vetorDifs[i + min];
                            y[m, i] = new Complex(vetorDifs[i + min], 0);
                        }

                    }
                    // Gráfico da frequência
                    if (opGraf == 11)
                    {
                        // Título do gráfico
                        if (taxa == 1)
                        {
                            chart1.Titles[0].Text = "Espectro de Freq. da Frequência - " +
                                Convert.ToString(taxa) + " fasor/s";
                        }
                        else
                        {
                            chart1.Titles[0].Text = "Espectro de Freq. da Frequência - " +
                                Convert.ToString(taxa) + " fasores/s";
                        }

                        for (int i = 0; i < Ndat; i++) // tamanho dos sinais que serão traçados
                        {
                            // Vetor dos dados de entrada para a FFT
                            //fin[i] = (float)Objetos.ObjMat[indCons].matFreq[indSelec[k]][i + min];
                            y[k, i] = new Complex(selectedMeasurements[k].Series[Channel.FREQ].Reading(i+min), 0);
                        }
                    }
                    // Gráfico da potência ativa trifásica
                    if (opGraf == 21)
                    {
                        // Título do gráfico
                        if (taxa == 1)
                        {
                            chart1.Titles[0].Text = "Espectro de Freq. da Potência Ativa - " +
                                Convert.ToString(taxa) + " fasor/s";
                        }
                        else
                        {
                            chart1.Titles[0].Text = "Espectro de Freq. da Potência Ativa - " +
                                Convert.ToString(taxa) + " fasores/s";
                        }

                        for (int i = 0; i < Ndat; i++) // tamanho dos sinais que serão traçados
                        {
                            // Vetor dos dados de entrada para a FFT
                            //fin[i] = (float)(Objetos.ObjMat[indCons].matPotAtiva[indSelec[k]][i + indIni + min] / 1e6);
                            y[k, i] = new Complex(selectedMeasurements[k].Series[Channel.ACTIVE_POWER].Reading(i + min) / 1e6, 0);
                        }
                    }
                    // Gráfico da potência reativa trifásica
                    if (opGraf == 22)
                    {
                        // Título do gráfico
                        if (taxa == 1)
                        {
                            chart1.Titles[0].Text = "Espectro de Freq. da Potência Reativa - " +
                                Convert.ToString(taxa) + " fasor/s";
                        }
                        else
                        {
                            chart1.Titles[0].Text = "Espectro de Freq. da Potência Reativa - " +
                                Convert.ToString(taxa) + " fasores/s";
                        }

                        for (int i = 0; i < Ndat; i++) // tamanho dos sinais que serão traçados
                        {
                            // Vetor dos dados de entrada para a FFT
                            //fin[i] = (float)(Objetos.ObjMat[indCons].matPotReativa[indSelec[k]][i + indIni + min] / 1e6);
                            y[k, i] = new Complex(selectedMeasurements[k].Series[Channel.REACTIVE_POWER].Reading(i + min) / 1e6, 0);
                        }
                    }
                    // Gráfico da variação de frequência - DFREQ
                    if (opGraf == 23)
                    {
                        // Título do gráfico
                        if (taxa == 1)
                        {
                            chart1.Titles[0].Text = "Espectro de Freq. da Variação de Frequência - " +
                                Convert.ToString(taxa) + " fasor/s";
                        }
                        else
                        {
                            chart1.Titles[0].Text = "Espectro de Freq. da Variação de Frequência - " +
                                Convert.ToString(taxa) + " fasores/s";
                        }

                        for (int i = 0; i < Ndat; i++) // tamanho dos sinais que serão traçados
                        {
                            // Vetor dos dados de entrada para a FFT
                            //fin[i] = (float)Objetos.ObjMat[indCons].matFreq[indSelec[k]][i + min];
                            y[k, i] = new Complex(selectedMeasurements[k].Series[Channel.DFREQ].Reading(i + min), 0);
                        }
                    }

                    ////////////////////////////////////////////////////////////////////////////////////////

                    // Módulos de tensões e correntes de fase e SP , frequência calculada e potências
                    if ((opGraf == 1) || (opGraf == 2) || (opGraf == 3) || (opGraf == 5) || (opGraf == 11)
                        || (opGraf == 12) || (opGraf == 13) || (opGraf == 14) || (opGraf == 16)
                        || (opGraf == 21) || (opGraf == 22) || (opGraf ==23))
                    {
                        chart1.Series.Add(selectedMeasurements[k].Terminal.DisplayName);
                        // Define a cor da série
                        chart1.Series[m].Color = cores[m];
                    }
                    // Módulo trifásico da tensão ou da corrente
                    else if ((opGraf == 4) || (opGraf == 15))
                    {
                        chart1.Series.Add(phases[k].Phase.ToString());
                        // Define a cor da série
                        chart1.Series[m].Color = cores[m];
                    }
                    // Diferenças angulares de fase e SP
                    else if ((opGraf == 7) || (opGraf == 8) || (opGraf == 9) || (opGraf == 10) ||
                        (opGraf == 17) || (opGraf == 18) || (opGraf == 19) || (opGraf == 20))
                    {
                        chart1.Series.Add(selectedMeasurements[k].Terminal.DisplayName);
                        // Define a cor da série
                        chart1.Series[m].Color = cores[m];
                    }

                    m++; // índice para série do gráfico                  
                }
            }

            // Executa a função que faz o cálculo da DFT
            DFT(y, taxa, out freq, out psd);

            #region Traçar séries no gráfico

            for (int i = 0; i < chart1.Series.Count; i++)
            {
                for (int j = 0; j < freq.Count; j++)
                {
                    chart1.Series[i].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastLine;
                    // Traça a série
                    if (j == 0)
                        chart1.Series[i].Points.AddXY(freq[j].Real, 0);
                    else
                        chart1.Series[i].Points.AddXY(freq[j].Real, 2 * psd[j, i].Magnitude); // 2*abs(amplitude) => para traçar single-sided spectrum
                }
            }

            #endregion

            #region Configurações gerais do gráfico

            chart1.ChartAreas[0].AxisX.Minimum = 0;
            chart1.ChartAreas[0].AxisX.Maximum = taxa / 2;  //1.6;
            chart1.ChartAreas[0].AxisY.Minimum = 0;

            // Valor de mínimo do eixo X, valor calculado a partir do periodo selecionado
            // para esconder o efeito da janela
            fMin = 2 / (Ndat / taxa);

            if (taxa != 1)
            {
                // Ajeitar a posição inicial do gráfico, independente dos valores de mínimo e máximo
                if (fMin >= 1.6)
                {
                    chart1.ChartAreas[0].AxisX.ScaleView.Position = 0;
                    chart1.ChartAreas[0].AxisX.ScaleView.Size = 1.6;
                }

                else
                {
                    chart1.ChartAreas[0].AxisX.ScaleView.Position = fMin;
                    chart1.ChartAreas[0].AxisX.ScaleView.Size = 1.6 - fMin;
                }
            }
            else
            {
                // Ajeitar a posição inicial do gráfico, independente dos valores de mínimo e máximo
                if (fMin >= 0.5)
                {
                    chart1.ChartAreas[0].AxisX.ScaleView.Position = 0;
                    chart1.ChartAreas[0].AxisX.ScaleView.Size = 0.5;
                }
                else
                {
                    chart1.ChartAreas[0].AxisX.ScaleView.Position = fMin;
                    chart1.ChartAreas[0].AxisX.ScaleView.Size = 0.5 - fMin;
                }
            }

            #endregion

        }

        public void DFT(DenseMatrix y, double sr, out DenseVector freq, out DenseMatrix psd)
        {
            try
            {
                // Número de sinais
                int p = y.RowCount;
                // Número de amostras em cada sinal
                int Ndat = y.ColumnCount;

                freq = new DenseVector(Ndat / 2 + 1);
                psd = new DenseMatrix(Ndat / 2 + 1, p);

                // Vetor de frequências
                for (int i = 0; i < Ndat / 2 + 1; i++)
                {
                    freq[i] = Convert.ToDouble(i * sr) / Ndat;
                }

                // Calcula as amplitudes da DFT para cada sinal
                for (int i = 0; i < p; i++)
                {
                    Complex[] aux = y.Row(i).ToArray();
                    // FourierOptions.Matlab
                    // Only scale by 1/N in the inverse direction; No scaling in forward direction (used in Matlab). [= AsymmetricScaling]
                    Fourier.BluesteinForward(aux, FourierOptions.Matlab);
                    psd.SetColumn(i, 0, Ndat / 2 + 1, Vector.Build.DenseOfArray(aux));
                }
            }
            catch (Exception)
            {
                freq = null;
                psd = null;
            }
        }

        private void Form7_Leave(object sender, EventArgs e)
        {
            chart1.BorderlineDashStyle = ChartDashStyle.NotSet;
        }

        private void toolStripTextBox1_TextChanged(object sender, EventArgs e)
        {           
            // mínimo eixo Y
            minY = chart1.ChartAreas[0].AxisY.Minimum;

            try
            {
                if (Convert.ToDouble(toolStripTextBox1.Text) < chart1.ChartAreas[0].AxisY.ScaleView.ViewMaximum
                    && (Convert.ToDouble(toolStripTextBox1.Text) >= minY))
                {
                    chart1.ChartAreas[0].AxisY.ScaleView.Zoom(Convert.ToDouble(toolStripTextBox1.Text),
                        (chart1.ChartAreas[0].AxisY.ScaleView.ViewMaximum));
                }
            }
            catch (Exception)
            {

            }
        }

        private void toolStripTextBox2_TextChanged(object sender, EventArgs e)
        {                        
            // máximo eixo Y

            maxY = chart1.ChartAreas[0].AxisY.Maximum;

            try
            {
                if ((chart1.ChartAreas[0].AxisY.ScaleView.ViewMinimum < Convert.ToDouble(toolStripTextBox2.Text))
                    && (Convert.ToDouble(toolStripTextBox2.Text) <= maxY))
                {
                    chart1.ChartAreas[0].AxisY.ScaleView.Zoom((chart1.ChartAreas[0].AxisY.ScaleView.ViewMinimum),
                        Convert.ToDouble(toolStripTextBox2.Text));
                }
            }
            catch (Exception)
            {

            }
        }

        private void toolStripTextBox3_TextChanged(object sender, EventArgs e)
        {
            // mínimo eixo X

            minX = chart1.ChartAreas[0].AxisX.Minimum;            

            try
            {
                if (chart1.ChartAreas[0].AxisX.ScaleView.ViewMaximum > (Convert.ToDouble(toolStripTextBox3.Text))
                    && (Convert.ToDouble(toolStripTextBox3.Text) >= minX)) // + taxa / n))
                {
                    chart1.ChartAreas[0].AxisX.ScaleView.Zoom(Convert.ToDouble(toolStripTextBox3.Text), //+ taxa/n
                        (chart1.ChartAreas[0].AxisX.ScaleView.ViewMaximum));
                }
            }
            catch (Exception)
            {

            }
        }

        private void toolStripTextBox4_TextChanged(object sender, EventArgs e)
        {
            // máximo eixo X

            maxX = chart1.ChartAreas[0].AxisX.Maximum;

            try
            {
                if (chart1.ChartAreas[0].AxisX.ScaleView.ViewMinimum < (Convert.ToDouble(toolStripTextBox4.Text))
                    && (Convert.ToDouble(toolStripTextBox4.Text) <= maxX))  // + taxa / n))
                {
                    chart1.ChartAreas[0].AxisX.ScaleView.Zoom((chart1.ChartAreas[0].AxisX.ScaleView.ViewMinimum),  // + taxa / n
                        Convert.ToDouble(toolStripTextBox4.Text)); // - taxa/n
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

        private void toolStripTextBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == Convert.ToChar("."))
                e.KeyChar = Convert.ToChar(",");
        }

        private void toolStripTextBox3_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == Convert.ToChar("."))
                e.KeyChar = Convert.ToChar(",");
        }

        private void toolStripTextBox4_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == Convert.ToChar("."))
                e.KeyChar = Convert.ToChar(",");
        }

        private void chart1_DoubleClick(object sender, EventArgs e)
        {
            chart1.ChartAreas[0].AxisY.Minimum = 0;
            chart1.ChartAreas[0].AxisY.Maximum = Double.NaN;

            // reset dos intervalos e intervalos de label
            chart1.ChartAreas[0].AxisX.LabelStyle.Interval = 0;
            chart1.ChartAreas[0].AxisX.Interval = 0;
            chart1.ChartAreas[0].AxisY.LabelStyle.Interval = 0;
            chart1.ChartAreas[0].AxisY.Interval = 0;

            chart1.ChartAreas[0].AxisX.ScaleView.ZoomReset();
            chart1.ChartAreas[0].AxisY.ScaleView.ZoomReset();

            if (chart1.ChartAreas[0].AxisX.ScaleView.ViewMaximum == taxa / 2)
            {
                if (taxa != 1)
                {
                    // Ajeitar a posição inicial do gráfico, independente dos valores de mínimo e máximo
                    if (fMin >= 1.6)
                    {
                        chart1.ChartAreas[0].AxisX.ScaleView.Position = 0;
                        chart1.ChartAreas[0].AxisX.ScaleView.Size = 1.6;
                    }
                        
                    else
                    {
                        chart1.ChartAreas[0].AxisX.ScaleView.Position = fMin;
                        chart1.ChartAreas[0].AxisX.ScaleView.Size = 1.6 - fMin;
                    }                    
                }
                else
                {
                    // Ajeitar a posição inicial do gráfico, independente dos valores de mínimo e máximo
                    if (fMin >= 0.5)
                    {
                        chart1.ChartAreas[0].AxisX.ScaleView.Position = 0;
                        chart1.ChartAreas[0].AxisX.ScaleView.Size = 0.5;
                    }                        
                    else
                    {
                        chart1.ChartAreas[0].AxisX.ScaleView.Position = fMin;
                        chart1.ChartAreas[0].AxisX.ScaleView.Size = 0.5 - fMin;
                    }                    
                }
            }
        }

        private void chart1_KeyDown(object sender, KeyEventArgs e)
        {
            // Código para copiar o gráfico da DFT para a área de trabalho

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

        private void chart1_MouseDown(object sender, MouseEventArgs e)
        {
            tip.IsBalloon = true;

            if (e.Button == MouseButtons.Right)
            {
                // Call Hit Test Method
                HitTestResult result = chart1.HitTest(e.X, e.Y);


                if (result.ChartElementType == ChartElementType.DataPoint)
                {
                    // index of the clicked point in its series
                    //int index = result.PointIndex;
                    tip.ToolTipTitle = result.Series.Name;
                    /*
                    tip.Show(result.Series.Name + "\nX = " + result.Series.Points[index].XValue + "\nY = " + result.Series.Points[index].YValues[0], chart1, e.X, e.Y);
                    tip.Show(result.Series.Name + "\nX = " + result.Series.Points[index].XValue + "\nY = " + result.Series.Points[index].YValues[0], chart1, e.X, e.Y);
                     */
                    tip.Show("X = " + Math.Round(chart1.ChartAreas[0].AxisX.PixelPositionToValue(e.Location.X), 5)
                        + "\nY = " + Math.Round(chart1.ChartAreas[0].AxisY.PixelPositionToValue(e.Location.Y), 5), chart1, e.X, e.Y);
                    tip.Show("X = " + Math.Round(chart1.ChartAreas[0].AxisX.PixelPositionToValue(e.Location.X), 5)
                        + "\nY = " + Math.Round(chart1.ChartAreas[0].AxisY.PixelPositionToValue(e.Location.Y), 5), chart1, e.X, e.Y, 3000);

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

                    AjustaDFT f = new AjustaDFT(this, chart1, axis, taxa, fMin);
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
            }
        }

        private void chart1_MouseLeave(object sender, EventArgs e)
        {
            tip.Hide(chart1);
        }
    }
}