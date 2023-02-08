using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;
using MedFasee.Structure;
using MedFasee.Equipment;
using MedFasee.Data;
using MedFasee.Repository;

namespace MedPlot
{
    public partial class BuscaDados : Form
    {
        private JanelaPrincipal pai;

        object[] queryResult;
        bool queryFinished = false;

        private SystemData system;

        List<int> indVisible = new List<int>();

        private bool busReal;

        // Declare P/Invoke methods

        [DllImport("user32")]

        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool revert);

        [DllImport("user32")]

        private static extern int EnableMenuItem(IntPtr hWndMenu, int itemID, int enable);

        // Declare constants (from winuser.h) to pass to the methods

        private const int SC_CLOSE = 0xF060;

        private const int MF_ENABLED = 0x0000;

        private const int MF_GREYED = 0x0001;

        private bool closeBoxEnabled = true;

        public BuscaDados(JanelaPrincipal frm1)
        {
            InitializeComponent();
            pai = frm1;

        }

        private void SetUpQueryFields()
        {
            indVisible.Clear();
            system = SystemData.ReadConfig(Properties.Settings.Default.ConfigFolder + "\\" + Properties.Settings.Default.CurrentConfig + ".xml");
            PreencheResolucoes(system.NominalFrequency);

            SetupDefaultTime();

            FillTree();
            dataGridView1.DataSource = null;
            FillDGView();



            richTextBox1.Clear();
        }

        private void FillListPDC()
        {
            DirectoryInfo dir = new DirectoryInfo(Properties.Settings.Default.ConfigFolder);

            FileInfo[] dir2 = dir.GetFiles();

            if (dir2.Length == 0)
            {
                MessageBox.Show("Você não possuí nenhum arquivo de configuração para o aplicativo Histórico em sua pasta de Configs.");
            }
            else
            {
                string temp;
                foreach (FileInfo folder in dir2)
                {
                    temp = folder.Name.Substring(0, folder.Name.Length - 4);
                    ListaPDC.Items.Add(temp);
                    if (Properties.Settings.Default.CurrentConfig == temp)
                        ListaPDC.SelectedIndex = ListaPDC.Items.Count - 1;

                }
            }

        }

        public bool CloseEnabled
        {
            get { return closeBoxEnabled; }
            set
            {
                EnableMenuItem(GetSystemMenu(this.Handle, false), SC_CLOSE,
                  value ? MF_ENABLED : MF_GREYED);

                closeBoxEnabled = value;
            }
        }

        private void CopyConfig(string dirConsulta)
        {
                File.Copy(Path.Combine(Properties.Settings.Default.ConfigFolder, Properties.Settings.Default.CurrentConfig + ".xml")
                    , Path.Combine(dirConsulta, Properties.Settings.Default.CurrentConfig + ".xml"), true);
        }

        public DateTime SetupDefaultTime( )
        {
            // Horário UTC (Universal Time Coordinated), um minuto atrás
            DateTime result = DateTime.UtcNow.AddMinutes(-1);

            // Preenche todos os campos dos horários, quando o programa é inicializado
            tbHoraInicial.Text = Convert.ToString(result.Hour);
            tbMinutoInicial.Text = Convert.ToString(result.Minute);
            tbSegundoInicial.Text = Convert.ToString(00);
            tbHoraFinal.Text = Convert.ToString(result.Hour);
            tbMinutoFinal.Text = Convert.ToString(result.Minute);
            tbSegundoFinal.Text = Convert.ToString(59);
            tbDia.Text = Convert.ToString(result.Day);
            tbMes.Text = Convert.ToString(result.Month);
            tbAno.Text = Convert.ToString(result.Year);

            return result;
        }

        public DateTime GetStartDate()
        {
            return new DateTime(short.Parse(tbAno.Text), short.Parse(tbMes.Text), short.Parse(tbDia.Text),
                short.Parse(tbHoraInicial.Text), short.Parse(tbMinutoInicial.Text), short.Parse(tbSegundoInicial.Text));
        }

        public DateTime GetFinishDate()
        {
            return new DateTime(short.Parse(tbAno.Text), short.Parse(tbMes.Text), short.Parse(tbDia.Text),
                short.Parse(tbHoraFinal.Text), short.Parse(tbMinutoFinal.Text), short.Parse(tbSegundoFinal.Text),999);
        }

