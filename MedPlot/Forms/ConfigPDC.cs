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
using System.Xml.Linq;
using System.Xml.Schema;
using System.IO;
using System.Globalization;

namespace MedPlot
{
    public partial class ConfigPDC : Form
    {
        JanelaPrincipal pai;

        bool saved = true;
        bool ignoreComboChanges = false;
        System.IO.DirectoryInfo pdcDir = new System.IO.DirectoryInfo(System.IO.Directory.GetCurrentDirectory() + "\\Config");
        XNamespace ns = "smsf2";
        int terminalCounter = 1;
        List<Terminal> terminais = new List<Terminal>();

        static string[] lookupType = new string[] { "TENSAO_", "CORRENTE_" };
        static string[] lookupPhase = new string[] { "A", "B", "C" };

        public ConfigPDC(JanelaPrincipal frm1)
        {
            pai = frm1;
            InitializeComponent();
            setupForm();

            this.toolTips.SetToolTip(terminalAdd, "Adicionar Terminal");
            this.toolTips.SetToolTip(measureAdd, "Adicionar Medida");
            this.toolTips.SetToolTip(nodeDelete, "Deletar Terminal/Medida");


        }



        #region CLASSES

        private class Terminal
        {

            public List<int> availableVoltage = new List<int> { 0, 1, 2 };
            public List<int> availableCurrent = new List<int> { 0, 1, 2 };
            public int n = 1;
            public string id = "Novo Terminal";
            public string nome = "";
            public double tensao = 0;
            public string area = "";
            public string estado = "";
            public string station = "";
            public double lat = 0;
            public double lon = 0;
            public string freq = "0";
            public string dfreq = "0";
            public List<Medida> measures = new List<Medida>();

            public Terminal(int terminalCounter=1)
            {
                n = terminalCounter;
            }

            public Terminal(XElement node, int type, XNamespace ns, int probableN)
            {
                n = probableN;
                if (type == 1)
                    try
                    {
                        n = Convert.ToInt16(node.Element(ns + "idNumber").Value);
                    }
                    catch
                    {

                    }
                id = node.Element(ns + "idName").Value;
                nome = node.Element(ns + "fullName").Value;
                tensao = Convert.ToDouble(node.Element(ns + "voltLevel").Value);
                area = node.Element(ns + "local").Element(ns + "area").Value;
                estado = node.Element(ns + "local").Element(ns + "state").Value;
                station = node.Element(ns + "local").Element(ns + "station").Value;

                lat = double.Parse(node.Element(ns + "local").Element(ns + "lat").Value, CultureInfo.InvariantCulture);
                lon = double.Parse(node.Element(ns + "local").Element(ns + "lon").Value, CultureInfo.InvariantCulture);
                string reading;

                reading = node.Element(ns + "measurements").Element(ns + "freq").Element(ns + "fId").Value;
                freq = (reading == null ? "0" : reading);
                reading = node.Element(ns + "measurements").Element(ns + "dFreq").Element(ns + "dfId").Value;
                dfreq = (reading == null ? "0" : reading);


                foreach (XElement measure in node.Element(ns + "measurements").Elements())
                    if(measure.Name == ns + "phasor")
                    {
                        Medida tmp = new Medida( measure, type, ns);
                        if (tmp.type == 0)
                        {
                            if (availableVoltage.Contains(tmp.phase))
                                availableVoltage.Remove(tmp.phase);
                        }
                        else
                        {
                            if (availableCurrent.Contains(tmp.phase))
                                availableCurrent.Remove(tmp.phase);
                        }
                        measures.Add(tmp);
                    }
            }

            public bool validate()
            {
                try
                {
                    if (!(id.Length > 0 && nome.Length > 0 && area.Length > 0 && estado.Length > 0 && station.Length > 0))
                        return false;
                    if (!(tensao >= 0 && Math.Abs(lat) <= 90 && Math.Abs(lon) <= 90))
                        return false;
                }
                catch
                {
                    return false;
                }

                return true;
            }

            public override string ToString()
            {
                return Id;
            }

            public string Id
            {
                get
                {
                    return id;
                }
                set
                {
                    id = value;
                }

            }

