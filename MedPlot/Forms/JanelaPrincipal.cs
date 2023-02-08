using MedFasee.Data;
using MedFasee.Equipment;
using MedFasee.Structure;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Windows.Forms;

namespace MedPlot
{
    public partial class JanelaPrincipal : Form
    {
        // Busca realizada
        public bool br;
        public string nomeAtual;

        public List<Query> queries = new List<Query>(); 

        // Current Selected Query
        public string selectedQuery;

        // Índices de entrada na lista deste form, a lista contém as consultas abertas no momento
        public int indEnt;

        OpenFileDialog opf = new OpenFileDialog();

        // PMUs contidas no arquivo de configuração

        public JanelaPrincipal()
        {
            InitializeComponent();

            try
            {
                // Inicia o programa com o botão "Processar consulta" desabilitado
                HabilitaBotao(false);

                #region Encriptação da seção de settings
                // Chama a função para encriptar a seção de settings
                ConfigEncryption(System.Reflection.Assembly.GetExecutingAssembly().GetName().Name);
                #endregion

                #region Verificação das user settings

                if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\MedPlot")) // se o diretório não existe
                    Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\MedPlot");

                if (Properties.Settings.Default.QueryFolder == "")
                    Properties.Settings.Default.QueryFolder = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\MedPlot\\Dados\\Consultas";

                if (Properties.Settings.Default.OscillationFolder == "")
                    Properties.Settings.Default.OscillationFolder = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\MedPlot\\Dados\\Oscillations";

                if (Properties.Settings.Default.ConfigFolder == "")
                    Properties.Settings.Default.ConfigFolder = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\MedPlot\\Config\\Historico";

                Directory.CreateDirectory(Properties.Settings.Default.ConfigFolder);
                Directory.CreateDirectory(Properties.Settings.Default.QueryFolder);
                Directory.CreateDirectory(Properties.Settings.Default.OscillationFolder);


                if ( new DirectoryInfo(Properties.Settings.Default.ConfigFolder).GetFiles().Length == 0)
                {
                    DirectoryInfo dir = new DirectoryInfo(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\Config");

                    FileInfo[] files = dir.GetFiles();

                    foreach(FileInfo file in files)
                    {
                        if (file.Extension == ".xml")
                            File.Copy(file.FullName, Properties.Settings.Default.ConfigFolder  + "\\" + file.Name);
                    }

                }

                // Preenche o endereço do diretório de configuração se não estiver definido
                if (Properties.Settings.Default.CurrentConfig == "")
                {
                    DirectoryInfo dir = new DirectoryInfo(Properties.Settings.Default.ConfigFolder);

                    FileInfo[] fi = dir.GetFiles();

                    if (fi.Length > 0)
                        Properties.Settings.Default.CurrentConfig = Path.GetFileNameWithoutExtension(fi[0].Name);
                    else
                        MessageBox.Show("Você não possui nenhum arquivo de configuração na sua pasta de configuração do Medplot Histórico!");
                }

                // Salvar as configurações
                Properties.Settings.Default.Save();

                #endregion

            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private void cascadeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.LayoutMdi(System.Windows.Forms.MdiLayout.Cascade);
        }

        private void umToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.LayoutMdi(System.Windows.Forms.MdiLayout.TileHorizontal);
        }

        private void doisToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.LayoutMdi(System.Windows.Forms.MdiLayout.TileVertical);
        }

        private void tresToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.LayoutMdi(System.Windows.Forms.MdiLayout.ArrangeIcons);
        }

        private void sobreToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            Form[] children = MdiChildren;

            foreach (Form child in children)
            {
                if (child.GetType() == typeof(BuscaDados)) // Formulário 4 foi encontrado
                {
                    child.Activate();
                    HabilitaBotao(false);
                    HabilitaMenuTerminais(false);
                    return;
                }

            }

            // Não foi encontrado um Form4
            BuscaDados novo = new BuscaDados(this);
            novo.MdiParent = this;
            novo.Show();

            HabilitaBotao(false);
            HabilitaMenuTerminais(false);

        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            // array com os forms filhos do Form1
            Form[] filhos = MdiChildren;