        private void Form4_FormClosed(object sender, FormClosedEventArgs e)
        {
            Form[] filhos = pai.MdiChildren;
            bool a = false;

            foreach (Form filho in filhos)
            {
                if (filho.GetType() == typeof(BuscasFeitas))
                {
                    a = true;
                }
            }

            if (a)
                pai.HabilitaBotao(true);

            pai.HabilitaMenuTerminais(true);

            // Cuidar com esse comando!!!!
            GC.Collect();
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            DateTime queryStart = DateTime.Now;

            // Recebendo as variáveis do obejto de argumentos
            object[] arg = (object[])e.Argument;

            // Repassando os argumentos do objeto para as variáveis internas
            int selectRate = (int)arg[0];
            List<Terminal> terminals = (List<Terminal>)arg[1];
            DateTime start = (DateTime)arg[2];
            DateTime finish = (DateTime)arg[3];

            // Nome da pasta de dados: aaaammdd_hhmmss_hhmmss_tx
            string folderName = string.Format("{0}{1:D2}{2:D2}_{3:D2}{4:D2}{5:D2}_{6:D2}{7:D2}{8:D2}_{9:D2}_{10}",
                start.Year, start.Month, start.Day,
                start.Hour, start.Minute, start.Second,
                finish.Hour, finish.Minute, finish.Second, selectRate, system.Name);

            //string dirDados = Parametros.DirDados;

            Directory.CreateDirectory(Properties.Settings.Default.QueryFolder + "\\" + folderName);

            /////////////////////////////////////////////////////

            // Horários convertidos para SOC
            IMeasurementDb repository = null;

            Query query = new Query(folderName, system, new List<Measurement>());

            if (system.Type == DatabaseType.Medfasee) // PDC MedFasee
                repository = new MeasurementMedFasee(system.Ip,  system.Port, system.User, system.Password, system.Database);
            else if (system.Type == DatabaseType.Historian_OpenPDC)
                repository = new MeasurementHistorian(system.Ip, system.Port, system.User, system.Password);

            int count = 0;

            backgroundWorker1.ReportProgress(0, "Estabelecendo conexão com o PDC...");
            foreach (Terminal terminal in terminals)
            {
                try
                {

                    backgroundWorker1.ReportProgress(0, string.Format("\n\nConsultando o terminal: {0}...\n", terminal.Id));


                    DoQueryWorker.RunWorkerAsync(new object[] { repository, terminal.IdNumber.ToString(), start, finish, terminal.Channels, selectRate, terminal.EquipmentRate, false });

                    while((!queryFinished || DoQueryWorker.IsBusy) && !backgroundWorker1.CancellationPending)
                        System.Threading.Thread.Sleep(500);

                    if (backgroundWorker1.CancellationPending)
                    {
                        e.Result = new object[] { null, folderName };

                        //repository.CancelQuery();

                        return;
                    }

                    Dictionary<Channel, ITimeSeries> result = (Dictionary<Channel, ITimeSeries>)queryResult[0];
                    string errorMessage = (string)queryResult[1];

                    if(result != null)
                    {
                        query.Measurements.Add(new Measurement(terminal, start, finish, selectRate, result));

                        int missing = FindNumberOfMissingData(
                            (int)(Math.Round((finish - start).TotalSeconds) * selectRate), result);

                        // Reporta o andamento das operações
                        backgroundWorker1.ReportProgress(0, string.Format("Total de frames faltantes: {0}\n\n", missing));
                    }
                    else
                    {
                        backgroundWorker1.ReportProgress(0, errorMessage);
                    }



                }
                catch(InvalidConnectionException)
                {
                    MessageBox.Show("Não foi possível conectar ao PDC.", "MedPlot", MessageBoxButtons.OK);
                    busReal = false;
                    break;
                }
                finally
                {
                    backgroundWorker1.ReportProgress(0, string.Format("Total de buscas a realizar: {0}", terminals.Count - count - 1));
                    count++;
                }
            }

            e.Result = new object[] { query, folderName, queryStart };

        }