            public string this[string key] // Simula um dictionary
            {
                get
                {
                    if (key == "terminalN")
                        return n.ToString();

                    if (key == "terminalID")
                        return id;

                    if (key == "terminalFreq")
                        return freq;

                    if (key == "terminaldFreq")
                        return dfreq;

                    if (key == "terminalName")
                        return nome;

                    if (key == "terminalVoltage")
                        return (tensao/1000).ToString();

                    if (key == "terminalArea")
                        return area;

                    if (key == "terminalState")
                        return estado;

                    if (key == "terminalStation")
                        return station;

                    if (key == "terminalLat")
                        return lat.ToString();

                    if (key == "terminalLon")
                        return lon.ToString();
                    return "";
                }
                set
                {
                    if (key == "terminalN")
                        try
                        {
                            n = Convert.ToInt16(value);
                        }
                        catch
                        {
                            n = 1;
                        }
                    if (key == "terminalID")
                        id = value;

                    if (key == "terminalFreq")
                        freq = value;

                    if (key == "terminaldFreq")
                        dfreq = value;

                    if (key == "terminalName")
                        nome = value;

                    if (key == "terminalVoltage")
                        try
                        {
                            if (value.Contains('-'))
                                tensao = 0;
                            else
                            tensao = Convert.ToDouble(value)*1000;
                        }
                        catch
                        {
                            tensao = 0;
                        }

                    if (key == "terminalArea")
                        area = value;

                    if (key == "terminalState")
                        estado = value;

                    if (key == "terminalStation")
                        station = value;

                    if (key == "terminalLat")
                        try
                        {
                            lat = Convert.ToDouble(value);
                        }
                        catch
                        {
                            lat = 0;
                        }

                    if (key == "terminalLon")
                        try
                        {
                            lon = Convert.ToDouble(value);
                        }
                        catch
                        {
                            lon = 0;
                        }
                }
            }

            public XElement getNode(int pdc)
            {
                XNamespace ns = "smsf2";

                if (!validate())
                    return null;
                if (n <= 0)
                    return null;

                //Tags de Area
                XElement local = new XElement(ns + "local",
                                    new XElement(ns + "area", area),
                                    new XElement(ns + "state", estado),
                                    new XElement(ns + "station", station),
                                    new XElement(ns + "lat", lat),
                                    new XElement(ns + "lon", lon)

                    );

                //Tags de measurements
                XElement measurements = new XElement(ns + "measurements");
                XElement phasor;
                foreach (Medida med in measures)
                {
                    phasor = med.getNode(pdc);
                    if (phasor == null)
                        return null;
                    measurements.Add(phasor);
                }

                measurements.Add(new XElement(ns + "freq",
                                    new XElement(ns + "fName", "FREQUENCIA"),
                                    new XElement(ns + "fId", (freq.Trim() == ""? "0" : freq) )));
                measurements.Add(new XElement(ns + "dFreq",
                                    new XElement(ns + "dfName", "DFREQ"),
                                    new XElement(ns + "dfId", (dfreq.Trim() == "" ? "0" : dfreq))));


                XElement temp = new XElement(ns + "pmu", new XElement(ns + "idName", id),
                                                new XElement(ns + "fullName", nome),
                                                new XElement(ns + "voltLevel", tensao),
                                                local, measurements);

                if (pdc == 1)
                    temp.AddFirst(new XElement(ns + "idNumber", n));


                return temp; 


            }


        }

        private class Medida
        {
            public string nome = "Nova Medida";
            public int type = 0;
            public int phase = 0;
            public string id1 = "";
            public string id2 = "";

            public Medida(int pdc, int type = 0, int phase = 0)
            {
                this.type = type;
                this.phase = phase;
                if(pdc != 0)
                    id1 = (3 * type + phase).ToString();

                nome = lookupType[type] + lookupPhase[phase];


            }