            foreach (Form filho in filhos)
            {
                if (filho.GetType() == typeof(BuscasFeitas))
                {
                    filho.Activate(); // ativa o Form2 que já existe
                    return;
                }
            }
            // Não foi encontrado um Form2
            BuscasFeitas novo = new BuscasFeitas(this);
            novo.MdiParent = this;
            novo.Show();
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            string nomeCompleto = Properties.Settings.Default.QueryFolder + "\\" + selectedQuery;

            string[] files = Directory.GetFiles(nomeCompleto, "*.xml");

            if (files.Length > 1)
            {
                MessageBox.Show("Esta consulta apresenta mais de um arquivo .xml, por favor abra a pasta e apague os arquivos desnecessários.", "Atenção");
                return;
            }
            else if(files.Length == 0)
            {
                MessageBox.Show("O arquivo de configuração não existe na pasta de dados ou encontra-se corrompido. A seguir, escolha a opção de configuração adequada para a consulta.",
                   "Erro!", MessageBoxButtons.OK);

                Form[] children = MdiChildren;

                foreach (Form child in children)
                {
                    if (child.GetType() == typeof(ConfigPick)) // Formulário 11 foi encontrado
                    {
                        child.Activate();
                        return;
                    }
                }

                // Não foi encontrado um Form11
                ConfigPick novo = new ConfigPick(this, nomeCompleto);
                //novo.MdiParent = this;
                novo.ShowDialog();
            }

            
            try
            {
                // Lê o arquivo de configuração XML
                SystemData data = SystemData.ReadConfig(files[0]);

                // Informação sobre o diretório da consulta corrente
                DirectoryInfo dir = new DirectoryInfo(nomeCompleto);
                // Arquivos dos terminais dentro da pasta da consulta
                FileInfo[] arquivos = dir.GetFiles("*.txt");

                // Se não existem arquivos de dados na pasta.
                if (arquivos.Count() == 0)
                {
                    MessageBox.Show("Não existem arquivos de dados na pasta.", "Erro!", MessageBoxButtons.OK);
                    return;
                }

                // Número de arquivos de dados na pasta
                bool foundTerminal = false;
                foreach (FileInfo file in arquivos)
                {
                    if (data.Terminals.Find(terminal => (terminal.Id + ".txt") == file.Name) != null)
                    {
                        foundTerminal = true;
                        break;
                    }
                }

                // Se houver arquivos mas nenhum estiver listado no 'terminais.cfg'
                if (!foundTerminal)
                {
                    MessageBox.Show("A pasta não contém arquivos de dados listados no arquivo de configuração. A seguir, escolha a opção de configuração adequada para a consulta.",
                        "Erro!", MessageBoxButtons.OK);

                    Form[] children = MdiChildren;

                    foreach (Form child in children)
                    {
                        if (child.GetType() == typeof(ConfigPick)) // Formulário 11 foi encontrado
                        {
                            child.Activate();
                            return;
                        }
                    }

                    // Não foi encontrado um Form11
                    ConfigPick novo = new ConfigPick(this, nomeCompleto);
                    novo.MdiParent = this;
                    novo.Show();

                    foreach (Form child in children)
                    {
                        if (child.GetType() == typeof(BuscasFeitas)) // Formulário 2 foi encontrado
                        {
                            child.Enabled = false;
                            return;
                        }
                    }

                    //return;
                }


                // Flag de erro
                bool flagErro = false;

                Query query = null;
                try
                {
                    query = DataReader.ReadMedFasee(selectedQuery, data, nomeCompleto);

                    foreach(Measurement measurement in query.Measurements)
                    {
                        if(!measurement.Series.ContainsKey(Channel.VOLTAGE_POS_MOD) && Measurement.CanCalculateSequences(measurement))
                        {
                            var voltageSequence = Measurement.CalculatePositiveSequence(measurement);
                            foreach (var keyvalue in voltageSequence)
                                measurement.Series.Add(keyvalue.Key, keyvalue.Value);
                        }

                        if (!measurement.Series.ContainsKey(Channel.CURRENT_POS_MOD) && Measurement.CanCalculateSequences(measurement,true))
                        {
                            var currentSequence = Measurement.CalculatePositiveSequence(measurement,true);
                            foreach (var keyvalue in currentSequence)
                                measurement.Series.Add(keyvalue.Key, keyvalue.Value);
                        }

                        if (!measurement.Series.ContainsKey(Channel.ACTIVE_POWER) && Measurement.CanCalculatePowers(measurement))
                        {
                            var powers = Measurement.CalculatePowers(measurement);
                            foreach (var keyvalue in powers)
                                measurement.Series.Add(keyvalue.Key, keyvalue.Value);
                        }

                        if (!measurement.Series.ContainsKey(Channel.FREQ) && Measurement.CanCalculateFrequency(measurement))
                        {
                            var frequency = Measurement.CalculateFrequency(measurement,query.System.NominalFrequency);
                            measurement.Series.Add(Channel.FREQ, frequency);
                        }

                        if (!measurement.Series.ContainsKey(Channel.DFREQ) && Measurement.CanCalculateRocof(measurement))
                        {
                            var rocof = Measurement.CalculateRocof(measurement);
                            measurement.Series.Add(Channel.FREQ, rocof);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Arquivo corrompido. Mensagem de erro: ' " + ex.Message, "Erro!", MessageBoxButtons.OK);
                    flagErro = true;
                }


                // Criação do Form6
                foreach (Form filho in MdiChildren)
                {
                    if (filho.GetType() == typeof(ProcessaBusca))
                    {
                        HabilitaBotao2(false);
                        filho.Activate();
                        return;
                    }
                }

                if (flagErro != true)
                {
                    // Atualiza a lista com o índice da consulta aberta
                    AddLista(query);

                    HabilitaBotao2(false);
                    // Cria o form de escolha do tipo de gráfico
                    ProcessaBusca novo = new ProcessaBusca(this, query);
                    novo.MdiParent = this;
                    novo.Show();
                }

            }
            catch (Exception ex)
            {
               

                MessageBox.Show("Houve um erro ao carregar o arquivo de configuração: " + ex.Message);
                return;
            }

        }

        // Botões "Processar busca" e "arquivos de dados"
        internal void HabilitaBotao(bool flag)
        {
            // Define a condição de habilitado ou não, dos botões "Processar Consulta" e "Arquivos de Dados"
            toolStripButton3.Enabled = flag;
            toolStripButton4.Enabled = flag;
            toolStripButton5.Enabled = flag;
        }
        // Botão "Nova Busca"
        internal void HabilitaBotao2(bool flag)
        {
            toolStripButton1.Enabled = flag;
        }
        // Menu 'Configurações\Terminais'
        internal void HabilitaMenuTerminais(bool p)
        {
            diretórioDeDadosToolStripMenuItem.Enabled = p;
            editarTerminaisToolStripMenuItem.Enabled = p;
        }
        // Quantos forms de "Geração de Gráficos" estão abertos
        internal void NumeroProcessos()
        {
            Form[] filhos = MdiChildren;
            int i = 0;

            foreach (Form filho in filhos)
            {
                if (filho.GetType() == typeof(ProcessaBusca))
                {
                    i++;
                }
            }

            if (i <= 1)
            {
                HabilitaBotao2(true);
            }
            else
            {
                HabilitaBotao2(false);
            }

        }

        internal void HabilitaLista(bool p)
        {
            Form[] children = MdiChildren;

            foreach (Form child in children)
            {
                if (child.GetType() == typeof(BuscasFeitas)) // Formulário 2 foi encontrado
                {
                    child.Enabled = p;
                    return;
                }
            }

        }
        // Verifica se a janela de buscas de dados está aberta
        internal void NovaBuscaAberta()
        {
            Form[] filhos = MdiChildren;
            int i = 0;

            foreach (Form filho in filhos)
            {
                if (filho.GetType() == typeof(BuscaDados))
                {
                    i++;
                }
            }

            //if (filhos.Where(b => b.GetType() == typeof(BuscaDados)).ToList().Count == 0)
            //    HabilitaBotao(true);

            if (i == 0)
            {
                HabilitaBotao(true);
            }
        }

        internal void AddLista(Query query)
        {
            // Adiciona o item na lista
            queries.Add(query);
        }

        internal void RemLista(Query query)
        {
            //Remove o item da lista
            queries.Remove(query);

            // Fechamento dos gráficos originados da consulta que está sendo encerrada
            Form[] filhos = MdiChildren;
            foreach (Form filho in filhos)
            {
                //se o índice da consulta é igual ao da que está sendo fechada
                if (filho.GetType() == typeof(GraficoTempo))
                {
                    if (((GraficoTempo)filho).Query == query)
                    {
                        filho.Close();
                    }
                }
                else if (filho.GetType() == typeof(GraficoDFT))
                {
                    if (((GraficoDFT)filho).Query == query)
                    {
                        filho.Close();
                    }
                }
                else if (filho.GetType() == typeof(GraficoProny))
                {
                    if (((GraficoProny)filho).Query == query)
                    {
                        filho.Close();
                    }
                }
                else if (filho.GetType() == typeof(GraficoModes))
                {
                    if (((GraficoModes)filho).Query == query)
                    {
                        filho.Close();
                    }
                }
                else if (filho.GetType() == typeof(GraficoCVA))
                {
                    if (((GraficoCVA)filho).Query == query)
                    {
                        filho.Close();
                    }
                }
                else if (filho.GetType() == typeof(GraficoEventos))
                {
                    if (((GraficoEventos)filho).Query == query)
                    {
                        filho.Close();
                    }
                }
                else if (filho.GetType() == typeof(MapaEventos))
                {
                    if (((MapaEventos)filho).Query == query)
                    {
                        filho.Close();
                    }
                }
            }
        }

        internal void CriarDFT(Query query, int op, Measurement reference, Measurement[] measurements, int minInd, int maxInd, int opPu, Color[] coresAtuais)
        {
            GraficoDFT novo = new GraficoDFT(this, query, op, reference, measurements, minInd, maxInd, opPu, coresAtuais);
            novo.MdiParent = this;
            novo.Show();

        }

        internal void CriarProny(Query query, int op, Measurement reference, Measurement[] measurements, int minInd, int maxInd, int opPu, int ordemEsc, Color[] coresAtuais)
        {
            GraficoProny novo = new GraficoProny(this, query, op, reference, measurements, minInd, maxInd, opPu, ordemEsc, coresAtuais);
            novo.MdiParent = this;
            novo.Show();
        }

        internal void CriarRBE(Query query, int op, Measurement reference, Measurement[] measurements, int minInd, int maxInd, int opPu, Color[] coresAtuais, int dimensao, int numBlocos, int tamJanela, int pasJanela, double freqMin, double freqMax)
        {
            GraficoCVA novo = new GraficoCVA(this, query, op, reference, measurements, minInd, maxInd, opPu, coresAtuais, dimensao, numBlocos, tamJanela, pasJanela, freqMin, freqMax);
            novo.MdiParent = this;
            novo.Show();
        }

        internal void CriarModeShapes(double modoEsc, string[] nomesMS, double[] ampMS, double[] angMS, DateTime dataIniMS, DateTime dataFinMS, string titulo, Color[] coresAtuais, Query query)
        {
            GraficoModes novo = new GraficoModes(this, modoEsc, nomesMS, ampMS, angMS, dataIniMS, dataFinMS, titulo, coresAtuais, query);
            novo.MdiParent = this;
            novo.Show();
        }

        internal void CriarGrafico(Query query, int op, Measurement reference, Measurement[] measurements, string diretorio, double vMinFreq)
        {
            GraficoTempo novo = new GraficoTempo(this, query, op, reference, measurements, diretorio, vMinFreq);
            novo.MdiParent = this;
            novo.Show();
        }

        internal void CriarEvento(Query query, Measurement[] measurements, int minInd, int maxInd, Color[] coresAtuais, parametrosFreqFiltrada param, string dir, double vMinFreq)
        {
            GraficoEventos novo = new GraficoEventos(this, query, measurements, minInd, maxInd, coresAtuais, param, dir, vMinFreq);
            novo.MdiParent = this;
            novo.Show();
        }

        internal void CriarEvento(Query query, Measurement[] measurements, int minInd, int maxInd, Color[] coresAtuais, parametrosFMMeTX param, string dir, double vMinFreq)
        {
            GraficoEventos novo = new GraficoEventos(this, query, measurements, minInd, maxInd, coresAtuais, param, dir, vMinFreq);
            novo.MdiParent = this;
            novo.Show();
        }

        internal void CriarEvento(Query query, Measurement[] measurements, int minInd, int maxInd, Color[] coresAtuais, parametrosPassaFaixa param, string dir, double vMinFreq)
        {
            GraficoEventos novo = new GraficoEventos(this, query, measurements, minInd, maxInd, coresAtuais, param, dir, vMinFreq);
            novo.MdiParent = this;
            novo.Show();
        }

        internal void CriarEvento(Query query, Measurement[] measurements, int minInd, int maxInd, Color[] coresAtuais, parametrosFiltroDeKalman param, string dir, double vMinFreq)
        {
            GraficoEventos novo = new GraficoEventos(this, query, measurements, minInd, maxInd, coresAtuais, param, dir, vMinFreq);
            novo.MdiParent = this;
            novo.Show();
        }

        internal void CriaLocalização(string dircons, Query query, Measurement[] measurements, EventosDetectados evento)
        {
            MapaEventos novo = new MapaEventos(dircons, query, measurements, evento);
            novo.MdiParent = this;
            novo.Show();
        }

        private void minimizarTudoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Minimizar todos os forms
            Form[] filhos = this.MdiChildren;

            foreach (Form filho in filhos)
                filho.WindowState = FormWindowState.Minimized;

        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            // Inicia o processo de abertura da pastas
            try
            {
                //Process.Start("explorer.exe", Parametros.DirDados + "\\" + PastaCorrente.NP);
                Process.Start("explorer.exe", Properties.Settings.Default.QueryFolder + "\\" + selectedQuery);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            } 
        }

