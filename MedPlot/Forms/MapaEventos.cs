using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using mshtml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.IO;
using Microsoft.Win32;
using System.Diagnostics;
using System.Security.Principal;
using MedFasee.Structure;

namespace MedPlot
{
    public partial class MapaEventos : Form
    {
       // private double[][] coordenadas; //coordenadas geográficas das PMUs
        public string dirConsulta; //diretório de consulta
        public int indConsulta; //índice da consulta
        public int[] indSelec; //índice dos terminais selecionados
        public EventosDetectados Evento; //evento a ser localizado

        public Query Query { get; internal set; }
        Measurement[] SelectedMeasurements { get; }

        public MapaEventos(string dircons, Query query, Measurement[] measurements, EventosDetectados evento)
        {
            InitializeComponent();

            //passa algumas variáveis
            dirConsulta = dircons;
            Query = query;
            SelectedMeasurements = measurements;
            Evento = evento;
        }

        private void Form21_Shown(object sender, EventArgs e)
        {
            try
            {
                //lê o arquivo de localização das PMUs
                LerArquivoLocal();
                //localiza o evento, com as coordenadas passadas
                Evento.Localiza();
                if (!Evento.Localização.Encontrada) throw new ArgumentException("Evento não localizado. Altere as configurações da sua análise.");
                //carrega o mapa
                webBrowser1.Navigate(System.IO.Directory.GetCurrentDirectory() + "\\Mapa.html");                                             
            }
            catch (Exception ex)
            {
                this.Hide();
                MessageBox.Show(ex.Message, "Erro!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.Close();
            }
        }

        private void LerArquivoLocal()
        {
            string[] xml = Directory.GetFiles(dirConsulta, "*.XML");
            if (xml.Count() == 0)
                throw new ArgumentException("Impossivel achar o arquivo .xml de configuração");

            XElement root = XElement.Load(xml[0]);
            XNamespace ns = "smsf2";

            List<string> temp = new List<string>();

            for (int i = 0; i < Evento.TerminaisAfetados.Count; i++)
                temp.Add(SelectedMeasurements[Evento.TerminaisAfetados[i].Index].Terminal.DisplayName);

            var pmus =
                                       from c in root.Elements(ns + "pmu")
                                       where (temp.Contains((string)c.Element(ns + "idName")))

                                       select new { name = (string)c.Element(ns + "idName"), id = (int)temp.IndexOf((string)c.Element(ns + "idName")), lat = (double)c.Element(ns + "local").Element(ns+"lat"), lon = (double)c.Element(ns + "local").Element(ns + "lon") };



            foreach(var pmu in pmus)
            {

                Evento.TerminaisAfetados[pmu.id].Localização.Latitude = Convert.ToDouble(pmu.lat);
                Evento.TerminaisAfetados[pmu.id].Localização.Longitude = Convert.ToDouble(pmu.lon);

            }

            
            //verifica se foram encontradas as coordenadas de todos os terminais
            if (pmus.Count() != Evento.TerminaisAfetados.Count)
            {
               // this.Hide();
               // MessageBox.Show("O arquivo local.cfg não contém a localização de todas as PMUs", "Erro!", MessageBoxButtons.OK);            
               // this.Close(); //se não encontrou todas, fecha o form
                throw new ArgumentException("O arquivo local.cfg não contém a localização de todas as PMUs");
            }

        }

        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            
            //verifica se o local do evento é dentro do brasil (a informação do país estará disponível no título do documento)
            this.webBrowser1.DocumentTitleChanged += new System.EventHandler(this.webBrowser1_DocumentTitleChanged);

            string script = "var geocoder = new google.maps.Geocoder();" +
                "var latlng = new google.maps.LatLng(" + Convert.ToString(Evento.Localização.Latitude, new CultureInfo("en-US")) + "," + Convert.ToString(Evento.Localização.Longitude, new CultureInfo("en-US")) + ");" +

                "geocoder.geocode({'latLng': latlng}, function(results, status) {" +
                    "if (status == google.maps.GeocoderStatus.OK) {" +
                        "for (var i = 0; i < results[0].address_components.length; i++) {" +
                            "if (results[0].address_components[i].types[0] == \"country\") {" +
                                "document.title = results[0].address_components[i].short_name;" +
                            "}" +
                        "}" +
                    "}" +
                    "else {" +
                        "document.title = \"nada\";" +
                    "}"+
                "});";

            HtmlElement head = webBrowser1.Document.GetElementsByTagName("head")[0];
            HtmlElement scriptEl = webBrowser1.Document.CreateElement("script");
            IHTMLScriptElement element = (IHTMLScriptElement)scriptEl.DomElement;
            element.text = "function verificapais() { " + script + " }";
            head.AppendChild(scriptEl);
            webBrowser1.Document.InvokeScript("verificapais");
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            //monta a string que adiciona os pinos das PMUs
            string script = "";
            for (int i = 0; i < Evento.TerminaisAfetados.Count; i++)
            {
                script = script + "var marker" + Convert.ToInt32(i) + " = new google.maps.Marker({position: new google.maps.LatLng(" + Convert.ToString(Evento.TerminaisAfetados[i].Localização.Latitude, new CultureInfo("en-US")) + "," + Convert.ToString(Evento.TerminaisAfetados[i].Localização.Longitude, new CultureInfo("en-US")) + "), map: map, title: '" + SelectedMeasurements[Evento.TerminaisAfetados[i].Index] + "'});";
            }

            //executa a string como um script, pois o método "Navigate" tem limite de número de caracteres
            HtmlElement head = webBrowser1.Document.GetElementsByTagName("head")[0];
            HtmlElement scriptEl = webBrowser1.Document.CreateElement("script");
            IHTMLScriptElement element = (IHTMLScriptElement)scriptEl.DomElement;
            element.text = "function adicionaPinos() { " + script + " }";
            head.AppendChild(scriptEl);
            webBrowser1.Document.InvokeScript("adicionaPinos");

            //habilita o timer que adiciona o local do evento
            timer2.Enabled = true;
            //e para este timer
            timer1.Enabled = false;
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            //monta a string que adiciona o local do evento
            string script =
                "var evento = new google.maps.Marker({" +
                              "position: new google.maps.LatLng(" + Convert.ToString(Evento.Localização.Latitude, new CultureInfo("en-US")) + "," + Convert.ToString(Evento.Localização.Longitude, new CultureInfo("en-US")) + "), " +
                              "map: map, " +
                              "title: 'Evento', " +
                              "icon: 'icone.png'" +
                              "});" +

                "var infowindow = null; " +

                "google.maps.event.addListener(evento, 'click',function() {" +
                              "if (infowindow) infowindow.close();" +
//                              "infowindow = new google.maps.InfoWindow({content: 'Localização: " + Evento.Localização.Latitude.ToString() + ", " + Evento.Localização.Longitude.ToString() + "<br>Início: " + Math.Round(Evento.Localização.Inicio, 3).ToString() + "s antes de atingir o primeiro terminal<br>Velocidade de propagação: " + Math.Round(Evento.Localização.VelocidadeDePropagação, 3).ToString() + "km/s<br>Nº de terminais utilizados na triangulação: " + Evento.Localização.NumTerminaisUtilizados.ToString() + "<br>'});" +
                              "infowindow = new google.maps.InfoWindow({content: 'Localização: " + Evento.Localização.Latitude.ToString() + ", " + Evento.Localização.Longitude.ToString() + "<br>Nº de terminais utilizados na triangulação: " + Evento.Localização.NumTerminaisUtilizados.ToString() + "<br>'});" +
                              "infowindow.open(map, evento);" +
                              "});";            
                                    
            HtmlElement head = webBrowser1.Document.GetElementsByTagName("head")[0];
            HtmlElement scriptEl = webBrowser1.Document.CreateElement("script");
            IHTMLScriptElement element = (IHTMLScriptElement)scriptEl.DomElement;
            element.text = "function adicionaevento() { " + script + " }";
            head.AppendChild(scriptEl);
            webBrowser1.Document.InvokeScript("adicionaevento");

            timer2.Enabled = false;
        }
        