            public Medida(XElement node, int pdc, XNamespace ns)
            {

                    nome = node.Element(ns + "pName").Value;
                    if (node.Element(ns + "pType").Value == "Current")
                        type = 1;
                    else
                        type = 0;

                    if (node.Element(ns + "pPhase").Value == "A")
                        phase = 0;
                    else if (node.Element(ns + "pPhase").Value == "B")
                        phase = 1;
                    else phase = 2;


                    if (pdc == 0)
                    {
                        id1 = node.Element(ns + "modId").Value;
                        id2 = node.Element(ns + "angId").Value;
                    }
                    else
                        id1 = node.Element(ns + "chId").Value;



            }

            public bool validate(int pdc)
            {
                try
                {
                    if (nome.Length <= 0)
                        return false;
                    if (!(Convert.ToInt16(id1) >= 0 && type >= 0 && type < 4 && phase >= 0 && phase < 3))
                        return false;
                    if (pdc == 0)
                        if (!(Convert.ToInt16(id2) >= 0))
                            return false;
                }
                catch
                {
                    return false;
                }

                return true;
            }

            public override string ToString()
            {
                return nome;
            }

            public string Nome
            {
                get
                {
                    return nome;
                }
                set
                {
                    nome = value;
                }
            }

            public string this[string key] // Simula um dictionary
            {
                get
                {
                    if (key == "measureName")
                        return nome;

                    if (key == "measureID1")
                        return id1;

                    if (key == "measureID2")
                        return id2;

                    return "";
                }
                set
                {

                    if (key == "measureName")
                        nome = value;

                    if (key == "measureID1")
                        id1 = value;

                    if (key == "measureID2")
                        id2 = value;


                }
            }

            public XElement getNode(int pdc)
            {

                if (!validate(pdc))
                    return null;

                XNamespace ns = "smsf2";
                XElement phasor;

                string measure;
                if (type == 0)
                    measure = "Voltage";
                else
                    measure = "Current";
                string p;
                if (phase == 0)
                    p = "A";
                else if (phase == 1)
                    p = "B";
                else
                    p = "C";


                phasor = new XElement(ns + "phasor",
                    new XElement(ns + "pName", nome),
                    new XElement(ns + "pType", measure),
                    new XElement(ns + "pPhase", p));


                    if (pdc == 1)
                    {
                        phasor.Add(new XElement(ns + "chId", id1));
                    }
                    else
                    {
                        phasor.Add(new XElement(ns + "modId", id1));
                        phasor.Add(new XElement(ns + "angId", id2));
                    }

                return phasor;
            }

        }

        #endregion

        #region METODOS DO FORM

        private void setupForm()
        {
            pdcGroup.Enabled = false;
            listGroup.Enabled = false;
            terminalGroup.Visible = true;
            terminalGroup.Enabled = false;
            loadPDCList();

            pdcAdd.Enabled = true;



            pdcSave.Enabled = pdcCancel.Enabled = pdcSave.Visible = pdcCancel.Visible = false;

        }

        private bool resetForm()
        {
            if (!saved && DialogResult.Cancel == MessageBox.Show("Esta ação irá cancelar qualquer alteração feita até então, deseja prosseguir sem salvar?", "Aviso", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation))
                return false;

            saved = true;

            

            terminalCount.Text = "0";
            terminalCounter = 1;

            measureType.SelectedItem = 0;
            measurePhase.SelectedItem = 0;
            measureID1.Text = "ID Módulo";
            measureID2.Text = "ID Ângulo";

            terminalN.Text = "";
            terminalID.Text = "";
            terminalName.Text = "";
            terminalVoltage.Text = "";
            terminalArea.Text = "";
            terminalState.Text = "";
            terminalStation.Text = "";
            terminalLat.Text = "";
            terminalLon.Text = "";
            terminalFreq.Text = "";
            terminaldFreq.Text = "";

            pdcBanco.Text = "";
            pdcLogin.Text = "";
            pdcPass.Text = "";
            pdcName.Text = "";
            pdcFPS.SelectedIndex = 0;
            pdcType.SelectedIndex = 0;
            pdcIP.Text = "";

            terminalTree.Nodes.Clear();
            terminais.Clear();

            terminalGroup.Visible = true;
            terminalGroup.Enabled = false;

            listGroup.Enabled = false;

            pdcGroup.Enabled = false;

            pdcAdd.Enabled = true;

            pdcSave.Enabled = pdcCancel.Enabled = pdcSave.Visible = pdcCancel.Visible = false;
            pdcAdd.Enabled = pdcAdd.Visible = pdcDelete.Visible = pdcEdit.Visible = true;

            terminalAdd.Enabled = measureAdd.Enabled = nodeDelete.Enabled = false;

            return true;
        }

