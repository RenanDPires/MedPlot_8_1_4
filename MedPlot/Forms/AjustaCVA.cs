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
    public partial class AjustaCVA : Form
    {
        GraficoCVA f;
        string area, axis;
        Chart graf;
        // Separador decimal
        char decSep;

        public AjustaCVA(GraficoCVA frm15, Chart grafico, string chartArea, string chartAxis)
        {
            InitializeComponent();

            // Repasse a variáveis internas
            f = frm15;
            graf = grafico;             
            area = chartArea;
            axis = chartAxis;
        }

        private void Form16_Shown(object sender, EventArgs e)
        {
            try
            {
                // Identificação do separador decimal segundo a cultura
                decSep = Convert.ToChar(System.Globalization.CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator);

                // Preencher os campos com os valores atuais
                // Eixo Y
                textBox1.Text = graf.ChartAreas[area].AxisY.LabelStyle.Interval.ToString();
                textBox2.Text = graf.ChartAreas[area].AxisY.ScaleView.ViewMinimum.ToString();
                textBox3.Text = graf.ChartAreas[area].AxisY.ScaleView.ViewMaximum.ToString();
                // Eixo X
                DateTimeIntervalType tipoIntervalo = graf.ChartAreas[area].AxisX.LabelStyle.IntervalType;
                if (tipoIntervalo == DateTimeIntervalType.Seconds)
                    textBox6.Text = graf.ChartAreas[area].AxisX.LabelStyle.Interval.ToString();
                else if (tipoIntervalo == DateTimeIntervalType.Minutes)
                    textBox6.Text = (graf.ChartAreas[area].AxisX.LabelStyle.Interval * 60).ToString();
                else if (tipoIntervalo == DateTimeIntervalType.Hours)
                    textBox6.Text = (graf.ChartAreas[area].AxisX.LabelStyle.Interval * 3600).ToString();

                DateTime dataIni = DateTime.FromOADate(graf.ChartAreas[area].AxisX.ScaleView.ViewMinimum);
                DateTime dataFin = DateTime.FromOADate(graf.ChartAreas[area].AxisX.ScaleView.ViewMaximum);

                maskedTextBox1.Text = dataIni.ToString("HH:mm:ss");
                maskedTextBox2.Text = dataFin.ToString("HH:mm:ss");

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
                f.xAuto = !graf.ChartAreas[area].AxisX.ScaleView.IsZoomed;
                checkBox2.Checked = f.xAuto;
                // Frequência
                if (area == "ChartArea1")
                {
                    f.freqYAuto = !graf.ChartAreas[area].AxisY.ScaleView.IsZoomed;
                    // Marca os checkbox conforme o flag do form solicitante
                    checkBox1.Checked = f.freqYAuto;
                }
                // Amortecimento
                if (area == "ChartArea2")
                {
                    f.dampYAuto = !graf.ChartAreas[area].AxisY.ScaleView.IsZoomed;
                    // Marca os checkbox conforme o flag do form solicitante
                    checkBox1.Checked = f.dampYAuto;
                }

            }
            catch (Exception)
            {

            }
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
                        graf.ChartAreas[area].AxisY.Minimum = minimumChosen;
                        graf.ChartAreas[area].AxisY.Maximum = maximumChosen;
                        // Limites da visualização
                        graf.ChartAreas[area].AxisY.ScaleView.Zoom(minimumChosen, maximumChosen);
                        // Frequência
                        if (area == "ChartArea1")
                        {
                            f.freqYAuto = false;
                        }
                        // Amortecimento
                        if (area == "ChartArea2")
                        {
                            f.dampYAuto = false;
                        }
                    }
                    else
                    {
                        MessageBox.Show("Os valores definidos para os limites do eixo vertical não são coerentes.", "MedPlot - RT", MessageBoxButtons.OK);
                        return;
                    }

                    // Intervalo - eixo vertical
                    graf.ChartAreas[area].AxisY.LabelStyle.Interval = Convert.ToDouble(textBox1.Text);
                    graf.ChartAreas[area].AxisY.Interval = Convert.ToDouble(textBox1.Text);
                }
                else
                {
                    // Desfaz novos valores de mínimo e máximo do eixo
                    graf.ChartAreas[area].AxisY.Minimum = Double.NaN;
                    graf.ChartAreas[area].AxisY.Maximum = Double.NaN;
                    // Reseta todos os zooms de uma vez só (parâmetro igual a 0)
                    graf.ChartAreas[area].AxisY.ScaleView.ZoomReset(0);
                    // Desfaz novos valores de intervalo para os originais
                    graf.ChartAreas[area].AxisY.LabelStyle.Interval = Double.NaN;
                    graf.ChartAreas[area].AxisY.Interval = Double.NaN;
                    // Atualiza o flag no form solicitante
                    // Frequência
                    if (area == "ChartArea1")
                    {
                        f.freqYAuto = true;
                    }
                    // Amortecimento
                    if (area == "ChartArea2")
                    {
                        f.dampYAuto = true;
                    }
                }

                #endregion
                
                #region Eixo Horizontal
                if (!checkBox2.Checked)
                {
                    // Horário escolhido pelo usuário (data estará errada, será a data atual)
                    DateTime chosenTime = DateTime.Parse(maskedTextBox1.Text);
                    // Data e horário do início do período da consulta
                    DateTime chartDate = DateTime.FromOADate(graf.ChartAreas[0].AxisX.Minimum);
                    // Horário inicial - Atribui a data do período em análise e o horário escolhido pelo usuário
                    DateTime minimumDate = new DateTime(chartDate.Year, chartDate.Month, chartDate.Day, chosenTime.Hour, chosenTime.Minute, chosenTime.Second);
                    // Horário final
                    chosenTime = DateTime.Parse(maskedTextBox2.Text);
                    DateTime maximumDate = new DateTime(chartDate.Year, chartDate.Month, chartDate.Day, chosenTime.Hour, chosenTime.Minute, chosenTime.Second);

                    // Caso não hajam incoerências nos valores digitados pelo usuário
                    if ((minimumDate.TimeOfDay >=  DateTime.FromOADate(graf.ChartAreas[0].AxisX.Minimum).TimeOfDay) &&
                        (maximumDate.TimeOfDay < DateTime.FromOADate(graf.ChartAreas[0].AxisX.Maximum).TimeOfDay) &&
                        (minimumDate.TimeOfDay < maximumDate.TimeOfDay))
                    {
                        // Limites da visualização
                        graf.ChartAreas[0].AxisX.ScaleView.Zoom(minimumDate.ToOADate(), maximumDate.ToOADate());

                        // Atualiza o flag no form solicitante
                        f.xAuto = false;
                    }
                    else if (minimumDate.TimeOfDay > maximumDate.TimeOfDay)
                    {
                        MessageBox.Show("Os valores definidos para os limites do eixo horizontal não são coerentes.", "MedPlot - RT", MessageBoxButtons.OK);
                        return;
                    }
                    else if ((minimumDate.TimeOfDay < DateTime.FromOADate(graf.ChartAreas[0].AxisX.Minimum).TimeOfDay) ||
                        (maximumDate.TimeOfDay > DateTime.FromOADate(graf.ChartAreas[0].AxisX.Maximum).TimeOfDay))
                    {
                        MessageBox.Show("Valor de limite além do período da consulta.", "MedPlot - RT", MessageBoxButtons.OK);
                        return;
                    }

                    // Intervalo
                    // FREQUÊNCIA
                    DateTimeIntervalType tipoIntervalo = graf.ChartAreas[0].AxisX.LabelStyle.IntervalType;
                    if (tipoIntervalo == DateTimeIntervalType.Seconds)
                    {
                        graf.ChartAreas[0].AxisX.Interval = Convert.ToDouble(textBox6.Text);
                        graf.ChartAreas[0].AxisX.LabelStyle.Interval = Convert.ToDouble(textBox6.Text);
                        graf.ChartAreas[1].AxisX.Interval = Convert.ToDouble(textBox6.Text);
                        graf.ChartAreas[1].AxisX.LabelStyle.Interval = Convert.ToDouble(textBox6.Text);
                    }
                    else if (tipoIntervalo == DateTimeIntervalType.Minutes)
                    {
                        graf.ChartAreas[0].AxisX.LabelStyle.Interval = Convert.ToDouble(textBox6.Text) / 60.0;
                        graf.ChartAreas[0].AxisX.LabelStyle.Interval = Convert.ToDouble(textBox6.Text) / 60.0;
                        graf.ChartAreas[1].AxisX.LabelStyle.Interval = Convert.ToDouble(textBox6.Text) / 60.0;
                        graf.ChartAreas[1].AxisX.LabelStyle.Interval = Convert.ToDouble(textBox6.Text) / 60.0;
                    }
                    else if (tipoIntervalo == DateTimeIntervalType.Hours)
                    {
                        graf.ChartAreas[0].AxisX.Interval = Convert.ToDouble(textBox6.Text) / 3600.0;
                        graf.ChartAreas[0].AxisX.LabelStyle.Interval = Convert.ToDouble(textBox6.Text) / 3600.0;
                        graf.ChartAreas[1].AxisX.Interval = Convert.ToDouble(textBox6.Text) / 3600.0;
                        graf.ChartAreas[1].AxisX.LabelStyle.Interval = Convert.ToDouble(textBox6.Text) / 3600.0;
                    }
                }
                else
                {
                    // FREQUÊNCIA
                    // Desfaz novos valores de mínimo e máximo do eixo
                    //graf.ChartAreas[0].AxisX.Minimum = Double.NaN;
                    //graf.ChartAreas[0].AxisX.Maximum = Double.NaN;
                    // Reseta todos os zooms de uma vez só (parâmetro igual a 0)
                    graf.ChartAreas[0].AxisX.ScaleView.ZoomReset(0);
                    // Desfaz novos valores de intervalo para os originais
                    graf.ChartAreas[0].AxisX.Interval = Double.NaN;
                    graf.ChartAreas[0].AxisX.LabelStyle.Interval = Double.NaN;
                    // AMORTECIMENTO
                    //graf.ChartAreas[1].AxisX.Minimum = Double.NaN;
                    //graf.ChartAreas[1].AxisX.Maximum = Double.NaN;
                    graf.ChartAreas[1].AxisX.ScaleView.ZoomReset(0);
                    graf.ChartAreas[1].AxisX.Interval = Double.NaN;
                    graf.ChartAreas[1].AxisX.LabelStyle.Interval = Double.NaN;

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
            // Só permite números, quantidade de segundos
            if (!char.IsControl(e.KeyChar)
                && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }
    }
}
