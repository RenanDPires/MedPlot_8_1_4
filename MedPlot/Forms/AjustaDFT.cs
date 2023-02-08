using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace MedPlot
{
    public partial class AjustaDFT : Form
    {
        GraficoDFT f;
        Chart graf;
        string axis;
        double tx;
        double fIni;
        // Separador decimal
        char decSep;

        public AjustaDFT(GraficoDFT form, Chart grafico, string chartAxis, double taxa, double fMin)
        {
            InitializeComponent();

            f = form;
            graf = grafico;
            axis = chartAxis;
            tx = taxa;
            fIni = fMin;
        }

        private void Form14_Shown(object sender, EventArgs e)
        {
            // Preencher os campos com os valores atuais
            // Eixo Y
            textBox1.Text = graf.ChartAreas[0].AxisY.LabelStyle.Interval.ToString();
            textBox2.Text = graf.ChartAreas[0].AxisY.ScaleView.ViewMinimum.ToString();
            textBox3.Text = graf.ChartAreas[0].AxisY.ScaleView.ViewMaximum.ToString();
            // Eixo X
            textBox4.Text = graf.ChartAreas[0].AxisX.ScaleView.ViewMinimum.ToString();
            textBox5.Text = graf.ChartAreas[0].AxisX.ScaleView.ViewMaximum.ToString();
            textBox6.Text = graf.ChartAreas[0].AxisX.LabelStyle.Interval.ToString();

            // Define qual tab vai estar selecionado ao abrir o form de acordo
            // com qual eixo foi clicado no gráfico 
            if (axis == "X")
            {
                tabControl1.SelectedTab = tabPage2;
            }
            else if (axis == "Y")
            {
                tabControl1.SelectedTab = tabPage1;
            }

            // Se estiver com zoom, desmarca o automático
            f.yAuto = !graf.ChartAreas[0].AxisY.ScaleView.IsZoomed;
            f.xAuto = !graf.ChartAreas[0].AxisX.ScaleView.IsZoomed;

            // Marca os checkbox conforme o flag do form solicitante
            checkBox1.Checked = f.yAuto;
            checkBox2.Checked = f.xAuto;

            // Identificação do separador decimal segundo a cultura
            decSep = Convert.ToChar(System.Globalization.CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator);
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            // apenas fecha o form sem alterar algo
            this.Close();
        }

        private void buttonApply_Click(object sender, EventArgs e)
        {
            AppliesProperties();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            AppliesProperties();
            // Fecha o 'form'
            this.Close();
        }

        private void AppliesProperties()
        {
            try
            {
                #region Eixo vertical
                if (!checkBox1.Checked)
                {
                    // Configurações do eixo vertical
                    double minimumChosen = Convert.ToDouble(textBox2.Text);
                    double maximumChosen = Convert.ToDouble(textBox3.Text);

                    if (minimumChosen < maximumChosen)
                    {
                        // Limites de fato
                        graf.ChartAreas[0].AxisY.Minimum = minimumChosen;
                        graf.ChartAreas[0].AxisY.Maximum = maximumChosen;
                        // Limites da visualização
                        graf.ChartAreas[0].AxisY.ScaleView.Zoom(minimumChosen, maximumChosen);

                        // Atualiza o flag no form solicitante
                        f.yAuto = false;
                    }
                    else
                    {
                        MessageBox.Show("Os valores definidos para os limites do eixo vertical não são coerentes.", "MedPlot - RT", MessageBoxButtons.OK);
                        return;
                    }

                    // Intervalo - eixo vertical
                    graf.ChartAreas[0].AxisY.LabelStyle.Interval = Convert.ToDouble(textBox1.Text);
                    graf.ChartAreas[0].AxisY.Interval = Convert.ToDouble(textBox1.Text);
                }
                else
                {
                    // Desfaz novos valores de mínimo e máximo do eixo
                    graf.ChartAreas[0].AxisY.Minimum = 0;
                    graf.ChartAreas[0].AxisY.Maximum = Double.NaN;
                    // Reseta todos os zooms de uma vez só (parâmetro igual a 0)
                    graf.ChartAreas[0].AxisY.ScaleView.ZoomReset(0);
                    // Desfaz novos valores de intervalo para os originais
                    graf.ChartAreas[0].AxisY.LabelStyle.Interval = Double.NaN;
                    graf.ChartAreas[0].AxisY.Interval = Double.NaN;
                    // Atualiza o flag no form solicitante
                    f.yAuto = true;
                }

                #endregion

                #region Eixo horixontal
                if (!checkBox2.Checked)
                {
                    // Configurações do eixo vertical
                    double minimumChosen = Convert.ToDouble(textBox4.Text);
                    double maximumChosen = Convert.ToDouble(textBox5.Text);

                    if ((minimumChosen < maximumChosen) && (minimumChosen >= 0)
                        && (maximumChosen < graf.ChartAreas[0].AxisX.Maximum))
                    {
                        // Limites da visualização
                        graf.ChartAreas[0].AxisX.ScaleView.Zoom(minimumChosen, maximumChosen);

                        // Atualiza o flag no form solicitante
                        f.xAuto = false;
                    }
                    else
                    {
                        MessageBox.Show("Os valores definidos para os limites do eixo vertical não são coerentes.", "MedPlot - RT", MessageBoxButtons.OK);
                        return;
                    }

                    // Intervalo - eixo horizontal
                    graf.ChartAreas[0].AxisX.LabelStyle.Interval = Convert.ToDouble(textBox6.Text);
                    graf.ChartAreas[0].AxisX.Interval = Convert.ToDouble(textBox6.Text);
                }
                else
                {
                    // Faz o ajuste do início e fim da escala de acordo com a taxa
                    if (tx != 1)
                    {
                        // Ajeitar a posição inicial do gráfico, independente dos valores de mínimo e máximo
                        if (fIni >= 1.6)
                        {
                            graf.ChartAreas[0].AxisX.ScaleView.Position = 0;
                            graf.ChartAreas[0].AxisX.ScaleView.Size = 1.6;
                        }

                        else
                        {
                            graf.ChartAreas[0].AxisX.ScaleView.Position = fIni;
                            graf.ChartAreas[0].AxisX.ScaleView.Size = 1.6 - fIni;
                        }
                    }
                    else
                    {
                        // Ajeitar a posição inicial do gráfico, independente dos valores de mínimo e máximo
                        if (fIni >= 0.5)
                        {
                            graf.ChartAreas[0].AxisX.ScaleView.Position = 0;
                            graf.ChartAreas[0].AxisX.ScaleView.Size = 0.5;
                        }
                        else
                        {
                            graf.ChartAreas[0].AxisX.ScaleView.Position = fIni;
                            graf.ChartAreas[0].AxisX.ScaleView.Size = 0.5 - fIni;
                        }
                    }
                    // Desfaz novos valores de intervalo para os originais
                    graf.ChartAreas[0].AxisX.LabelStyle.Interval = Double.NaN;
                    graf.ChartAreas[0].AxisX.Interval = Double.NaN;
                    // Atualiza o flag no form solicitante
                    f.xAuto = true;
                }

                #endregion
            }
            catch (Exception)
            {
                MessageBox.Show("Campo inválido.", "MedPlot - RT", MessageBoxButtons.OK);
            }
        }

        private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar)
                && !char.IsDigit(e.KeyChar)
                && e.KeyChar != decSep)
                    e.Handled = true;
        }

        private void textBox3_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar)
                && !char.IsDigit(e.KeyChar)
                && e.KeyChar != decSep)
                    e.Handled = true;
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar)
                && !char.IsDigit(e.KeyChar)
                && e.KeyChar != decSep)
                    e.Handled = true;
        }

        private void textBox4_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar)
                && !char.IsDigit(e.KeyChar)
                && e.KeyChar != decSep)
                    e.Handled = true;
        }

        private void textBox5_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar)
                && !char.IsDigit(e.KeyChar)
                && e.KeyChar != decSep)
                    e.Handled = true;
        }

        private void textBox6_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar)
                && !char.IsDigit(e.KeyChar)
                && e.KeyChar != decSep)
                    e.Handled = true;
        }        
    }
}