        private void loadPDCList()
        {
            bancosDeDados.Items.Clear();

            foreach (System.IO.FileInfo fi in pdcDir.GetFiles()) //Adiciona os arquivos XML existentes em pdcDir ao DropDown do Form.
                if (fi.Extension == ".xml")
                    bancosDeDados.Items.Add(fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length));

            if (bancosDeDados.Items.Count > 0)
            {
                bancosDeDados.Enabled = true;
                pdcEdit.Enabled = pdcDelete.Enabled = true;
                bancosDeDados.SelectedIndex = 0;
            }
            else
                pdcEdit.Enabled = pdcDelete.Enabled = false;
        }

        #endregion

        #region BOTOES
        private void pdcAdd_Click(object sender, EventArgs e)
        {
            if (!resetForm())
                return;

            pdcGroup.Enabled = listGroup.Enabled = true;
            pdcName.Text = "Nome do Banco";
            pdcType.SelectedIndex = 1;
            pdcType.SelectedIndex = 0;
            pdcFPS.SelectedIndex = 1;
            pdcFPS.SelectedIndex = 0;
            pdcIP.Text = "127.0.0.1";

            saved = true;

            bancosDeDados.Enabled = pdcAdd.Enabled = pdcAdd.Visible = pdcEdit.Enabled = pdcEdit.Visible = pdcDelete.Enabled = pdcDelete.Visible = false;
            pdcSave.Enabled = pdcSave.Visible = pdcCancel.Enabled = pdcCancel.Visible = terminalAdd.Enabled = true;
        }

        private void pdcEdit_Click(object sender, EventArgs e)
        {
            XDocument file = XDocument.Load(pdcDir + "\\" + bancosDeDados.Items[bancosDeDados.SelectedIndex] + ".xml");

            resetForm();

            

            XElement pdc = file.Root.Element(ns + "pdc");
            pdcName.Text = pdc.Element(ns + "name").Value;
            pdcType.SelectedIndex = 0;
            pdcType.SelectedIndex = 1;
            if (pdc.Element(ns + "type").Value == "openpdc")
                pdcType.SelectedIndex = 0;
            else
                pdcType.SelectedIndex = 1;
            string fps = pdc.Element(ns + "fps").Value;
            if (fps == "01")
                pdcFPS.SelectedIndex = 0;
            else if (fps == "10")
                pdcFPS.SelectedIndex = 1;
            else if (fps == "25")
                pdcFPS.SelectedIndex = 2;
            else if (fps == "30")
                pdcFPS.SelectedIndex = 3;
            else if (fps == "50")
                pdcFPS.SelectedIndex = 4;
            else
                pdcFPS.SelectedIndex = 5;

            pdcIP.Text = pdc.Element(ns + "address").Value;
            pdcLogin.Text = pdc.Element(ns + "security").Element(ns + "user").Value;
            pdcPass.Text = pdc.Element(ns + "security").Element(ns + "pswd").Value;
            if (pdcType.SelectedIndex == 1)
                pdcBanco.Text = pdc.Element(ns + "dataBank").Value;


            foreach (XElement element in file.Root.Elements())
            {
                if (element.Name == ns + "pmu")
                {
                    Terminal term = new Terminal(element, pdcType.SelectedIndex, ns, terminalCounter);
                    terminalCounter++;
                    terminais.Add(term);
                    TreeNode node = terminalTree.Nodes.Add(element.Element(ns+"idName").Value);
                    foreach (Medida med in term.measures)
                        node.Nodes.Add(med.nome);
                }
            }

            bancosDeDados.Enabled = pdcAdd.Enabled = pdcAdd.Visible = pdcEdit.Enabled = pdcEdit.Visible = pdcDelete.Enabled = pdcDelete.Visible = false;
            pdcSave.Enabled = pdcSave.Visible = pdcCancel.Enabled = pdcCancel.Visible = terminalAdd.Enabled = true;

            measureGroup.Visible = measureGroup.Enabled = false;

            pdcGroup.Enabled = true;
            listGroup.Enabled = true;
            terminalGroup.Visible = true;
            terminalAdd.Enabled = true;
            if (terminais.Count > 0)
            {
                measureAdd.Enabled = true;
                nodeDelete.Enabled = true;
                terminalGroup.Enabled = true;   

            }


            terminalCount.Text = terminalTree.Nodes.Count.ToString();

            if (terminalTree.Nodes.Count != 0)
                terminalTree.SelectedNode = terminalTree.Nodes[0];
            terminalTree.Focus();

            saved = true;

        }

