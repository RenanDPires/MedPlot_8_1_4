using MedFasee.Equipment;
using MedFasee.Repository;
using MedFasee.Structure;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MedPlot
{
    public partial class ProcessaBusca : Form
    {
        private JanelaPrincipal pai;

        Tuple<string, string, string> LastSelection { get; set; }

        Query Query { get; }

        public ProcessaBusca(JanelaPrincipal frm1, Query query)
        {
            InitializeComponent();

            pai = frm1;

            Query = query;

            // preenche o label1 com o nome da pasta da consulta
            label1.Text = pai.selectedQuery;
            // ajusta a fonte, se necessário, para caber o nome da pasta
            if (label1.Text.Length > 42 && label1.Text.Length < 49)
                label1.Font = new Font(label1.Font.FontFamily, 7);
            else if (label1.Text.Length > 49)
            {
                label1.Text = pai.selectedQuery.Substring(0, 49);
                label1.Font = new Font(label1.Font.FontFamily, 7);
            }

            // Preenche o valor mínimo de tensão default para validar as medidas de frequência
            tbTenMin.Text = Convert.ToString(0.1);

            MedicoesTerminais();

            // Preenche a árvore de acordo com as informações do arq. config.
            FillTree();
            // Preenche o DGView
            FillDGView();
        }

        
        Dictionary<string, bool[]> flagsTen;
        Dictionary<string, bool[]> flagsCor;
        Dictionary<string, bool> flagsFreq;
        Dictionary<string, bool> flagsDFreq;

        public void MedicoesTerminais()
        {
            // Número de terminais na consulta
            int tt = Query.Measurements.Count;



            // Declaração inicial dos vetores, dimensão de linhas
            flagsTen = new Dictionary<string, bool[]>();
            flagsCor = new Dictionary<string, bool[]>();
            flagsFreq = new Dictionary<string, bool>();
            flagsDFreq = new Dictionary<string, bool>();

            // Declaração da dimensão coluna para cada linha
            // Tensões e correntes com 4 campos, 3 para as fases A, B e C e uma para SP
            for (int i = 0; i < tt; i++)
            {
                string terminalId = Query.Measurements[i].Terminal.Id;
                flagsTen[terminalId] = new bool[4];
                flagsCor[terminalId] = new bool[4];

                #region Voltage

                flagsTen[terminalId][0] = Query.Measurements[i].Series.ContainsKey(MedFasee.Equipment.Channel.VOLTAGE_A_MOD);
                flagsTen[terminalId][1] = Query.Measurements[i].Series.ContainsKey(MedFasee.Equipment.Channel.VOLTAGE_B_MOD);
                flagsTen[terminalId][2] = Query.Measurements[i].Series.ContainsKey(MedFasee.Equipment.Channel.VOLTAGE_C_MOD);
                flagsTen[terminalId][3] = Query.Measurements[i].Series.ContainsKey(MedFasee.Equipment.Channel.VOLTAGE_POS_MOD);
                #endregion

                #region Current
                flagsCor[terminalId][0] = Query.Measurements[i].Series.ContainsKey(MedFasee.Equipment.Channel.CURRENT_A_MOD);
                flagsCor[terminalId][1] = Query.Measurements[i].Series.ContainsKey(MedFasee.Equipment.Channel.CURRENT_B_MOD);
                flagsCor[terminalId][2] = Query.Measurements[i].Series.ContainsKey(MedFasee.Equipment.Channel.CURRENT_C_MOD);
                flagsCor[terminalId][3] = Query.Measurements[i].Series.ContainsKey(MedFasee.Equipment.Channel.CURRENT_POS_MOD);
                #endregion

                // Frequências
                flagsFreq[terminalId] = Query.Measurements[i].Series.ContainsKey(MedFasee.Equipment.Channel.FREQ);
                flagsDFreq[terminalId] = Query.Measurements[i].Series.ContainsKey(MedFasee.Equipment.Channel.DFREQ);
            }
        }

        public void Habilitacao()
        {
            UpdateQuantityBox();

            #region Inicialização
            // Vetor com os índices das linhas selecionadas
            //int[] indices = new int[dataGridView1.SelectedRows.Count];
            string[] indices = new string[dataGridView1.SelectedRows.Count];

            int cont = 0;
            foreach (DataGridViewRow linha in dataGridView1.SelectedRows)
            {
                DataRowView selectedRow = linha.DataBoundItem as DataRowView;
                indices[cont]= ((string)selectedRow["idName"]);
                cont++;
            }
            if (dataGridView1.SelectedRows.Count == 0)
                PlottingTab.Enabled = false;
            else
                PlottingTab.Enabled = true;

            #endregion

            #region Grandezas

            // Inicializa com tudo habilitado
            rbTen.Enabled = true;
            cbPu.Enabled = true;
            rbCor.Enabled = true;
            rbFreq.Enabled = true;
            rbDFreq.Enabled = true;
            rbPotAtiva.Enabled = true;
            rbPotReativa.Enabled = true;
            lbTenMin.ForeColor = Color.Black;
            tbTenMin.Enabled = true;

            // Tensão
            for (int i = 0; i < indices.Length; i++)
            {
                bool a = Array.Exists(flagsTen[indices[i]], item => item == true);
                if (!a)
                {
                    rbTen.Checked = false;
                    rbTen.Enabled = false;
                    cbPu.Checked = false;
                    cbPu.Enabled = false;
                    lbTenMin.ForeColor = Color.Gray;
                    tbTenMin.Enabled = false;
                    break;
                }
            }
            // Corrente
            for (int i = 0; i < indices.Length; i++)
            {
                bool a = Array.Exists(flagsCor[indices[i]], item => item == true);
                if (!a)
                {
                    rbCor.Checked = false;
                    rbCor.Enabled = false;
                    break;
                }
            }
            // Frequência
            for (int i = 0; i < indices.Length; i++)
            {                
                if (flagsFreq[indices[i]] == false)
                {
                    rbFreq.Checked = false;
                    rbFreq.Enabled = false;
                    break;
                }
            }
            // Variação de Frequência
            for (int i = 0; i < indices.Length; i++)
            {
                if (flagsDFreq[indices[i]] == false)
                {
                    rbDFreq.Checked = false;
                    rbDFreq.Enabled = false;
                    break;
                }
            }
            // Potências
            // Verifica as condições de tensão e corrente
            if (rbTen.Enabled == false || rbCor.Enabled == false)
            {
                rbPotAtiva.Checked = false;
                rbPotAtiva.Enabled = false;
                rbPotReativa.Checked = false;
                rbPotReativa.Enabled = false;
            }
            #endregion

            #region Componentes do fasor
            // Inicializa com o ângulo desabilitado, até porque sempre inicia com apenas 1 terminal selecionado
            rbMod.Enabled = true;
            rbAng.Enabled = false;
            comboBoxAng.Items.Clear();
            comboBoxAng.Enabled = false;
            label2.ForeColor = Color.Gray;

            /* Desabilita a opção de ângulo se apenas um terminal estiver selecionado,
            ou se todos os terminais selecionados possuírem apenas medição de corrente. 
            Isto porque, a referência deve ser sempre uma tensão.*/
            if (indices.Length > 1)
            {
                for (int i = 0; i < indices.Length; i++)
                {
                    bool a = Array.Exists(flagsTen[indices[i]], item => item == true);
                    if (a)
                    {
                        // se ainda não está habilitado
                        if (!rbAng.Enabled)
                        {
                            rbAng.Enabled = true;
                            comboBoxAng.Enabled = true;                            
                            label2.ForeColor = Color.Black;
                        }                            
                        comboBoxAng.Items.Add(indices[i]);
                        comboBoxAng.SelectedIndex = 0;
                    }
                }
            }

            // Se o ângulo não foi habilitado, desmarca
            if (!rbAng.Enabled)
                rbAng.Checked = false;
            #endregion

            #region Fases
            // Coemça com tudo habilitado
            rbFaseA.Enabled = true;
            rbFaseB.Enabled = true;
            rbFaseC.Enabled = true;
            rbTri.Enabled = true;
            rbSP.Enabled = true;

            // Desabilita todas as fases se freq ou potências estão marcadas
            if (rbFreq.Checked == true || rbPotAtiva.Checked == true || rbPotReativa.Checked == true || rbDFreq.Checked == true)
            {
                rbFaseA.Checked = false;
                rbFaseA.Enabled = false;
                rbFaseB.Checked = false;
                rbFaseB.Enabled = false;
                rbFaseC.Checked = false;
                rbFaseC.Enabled = false;
                rbTri.Checked = false;
                rbTri.Enabled = false;
                rbSP.Checked = false;
                rbSP.Enabled = false;
            }
            // Se for tensão ou corente habilita fases adequadas
            else if (rbTen.Checked == true || rbCor.Checked == true)
            {
                // 3 fases + SP
                for (int i = 0; i < 4; i++)
                {
                    // For dos selecionados para cada opção de fase
                    for (int j = 0; j < indices.Length; j++)
                    {
                        // Tensão
                        if (rbTen.Checked == true)
                        {
                            if (!flagsTen[indices[j]][i])
                            {
                                // Desabilita a opção da fase correspondente à coluna de flags
                                switch (i)
                                {
                                    case 0:
                                        rbFaseA.Checked = false;
                                        rbFaseA.Enabled = false;
                                        break;
                                    case 1:
                                        rbFaseB.Checked = false;
                                        rbFaseB.Enabled = false;
                                        break;
                                    case 2:
                                        rbFaseC.Checked = false;
                                        rbFaseC.Enabled = false;
                                        break;
                                    case 3:
                                        rbSP.Checked = false;
                                        rbSP.Enabled = false;
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }
                        // Corrente
                        else if (rbCor.Checked == true)
                        {
                            if (!flagsCor[indices[j]][i])
                            {
                                // Desabilita a opção da fase correspondente à coluna de flags
                                switch (i)
                                {
                                    case 0:
                                        rbFaseA.Checked = false;
                                        rbFaseA.Enabled = false;
                                        break;
                                    case 1:
                                        rbFaseB.Checked = false;
                                        rbFaseB.Enabled = false;
                                        break;
                                    case 2:
                                        rbFaseC.Checked = false;
                                        rbFaseC.Enabled = false;
                                        break;
                                    case 3:
                                        rbSP.Checked = false;
                                        rbSP.Enabled = false;
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }
                    }
                }
            }
            // Desabilita a opção trifásico se mais de um terminal estiver selecionado
            if (indices.Length > 1 || rbFaseA.Enabled == false 
                || rbFaseB.Enabled == false || rbFaseC.Enabled == false)
            {
                rbTri.Checked = false;
                rbTri.Enabled = false;
            }

            #endregion

            #region Marcação padrão

            // Habilita botão
            buttonPlotar.Enabled = true;

            // Se nenhum terminal for selecionado
            if (indices.Length == 0)
                buttonPlotar.Enabled = false;

            // Grandeza marcada
            var chkGrand = groupBox2.Controls.OfType<RadioButton>()
                           .FirstOrDefault(n => n.Checked);
            // Grandeza habilitada
            var enbGrand = groupBox2.Controls.OfType<RadioButton>()
               .FirstOrDefault(n => n.Enabled);
            // Se nenhuma grandeza está marcada
            if (chkGrand == null)
            {
                // se há grandeza habilitada
                if (enbGrand != null)
                    enbGrand.Checked = true;
                else
                    buttonPlotar.Enabled = false;
            }

            // Desabilita as componentes do fasor e fases quando freq e potências estão marcadas
            if (rbFreq.Checked == true || rbPotAtiva.Checked == true || rbPotReativa.Checked == true || rbDFreq.Checked == true)
            {
                // Componentes
                rbMod.Checked = false;
                rbMod.Enabled = false;
                rbAng.Checked = false;
                rbAng.Enabled = false;                
                comboBoxAng.Items.Clear(); // limpa os itens do componente
                comboBoxAng.ResetText(); // limpa o texto escrito no componente
                comboBoxAng.Enabled = false;
                label2.ForeColor = Color.Gray;
                // Fases
                rbFaseA.Checked = false;
                rbFaseA.Enabled = false;
                rbFaseB.Checked = false;
                rbFaseB.Enabled = false;
                rbFaseC.Checked = false;
                rbFaseC.Enabled = false;
                rbTri.Checked = false;
                rbTri.Enabled = false;
                rbSP.Checked = false;
                rbSP.Enabled = false;
            }

            // Componente do fasor marcada
            var chkCmpFasor = groupBox3.Controls.OfType<RadioButton>()
                           .FirstOrDefault(n => n.Checked);
            // Componente do fasor habilitada
            var enbCmpFasor = groupBox3.Controls.OfType<RadioButton>()
                           .FirstOrDefault(n => n.Enabled);
            // Se nenhuma componente do fasor está marcada
            if (chkCmpFasor == null)
            {
                // se há componente do fasor habilitada
                if (enbCmpFasor != null)
                    enbCmpFasor.Checked = true;
            }

            // Desabilita a caixa com a referência e limpa os itens se o módulo está marcado
            if (rbMod.Checked == true)
            {
                comboBoxAng.Items.Clear(); // limpa os itens do componente
                comboBoxAng.ResetText(); // limpa o texto escrito no componente
                comboBoxAng.Enabled = false;
                label2.ForeColor = Color.Gray;
            }

            // Somente habilita o pu se o módulo da tensão for a opção de gráfico escolhida
            if (rbTen.Checked && rbMod.Checked)
            {
                cbPu.Enabled = true;
            }
            else
            {
                cbPu.Checked = false;
                cbPu.Enabled = false;
            }


            // Fase marcada
            var chkFase = groupBox4.Controls.OfType<RadioButton>()
                           .FirstOrDefault(n => n.Checked);
            // Fase habilitada
            var enbFase = groupBox4.Controls.OfType<RadioButton>()
                           .FirstOrDefault(n => n.Enabled);
            // Se nenhuma fase está marcada
            if (chkFase == null)
            {
                // se há componente do fasor habilitada
                if (enbFase != null)
                    enbFase.Checked = true;
            }

            #endregion
        }

        private void buttonPlotar_Click(object sender, EventArgs e)
        {
            Measurement reference = null;

            Measurement[] indices = new Measurement[dataGridView1.SelectedRows.Count];
            int cont = 0;
            foreach (DataGridViewRow linha in dataGridView1.SelectedRows)
            {
                // Indíces das linhas selecionadas no dataGrid
                //indices[cont] = Convert.ToInt16(dataGridView1.SelectedRows[cont].Cells[5].Value);
                indices[cont] = (Measurement)linha.Cells[5].Value;
                cont++;
            }

            double vMinFreq = 0;

            if (PlottingTab.SelectedIndex == 0)
            {


                // Escolha do tipo de gráfico
                ////////////////////////////////////////////////////////////////////////////
                /* Gráfico do módulo da tensão - FASE A */
                if ((rbTen.Checked == true) && (rbMod.Checked == true) && (rbFaseA.Checked == true))
                {
                    OpcaoGrafico.Opcao = 1;
                    if (cbPu.Checked == true)
                    {
                        OpcaoGrafico.Pu = 1;
                    }
                    else
                    {
                        OpcaoGrafico.Pu = 0;
                    }
                }
                /* Gráfico do módulo da tensão - FASE B */
                if ((rbTen.Checked == true) && (rbMod.Checked == true) && (rbFaseB.Checked == true))
                {
                    OpcaoGrafico.Opcao = 2;
                    if (cbPu.Checked == true)
                    {
                        OpcaoGrafico.Pu = 1;
                    }
                    else
                    {
                        OpcaoGrafico.Pu = 0;
                    }
                }
                /* Gráfico do módulo da tensão - FASE C */
                if ((rbTen.Checked == true) && (rbMod.Checked == true) && (rbFaseC.Checked == true))
                {
                    OpcaoGrafico.Opcao = 3;
                    if (cbPu.Checked == true)
                    {
                        OpcaoGrafico.Pu = 1;
                    }
                    else
                    {
                        OpcaoGrafico.Pu = 0;
                    }
                }
                /* Gráfico do módulo da tensão - Trifásico */
                if ((rbTen.Checked == true) && (rbMod.Checked == true) && (rbTri.Checked == true))
                {
                    OpcaoGrafico.Opcao = 4;
                    if (cbPu.Checked == true)
                    {
                        OpcaoGrafico.Pu = 1;
                    }
                    else
                    {
                        OpcaoGrafico.Pu = 0;
                    }
                }
                /* Gráfico do módulo da tensão - Seq. Positiva */
                if ((rbTen.Checked == true) && (rbMod.Checked == true) && (rbSP.Checked == true))
                {
                    OpcaoGrafico.Opcao = 5;
                    if (cbPu.Checked == true)
                    {
                        OpcaoGrafico.Pu = 1;
                    }
                    else
                    {
                        OpcaoGrafico.Pu = 0;
                    }
                }
                // Encontrar o índice da referência
                if (rbAng.Checked == true)
                {
                    string nomeRef = comboBoxAng.SelectedItem.ToString();

                    //int a = Array.FindIndex(Objetos.ObjMat[indCons].matNomes, item => item == nomeRef);
                    //ReferenciaAngular.Referencia = a;

                    for (int i = 0; i < indices.Length; i++)
                    {
                        if (indices[i].Terminal.Id == nomeRef)
                            reference = indices[i];
                    }                
                }

                /* Gráfico da diferença angular da tensão - FASE A  */
                if ((rbTen.Checked == true) && (rbAng.Checked == true) && (rbFaseA.Checked == true))
                {
                    OpcaoGrafico.Opcao = 7;
                    // Atribui a referência ao item selecionado no combobox
                    //ReferenciaAngular.Referencia = comboBoxAng.SelectedIndex;
                }
                /* Gráfico da diferença angular da tensão - FASE B  */
                if ((rbTen.Checked == true) && (rbAng.Checked == true) && (rbFaseB.Checked == true))
                {
                    OpcaoGrafico.Opcao = 8;
                    // Atribui a referência ao item selecionado no combobox
                    //ReferenciaAngular.Referencia = comboBoxAng.SelectedIndex;
                }
                /* Gráfico da diferença angular da tensão - FASE C  */
                if ((rbTen.Checked == true) && (rbAng.Checked == true) && (rbFaseC.Checked == true))
                {
                    OpcaoGrafico.Opcao = 9;
                    // Atribui a referência ao item selecionado no combobox
                    //ReferenciaAngular.Referencia = comboBoxAng.SelectedIndex;
                }
                /* Gráfico da diferença angular da tensão - Seq. Pos.  */
                if ((rbTen.Checked == true) && (rbAng.Checked == true) && (rbSP.Checked == true))
                {
                    OpcaoGrafico.Opcao = 10;
                    // Atribui a referência ao item selecionado no combobox
                    //ReferenciaAngular.Referencia = comboBoxAng.SelectedIndex;
                }
                /* Gráfico da frequência calculada pela variação angular */
                if (rbFreq.Checked == true)
                {
                    OpcaoGrafico.Opcao = 11;

                    try
                    {
                        // Tensão mínima para consideração da validade de medidas de frequência
                        vMinFreq = Convert.ToDouble(tbTenMin.Text);

                        if (vMinFreq > 0.9)
                        {
                            MessageBox.Show("Valor de tensão mínima para validação de frequências deve ser igual ou inferior a 0,9 pu.", "MedPlot", MessageBoxButtons.OK);
                            return;
                        }
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Valor de tensão mínima para validação de medidas de frequência incorreto.", "MedPlot", MessageBoxButtons.OK);
                        return;
                    }
                }
                /* Gráfico do módulo da corrente - FASE A */
                if ((rbCor.Checked == true) && (rbMod.Checked == true) && (rbFaseA.Checked == true))
                {
                    OpcaoGrafico.Opcao = 12;
                    if (cbPu.Checked == true)
                    {
                        OpcaoGrafico.Pu = 1;
                    }
                    else
                    {
                        OpcaoGrafico.Pu = 0;
                    }
                }
                /* Gráfico do módulo da corrente - FASE B */
                if ((rbCor.Checked == true) && (rbMod.Checked == true) && (rbFaseB.Checked == true))
                {
                    OpcaoGrafico.Opcao = 13;
                    if (cbPu.Checked == true)
                    {
                        OpcaoGrafico.Pu = 1;
                    }
                    else
                    {
                        OpcaoGrafico.Pu = 0;
                    }
                }
                /* Gráfico do módulo da corrente - FASE C */
                if ((rbCor.Checked == true) && (rbMod.Checked == true) && (rbFaseC.Checked == true))
                {
                    OpcaoGrafico.Opcao = 14;
                    if (cbPu.Checked == true)
                    {
                        OpcaoGrafico.Pu = 1;
                    }
                    else
                    {
                        OpcaoGrafico.Pu = 0;
                    }
                }
                /* Gráfico do módulo da corrente - Trifásico */
                if ((rbCor.Checked == true) && (rbMod.Checked == true) && (rbTri.Checked == true))
                {
                    OpcaoGrafico.Opcao = 15;
                    if (cbPu.Checked == true)
                    {
                        OpcaoGrafico.Pu = 1;
                    }
                    else
                    {
                        OpcaoGrafico.Pu = 0;
                    }
                }
                /* Gráfico do módulo da corrente - Seq. Positiva */
                if ((rbCor.Checked == true) && (rbMod.Checked == true) && (rbSP.Checked == true))
                {
                    OpcaoGrafico.Opcao = 16;
                    if (cbPu.Checked == true)
                    {
                        OpcaoGrafico.Pu = 1;
                    }
                    else
                    {
                        OpcaoGrafico.Pu = 0;
                    }
                }
                /* Gráfico da diferença angular da corrente - FASE A  */
                if ((rbCor.Checked == true) && (rbAng.Checked == true) && (rbFaseA.Checked == true))
                {
                    OpcaoGrafico.Opcao = 17;
                    // Atribui a referência ao item selecionado no combobox
                    //ReferenciaAngular.Referencia = comboBoxAng.SelectedIndex;
                }
                /* Gráfico da diferença angular da corrente - FASE B  */
                if ((rbCor.Checked == true) && (rbAng.Checked == true) && (rbFaseB.Checked == true))
                {
                    OpcaoGrafico.Opcao = 18;
                    // Atribui a referência ao item selecionado no combobox
                    //ReferenciaAngular.Referencia = comboBoxAng.SelectedIndex;
                }
                /* Gráfico da diferença angular da corrente - FASE C  */
                if ((rbCor.Checked == true) && (rbAng.Checked == true) && (rbFaseC.Checked == true))
                {
                    OpcaoGrafico.Opcao = 19;
                    // Atribui a referência ao item selecionado no combobox
                    //ReferenciaAngular.Referencia = comboBoxAng.SelectedIndex;
                }
                /* Gráfico da diferença angular da corrente - Seq. Pos.  */
                if ((rbCor.Checked == true) && (rbAng.Checked == true) && (rbSP.Checked == true))
                {
                    OpcaoGrafico.Opcao = 20;
                    // Atribui a referência ao item selecionado no combobox
                    //ReferenciaAngular.Referencia = comboBoxAng.SelectedIndex;
                }
                /* Gráfico da potência ativa trifásica*/
                if (rbPotAtiva.Checked == true)
                {
                    OpcaoGrafico.Opcao = 21;
                }
                /* Gráfico da potência reativa trifásica*/
                if (rbPotReativa.Checked == true)
                {
                    OpcaoGrafico.Opcao = 22;
                }
                /* Gráfico da variação da frequência*/
                if (rbDFreq.Checked == true)
                {
                    OpcaoGrafico.Opcao = 23;

                    try
                    {
                        // Tensão mínima para consideração da validade de medidas de frequência
                        vMinFreq = Convert.ToDouble(tbTenMin.Text);

                        if (vMinFreq > 0.9)
                        {
                            MessageBox.Show("Valor de tensão mínima para validação de variações de frequência deve ser igual ou inferior a 0,9 pu.", "MedPlot", MessageBoxButtons.OK);
                            return;
                        }
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Valor de tensão mínima para validação de medidas de variações de frequência incorreto.", "MedPlot", MessageBoxButtons.OK);
                        return;
                    }
                }
            }
            else
            {
                if (QuantityBox.SelectedItem?.ToString() == "VIMB")
                {
                    OpcaoGrafico.Opcao = 24;
                }
                if (QuantityBox.SelectedItem?.ToString() == "THD" || QuantityBox.SelectedItem?.ToString() == "THDV")
                {
                    if(PhaseBox.SelectedItem?.ToString() == "A")
                        OpcaoGrafico.Opcao = 25;
                    else if (PhaseBox.SelectedItem?.ToString() == "B")
                        OpcaoGrafico.Opcao = 26;
                    else if (PhaseBox.SelectedItem?.ToString() == "C")
                        OpcaoGrafico.Opcao = 27;
                    else if (PhaseBox.SelectedItem?.ToString() == "Trifasico")
                        OpcaoGrafico.Opcao = 28;
                }
                

            }

            ////////////////////////////////////////////////////////////////////////////
            // Parâmetros repassados para o form 3

            // Índice da consulta
            int op = OpcaoGrafico.Opcao;
            
            //pai.Criar(ind);

            //pai.CriarGrafico(ind, op, indRef, indices, Parametros.DirDados + "\\" + PastaCorrente.NP, vMinFreq);
            pai.CriarGrafico(Query, op, reference, indices, Properties.Settings.Default.QueryFolder + "\\" + pai.selectedQuery, vMinFreq);

            //Form f = new Form3();
            //f.Show();        
        }

        private void UpdateQuantityBox()
        {
            LastSelection = new Tuple<string, string, string>(
                QuantityBox.SelectedItem?.ToString(),
                TypeBox.SelectedItem?.ToString(),
                PhaseBox.SelectedItem?.ToString());

            QuantityBox.Enabled = true;
            QuantityBox.Items.Clear();  
            Measurement[] measurements = new Measurement[dataGridView1.SelectedRows.Count];
            int cont = 0;
            foreach (DataGridViewRow linha in dataGridView1.SelectedRows)
            {
                measurements[cont] = (Measurement)linha.Cells[5].Value;
                cont++;
            }


            List<ChannelQuantity> quantities = null;
            foreach (Measurement measurement in measurements)
            {
                List<ChannelQuantity> measureQuantities = new List<ChannelQuantity>();
                foreach (Channel channel in measurement.Series.Keys)
                    if(!measureQuantities.Contains(channel.Quantity) && channel.Quantity != ChannelQuantity.MISSING && (channel.Quantity == ChannelQuantity.VIMB || channel.Quantity == ChannelQuantity.THD || channel.Quantity == ChannelQuantity.THDV))
                        measureQuantities.Add(channel.Quantity);

                if (quantities == null)
                {
                    quantities = measureQuantities;
                    continue;
                }


                for(int i = quantities.Count-1; i >= 0; i-- )
                    if (!measureQuantities.Contains(quantities[i]))
                        quantities.RemoveAt(i);

                if (quantities.Count == 0)
                    break;
            }


            if(quantities != null && quantities.Count != 0)
            {
                foreach (var quantity in quantities)
                    QuantityBox.Items.Add(quantity.ToString());

                if (LastSelection.Item1 != null && QuantityBox.Items.Contains(LastSelection.Item1))
                    QuantityBox.SelectedItem = LastSelection.Item1;
                else
                    QuantityBox.SelectedItem = QuantityBox.Items[0];
            }
            else
            {
                QuantityBox.Enabled = false;
                QuantityBox.Text = "";
                UpdateTypeBox();
            }



        }

        private void UpdateTypeBox()
        {
            // Retirar box Tipo
            TypeBox.Visible = false;
            label5.Visible = false;

            TypeBox.Enabled = true;
            TypeBox.Items.Clear();
            Measurement[] measurements = new Measurement[dataGridView1.SelectedRows.Count];
            int cont = 0;
            foreach (DataGridViewRow linha in dataGridView1.SelectedRows)
            {
                measurements[cont] = (Measurement)linha.Cells[5].Value;
                cont++;
            }

            ChannelQuantity quantity;
            if (QuantityBox.SelectedItem == null)
                quantity = ChannelQuantity.OTHER;
            else
                quantity = Channel.GetQuantityFromString(QuantityBox.SelectedItem.ToString());

            List<ChannelValueType> types = null;
            foreach (Measurement measurement in measurements)
            {
                List<ChannelValueType> measureQuantities = new List<ChannelValueType>();
                foreach (Channel channel in measurement.Series.Keys)
                    if (channel.Quantity == quantity && !measureQuantities.Contains(channel.Value) && channel.Value != ChannelValueType.NONE)
                        measureQuantities.Add(channel.Value);

                if (types == null)
                {
                    types = measureQuantities;
                    continue;
                }


                for (int i = types.Count - 1; i >= 0; i--)
                    if (!measureQuantities.Contains(types[i]))
                        types.RemoveAt(i);

                if (types.Count == 0)
                    break;
            }


            if (types != null && types.Count != 0)
            {
                
                foreach (var valueType in types)
                    TypeBox.Items.Add(valueType.ToString());

                if (LastSelection.Item2 != null && TypeBox.Items.Contains(LastSelection.Item2))
                    TypeBox.SelectedItem = LastSelection.Item2;
                else
                    TypeBox.SelectedItem = TypeBox.Items[0];
            }
            else
            {

                TypeBox.Text = "";
                TypeBox.Enabled = false;
                UpdatePhaseBox();
            }



        }

        private void UpdatePhaseBox()
        {
            PhaseBox.Enabled = true;
            PhaseBox.Items.Clear();
            Measurement[] measurements = new Measurement[dataGridView1.SelectedRows.Count];
            int cont = 0;
            foreach (DataGridViewRow linha in dataGridView1.SelectedRows)
            {
                measurements[cont] = (Measurement)linha.Cells[5].Value;
                cont++;
            }

            ChannelQuantity quantity;
            if (QuantityBox.SelectedItem == null)
                quantity = ChannelQuantity.OTHER;
            else
                quantity = Channel.GetQuantityFromString(QuantityBox.SelectedItem.ToString());

            ChannelValueType type;
            if (TypeBox.SelectedItem == null)
                type = ChannelValueType.NONE;
            else
                type = Channel.GetValueTypeFromString(TypeBox.SelectedItem.ToString());

            List<ChannelPhase> phases = null;
            foreach (Measurement measurement in measurements)
            {
                List<ChannelPhase> measureQuantities = new List<ChannelPhase>();
                foreach (Channel channel in measurement.Series.Keys)
                    if (channel.Quantity == quantity && channel.Value == type && !measureQuantities.Contains(channel.Phase) && channel.Phase != ChannelPhase.NONE)
                        measureQuantities.Add(channel.Phase);

                if (phases == null)
                {
                    phases = measureQuantities;
                    continue;
                }


                for (int i = phases.Count - 1; i >= 0; i--)
                    if (!measureQuantities.Contains(phases[i]))
                        phases.RemoveAt(i);

                if (phases.Count == 0)
                    break;
            }


            if (phases != null && phases.Count != 0)
            {
                if (measurements.Length == 1 && phases.Contains(ChannelPhase.PHASE_A) && phases.Contains(ChannelPhase.PHASE_B) && phases.Contains(ChannelPhase.PHASE_C))
                    PhaseBox.Items.Add("Trifasico");

                foreach (var phase in phases)
                    PhaseBox.Items.Add(phase.ToString().Substring(phase.ToString().Length - 1));

                if (LastSelection.Item3 != null && PhaseBox.Items.Contains(LastSelection.Item3))
                    PhaseBox.SelectedItem = LastSelection.Item3;
                else
                    PhaseBox.SelectedItem = PhaseBox.Items[0];
            }
            else
            {
                PhaseBox.Enabled = false;
                PhaseBox.Text = "";

            }



        }

        private void RadioButton_Click(object sender, EventArgs e)
        {
            Habilitacao();
        }

        private void Form6_FormClosed(object sender, FormClosedEventArgs e)
        {
            // Lê o valor do label3, este é o índice de saída da lista
            pai.RemLista(Query);

            pai.NumeroProcessos();

            long mem = GC.GetTotalMemory(false);
            GC.Collect();
            mem = GC.GetTotalMemory(false);
            GC.WaitForPendingFinalizers();
            mem = GC.GetTotalMemory(false);
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            //só permite que sejam inseridos números, backspace e separador decimal (. ou , dependendo da região)
            char separadorDecimal = Convert.ToChar(System.Globalization.CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator);
            if (!Char.IsDigit(e.KeyChar) && e.KeyChar != '\b' && (e.KeyChar != separadorDecimal || ((TextBox)sender).Text.Contains(separadorDecimal)))
            {
                e.Handled = true;
            }
        }

        private void FillTree()
        {
            try
            {
                treeView1.Nodes.Clear();

                TreeNode mainNode = new TreeNode("Todos os terminais") { Name = "Todos os terminais" };
                treeView1.Nodes.Add(mainNode);

                foreach (var s in Query.Measurements)
                {
                    // Área de controle
                    if (!mainNode.Nodes.ContainsKey(s.Terminal.Area))
                    {
                        //treeView1.Nodes.Add(s.area);                    
                        mainNode.Nodes.Add(new TreeNode(s.Terminal.Area) { Name = s.Terminal.Area });
                    }
                    // Unidade da Federação
                    if (!mainNode.Nodes[s.Terminal.Area].Nodes.ContainsKey(s.Terminal.State))
                    {
                        mainNode.Nodes[s.Terminal.Area].Nodes.Add(new TreeNode(s.Terminal.State) { Name = s.Terminal.State });
                    }
                    // Estação
                    if (!mainNode.Nodes[s.Terminal.Area].Nodes[s.Terminal.State].Nodes.ContainsKey(s.Terminal.Station))
                    {
                        mainNode.Nodes[s.Terminal.Area].Nodes[s.Terminal.State].Nodes.Add(new TreeNode(s.Terminal.Station) { Name = s.Terminal.Station });
                    }
                    // Nível de tensão
                    if (!mainNode.Nodes[s.Terminal.Area].Nodes[s.Terminal.State].Nodes[s.Terminal.Station].Nodes.ContainsKey(s.Terminal.VoltageLevel.ToString()))
                    {
                        mainNode.Nodes[s.Terminal.Area].Nodes[s.Terminal.State].Nodes[s.Terminal.Station].Nodes.Add(new TreeNode(s.Terminal.VoltageLevel.ToString()) { Name = s.Terminal.VoltageLevel.ToString() });
                    }
                    // PMU -> por enquanto só se pode ter PMUs com idNames únicos, acho que esse é o caminho mesmo
                    if (!mainNode.Nodes[s.Terminal.Area].Nodes[s.Terminal.State].Nodes[s.Terminal.Station].Nodes[s.Terminal.VoltageLevel.ToString()].Nodes.ContainsKey(s.Terminal.Id))
                    {
                        mainNode.Nodes[s.Terminal.Area].Nodes[s.Terminal.State].Nodes[s.Terminal.Station].Nodes[s.Terminal.VoltageLevel.ToString()].Nodes.Add(new TreeNode(s.Terminal.Id) { Name = s.Terminal.Id });
                    }
                }

                treeView1.Sort();

                mainNode.Checked = false;
                CheckAllChildNodes(mainNode, false);
            }
            catch (Exception)
            {

            }
        }

        private void FillDGView()
        {
            try
            {
                // Cria a tabela que será preenchida com o arquivo "terminais.cfg"
                DataTable tabelaTerminais = new DataTable();
                DataColumn[] keys = new DataColumn[5];
                // Coluna para a tabela
                DataColumn coluna = null;

                // Colunas
                coluna = new DataColumn("area");
                tabelaTerminais.Columns.Add(coluna);
                keys[0] = coluna;

                coluna = new DataColumn("state");
                tabelaTerminais.Columns.Add(coluna);
                keys[1] = coluna;

                coluna = new DataColumn("station");
                tabelaTerminais.Columns.Add(coluna);
                keys[2] = coluna;

                // necessário definir o tipo para que a ordenação numérica dos valores funcione
                coluna = new DataColumn("voltLevel", typeof(double));
                tabelaTerminais.Columns.Add(coluna);
                keys[3] = coluna;

                coluna = new DataColumn("idName");
                tabelaTerminais.Columns.Add(coluna);
                keys[4] = coluna;

                tabelaTerminais.PrimaryKey = keys;

                coluna = new DataColumn("measurement", typeof(Measurement));
                tabelaTerminais.Columns.Add(coluna);

                coluna = new DataColumn("id");
                tabelaTerminais.Columns.Add(coluna);

                // linhas
                DataRow linha = null;

                int i = 0;
                foreach (var s in Query.Measurements)
                {
                    linha = tabelaTerminais.NewRow();
                    linha["area"] = s.Terminal.Area;
                    linha["state"] = s.Terminal.State;
                    linha["station"] = s.Terminal.Station;
                    linha["voltLevel"] = s.Terminal.VoltageLevel;
                    linha["idName"] = s.Terminal.Id;
                    linha["measurement"] = s;
                    if (Query.System.Type == DatabaseType.Historian_OpenPDC)
                        linha["id"] = i++;
                    else if (Query.System.Type == DatabaseType.Medfasee)
                        linha["id"] = s.Terminal.IdNumber;

                    tabelaTerminais.Rows.Add(linha);
                }

                // Relaciona o DataGrid com a tabela
                dataGridView1.DataSource = tabelaTerminais;

                // Definir os títulos das colunas 
                dataGridView1.Columns[0].HeaderText = "Área / Região";
                dataGridView1.Columns[1].HeaderText = "Unidade";
                dataGridView1.Columns[2].HeaderText = "Estação";
                dataGridView1.Columns[3].HeaderText = "Nível de Tensão ( kV )";
                dataGridView1.Columns["voltLevel"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dataGridView1.Columns[4].HeaderText = "PMU / Terminal";

                // Tudo automático
                dataGridView1.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                dataGridView1.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                dataGridView1.Columns[2].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                dataGridView1.Columns[3].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                dataGridView1.Columns[4].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;

                // Coluna dos índices iniciais das linhas
                dataGridView1.Columns[5].Visible = false;
                dataGridView1.Columns[6].Visible = false;

                dataGridView1.Sort(dataGridView1.Columns[0], ListSortDirection.Ascending);

            }
            catch (Exception)
            {

            }
        }

        // Updates all child tree nodes recursively.
        private void CheckAllChildNodes(TreeNode treeNode, bool nodeChecked)
        {
            foreach (TreeNode node in treeNode.Nodes)
            {
                if (node.Checked != nodeChecked)
                    node.Checked = nodeChecked;
                if (node.Nodes.Count > 0)
                {
                    // If the current node has child nodes, call the CheckAllChildsNodes method recursively.
                    this.CheckAllChildNodes(node, nodeChecked);
                }
            }
        }

        private void treeView1_AfterCheck(object sender, TreeViewEventArgs e)
        {
            treeView1.BeginUpdate();

            // The code only executes if the user caused the checked state to change.
            if (e.Action != TreeViewAction.Unknown)
            {

                CheckParents(e.Node, !e.Node.Checked);

                if (e.Node.Nodes.Count > 0)
                {

                    /* Calls the CheckAllChildNodes method, passing in the current 
                    Checked value of the TreeNode whose checked state changed. */
                    this.CheckAllChildNodes(e.Node, e.Node.Checked);
                }

            }
            if (e.Node.Nodes.Count == 0)
                UpdateNodeList(e.Node);

            treeView1.EndUpdate();

            // Aqui pode contar o número de linhas no DGView para atualizar o contador de terminais disponíveis
            avCounter.Text = dataGridView1.Rows.GetRowCount(DataGridViewElementStates.Visible).ToString();
        }

        private void CheckParents(TreeNode node, bool uncheck = false)
        {
            if (uncheck)
            {
                TreeNode parent = node.Parent;
                while (parent != null)
                {
                    parent.Checked = false;
                    parent = parent.Parent;
                }
            }
            else
            {
                TreeNode parent = node.Parent;
                while (parent != null && IsChildrenChecked(parent))
                {

                    parent.Checked = true;
                    parent = parent.Parent;
                }
            }
        }

        private bool IsChildrenChecked(TreeNode node)
        {
            foreach (TreeNode child in node.Nodes)
            {
                if (!child.Checked)
                    return false;
            }
            return true;
        }

        List<int> indVisible = new List<int>();
        private void UpdateNodeList(TreeNode node)
        {
            indVisible.Clear();

            string[] s = node.FullPath.Replace("Todos os terminais\\", "").Split('\\');
            DataRow row = ((DataTable)dataGridView1.DataSource).NewRow();
            row["area"] = s[0];
            row["state"] = s[1];
            row["station"] = s[2];
            row["voltLevel"] = s[3];
            row["idName"] = s[4];

            int id = Convert.ToInt16(((DataTable)dataGridView1.DataSource).Rows.Find(s)["id"]); // Pega a celula ID da DataRow
            int i;
            for (i = 0; i < dataGridView1.Rows.Count; i++)
                if (Convert.ToInt16(dataGridView1.Rows[i].Cells[6].Value) == id) //Acha qual DataGridViewRow possui o campo ID igual ao id, vai ser diferente caso tenha ocorrido um sorting.
                    break;

            dataGridView1.Rows[i].Selected = false;

            if (node.Checked)
                dataGridView1.Rows[i].Visible = true;
            else
            {
                CurrencyManager currencyManager1 = (CurrencyManager)BindingContext[dataGridView1.DataSource]; // Necessario por que se você tentar tornar invisivel uma row selecionada o programa dá uma exceção
                currencyManager1.SuspendBinding();                                                            // Mesmo descelecionando todas as rows uma dela ainda fica selecionada.
                dataGridView1.Rows[i].Visible = false;
                currencyManager1.ResumeBinding();
            }
            buttonPlotar.Enabled = dataGridView1.Rows.GetRowCount(DataGridViewElementStates.Selected) > 0;

            foreach (DataGridViewRow r in dataGridView1.Rows)
            {
                if (r.Visible == true)
                {
                    indVisible.Add(Convert.ToInt16(r.Cells[6].Value));
                }
            }
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (!row.Visible)
                    row.Selected = false;
            }

            // Atualiza o contador de terminais selecionados
            selCounter.Text = dataGridView1.SelectedRows.Count.ToString();

            Habilitacao();
        }
        
        private void dataGridView1_Sorted(object sender, EventArgs e)
        {
            foreach (DataGridViewRow r in dataGridView1.Rows)
            {
                if (indVisible.FindIndex(item => item == Convert.ToInt16(r.Cells[6].Value)) == -1)
                {
                    CurrencyManager currencyManager1 = (CurrencyManager)BindingContext[dataGridView1.DataSource]; // Necessario por que se você tentar tornar invisivel uma row selecionada o programa dá uma exceção
                    currencyManager1.SuspendBinding();                                                            // Mesmo descelecionando todas as rows uma dela ainda fica selecionada.
                    r.Visible = false;
                    r.Selected = false;
                    currencyManager1.ResumeBinding();
                }
            }
            Habilitacao();
        }

        private void ProcessaBusca_Load(object sender, EventArgs e)
        {
            CurrencyManager currencyManager1 = (CurrencyManager)BindingContext[dataGridView1.DataSource]; // Necessario por que se você tentar tornar invisivel uma row selecionada o programa dá uma exceção
            currencyManager1.SuspendBinding();                                                            // Mesmo descelecionando todas as rows uma dela ainda fica selecionada.

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {

                row.Selected = false;
                row.Visible = false;
            }

            currencyManager1.ResumeBinding();

            avCounter.Text = dataGridView1.Rows.GetRowCount(DataGridViewElementStates.Visible).ToString();
        }

        private void QuantityBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateTypeBox();
            UpdatePhaseBox();
        }

        private void TypeBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdatePhaseBox();
        }

        private void PhaseBox_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

    }
}