        private void sobreToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Form[] children = MdiChildren;

            foreach (Form child in children)
            {
                if (child.GetType() == typeof(AboutMe)) // Formulário 9 foi encontrado
                {
                    child.Activate();
                    return;
                }

            }

            AboutMe novo = new AboutMe();
            novo.MdiParent = this;
            novo.Show();
        }

        internal void BuscaRealizada(bool busReal, string nomePasta)
        {
            br = busReal;
            nomeAtual = nomePasta;

            // array com os forms filhos do Form1
            Form[] filhos = MdiChildren;

            foreach (Form filho in filhos)
            {
                if (filho.GetType() == typeof(BuscasFeitas))
                {
                    filho.Activate(); // ativa o Form2 que já existe
                    return;
                }
            }
        }

        private void manualToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string dirManual = Path.GetDirectoryName(
                System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\Manual\\";

            string nomeManual = "MedPlot - Manual de Utilizacao.pdf";

            string nomeCompleto = dirManual + nomeManual;

            try
            {
                System.Diagnostics.Process.Start(nomeCompleto);
            }
            catch (Exception)
            {
                MessageBox.Show("O manual deve existir no diretório \"Manual\" do MedPlot e estar nomeado como \"MedPlot - Manual de Utilizacao.pdf\" para ser aberto.",
                    "MedPlot", MessageBoxButtons.OK);
            }

        }

        private void fecharTudoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Fechar todos os forms
            Form[] filhos = this.MdiChildren;

            foreach (Form filho in filhos)
                filho.Close();

            HabilitaBotao(false);
            HabilitaBotao2(true);
        }

        private void diretórioDeDadosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                //folderBrowserDialog1.SelectedPath = Parametros.DirDados;
                folderBrowserDialog1.SelectedPath = Properties.Settings.Default.QueryFolder;

                DialogResult result = folderBrowserDialog1.ShowDialog();

                // Armazena o diretório selecionado caso o botão "Ok" da caixa de diálogo tenha sido pressionado
                if (result == DialogResult.OK)
                {
                    //Parametros.DirDados = folderBrowserDialog1.SelectedPath;

                    //string path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().
                    //Location) + "\\Config\\medplot.ini";

                    //StreamWriter arq = new StreamWriter(path);
                    //arq.WriteLine(Parametros.DirPdc);
                    //arq.WriteLine(Parametros.DirDados);
                    //arq.Close();

                    // Armazena em settings o diretório de dados
                    Properties.Settings.Default.QueryFolder = folderBrowserDialog1.SelectedPath;
                    // Salva as user settings
                    Properties.Settings.Default.Save();

                    // array com os forms filhos do Form1
                    Form[] filhos = MdiChildren;

                    foreach (Form filho in filhos)
                    {
                        if (filho.GetType() == typeof(BuscasFeitas))
                        {
                            filho.Cursor = Cursors.WaitCursor;
                            filho.Cursor = Cursors.Arrow;
                            return;
                        }
                    }
                }
            }
            catch (Exception)
            {

            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            //Application.Exit();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Application.Exit();
        }

        private void toolStripButton6_Click(object sender, EventArgs e)
        {
            // array com os forms filhos do Form1
            Form[] filhos = MdiChildren;

            foreach (Form filho in filhos)
            {
                if (filho.GetType() == typeof(MapaTerminais))
                {
                    filho.Activate(); // ativa o Form2 que já existe
                    return;
                }
            }
            // Não foi encontrado um Form2
            MapaTerminais novo = new MapaTerminais(this);
            novo.MdiParent = this;
            novo.Show();
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            this.BackgroundImage = Properties.Resources.Logo_MedPlot2;
        }

        private void toolStripButton5_Click(object sender, EventArgs e)
        {
            //executa a conversão em um novo form18, passando as informações de pasta
            ConvertComtrade form = new ConvertComtrade();
            // Pasta 'Dados'
            //form.dirDados = Parametros.DirDados;
            form.dirDados = Properties.Settings.Default.QueryFolder;
            //pasta da consulta
            form.pastaCorrente = selectedQuery;
            form.ShowDialog();
        }


        private void editarTerminaisToolStripMenuItem_Click(object sender, EventArgs e)
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
            {
                MessageBox.Show("Você precisa abrir o MedPlot com privilégios de administrador para editar os bancos de dados.");
                return;
            }


            // array com os forms filhos do Form1
            Form[] filhos = MdiChildren;

            foreach (Form filho in filhos)
            {
                if (filho.GetType() == typeof(ConfigPDC))
                {
                    filho.Activate(); // ativa o Form23 que já existe
                    return;
                }
            }
            // Não foi encontrado um Form2
            ConfigPDC novo = new ConfigPDC(this);
            novo.MdiParent = this;
            novo.Show();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                //AtualizaMedPlot();

                this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch (Exception)
            {
                //MessageBox.Show(ex.Message, "Erro de atualização", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        static void ConfigEncryption(string exeConfigName)
        {
            // Takes the executable file name without the
            // .config extension.
            try
            {
                // Open the configuration file and retrieve 
                // the connectionStrings section.
                Configuration config = ConfigurationManager.
                    OpenExeConfiguration(exeConfigName + ".exe");

                //AppSettingsSection wfSection = config.AppSettings;
                // Seção com as application settings
                ConfigurationSection appSection = config.GetSection("applicationSettings/" + exeConfigName + ".Properties.Settings") as ConfigurationSection;
                //ConfigurationSection appSection = config.GetSection("applicationSettings") as ConfigurationSection;
                //ConfigurationSection appSection = config.GetSection("userSettings/" + exeConfigName + ".Properties.Settings");

                // Se a seção não está protegida (encriptada)
                if (!appSection.SectionInformation.IsProtected)
                {
                    // Encrypt the section.
                    appSection.SectionInformation.ProtectSection(
                        "DataProtectionConfigurationProvider");

                    // Save the encrypted section (?)
                    appSection.SectionInformation.ForceSave = true;
                    // Save the current configuration.
                    config.Save(ConfigurationSaveMode.Modified);

                    ConfigurationManager.RefreshSection(appSection.SectionInformation.SectionName);
                }                
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