        private void pdcDelete_Click(object sender, EventArgs e)
        {
            string name = bancosDeDados.Items[bancosDeDados.SelectedIndex].ToString();
            if (DialogResult.Cancel == MessageBox.Show("Você está prestes a deletar o banco de dados \"" + bancosDeDados.Items[bancosDeDados.SelectedIndex] + "\" Deseja prosseguir?", "Aviso", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation))
                return;

            File.Delete(pdcDir + "\\" + bancosDeDados.Items[bancosDeDados.SelectedIndex] + ".xml");
            loadPDCList();
            MessageBox.Show("Você deletou o banco de dados \"" + name + "\" com sucesso!");
        }

        private void pdcSave_Click(object sender, EventArgs e)
        {

            List<int> erro = new List<int>();


                XElement smsf = new XElement(ns + "smsf");
            XElement pdc = new XElement(ns + "pdc", new XElement(ns + "name", pdcName.Text),
                                                new XElement(ns + "type", pdcType.SelectedItem.ToString().ToLower()),
                                                new XElement(ns + "fps", Convert.ToInt16(pdcFPS.SelectedItem.ToString())),
                                                new XElement(ns + "address", pdcIP.Text),
                                                new XElement(ns + "security", new XElement(ns + "user", pdcLogin.Text), new XElement(ns + "pswd", pdcPass.Text)));
            if (pdcType.SelectedIndex == 1)
                pdc.Add(new XElement(ns + "dataBank", pdcBanco.Text));

            smsf.Add(pdc);
            int i = 0;
            foreach (Terminal term in terminais)
            {
                XElement pmu = term.getNode( pdcType.SelectedIndex );
                if (pmu == null)
                {
                    erro.Add(i);
                }
                else
                    smsf.Add(pmu);
                i++;
            }
            if (erro.Count != 0)
                if (DialogResult.Cancel == MessageBox.Show("Houve um erro ao tentar salvar " + erro.Count + " terminais, deseja salvar mesmo assim?\nIsto irá apagar as informações dos terminais com erro!", "Aviso", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation))
                {
                    MessageBox.Show("Os terminais com erro foram assinalados na lista.");
                    i = 0;
                    foreach (TreeNode node in terminalTree.Nodes)
                    {
                        if (erro.Contains(i))
                            node.ForeColor = System.Drawing.Color.Red;
                        else
                            node.ForeColor = System.Drawing.Color.Black;
                        i++;
                    }
                    return;
                }
                XDocument file = new XDocument(smsf);

            try
            {

                File.Delete(pdcDir.FullName + "\\" + pdcName.Text + ".xml");
                file.Save(pdcDir.FullName + "\\" + pdcName.Text + ".xml");
                MessageBox.Show("O banco de dados foi salvo com sucesso!");
            }
            catch
            {
                MessageBox.Show("Houve algum erro crítico ao salvar o banco de dados.");
            }

            saved = true;
            resetForm();
            pdcType.Text = "";
            pdcFPS.Text = "";
            loadPDCList();
            saved = true;

        }

        private void pdcCancel_Click(object sender, EventArgs e)
        {
            if (resetForm())
            {
                pdcType.Text = "";
                pdcFPS.Text = "";
                if (bancosDeDados.Items.Count > 0)
                {
                    bancosDeDados.Enabled = true;
                    pdcDelete.Enabled = true;
                    pdcEdit.Enabled = true;
                }
                saved = true;
            }
        }