        void webBrowser1_DocumentTitleChanged(object sender, EventArgs e)
        {
            timer1.Enabled = true;
        }

        private void MapaEventos_Load(object sender, EventArgs e)
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            if (principal.IsInRole(WindowsBuiltInRole.Administrator) && Properties.Settings.Default.enabledRegeditFix)
            {
                var appName = Process.GetCurrentProcess().ProcessName + ".exe";
                SetIE8KeyforWebBrowserControl(appName);
            }

        }

        private void SetIE8KeyforWebBrowserControl(string appName)
        {
            RegistryKey Regkey = null;
            try
            {
                // For 64 bit machine
                if (Environment.Is64BitOperatingSystem)
                    Regkey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\\Wow6432Node\\Microsoft\\Internet Explorer\\MAIN\\FeatureControl\\FEATURE_BROWSER_EMULATION", true);
                else  //For 32 bit machine
                    Regkey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\\Microsoft\\Internet Explorer\\Main\\FeatureControl\\FEATURE_BROWSER_EMULATION", true);

                // If the path is not correct or
                // if the user haven't priviledges to access the registry
                if (Regkey == null)
                {
                    //MessageBox.Show("Não foi possível encontrar o Registro de emulação de browser.");
                    return;
                }

                string FindAppkey = Convert.ToString(Regkey.GetValue(appName));

                // Check if key is already present
                if (FindAppkey == "11011")
                {
                    //MessageBox.Show("Required Application Settings Present");
                    Regkey.Close();
                    return;
                }

                // If a key is not present add the key, Key value 69649 (decimal)
                if (string.IsNullOrEmpty(FindAppkey))
                    Regkey.SetValue(appName, unchecked((int)11011), RegistryValueKind.DWord);

                // Check for the key after adding
                FindAppkey = Convert.ToString(Regkey.GetValue(appName));

                //if (FindAppkey != "11011")
                //    MessageBox.Show("Não foi possível aplicar a emulação de browser, Ref: " + FindAppkey);
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Houve algum erro na tentativa de aplicar a emulação de browser");
                //MessageBox.Show(ex.Message);
            }
            finally
            {
                // Close the Registry
                if (Regkey != null)
                    Regkey.Close();
            }
        }

    }
}
