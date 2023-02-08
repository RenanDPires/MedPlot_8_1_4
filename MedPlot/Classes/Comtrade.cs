using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml.Linq;
using System.Xml.Schema;


namespace MedPlot
{
    static class COMTRADE
    {
        public static void ExportaConsulta(ConvertComtrade parentForm, string nomeDoAplicativo, string diretorioDeDados, string pastaDaConsulta)
        {
            //declara algumas variáveis para armazenamento das configurações do terminais.cfg
            string[] nomes;
            List<List<string>> voltPhases;
            List<List<string>> currPhases;
            int[] qtFasesTen, qtFasesCor;

            //variáveis para salvar as configurações somente dos terminais que possuem arquivos de dados (nem todos que tem no terminais.cfg possuem arquivos .txt)
            List<string> nomes2 = new List<string>();
            List<List<string>> fasesTen2 = new List<List<string>>();
            List<List<string>> fasesCor2 = new List<List<string>>();
            List<int> qtFasesTen2 = new List<int>();
            List<int> qtFasesCor2 = new List<int>();

            // Diretório completo aonde estão os dados da consulta solicitada
            string nomeCompleto = diretorioDeDados + "\\" + pastaDaConsulta;

            // Arquivos XML no diretório de dados
            string[] files = Directory.GetFiles(nomeCompleto, "*.xml");

            string cfgFile = "";
            // Escolhe como configuração o 1º arquivo validado de acordo com o Schema
            foreach (string f in files)
            {
                bool aux = ValidateConfig(f);
                if (aux)
                {
                    cfgFile = f;
                    break;
                }
            }

            if (cfgFile == "") //se não existe o arquivo de config.
            {
                parentForm.Invoke((Action)(() => System.Windows.Forms.MessageBox.Show("Arquivo de configuração não encontrado!", "Erro!", System.Windows.Forms.MessageBoxButtons.OK)));
                parentForm.Invoke((Action)(() => parentForm.Close()));
                return;
            }

            // Leitura do arquivo 'terminais.cfg' presente na pasta de dados
            parentForm.Invoke((Action)(() => parentForm.Controls[0].Text = "Lendo arquivo de configuração."));
            //LeituraTerminais(nomeCompleto, out flt, out nomes, out fasesTen, out fasesCor, out qtFasesTen, out qtFasesCor);
            bool flt = ReadConfig(cfgFile, out nomes, out voltPhases, out currPhases, out qtFasesTen, out qtFasesCor);

            if (flt == false) //caso  não consiga ler o terminais.cfg
            {
                parentForm.Invoke((Action)(() => System.Windows.Forms.MessageBox.Show("Arquivo de configuração corrompido!", "Erro!", System.Windows.Forms.MessageBoxButtons.OK)));
                parentForm.Invoke((Action)(() => parentForm.Close()));
                return;
            }

            //verifica a existência dos arquivos de dados
            // Informação sobre o diretório da consulta corrente
            DirectoryInfo dir = new DirectoryInfo(nomeCompleto);
            // Arquivos dos terminais dentro da pasta da consulta
            FileInfo[] arquivos = dir.GetFiles("*.txt");
            parentForm.Invoke((Action)(() => parentForm.Controls[0].Text = "Verificando arquivos de dados"));
            if (arquivos.Count() <= 0) //se não possui nenhum arquivo de dados
            {
                parentForm.Invoke((Action)(() => System.Windows.Forms.MessageBox.Show("Não existem arquivos de dados na pasta.", "Erro!", System.Windows.Forms.MessageBoxButtons.OK)));
                parentForm.Invoke((Action)(() => parentForm.Close()));
                return;
            }

            //verifica quais terminais possuem arquivos de dados
            foreach (FileInfo file in arquivos)
            {
                for (int i = 0; i < nomes.Length; i++)
                {
                    if (file.Name == (nomes[i] + ".txt")) // se for um arquivo de dados
                    {
                        nomes2.Add(nomes[i]);
                        fasesTen2.Add(voltPhases[i]);
                        fasesCor2.Add(currPhases[i]);
                        qtFasesTen2.Add(qtFasesTen[i]);
                        qtFasesCor2.Add(qtFasesCor[i]);
                    }
                }
            }
            // Se houver arquivos mas nenhum estiver listado no 'terminais.cfg'
            if (nomes2.Count == 0)
            {
                parentForm.Invoke((Action)(() => System.Windows.Forms.MessageBox.Show("A pasta não contém arquivos de dados listados no arquivo de configuração!", "Erro!", System.Windows.Forms.MessageBoxButtons.OK)));
                return;
            }

            //lê as informações de tempo na pasta da consulta
            string a = pastaDaConsulta.Substring(0, 4);
            string m = pastaDaConsulta.Substring(4, 2);
            string d = pastaDaConsulta.Substring(6, 2);
            string hi = pastaDaConsulta.Substring(9, 2);
            string mi = pastaDaConsulta.Substring(11, 2);
            string si = pastaDaConsulta.Substring(13, 2);
            string hf = pastaDaConsulta.Substring(16, 2);
            string mf = pastaDaConsulta.Substring(18, 2);
            string sf = pastaDaConsulta.Substring(20, 2);
            string tx = pastaDaConsulta.Substring(23, 2);

            // Calcula o total de linhas de dados contidos no arquivo de dados
            DateTime dataIni = new DateTime(Convert.ToInt16(a), Convert.ToInt16(m), Convert.ToInt16(d), Convert.ToInt16(hi), Convert.ToInt16(mi), Convert.ToInt16(si));
            DateTime dataFin = new DateTime(Convert.ToInt16(a), Convert.ToInt16(m), Convert.ToInt16(d), Convert.ToInt16(hf), Convert.ToInt16(mf), Convert.ToInt16(sf));
            dataFin = dataFin.AddSeconds(1);


            int ff = DateTime.Compare(dataFin, dataIni);
            if (ff < 0)
                dataFin = dataFin.AddDays(1);
            Int64 totalLinhas = Convert.ToInt64(dataFin.Subtract(dataIni).TotalSeconds) * Convert.ToInt32(tx);

            //declara algumas variáveis para o armazenamento dos dados
            Leitura le = new Leitura();
            int cont = 0;
            double[][] VM = new double[totalLinhas][];
            double[][] VA = new double[totalLinhas][];
            double[][] IM = new double[totalLinhas][];
            double[][] IA = new double[totalLinhas][];

            //cria a pasta COMTRADE
            parentForm.Invoke((Action)(() => parentForm.Controls[0].Text = "Criando diretório COMTRADE"));
            parentForm.Invoke((Action)(() => ((System.Windows.Forms.ProgressBar)(parentForm.Controls[1])).Maximum = nomes2.Count+1));
            parentForm.Invoke((Action)(() => ((System.Windows.Forms.ProgressBar)(parentForm.Controls[1])).Value = 1));
            Directory.CreateDirectory(nomeCompleto + "\\COMTRADE");

            //le os arquivos txt e cria o COMTRADE para todos os terminais
            foreach (string Nome in nomes2)
            {
                parentForm.Invoke((Action)(() => parentForm.Controls[0].Text = "Lendo arquivo " + Nome + ".txt"));
                //le o arquivo de dados
                le.Le_Arquivos(nomeCompleto, Nome + ".txt", totalLinhas, fasesTen2[cont], fasesCor2[cont]);
                //salva as grandezas
                for (int i = 0; i < totalLinhas; i++)
                {
                    VM[i] = new double[qtFasesTen2[cont]];
                    VA[i] = new double[qtFasesTen2[cont]];
                    IM[i] = new double[qtFasesCor2[cont]];
                    IA[i] = new double[qtFasesCor2[cont]];
                    for (int j = 0; j < qtFasesTen2[cont]; j++)
                    {
                        VM[i][j] = le.v_med[i + totalLinhas * j * 2];
                        VA[i][j] = le.v_med[i + totalLinhas * (j * 2 + 1)];
                    }
                    for (int j = 0; j < qtFasesCor2[cont]; j++)
                    {
                        IM[i][j] = le.i_med[i + totalLinhas * j * 2];
                        IA[i][j] = le.i_med[i + totalLinhas * (j * 2 + 1)];
                    }
                }
                //escreve o arquivo de configuração
                parentForm.Invoke((Action)(() => parentForm.Controls[0].Text = "Criando arquivo " + Nome + ".cfg"));
                System.IO.StreamWriter arqv = new System.IO.StreamWriter(nomeCompleto + "\\COMTRADE\\" + Nome + ".cfg");

                arqv.WriteLine(Nome + "," + nomeDoAplicativo + "," + "1999"); //nome do terminal, do sistema, versão da norma

                int numCanais = qtFasesTen2[cont] * 2 + qtFasesCor2[cont] * 2;
                if (le.freqArq != null) numCanais++;
                //número de canais, analógicos e digitais
                arqv.WriteLine(Convert.ToString(numCanais) + "," + Convert.ToString(numCanais) + "A,0D");
                //fatores de conversão dos dados
                double[] fator_a = new double[numCanais];
                double[] fator_b = new double[numCanais];
                //módulo das tensões
                for (int i = 0; i < qtFasesTen2[cont]; i++)
                {
                    double[] temp = new double[totalLinhas];
                    for (int j = 0; j < totalLinhas; j++) temp[j] = VM[j][i];

                    double MAX = temp.Max();
                    double MIN = temp.Min();
                    fator_a[i] = (MAX - MIN) / (2 * 32767);
                    fator_b[i] = (MAX + MIN) / 2;
                    arqv.WriteLine(Convert.ToString(i + 1) + ",V" + fasesTen2[cont][i].ToLower() + " Mag RMS," + fasesTen2[cont][i] + "m,,V," + Convert.ToString(fator_a[i], new System.Globalization.CultureInfo("en-US")) + "," + Convert.ToString(fator_b[i], new System.Globalization.CultureInfo("en-US")) + ",0.0,-32767,32767,1.0,1.0,P");
                }
                //Ângulo das tensões
                for (int i = 0; i < qtFasesTen2[cont]; i++)
                {
                    double[] temp = new double[totalLinhas];
                    for (int j = 0; j < totalLinhas; j++) temp[j] = VA[j][i];

                    double MAX = temp.Max();
                    double MIN = temp.Min();
                    fator_a[i + qtFasesTen2[cont]] = (MAX - MIN) / (2 * 32767);
                    fator_b[i + qtFasesTen2[cont]] = (MAX + MIN) / 2;
                    arqv.WriteLine(Convert.ToString(i + qtFasesTen2[cont] + 1) + ",V" + fasesTen2[cont][i].ToLower() + " Phi," + fasesTen2[cont][i] + "a,,DEG," + Convert.ToString(fator_a[i + qtFasesTen2[cont]], new System.Globalization.CultureInfo("en-US")) + "," + Convert.ToString(fator_b[i + qtFasesTen2[cont]], new System.Globalization.CultureInfo("en-US")) + ",0.0,-32767,32767,1.0,1.0,P");
                }
                //módulo das correntes
                for (int i = 0; i < qtFasesCor2[cont]; i++)
                {
                    double[] temp = new double[totalLinhas];
                    for (int j = 0; j < totalLinhas; j++) temp[j] = IM[j][i];

                    double MAX = temp.Max();
                    double MIN = temp.Min();
                    fator_a[i + qtFasesTen2[cont] * 2] = (MAX - MIN) / (2 * 32767);
                    fator_b[i + qtFasesTen2[cont] * 2] = (MAX + MIN) / 2;
                    arqv.WriteLine(Convert.ToString(i + qtFasesTen2[cont] * 2 + 1) + ",I" + fasesCor2[cont][i].ToLower() + " Mag RMS," + fasesCor2[cont][i] + "m,,A," + Convert.ToString(fator_a[i + qtFasesTen2[cont] * 2], new System.Globalization.CultureInfo("en-US")) + "," + Convert.ToString(fator_b[i + qtFasesTen2[cont] * 2], new System.Globalization.CultureInfo("en-US")) + ",0.0,-32767,32767,1.0,1.0,P");
                }
                //ângulo das correntes
                for (int i = 0; i < qtFasesCor2[cont]; i++)
                {
                    double[] temp = new double[totalLinhas];
                    for (int j = 0; j < totalLinhas; j++) temp[j] = IA[j][i];

                    double MAX = temp.Max();
                    double MIN = temp.Min();
                    fator_a[i + qtFasesTen2[cont] * 2 + qtFasesCor2[cont]] = (MAX - MIN) / (2 * 32767);
                    fator_b[i + qtFasesTen2[cont] * 2 + qtFasesCor2[cont]] = (MAX + MIN) / 2;
                    arqv.WriteLine(Convert.ToString(i + qtFasesTen2[cont] * 2 + qtFasesCor2[cont] + 1) + ",I" + fasesCor2[cont][i].ToLower() + " Phi," + fasesCor2[cont][i] + "a,,DEG," + Convert.ToString(fator_a[i + qtFasesTen2[cont] * 2 + qtFasesCor2[cont]], new System.Globalization.CultureInfo("en-US")) + "," + Convert.ToString(fator_b[i + qtFasesTen2[cont] * 2 + qtFasesCor2[cont]], new System.Globalization.CultureInfo("en-US")) + ",0.0,-32767,32767,1.0,1.0,P");
                }
                //frequenca
                if (le.freqArq != null)
                {
                    double MAX = le.freqArq.Max();
                    double MIN = le.freqArq.Min();
                    fator_a[qtFasesTen2[cont] * 2 + qtFasesCor2[cont] * 2] = (MAX - MIN) / (2 * 32767);
                    fator_b[qtFasesTen2[cont] * 2 + qtFasesCor2[cont] * 2] = (MAX + MIN) / 2;
                    arqv.WriteLine(Convert.ToString(qtFasesTen2[cont] * 2 + qtFasesCor2[cont] * 2 + 1) + ",VFreq,+,,Hz," + Convert.ToString(fator_a[qtFasesTen2[cont] * 2 + qtFasesCor2[cont] * 2], new System.Globalization.CultureInfo("en-US")) + "," + Convert.ToString(fator_b[qtFasesTen2[cont] * 2 + qtFasesCor2[cont] * 2], new System.Globalization.CultureInfo("en-US")) + ",0.0,-32767,32767,1.0,1.0,P");
                }
                //frequência
                arqv.WriteLine("60.0");
                //informações de amostragem
                arqv.WriteLine("1");
                arqv.WriteLine(Convert.ToString(tx) + "," + Convert.ToString(totalLinhas));
                //estampas de tempo
                arqv.WriteLine(dataIni.ToString("dd/MM/yyyy,hh:mm:ss.ffffff"));
                arqv.WriteLine(dataIni.ToString("dd/MM/yyyy,hh:mm:ss.ffffff"));
                //arquivo binário
                arqv.WriteLine("BINARY");
                //fator de conversão das estampas de tempo
                arqv.WriteLine("1");
                arqv.Close();

                //cria arquivo .dat
                parentForm.Invoke((Action)(() => parentForm.Controls[0].Text = "Criando arquivo " + Nome + ".dat"));
                BinaryWriter arqvBinario = new BinaryWriter(File.Open(nomeCompleto + "\\COMTRADE\\" + Nome + ".dat", FileMode.Create));
                for (int i = 0; i < totalLinhas; i++)
                {
                    arqvBinario.Write(i + 1);
                    arqvBinario.Write((uint)4294967295);
                    for (int j = 0; j < qtFasesTen2[cont]; j++) //magnitude das tensões
                    {
                        arqvBinario.Write((short)((VM[i][j] - fator_b[j]) / fator_a[j]));
                    }
                    for (int j = 0; j < qtFasesTen2[cont]; j++)//ângulo das tensões
                    {
                        arqvBinario.Write((short)((VA[i][j] - fator_b[j + qtFasesTen2[cont]]) / fator_a[j + qtFasesTen2[cont]]));
                    }
                    for (int j = 0; j < qtFasesCor2[cont]; j++)//magnitude das correntes
                    {
                        arqvBinario.Write((short)((IM[i][j] - fator_b[j + qtFasesTen2[cont] * 2]) / fator_a[j + qtFasesTen2[cont] * 2]));
                    }
                    for (int j = 0; j < qtFasesCor2[cont]; j++)//ângulo das correntes
                    {
                        arqvBinario.Write((short)((IA[i][j] - fator_b[j + qtFasesTen2[cont] * 2 + qtFasesCor2[cont]]) / fator_a[j + qtFasesTen2[cont] * 2 + qtFasesCor2[cont]]));
                    }
                    //frequência
                    if (le.freqArq != null) arqvBinario.Write((short)((le.freqArq[i] - fator_b[qtFasesTen2[cont] * 2 + qtFasesCor2[cont] * 2]) / fator_a[qtFasesTen2[cont] * 2 + qtFasesCor2[cont] * 2]));
                }

                arqvBinario.Close();
                parentForm.Invoke((Action)(() => ((System.Windows.Forms.ProgressBar)(parentForm.Controls[1])).Value++));
                cont++;
            }
            System.Diagnostics.Process.Start(@nomeCompleto + "\\COMTRADE"); //abre a janela com os arquivos convertidos
            parentForm.Invoke((Action)(() => { parentForm.threafinalizada = true; parentForm.Close(); })); //manda fechar o form
        }