        private void terminalAdd_Click(object sender, EventArgs e)
        {
            saved = false;
            Terminal term = new Terminal(terminalCounter);
            terminalCounter++;
            if (terminalTree.Nodes.Count == 0)
            {
                terminalGroup.Enabled = terminalGroup.Visible = true;

                measureAdd.Enabled = true;
                nodeDelete.Enabled = true;
            }

            terminais.Add(term);
            terminalTree.Nodes.Add(term.id);
            terminalTree.SelectedNode = terminalTree.Nodes[terminalTree.Nodes.Count - 1];
            terminalCount.Text = terminalTree.Nodes.Count.ToString();

            terminalTree.Focus();
        }


        private void measureAdd_Click(object sender, EventArgs e)
        {
            Terminal term;
            TreeNode selectedNode;
            if (terminalTree.SelectedNode.Parent == null) //Terminal
                selectedNode = terminalTree.SelectedNode;
            else                                          //Medida
                selectedNode = terminalTree.SelectedNode.Parent;

            term = terminais[selectedNode.Index];
            saved = false;
            int type, phase;


            //Terminal term = terminais[terminalTree.SelectedNode.Index];

            ignoreComboChanges = true;
            findNextChannel(term, out type, out phase);
            Medida med = new Medida(pdcType.SelectedIndex,type, phase);
            term.measures.Add(med);
            if (term.measures.Count == 6)
                measureAdd.Enabled = false;
            selectedNode.Nodes.Add(med.nome);
            terminalTree.SelectedNode = selectedNode.LastNode;
            ignoreComboChanges = false;

            terminalTree.Focus();

        }

        private void nodeDelete_Click(object sender, EventArgs e)
        {
            saved = false;

            if (terminalTree.SelectedNode.Parent == null) //Terminal
            {
                Terminal term = terminais[terminalTree.SelectedNode.Index];

                if (terminalTree.Nodes.Count == 1)
                {
                    terminalN.Text = "";
                    terminalArea.Text = "";
                    terminalState.Text = "";
                    terminalStation.Text = "";
                    terminalID.Text = "";
                    terminaldFreq.Text = "";
                    terminalFreq.Text = "";
                    terminalLat.Text = "";
                    terminalLon.Text = "";
                    terminalVoltage.Text = "";
                    terminalName.Text = "";
                    terminalGroup.Visible = true;
                    terminalGroup.Enabled = measureGroup.Enabled = measureGroup.Visible = nodeDelete.Enabled = measureAdd.Enabled = false;
                    terminais.Clear();
                    terminalTree.Nodes.Clear();

                }
                else
                {
                    TreeNode parent = terminalTree.SelectedNode.PrevNode;
                    TreeNode child = terminalTree.SelectedNode;
                    if (parent == null)
                        parent = terminalTree.SelectedNode.NextNode;

                    terminalTree.SelectedNode = parent;
                    child.Remove();
                    terminais.Remove(term);

                    terminalTree.Focus();
                    terminalTree.HideSelection = false;
                }

            }
            else
            {
                Terminal term = terminais[terminalTree.SelectedNode.Parent.Index];
                Medida med = term.measures[terminalTree.SelectedNode.Index];
                if (med.type == 0 && !term.availableVoltage.Contains(med.phase) && !checkExistingMeasures(term, med))
                    term.availableVoltage.Add(med.phase);
                else if (med.type == 1 && !term.availableCurrent.Contains(med.phase) && !checkExistingMeasures(term, med))
                    term.availableCurrent.Add(med.phase);
                TreeNode parent = terminalTree.SelectedNode.Parent;
                TreeNode child = terminalTree.SelectedNode;
                if (terminalTree.SelectedNode.Parent.Nodes.Count == 1)
                {
                    ignoreComboChanges = true;
                    measureID1.Text = "";
                    measureID2.Text = "";
                    measureName.Text = "";
                    measureType.SelectedIndex = 1;
                    measureType.SelectedIndex = 0;
                    measurePhase.SelectedIndex = 1;
                    measurePhase.SelectedIndex = 0;

                    measureGroup.Enabled = measureGroup.Visible = false;
                    terminalGroup.Visible = terminalGroup.Enabled = true;

                    terminalTree.SelectedNode = parent;
                    child.Remove();
                    term.measures.Clear();
                    parent.Nodes.Clear();
                    ignoreComboChanges = false;
                }
                else
                {

                    ignoreComboChanges = true;
                    if (terminalTree.SelectedNode.Index == parent.Nodes.Count - 1)
                        terminalTree.SelectedNode = parent.Nodes[parent.Nodes.Count - 2];
                    else
                        terminalTree.SelectedNode = parent.Nodes[parent.Nodes.Count - 1];
                    child.Remove();
                    term.measures.Remove(med);
                    ignoreComboChanges = false;
                    measureAdd.Enabled = true;

                }
                terminalTree.Focus();
                terminalTree.HideSelection = false;
            }
            terminalCount.Text = terminalTree.Nodes.Count.ToString();

        }
        #endregion

