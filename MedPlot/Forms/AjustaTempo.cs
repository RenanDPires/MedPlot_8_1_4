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
    public partial class AjustaTempo : Form
    {
        GraficoTempo f;
        Chart graf;
        DateTime di;
        double tx;
        string axis;
        // Separador decimal
        char decSep;

        public AjustaTempo(GraficoTempo form, Chart grafico, DateTime dataIni, double taxa, string chartAxis)
        {
            InitializeComponent();

            f = form;
            graf = grafico;
            di = dataIni;
            tx = taxa;
            axis = chartAxis;
        }
        

        private void Form12_Shown(object sender, EventArgs e)
        {
            // Preencher os campos com os valores atuais
            // Eixo Y
            textBox1.Text = graf.ChartAreas[0].AxisY.LabelStyle.Interval.ToString();
            textBox2.Text = graf.ChartAreas[0].AxisY.ScaleView.ViewMinimum.ToString();
            textBox3.Text = graf.ChartAreas[0].AxisY.ScaleView.ViewMaximum.ToString();
            // Eixo X
            // Intervalo deve ser convertido de pontos para segundos
            textBox6.Text = (graf.ChartAreas[0].AxisX.LabelStyle.Interval / Convert.ToDouble(tx)).ToString();
            //textBox6.Text = graf.ChartAreas[0].AxisX.LabelStyle.Interval.ToString();

            // Variável de incremento para as datas do eixo X
            long inc = 0;
            DateTime data = new DateTime();
            int minX = Convert.ToInt32(graf.ChartAreas[0].AxisX.ScaleView.ViewMinimum - 1.0);
            int maxX = Convert.ToInt32(graf.ChartAreas[0].AxisX.ScaleView.ViewMaximum - 1.0);

            inc = Convert.ToInt64(Math.Floor((1 / Convert.ToDouble(tx)) * 1000 * (minX)) * 10000);
            data = di.AddTicks(inc);
            maskedTextBox1.Text = data.ToString("HH:mm:ss");
            inc = Convert.ToInt64(Math.Floor((1 / Convert.ToDouble(tx)) * 1000 * (maxX)) * 10000);
            data = di.AddTicks(inc);
            maskedTextBox2.Text = data.ToString("HH:mm:ss");

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
                    graf.ChartAreas[0].AxisY.Minimum = Double.NaN;
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

                #region Eixo Horizontal
                if (!checkBox2.Checked)
                {
                    DateTime minimumDate = DateTime.Parse(maskedTextBox1.Text);
                    DateTime maximumDate = DateTime.Parse(maskedTextBox2.Text);

                    // Caso não hajam incoerências nos valores digitados pelo usuário
                    if ((minimumDate.TimeOfDay.TotalSeconds - di.TimeOfDay.TotalSeconds >= 0) &&
                        ((maximumDate.TimeOfDay.TotalSeconds - di.TimeOfDay.TotalSeconds) * tx < graf.ChartAreas[0].AxisX.Maximum) &&
                        (minimumDate.TimeOfDay.TotalSeconds < maximumDate.TimeOfDay.TotalSeconds))
                    {
                        // Limites da visualização
                        graf.ChartAreas[0].AxisX.ScaleView.Zoom((minimumDate.TimeOfDay.TotalSeconds - di.TimeOfDay.TotalSeconds) * tx + 1.0,
                            (maximumDate.TimeOfDay.TotalSeconds - di.TimeOfDay.TotalSeconds) * tx + 1.0);

                        // Atualiza o flag no form solicitante
                        f.xAuto = false;
                    }
                    else if (minimumDate.TimeOfDay.TotalSeconds > maximumDate.TimeOfDay.TotalSeconds)
                    {
                        MessageBox.Show("Os valores definidos para os limites do eixo horizontal não são coerentes.", "MedPlot - RT", MessageBoxButtons.OK);
                        return;
                    }
                    else if ((minimumDate.TimeOfDay.TotalSeconds - di.TimeOfDay.TotalSeconds < 0) ||
                        ((maximumDate.TimeOfDay.TotalSeconds - di.TimeOfDay.TotalSeconds) * tx > graf.ChartAreas[0].AxisX.Maximum))
                    {
                        MessageBox.Show("Valor de limite além do período da consulta.", "MedPlot - RT", MessageBoxButtons.OK);
                        return;
                    }

                    // Intervalo - eixo horizontal
                    graf.ChartAreas[0].AxisX.Interval = Math.Round(Convert.ToDouble(textBox6.Text) * Convert.ToDouble(tx), 0);
                    graf.ChartAreas[0].AxisX.LabelStyle.Interval = Math.Round(Convert.ToDouble(textBox6.Text) * Convert.ToDouble(tx), 0);
                }
                else
                {
                    // Desfaz novos valores de mínimo e máximo do eixo
                    //graf.ChartAreas[0].AxisX.Minimum = Double.NaN;
                    //graf.ChartAreas[0].AxisX.Maximum = Double.NaN;
                    // Reseta todos os zooms de uma vez só (parâmetro igual a 0)
                    graf.ChartAreas[0].AxisX.ScaleView.ZoomReset(0);
                    // Desfaz novos valores de intervalo para os originais
                    graf.ChartAreas[0].AxisX.Interval = Double.NaN;
                    graf.ChartAreas[0].AxisX.LabelStyle.Interval = Double.NaN;
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

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Intervalo do eixo Y
            if (!char.IsControl(e.KeyChar)
                && !char.IsDigit(e.KeyChar)
                && e.KeyChar != decSep)
                e.Handled = true;
        }

        private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar)
                && !char.IsDigit(e.KeyChar)
                && e.KeyChar != decSep
                && e.KeyChar != '-')
                    e.Handled = true;
        }

        private void textBox3_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar)
                && !char.IsDigit(e.KeyChar)
                && e.KeyChar != decSep
                && e.KeyChar != '-')
                    e.Handled = true;
        }

        private void textBox6_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Intervalo do eixo X
            if (!char.IsControl(e.KeyChar)
                && !char.IsDigit(e.KeyChar)
                && e.KeyChar != decSep)
                e.Handled = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            AppliesProperties();
            // Fecha o 'form'
            this.Close();
        }
    }
}