        private static int FindNumberOfMissingData(int estimatedNumber, Dictionary<Channel, ITimeSeries> series)
        {
            int missing = 0;

            foreach (KeyValuePair<Channel, ITimeSeries> serie in series)
                if (estimatedNumber - serie.Value.Count > missing)
                    missing = estimatedNumber - serie.Value.Count;
            return missing;
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            // mensagem que será mostrada na caixa de informações
            string msg = (string)e.UserState;

            richTextBox1.AppendText(msg);

            richTextBox1.SelectionStart = richTextBox1.Text.Length;
            richTextBox1.ScrollToCaret();
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            object[] arg = (object[])e.Result;

            Query query = (Query)arg[0];
            string folderName = (string)arg[1];
            string fullQueryPath = Properties.Settings.Default.QueryFolder + "\\" + folderName;

            // Depois da operação em paralelo, habilita os comandos
            button1.Enabled = true;
            CancelButton.Enabled = CancelButton.Visible = false;
            tbHoraInicial.Enabled = true;
            tbHoraFinal.Enabled = true;
            tbMinutoInicial.Enabled = true;
            tbMinutoFinal.Enabled = true;
            tbSegundoInicial.Enabled = true;
            tbSegundoFinal.Enabled = true;
            tbDia.Enabled = true;
            tbMes.Enabled = true;
            tbAno.Enabled = true;

            comboBox1.Enabled = true;

            // Desbloquear o botão de fechamento "X"
            CloseEnabled = true;

            // Esconde a gif novamente 
            pictureBox1.Visible = false;

            richTextBox1.SelectionStart = richTextBox1.Text.Length;
            richTextBox1.ScrollToCaret();

            try
            {

                if(query != null)
                {

                    DateTime queryStart = (DateTime)arg[2];


                    CopyConfig(fullQueryPath);
                    DataWriter.WriteMedFasee(query, fullQueryPath);
                    query = null;
                    GC.Collect();


                    // Informação sobre o diretório da consulta corrente
                    DirectoryInfo dir = new DirectoryInfo(fullQueryPath);
                    // Arquivos dos terminais dentro da pasta da consulta
                    FileInfo[] arquivos = dir.GetFiles();

                    // se não existem arquivos de dados, a pasta da conculta é deletada
                    if (!arquivos.Any(item => item.Extension == ".txt"))
                    {
                        Directory.Delete(fullQueryPath, true);
                        busReal = false;
                    }
                    else
                    {
                        // Tempo de execução na leitura dos dados
                        DateTime queryEnd = DateTime.Now;

                        TimeSpan duracao = queryEnd - queryStart;

                        var resposta = MessageBox.Show("Fim das buscas. Tempo de execução: " + duracao,
                            "MedFasee", MessageBoxButtons.OK);

                        // Para selecionar a última busca feita na lista de buscas do Form2
                        busReal = true;

                        pai.BuscaRealizada(busReal, folderName);

                    }
                }
                else
                {
                    richTextBox1.AppendText("\n\nA consulta foi Cancelada");
                    richTextBox1.SelectionStart = richTextBox1.Text.Length;
                    richTextBox1.ScrollToCaret();

                    Directory.Delete(fullQueryPath, true);
                    busReal = false;
                }
                
            }
            catch (Exception ex)
            {
                
            }
        }