        #region EVENTOS

        private void terminalID_TextChanged(object sender, EventArgs e)
        {
            saved = false;

            if (terminalTree.SelectedNode == null)
                return;

            Terminal term = terminais[ terminalTree.SelectedNode.Index ];
            TextBox tb = ((TextBox)sender);
            if (term == null || tb == null)
                return;


            term[tb.Name] = tb.Text;
            terminalTree.SelectedNode.Text = tb.Text;
        }

        private void terminal_TextChanged(object sender, EventArgs e)
        {
            saved = false;

            if (terminalTree.SelectedNode == null)
                return;

            Terminal term = terminais[terminalTree.SelectedNode.Index];
            TextBox tb = ((TextBox)sender);
            if (term == null || tb == null)
                return;


            term[tb.Name] = tb.Text;
        }

        private void measureName_TextChanged(object sender, EventArgs e)
        {
            saved = false;

            if (terminalTree.SelectedNode == null)
                return;

            Terminal term = terminais[terminalTree.SelectedNode.Parent.Index];
            Medida med = term.measures[terminalTree.SelectedNode.Index];
            TextBox tb = ((TextBox)sender);
            if (med == null || tb == null)
                return;


            med[tb.Name] = tb.Text;
            terminalTree.SelectedNode.Text = tb.Text;
        }

        private void measure_TextChanged(object sender, EventArgs e)
        {
            saved = false;

            if (terminalTree.SelectedNode == null || terminalTree.SelectedNode.Parent == null)
                return;


            Terminal term = terminais[terminalTree.SelectedNode.Parent.Index];
            Medida med = term.measures[terminalTree.SelectedNode.Index];
            TextBox tb = ((TextBox)sender);
            if (med == null || tb == null)
                return;


            med[tb.Name] = tb.Text;
        }

        private void measuresComboChanged(object sender, EventArgs e)
        {
            saved = false;

            Terminal term = terminais[terminalTree.SelectedNode.Parent.Index];

            Medida med = term.measures[terminalTree.SelectedNode.Index];
            ComboBox cb = ((ComboBox)sender);
            if (med == null || cb == null)
                return;

            if (ignoreComboChanges)
                return;
            if (cb.Name == "measureType")
            {
                if (cb.SelectedIndex == med.type) return;
                if (med.type == 0)
                {
                    if (!term.availableVoltage.Contains(med.phase))
                        term.availableVoltage.Add(med.phase);
                    if (term.availableCurrent.Contains(med.phase))
                        term.availableCurrent.Remove(med.phase);
                }
                else
                {
                    if (term.availableVoltage.Contains(med.phase))
                        term.availableVoltage.Remove(med.phase);
                    if (!term.availableCurrent.Contains(med.phase))
                        term.availableCurrent.Add(med.phase);
                }

                med.type = cb.SelectedIndex;

            }
            else
            {
                if (cb.SelectedIndex == med.phase) return;
                if (med.type == 0)
                {
                    if (!term.availableVoltage.Contains(med.phase))
                        term.availableVoltage.Add(med.phase);
                    if (term.availableVoltage.Contains(cb.SelectedIndex))
                        term.availableVoltage.Remove(cb.SelectedIndex);
                }
                else
                {
                    if (!term.availableCurrent.Contains(med.phase))
                        term.availableCurrent.Add(med.phase);
                    if (term.availableCurrent.Contains(cb.SelectedIndex))
                        term.availableCurrent.Remove(cb.SelectedIndex);

                }

                med.phase = cb.SelectedIndex;

            }
        }

