namespace MedPlot
{
    partial class BuscasFeitas
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BuscasFeitas));
            this.treeBuscas = new System.Windows.Forms.TreeView();
            this.SuspendLayout();
            // 
            // treeBuscas
            // 
            this.treeBuscas.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.treeBuscas.Location = new System.Drawing.Point(12, 12);
            this.treeBuscas.Name = "treeBuscas";
            this.treeBuscas.Size = new System.Drawing.Size(373, 329);
            this.treeBuscas.TabIndex = 1;
            this.treeBuscas.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeBuscas_AfterSelect);
            this.treeBuscas.KeyUp += new System.Windows.Forms.KeyEventHandler(this.treeBuscas_KeyUp);
            // 
            // BuscasFeitas
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(397, 353);
            this.Controls.Add(this.treeBuscas);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimumSize = new System.Drawing.Size(370, 392);
            this.Name = "BuscasFeitas";
            this.Text = "Buscas Realizadas";
            this.Activated += new System.EventHandler(this.Form2_Activated);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Form2_FormClosed);
            this.CursorChanged += new System.EventHandler(this.Form2_CursorChanged);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.TreeView treeBuscas;
    }
}