        private void FillTree()
        {
            try
            {
                treeView1.Nodes.Clear();

                TreeNode mainNode = new TreeNode("Todos os terminais") { Name = "Todos os terminais" };
                treeView1.Nodes.Add(mainNode);

                foreach (var s in system.Terminals)
                {
                    // Área de controle
                    if (!mainNode.Nodes.ContainsKey(s.Area))
                    {
                        //treeView1.Nodes.Add(s.area);                    
                        mainNode.Nodes.Add(new TreeNode(s.Area) { Name = s.Area });
                    }
                    // Unidade da Federação
                    if (!mainNode.Nodes[s.Area].Nodes.ContainsKey(s.State))
                    {
                        mainNode.Nodes[s.Area].Nodes.Add(new TreeNode(s.State) { Name = s.State });
                    }
                    // Estação
                    if (!mainNode.Nodes[s.Area].Nodes[s.State].Nodes.ContainsKey(s.Station))
                    {
                        mainNode.Nodes[s.Area].Nodes[s.State].Nodes.Add(new TreeNode(s.Station) { Name = s.Station });
                    }
                    // Nível de tensão
                    if (!mainNode.Nodes[s.Area].Nodes[s.State].Nodes[s.Station].Nodes.ContainsKey(s.VoltageLevel.ToString()))
                    {
                        mainNode.Nodes[s.Area].Nodes[s.State].Nodes[s.Station].Nodes.Add(new TreeNode(s.VoltageLevel.ToString()) { Name = s.VoltageLevel.ToString() });
                    }
                    // PMU -> por enquanto só se pode ter PMUs com idNames únicos, acho que esse é o caminho mesmo
                    if (!mainNode.Nodes[s.Area].Nodes[s.State].Nodes[s.Station].Nodes[s.VoltageLevel.ToString()].Nodes.ContainsKey(s.Id))
                    {
                        mainNode.Nodes[s.Area].Nodes[s.State].Nodes[s.Station].Nodes[s.VoltageLevel.ToString()].Nodes.Add(new TreeNode(s.Id) { Name = s.Id });
                    }
                }

                mainNode.Checked = false;
                CheckAllChildNodes(mainNode, false);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
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
               
                coluna = new DataColumn("terminal", typeof(Terminal));
                tabelaTerminais.Columns.Add(coluna);

                coluna = new DataColumn("id");
                tabelaTerminais.Columns.Add(coluna);

                // linhas
                DataRow linha = null;
                
                int i = 0;
                foreach (var s in system.Terminals)
                {
                    linha = tabelaTerminais.NewRow();
                    linha["area"] = s.Area;
                    linha["state"] = s.State;
                    linha["station"] = s.Station;
                    linha["voltLevel"] = s.VoltageLevel;
                    linha["idName"] = s.Id;
                    linha["terminal"] = s;
                    if (system.Type == DatabaseType.Historian_OpenPDC)
                        linha["id"] = i++;
                    else if (system.Type == DatabaseType.Medfasee)
                        linha["id"] = s.IdNumber;

                    tabelaTerminais.Rows.Add(linha);
                }

                

                // Relaciona o DataGrid com a tabela
                dataGridView1.DataSource = tabelaTerminais;

                // Definir os títulos das colunas 
                dataGridView1.Columns["area"].HeaderText = "Área / Região";
                dataGridView1.Columns["state"].HeaderText = "Unidade";
                dataGridView1.Columns["station"].HeaderText = "Estação";
                dataGridView1.Columns["voltLevel"].HeaderText = "Nível de Tensão ( kV )";
                dataGridView1.Columns["voltLevel"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dataGridView1.Columns["idName"].HeaderText = "PMU / Terminal";

                // Tudo automático
                dataGridView1.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                dataGridView1.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                dataGridView1.Columns[2].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                dataGridView1.Columns[3].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                dataGridView1.Columns[4].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;

                // Deixa invisível as colunas que não importam ao usuário
                dataGridView1.Columns[5].Visible = false;
                dataGridView1.Columns[6].Visible = false;

            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void checkBoxWithActions(TreeNode node, bool known)
        {
            treeView1.BeginUpdate();

            // The code only executes if the user caused the checked state to change.
            if (known)
            {

                CheckParents(node, !node.Checked);

                if (node.Nodes.Count > 0)
                {

                    /* Calls the CheckAllChildNodes method, passing in the current 
                    Checked value of the TreeNode whose checked state changed. */
                    this.CheckAllChildNodes(node, node.Checked);
                }

            }
            if (node.Nodes.Count == 0)
                UpdateNodeList(node);

            treeView1.EndUpdate();

            // Aqui pode contar o número de linhas no DGView para atualizar o contador de terminais disponíveis
            avCounter.Text = dataGridView1.Rows.GetRowCount(DataGridViewElementStates.Visible).ToString();
        }

        private void treeView1_AfterCheck(object sender, TreeViewEventArgs e)
        {
            checkBoxWithActions(e.Node, e.Action != TreeViewAction.Unknown);
            
        }
        
        // Updates all child tree nodes recursively.
        private void CheckAllChildNodes(TreeNode treeNode, bool nodeChecked)
        {
            foreach (TreeNode node in treeNode.Nodes)
            {
                if(node.Checked != nodeChecked)
                    node.Checked = nodeChecked;
                if (node.Nodes.Count > 0)
                {
                    // If the current node has child nodes, call the CheckAllChildsNodes method recursively.
                    this.CheckAllChildNodes(node, nodeChecked);
                }
            }
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

        private void PreencheResolucoes(int tx)
        {
            // Preenche o combobox com as resoluções com as quais pode-se realizar buscas de dados
            switch (tx)
            {
                case 1:
                    comboBox1.Items.Add("01 fasor/s");
                    comboBox1.SelectedIndex = 0;
                    break;
                case 10:
                    comboBox1.Items.Add("01 fasor/s");
                    comboBox1.Items.Add("10 fasores/s");
                    comboBox1.SelectedIndex = 1;
                    break;
                case 25:
                    comboBox1.Items.Add("01 fasor/s");
                    comboBox1.Items.Add("10 fasores/s");
                    comboBox1.Items.Add("25 fasores/s");
                    comboBox1.SelectedIndex = 2;
                    break;
                case 30:
                    comboBox1.Items.Add("01 fasor/s");
                    comboBox1.Items.Add("10 fasores/s");
                    comboBox1.Items.Add("30 fasores/s");
                    comboBox1.SelectedIndex = 2;
                    break;
                case 50:
                    comboBox1.Items.Add("01 fasor/s");
                    comboBox1.Items.Add("10 fasores/s");
                    comboBox1.Items.Add("25 fasores/s");
                    comboBox1.Items.Add("50 fasores/s");
                    comboBox1.SelectedIndex = 3;
                    break;
                case 60:
                    comboBox1.Items.Add("01 fasor/s");
                    comboBox1.Items.Add("10 fasores/s");
                    comboBox1.Items.Add("30 fasores/s");
                    comboBox1.Items.Add("60 fasores/s");
                    comboBox1.SelectedIndex = 3;
                    break;
                default:
                    MessageBox.Show("Taxa de aquisição desconhecida informada no arquivo 'terminais.cfg'.");
                    break;
            }
        }

        private void SelectPDC_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.CurrentConfig = (string)ListaPDC.SelectedItem;
            SelectPDC.Enabled = false;

            comboBox1.Items.Clear();
            treeView1.Nodes.Clear();
            dataGridView1.DataSource = null;

            dataGridView1.Refresh();

            Properties.Settings.Default.Save();

            try
            {

                SetUpQueryFields();
                ResetInvisibleRows();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Houve um erro ao carregar o arquivo de configuração: " + ex.Message);
                this.button1.Enabled = false;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Inicia o trabalho de buscas que está no 'backgroundworker'
            if (backgroundWorker1.IsBusy != true)
            {
                // Desabilitar os comandos da janela de buscas
                button1.Enabled =  tbHoraInicial.Enabled = tbHoraFinal.Enabled = false;
                tbMinutoInicial.Enabled = tbMinutoFinal.Enabled =  tbSegundoInicial.Enabled = false;
                tbSegundoFinal.Enabled = tbDia.Enabled = tbMes.Enabled = false;
                tbAno.Enabled =  comboBox1.Enabled =  CloseEnabled = false;

                // Limpa a caixa de informações.
                richTextBox1.Clear();

                List<Terminal> terminals = new List<Terminal>();

                foreach (DataGridViewRow row in dataGridView1.SelectedRows)
                    terminals.Add((Terminal)row.Cells[5].Value);

                DateTime start, finish;

                try
                {
                    start = GetStartDate();
                    finish = GetFinishDate();
                }
                catch(ArgumentOutOfRangeException ex)
                {
                    MessageBox.Show("A data digitada é inválida. Verifique os campos de data.", "Erro!");
                    return;
                }


                if (DateTime.Compare(finish, start) < 0)
                    finish = finish.AddDays(1);

                if (DateTime.Compare(finish, DateTime.UtcNow) > 0)
                {
                    MessageBox.Show("Data inválida, não é possível realizar consultas de dados futuros.", "Erro!");
                    return;
                }

                // Objeto repassado ao outro thread com os parâmetros necessários às buscas
                object[] argumentos = { int.Parse(comboBox1.GetItemText(comboBox1.SelectedItem).Split(new char[] { ' ' })[0]), terminals, start, finish };

                // Inicia a apresentação da gif
                pictureBox1.Visible = true;

                CancelButton.Enabled = CancelButton.Visible = true;

                // Start the asynchronous operation.
                backgroundWorker1.RunWorkerAsync(argumentos);
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


        private void UpdateNodeList(TreeNode node)
        {
            indVisible.Clear();


            string[] s = node.FullPath.Replace("Todos os terminais\\","").Split('\\');
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
            button1.Enabled = dataGridView1.Rows.GetRowCount(DataGridViewElementStates.Selected) > 0;

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
            // Se nenhuma linha está selecionada deve bloquear o botão de buscas
            if (dataGridView1.SelectedRows.Count == 0)
                button1.Enabled = false;
            else
                button1.Enabled = true;

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (!row.Visible)
                    row.Selected = false;                
            }

            // Atualiza o contador de terminais selecionados
            selCounter.Text = dataGridView1.SelectedRows.Count.ToString();
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
        }

        private void ResetInvisibleRows()
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

        private void BuscaDados_Load(object sender, EventArgs e)
        {
            try
            {
                FillListPDC();
                SetUpQueryFields();
                ResetInvisibleRows();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Houve um erro ao carregar o arquivo de configuração: " + ex.Message);
                this.button1.Enabled = false;
            }

        }

        private void ListaPDC_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.CurrentConfig == (string)ListaPDC.SelectedItem)
                SelectPDC.Enabled = false;
            else
                SelectPDC.Enabled = true;
        }

        private void DoQueryWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            queryFinished = false;
            object[] objects = (object[])e.Argument;
            IMeasurementDb repository = (IMeasurementDb)objects[0];
            string idNumber = (string)objects[1];
            DateTime start = (DateTime)objects[2];
            DateTime finish = (DateTime)objects[3];
            List<Channel> channels = (List<Channel>)objects[4];
            int selectRate = (int)objects[5];
            int equipmentRate = (int)objects[6];

            Dictionary<Channel, ITimeSeries> result = null;
            string errorMessage = string.Empty;

            try
            {
                result = repository.QueryTerminalSeries(idNumber, start, finish, channels, selectRate, equipmentRate, false);
            }
            catch (InvalidQueryException ex)
            {
                if (ex.Message == InvalidQueryException.NO_VALID)
                    errorMessage = "Problema na qualidade dos dados. Todos os dados do terminal não apresentam qualidade adequada no período requisitado.\n\n";
                else if (ex.Message == InvalidQueryException.EMPTY)
                    errorMessage = "Não há dados para este terminal no período requisitado.\n\n";
                else if (ex.Message == InvalidQueryException.NO_TABLE)
                    errorMessage = "O Banco SQL não possui tabela correspondente aos dados requisitados.\n\n";
                else if (ex.Message == InvalidQueryException.BAD_HIST_QUERY)
                    errorMessage = "Houve um erro com o webservice do Historian.\n\n";
                else
                    errorMessage = string.Format("Houve um erro não identificado na consulta: {0}\n\n", ex.Message);
            }
            catch (QueryTimeoutException)
            {
                errorMessage = "Problema na requisição dos dados. Tente reduzir o período de dados selecionado.\n\n";
            }
            finally
            {
                if(!DoQueryWorker.CancellationPending)
                    e.Result = new object[] { result , errorMessage};
            }

        }

        private void DoQueryWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error == null && e.Result != null)
                queryResult = (object[])e.Result;
            else
                queryResult = null;
            queryFinished = true;
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            backgroundWorker1.CancelAsync();
            DoQueryWorker.CancelAsync();
            CancelButton.Enabled = false;
        }

        private void DoQueryWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            // mensagem que será mostrada na caixa de informações
            string msg = (string)e.UserState;

            richTextBox1.AppendText(msg);

            richTextBox1.SelectionStart = richTextBox1.Text.Length;
            richTextBox1.ScrollToCaret();
        }
    }
}