        private static bool ReadConfig(string cfgFile, out string[] nomes, out List<List<string>> voltPhases, out List<List<string>> currPhases, out int[] qtFasesTen, out int[] qtFasesCor)
        {
            try
            {
                XElement root = XElement.Load(cfgFile);
                XNamespace ns = "smsf2";

                #region PDC info

                var pdcNode =
                    from c in root.Elements(ns + "pdc")
                        //let name = (string)c.Element(ns + "name")
                        //let type = (string)c.Element(ns + "type")
                    let fps = (decimal)c.Element(ns + "fps")
                    //let address = (string)c.Element(ns + "address")
                    //let user = (string)c.Element(ns + "security").Element(ns + "user")
                    //let pswd = (string)c.Element(ns + "security").Element(ns + "pswd")
                    //let db = (string)c.Element(ns + "dataBank")
                    select new { fps };

                //freqNominal = (double)pdcNode.First().fps;

                #endregion

                // PMUs contidas no arquivo de configuração
                IEnumerable<Pmu> pmus;

                voltPhases = new List<List<string>>();
                currPhases = new List<List<string>>();

                // Extração das PMus a partir do arquivo XML
                pmus =
                                    from c in root.Elements(ns + "pmu")
                                    select new Pmu()
                                    {
                                        VoltLevel = (double)c.Element(ns + "voltLevel"),
                                        Area = (string)c.Element(ns + "local").Element(ns + "area"),
                                        State = (string)c.Element(ns + "local").Element(ns + "state"),
                                        Station = (string)c.Element(ns + "local").Element(ns + "station"),
                                        IdName = (string)c.Element(ns + "idName"),
                                        Phasors = c.Element(ns + "measurements").Elements(ns + "phasor").Select(
                                            ph => new Phasor()
                                            {
                                                PName = (string)ph.Element(ns + "pName"),
                                                PType = (string)ph.Element(ns + "pType"),
                                                PPhase = (string)ph.Element(ns + "pPhase"),
                                            }).ToList()
                                    };


                // Number of terminals
                int numTerm = pmus.Count();

                nomes = new string[numTerm];
                //vb = new double[numTerm];

                qtFasesTen = new int[numTerm];
                qtFasesCor = new int[numTerm];
                //qtSeqPosTen = new int[numTerm];
                //qtSeqPosCor = new int[numTerm];

                // Preenche as informações de cada terminal
                int i = 0;
                foreach (Pmu p in pmus)
                {
                    // Nomes dos terminais
                    nomes[i] = p.IdName;
                    // Tensões base
                    //vb[i] = p.VoltLevel;

                    List<string> auxVoltPhase = new List<string>();
                    List<string> auxCurrPhase = new List<string>();

                    // Para cada fasor do terminal
                    foreach (Phasor ph in p.Phasors)
                    {
                        // Fasores de tensão
                        if (ph.PType == "Voltage")
                        {
                            // Adiciona a fase à lista
                            auxVoltPhase.Add(ph.PPhase);
                            if (ph.PPhase == "A" || ph.PPhase == "B" || ph.PPhase == "C")
                                qtFasesTen[i]++;
                        }
                        // Fasores de tensão
                        else if (ph.PType == "Current")
                        {
                            // Adiciona a fase à lista
                            auxCurrPhase.Add(ph.PPhase);
                            if (ph.PPhase == "A" || ph.PPhase == "B" || ph.PPhase == "C")
                                qtFasesCor[i]++;
                        }

                    }

                    // Adiciona a lista auxiliar para o terminal correspondente
                    voltPhases.Add(auxVoltPhase);
                    currPhases.Add(auxCurrPhase);

                    i++;
                }

                return true;
            }
            catch (Exception ex)
            {
                nomes = null;
                voltPhases = null;
                currPhases = null;
                qtFasesCor = null;
                qtFasesTen = null;

                return false;
            }
        }

        // Classes para leitura do arquivo de configuração XML
        private class Pmu
        {
            public double VoltLevel { get; set; }
            public string Area { get; set; }
            public string State { get; set; }
            public string Station { get; set; }
            public string IdName { get; set; }
            public int IdNumber { get; set; }
            public List<Phasor> Phasors { get; set; }
        }

        private class Phasor
        {
            public string PName { get; set; }
            public string PType { get; set; }
            public string PPhase { get; set; }
            public int ChId { get; set; }
        }

        private static bool ValidateConfig(string filePath)
        {
            try
            {
                // Load XML file
                //XDocument xmlDoc = XDocument.Load(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\Config\\pdc\\" + Properties.Settings.Default.dirPDC + "\\terminais.xml");
                XDocument xmlDoc = XDocument.Load(filePath);

                // XSD - schema
                XmlSchemaSet ss = new XmlSchemaSet();
                ss.Add("smsf2", "smsf2.xsd");

                // Validate XML file
                bool errors = false;
                xmlDoc.Validate(ss, (o, ex) =>
                {
                    errors = true;
                    //MessageBox.Show(ex.Message);
                });

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

    }
}
