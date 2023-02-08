using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace WindowsFormsApplication4
{
    public partial class TextBoxExtendido : TextBox
    {
        private string textOLD;
        public TipoDeTexto TiposAceitos { get; set; }
        public enum TipoDeTexto { Int, Float, String };

        public bool PermiteEspaço { get; set; }

        private ToolTip tip;

        public TextBoxExtendido()
        {
            textOLD = "";
            TiposAceitos = TipoDeTexto.String;
            tip = new ToolTip();
            tip.IsBalloon = true;
            PermiteEspaço = true;
        }
        protected override void OnTextChanged(EventArgs e)
        {
            string sp = @System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            if (this.TiposAceitos == TipoDeTexto.Int && !Regex.IsMatch(this.Text, @"^[0-9]*$"))
            {
                this.Text = textOLD;
                this.Select(this.Text.Length, 0);
            }
            else if (this.TiposAceitos == TipoDeTexto.Float && !Regex.IsMatch(this.Text, @"^([-]?([0-9]+(\" + sp + "[0-9]*)?)?)?$"))
            {
                this.Text = textOLD;
                this.Select(this.Text.Length, 0);
            }

            if (this.Text.Contains(" ") && !PermiteEspaço)
            {
                this.Text = this.Text.Replace(" ", string.Empty); 
                this.Select(this.Text.Length, 0);
            }

            textOLD = this.Text;
            base.OnTextChanged(e);
        }

        protected override void OnLeave(EventArgs e)
        {
          /*  if (this.Text.Length < 1 || (this.TiposAceitos == TipoDeTexto.Float && this.Text == "-"))
            {
                this.Text = "0";
            }*/
            base.OnLeave(e);
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            base.OnPaint(pe);
        }

        public double ToDouble()
        {
            if (this.Text == "-" || this.Text.Length < 1) return 0;
            else return Convert.ToDouble(this.Text);
        }
        public Int32 ToInt()
        {
            if (this.Text.Length < 1) return 0;
            else return Convert.ToInt32(this.Text);
        }

    }
}