        private void terminalTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (terminalTree.SelectedNode.Parent == null) //Terminal
            {
                Terminal term = terminais[terminalTree.SelectedNode.Index];

                if (term.measures.Count == 6)
                    measureAdd.Enabled = false;
                else
                    measureAdd.Enabled = true;
                terminalGroup.Enabled = terminalGroup.Visible = true;
                measureGroup.Enabled = measureGroup.Visible = false;

                terminalN.Text = term.n.ToString();
                terminalID.Text = term.id;
                terminalFreq.Text = term.freq;
                terminaldFreq.Text = term.dfreq;
                terminalName.Text = term.nome;
                terminalVoltage.Text = (term.tensao/1000).ToString();
                terminalLat.Text = term.lat.ToString();
                terminalLon.Text = term.lon.ToString();
                terminalArea.Text = term.area;
                terminalState.Text = term.estado;
                terminalStation.Text = term.station;

            }
            else                                          //Medida
            {
                measureAdd.Enabled = true;
                terminalGroup.Enabled = terminalGroup.Visible = false;
                measureGroup.Enabled = measureGroup.Visible = true;

                Terminal term = terminais[terminalTree.SelectedNode.Parent.Index];
                Medida med = term.measures[terminalTree.SelectedNode.Index];

                if (term.measures.Count == 6)
                    measureAdd.Enabled = false;

                measurePhase.SelectedIndex = med.phase;
                measureType.SelectedIndex = med.type;
                measureID1.Text = med.id1;
                measureID2.Text = med.id2;
                measureName.Text = med.nome;
            }
            
        }


        private void pdcType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (pdcType.SelectedIndex == 0)
            {
                pdcBanco.Enabled = pdcBanco.Visible = pdcBancoLabel.Visible = terminalNLabel.Visible = terminalNLabel.Enabled = terminalN.Visible = terminalN.Enabled = false;
                measureID2Label.Visible = measureID2Label.Enabled = measureID2.Visible = measureID2.Enabled = true;
                measureID1Label.Text = "Módulo";
            }
            else
            {
                pdcBanco.Enabled = pdcBanco.Visible = pdcBancoLabel.Visible = terminalNLabel.Visible = terminalNLabel.Enabled = terminalN.Visible = terminalN.Enabled = true;
                measureID2Label.Visible = measureID2Label.Enabled = measureID2.Visible = measureID2.Enabled = false;
                measureID1Label.Text = "Canal";
            }
        }

        private void terminalTree_BeforeSelect(object sender, TreeViewCancelEventArgs e)
        {

            if (terminalTree.SelectedNode != null && terminalTree.SelectedNode.Parent == null)
            {
                if (terminalTree.SelectedNode.Text == "")
                {
                    terminalTree.SelectedNode.Text = "NovoTerminal";
                    terminais[terminalTree.SelectedNode.Index].id = "NovoTerminal";
                }
            }
        }




        #endregion

        #region OUTROS

        private void findNextChannel(Terminal term, out int type, out int phase)
        {
            type = 0;
            phase = 0;
            if (term.availableVoltage.Count == 0 && term.availableCurrent.Count == 0)
                return;
            if (term.availableVoltage.Count == 0)
            {
                type = 1;
                phase = term.availableCurrent.Min();
                term.availableCurrent.Remove(phase);
            }
            else
            {
                phase = term.availableVoltage.Min();
                term.availableVoltage.Remove(phase);
            }


        }

        private bool checkExistingMeasures(Terminal term, Medida ignore)
        {

            foreach (Medida med in term.measures)
            {
                if (med == ignore)
                    continue;
                if (med.type == ignore.type && med.phase == ignore.phase)
                    return true;
            }
            return false;

        }
        #endregion

    }
}

