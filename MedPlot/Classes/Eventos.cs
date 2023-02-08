using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNumerics.LinearAlgebra;
using MedFasee.Equipment;
using MedFasee.Structure;

namespace MedPlot
{
    public class Eventos
    {
        public List<EventosDetectados> Items; //lista de eventos detectados
        private double[][] VetorDeDados; //vetor de magnitudes
        private DateTime[] VetorDeTempo; //vetor de tempo
        private ListaDeLimites Limites;


        public Eventos(int numTerminais, int dadosCount, double[][] vetordeDados, DateTime[] vetordeTempo, ListaDeLimites limites)
        {
            //passa algumas variáveis
            VetorDeTempo = vetordeTempo;
            VetorDeDados = vetordeDados;
            Limites = limites;
            Items = new List<EventosDetectados>();

            for (int i = 0; i < numTerminais; i++) //verifica para todos os terminais
            {
                bool emEvento = false;
                int qt_eventos_ant = Items.Count; //quantidade de eventos já detectados anteriormente nos outros terminais
                DateTime instanteAbaixo = new DateTime(0); //instante de tempo em que a magnitude VOLTOU para abaixo do limite estabelecido
             //   double valorIni = 0; //variável que armazenar o valor inicial da detecção, para no fim, caso seja desbalanço, saber se é perda de carga ou geração (positivo/negativo)
                for (int ii = 0; ii < dadosCount; ii++) //percorre o vetor de dados
                {
                    if (Math.Abs(vetordeDados[i][ii]) >= Limites.MaxMagnitude && !emEvento) //se ainda não estava em um evento, e ultrapassou o limite, então inicia um evento
                    {
                        emEvento = true;
                        Items.Add(new EventosDetectados()); //novo evento
                        Items[Items.Count - 1].InstanteDeDetecção = VetorDeTempo[ii];
                        Items[Items.Count - 1].TerminaisAfetados.Add(new EventosDetectados.TerminaisEmEvento(i));
                        Items[Items.Count - 1].TerminaisAfetados[0].Pai = Items[Items.Count - 1];
                        Items[Items.Count - 1].TerminaisAfetados[0].Início = VetorDeTempo[ii];
                        
                       // Items[Items.Count - 1].TerminaisEmEvento.Add(i);
                        //Items[Items.Count - 1].TerminaisInicio.Add(VetorDeTempo[ii]);
                       // Items[Items.Count - 1].Duração.Add(new TimeSpan(long.MaxValue));
                        Items[Items.Count - 1].Tipo = EventosDetectados.TipoDeEvento.Indefinido;
                    //    valorIni = vetordeDados[i][ii];
                    }
                    else if (Math.Abs(vetordeDados[i][ii]) < 0.5 * Limites.MaxMagnitude && emEvento) //se foi para baixo do limite e estava em um evento, ou o evento acabou, ou foi uma passagem temporária por baixo do limite
                    {
                        //verifica se é evento do tipo salto, de acordo com o limite de tempo
                        if (VetorDeTempo[ii].Subtract(Items[Items.Count - 1].TerminaisAfetados[0].Início) <= Limites.MaxTempoSalto)
                        {
                            int p = Items.Count;
                            int q = Items[p - 1].TerminaisAfetados.Count;
                            Items[p - 1].Tipo = EventosDetectados.TipoDeEvento.Salto;
                            Items[p - 1].TerminaisAfetados[q - 1].Duração = VetorDeTempo[ii].Subtract(Items[Items.Count - 1].TerminaisAfetados[q - 1].Início);
                            emEvento = false;
                        }
                        //se for maior que o limite, então é desebalanço de carga/geração
                        else
                        {
                            if (instanteAbaixo == new DateTime(0)) //se ainda não havia passado para baixo do limite, define este como o instante em que primeiramente foi para baixo do limite
                            {
                                instanteAbaixo = VetorDeTempo[ii];
                            }

                            if (VetorDeTempo[ii].Subtract(instanteAbaixo) >= Limites.TempoAbaixoFimdoEvento) //se está já a mais de 2 segundos abaixo do limite, então o evento realmente acabou em "instanteAbaixo"
                            {
                                int p = Items.Count;
                                int q = Items[p - 1].TerminaisAfetados.Count;
                                Items[p - 1].TerminaisAfetados[q - 1].Duração = instanteAbaixo.Subtract(Items[p - 1].TerminaisAfetados[q - 1].Início); //calcula a duração do evento neste terminal

                                Items[p - 1].Tipo = EventosDetectados.TipoDeEvento.Desvio_de_Frequência;
                                //verifica se é sobre ou subfrequência
                              //  if (valorIni > 0) Items[p - 1].Tipo = EventosDetectados.TipoDeEvento.Sobrefrequência;
                              //  else Items[p - 1].Tipo = EventosDetectados.TipoDeEvento.Subfrequência;

                                emEvento = false;
                                instanteAbaixo = new DateTime(0);
                            }
                        }


                    }
                    else if (Math.Abs(vetordeDados[i][ii]) >= Limites.MaxMagnitude && emEvento)//se está acima do limite e está em evento, zera o instanteAbaixo, para caso tenha voltado para cima do limite desconsidere qualquer contagem anterior de tempo de verificação abaixo do limite
                    {
                        instanteAbaixo = new DateTime(0);
                    }
                }

                //caso tenha percorrido o gráfico todo, e não tenha ido abaixo do limite, então marca o evento como desbalanço
                if (emEvento)
                {
                    Items[Items.Count - 1].Tipo = EventosDetectados.TipoDeEvento.Desvio_de_Frequência;
                    //  if (valorIni > 0) Items[Items.Count - 1].Tipo = EventosDetectados.TipoDeEvento.Sobrefrequência;
                  //  else Items[Items.Count - 1].Tipo = EventosDetectados.TipoDeEvento.Subfrequência;
                }

                //verifica se este evento detectado é o mesmo já detectado anteriormente em outro terminal
                int qt_eventos_adicionados = Items.Count - qt_eventos_ant; //quantidade de eventos detectados neste terminal

                for (int ii = 0; ii < qt_eventos_adicionados; ii++)
                {
                    for (int iii = 0; iii < qt_eventos_ant; iii++)
                    {
                        if (Items[iii].Tipo == Items[qt_eventos_ant + qt_eventos_adicionados - ii - 1].Tipo) //se for o mesmo tipo de evento do evento anterior
                        {

                            if (Items[iii].Tipo == EventosDetectados.TipoDeEvento.Salto && Items[iii].InstanteDeDetecção.Subtract(Items[qt_eventos_ant + qt_eventos_adicionados - ii - 1].InstanteDeDetecção).Duration() <= Limites.MaxDiferencaMesmoEventodeSalto) // se for salto e a diferença de tempo entre eles for menor que "x"
                            {
                                //mescla os dois eventos
                                Items[iii].TerminaisAfetados.Add(Items[qt_eventos_ant + qt_eventos_adicionados - ii - 1].TerminaisAfetados[0]);
                               // Items[iii].TerminaisInicio.Add(Items[qt_eventos_ant + qt_eventos_adicionados - ii - 1].TerminaisInicio[0]);
                                if (Items[qt_eventos_ant + qt_eventos_adicionados - ii - 1].TerminaisAfetados[0].Início < Items[iii].InstanteDeDetecção) //se o instante de inicio deste terminal é menor do que o anterior, atribui este
                                {
                                    Items[iii].InstanteDeDetecção = Items[qt_eventos_ant + qt_eventos_adicionados - ii - 1].TerminaisAfetados[0].Início;
                                }
                                Items[iii].TerminaisAfetados[Items[iii].TerminaisAfetados.Count - 1].Duração = Items[qt_eventos_ant + qt_eventos_adicionados - ii - 1].TerminaisAfetados[0].Duração;
                                Items[iii].TerminaisAfetados[Items[iii].TerminaisAfetados.Count - 1].Pai = Items[iii];
                                //Items[iii].Duração.Add(Items[qt_eventos_ant + qt_eventos_adicionados - ii - 1].Duração[0]);
                                Items.RemoveAt(qt_eventos_ant + qt_eventos_adicionados - ii - 1);
                                break;
                            }
                            else if (Items[iii].Tipo == EventosDetectados.TipoDeEvento.Desvio_de_Frequência && Items[iii].InstanteDeDetecção.Subtract(Items[qt_eventos_ant + qt_eventos_adicionados - ii - 1].InstanteDeDetecção).Duration() <= Limites.MaxDiferencaMesmoEventodeDesbalanço) //se for sobrefrequência e a diferença de tempo entre eles for menor que y
                            {
                                //mescla os eventos
                                Items[iii].TerminaisAfetados.Add(Items[qt_eventos_ant + qt_eventos_adicionados - ii - 1].TerminaisAfetados[0]);
                                //Items[iii].TerminaisInicio.Add(Items[qt_eventos_ant + qt_eventos_adicionados - ii - 1].TerminaisInicio[0]);
                                if (Items[qt_eventos_ant + qt_eventos_adicionados - ii - 1].TerminaisAfetados[0].Início < Items[iii].InstanteDeDetecção) //se o instante de inicio deste terminal é menor do que o anterior, atribui este
                                {
                                    Items[iii].InstanteDeDetecção = Items[qt_eventos_ant + qt_eventos_adicionados - ii - 1].TerminaisAfetados[0].Início;
                                }
                                Items[iii].TerminaisAfetados[Items[iii].TerminaisAfetados.Count - 1].Duração = Items[qt_eventos_ant + qt_eventos_adicionados - ii - 1].TerminaisAfetados[0].Duração;
                                Items[iii].TerminaisAfetados[Items[iii].TerminaisAfetados.Count - 1].Pai = Items[iii];
                                Items.RemoveAt(qt_eventos_ant + qt_eventos_adicionados - ii - 1);
                                break;
                            }
                        /*    else if (Items[iii].Tipo == EventosDetectados.TipoDeEvento.Subfrequência && Items[iii].InstanteDeDetecção.Subtract(Items[qt_eventos_ant + qt_eventos_adicionados - ii - 1].InstanteDeDetecção).Duration() <= Limites.MaxDiferencaMesmoEventodeDesbalanço) //se for subfrequência e a diferença de tempo entre eles for menor que y
                            {
                                //mescla os eventos
                                Items[iii].TerminaisAfetados.Add(Items[qt_eventos_ant + qt_eventos_adicionados - ii - 1].TerminaisAfetados[0]);
                                //Items[iii].TerminaisEmEvento.Add(Items[qt_eventos_ant + qt_eventos_adicionados - ii - 1].TerminaisEmEvento[0]);
                                //Items[iii].TerminaisInicio.Add(Items[qt_eventos_ant + qt_eventos_adicionados - ii - 1].TerminaisInicio[0]);
                                if (Items[qt_eventos_ant + qt_eventos_adicionados - ii - 1].TerminaisAfetados[0].Início < Items[iii].InstanteDeDetecção) //se o instante de inicio deste terminal é menor do que o anterior, atribui este
                                {
                                    Items[iii].InstanteDeDetecção = Items[qt_eventos_ant + qt_eventos_adicionados - ii - 1].TerminaisAfetados[0].Início;
                                }
                                Items[iii].TerminaisAfetados[Items[iii].TerminaisAfetados.Count - 1].Duração = Items[qt_eventos_ant + qt_eventos_adicionados - ii - 1].TerminaisAfetados[0].Duração;
                                Items[iii].TerminaisAfetados[Items[iii].TerminaisAfetados.Count - 1].Pai = Items[iii];
                                Items.RemoveAt(qt_eventos_ant + qt_eventos_adicionados - ii - 1);
                                break;
                            }*/
                            else if (Items[iii].Tipo == EventosDetectados.TipoDeEvento.Indefinido && Items[iii].InstanteDeDetecção.Subtract(Items[qt_eventos_ant + qt_eventos_adicionados - ii - 1].InstanteDeDetecção).Duration() <= Limites.MaxDiferencaMesmoEventodeDesbalanço) //se for desbalanço e a diferença de tempo entre eles for menor que y
                            {
                                //mescla os eventos
                                Items[iii].TerminaisAfetados.Add(Items[qt_eventos_ant + qt_eventos_adicionados - ii - 1].TerminaisAfetados[0]);
                                // Items[iii].TerminaisEmEvento.Add(Items[qt_eventos_ant + qt_eventos_adicionados - ii - 1].TerminaisEmEvento[0]);
                               // Items[iii].TerminaisInicio.Add(Items[qt_eventos_ant + qt_eventos_adicionados - ii - 1].TerminaisInicio[0]);
                                if (Items[qt_eventos_ant + qt_eventos_adicionados - ii - 1].TerminaisAfetados[0].Início < Items[iii].InstanteDeDetecção) //se o instante de inicio deste terminal é menor do que o anterior, atribui este
                                {
                                    Items[iii].InstanteDeDetecção = Items[qt_eventos_ant + qt_eventos_adicionados - ii - 1].TerminaisAfetados[0].Início;
                                }
                                Items[iii].TerminaisAfetados[Items[iii].TerminaisAfetados.Count - 1].Duração = Items[qt_eventos_ant + qt_eventos_adicionados - ii - 1].TerminaisAfetados[0].Duração;
                                Items[iii].TerminaisAfetados[Items[iii].TerminaisAfetados.Count - 1].Pai = Items[iii];
                                Items.RemoveAt(qt_eventos_ant + qt_eventos_adicionados - ii - 1);
                                break;
                            }
                        }
                    }
                }
            }

            //depois de detectar tudo, calcula o tempo de atraso de cada terminal, em cada evento
            for (int i = 0; i < Items.Count; i++)
            {
                Items[i].TerminaisAfetados = Items[i].TerminaisAfetados.OrderBy(x => x.Index).ToList();

                for (int j = Items[i].TerminaisAfetados.Count - 1; j > 0; j--)
                {
                    if (Items[i].TerminaisAfetados[j].Index == Items[i].TerminaisAfetados[j - 1].Index)
                    {
                        if(Items[i].TerminaisAfetados[j].TempoDeAtraso > Items[i].TerminaisAfetados[j - 1].TempoDeAtraso)
                            Items[i].TerminaisAfetados.RemoveAt(j);
                        else
                            Items[i].TerminaisAfetados.RemoveAt(j-1);
                    }

                        
                }

                //ordena os terminais do evento pelo tempo de atraso, ou seja por ordem de detecção
                Items[i].TerminaisAfetados = Items[i].TerminaisAfetados.OrderBy(x => x.TempoDeAtraso).ToList();



            }
            Items = Items.OrderBy(x => x.InstanteDeDetecção).ToList();
        }
    }
    public class EventosDetectados
    {
     //   public List<DateTime> TerminaisInicio; //instante de inicio de cada terminal
     //   public List<TimeSpan> TempoDeAtraso;
     //   public List<int> TerminaisEmEvento; //indice dos terminais em evento
        //   public List<TimeSpan> Duração; //duração do evento em cada terminal
        #region Propriedades
        public TipoDeEvento Tipo { get; set; } //tipo do evento
        public DateTime InstanteDeDetecção { get; set; } //instante de detecção do evento (igual ao instante do primeiro terminal detectado)
        public CoordenadasDeEvento Localização; //localização do evento
        public List<TerminaisEmEvento> TerminaisAfetados { get; set; } //terminais afetados pelo evento
        #endregion

        #region Métodos
        public EventosDetectados()
        {
            //inicializa as variáveis
            TerminaisAfetados = new List<TerminaisEmEvento>();
          //  TerminaisInicio = new List<DateTime>();
          //  TempoDeAtraso = new List<TimeSpan>();
          //  TerminaisEmEvento = new List<int>();
          //  Duração = new List<TimeSpan>();
            Tipo = TipoDeEvento.Indefinido;
            InstanteDeDetecção = new DateTime(0);
            Localização.Latitude = 0;
            Localização.Longitude = 0;
            Localização.Inicio = 0;
            Localização.VelocidadeDePropagação = 0;
            Localização.NumTerminaisUtilizados = 0;
            Localização.Encontrada = false;
        }
        
        //método de localização do evento
        public void Localiza()
        {
            //if (coordenadas.Length != TerminaisAfetados.Count) throw new ArgumentException("O número de coordenadas informadas não corresponde ao número de terminais em evento.");

            Localização.Encontrada = false;

            //contador do número de terminais usados no cálculo
            int uti = TerminaisAfetados.Count;

            //vetor de variáveis estimadas (x, y, Tinicio, velocidade)
            Vector PE = new Vector(4);

            if (TerminaisAfetados.Count < 4) throw new ArgumentException("São necessários pelo menos 4 terminais para realizar a localização do evento.");

            //contador de iterações
            int k1 = 1;

            //chute inicial
            PE[0] = (TerminaisAfetados[0].Localização.Latitude + TerminaisAfetados[1].Localização.Latitude) / 2;
            PE[1] = (TerminaisAfetados[0].Localização.Longitude + TerminaisAfetados[1].Localização.Longitude) / 2;
            PE[2] = 0.1;
            PE[3] = 20;

            double XE, YE, TE, VE;

            while (uti > 3) //enquanto houver pelo menos 4 terminais
            {
                XE = PE[0];
                YE = PE[1];

                //impede que o valor do instante inicial fique fora de valores plausíveis, como maior que 1s ou negativo
                if (Math.Abs(PE[2]) > 1 || (PE[2] < 0)) PE[2] = 0.01;

                TE = PE[2];
                VE = PE[3];

                Vector FN = new Vector(uti); //função no ponto atual
                Matrix JN = new Matrix(uti, 4); //matriz jacobiana de F

                for (int ii = 0; ii < uti; ii++)
                {
                    FN[ii] = Math.Pow(TerminaisAfetados[ii].Localização.Latitude - XE, 2) + (Math.Pow(TerminaisAfetados[ii].Localização.Longitude - YE, 2)) - (Math.Pow(VE, 2) * Math.Pow(TerminaisAfetados[ii].TempoDeAtraso.TotalSeconds + TE, 2));
                    JN[ii, 0] = 2 * XE - 2 * TerminaisAfetados[ii].Localização.Latitude;
                    JN[ii, 1] = 2 * YE - 2 * TerminaisAfetados[ii].Localização.Longitude;
                    JN[ii, 2] = -Math.Pow(VE, 2) * ((2 * TE) + (2 * TerminaisAfetados[ii].TempoDeAtraso.TotalSeconds));
                    JN[ii, 3] = -2 * VE * Math.Pow(TerminaisAfetados[ii].TempoDeAtraso.TotalSeconds + TE, 2);
                }

                //solução do sistema
                Vector anterior = PE; //estimação anterior
                Matrix HH = (JN.Transpose() * JN).Inverse() * JN.Transpose(); //pseudo inversa da jacobiana
                PE = PE - (HH * FN).GetColumnVector(0);

                //teste de convergência
                Vector incremento = anterior - PE;
                Vector conve = new Vector(4);
                conve[0] = 0.0005; conve[1] = 0.0005; conve[2] = 0.01; conve[3] = 0.01;

                if (incremento.Abs() < conve)
                {
                    break;
                }

                if (k1 == 15) //se deu 15 iterações, zera tudo e tira a PMU mais distante, e reinicia o processo
                {
                    k1 = 0;
                    uti = uti - 1;
                    PE[0] = (TerminaisAfetados[0].Localização.Latitude + TerminaisAfetados[1].Localização.Latitude) / 2;
                    PE[1] = (TerminaisAfetados[0].Localização.Longitude + TerminaisAfetados[1].Localização.Longitude) / 2;
                    PE[2] = 0.1;
                    PE[3] = 20;
                }
                k1++;
            }
            // por fim define os valores encontrados, e caso não tenha convergido, avisa o usuário
            if (uti >= 4) Localização.Encontrada = true; //System.Windows.Forms.MessageBox.Show("Não foi possível obter a convergência. A localização apresentada corresponde ao valor obtido na última iteração, e pode não ter boa exatidão.", "Atenção!", System.Windows.Forms.MessageBoxButtons.OK,System.Windows.Forms.MessageBoxIcon.Warning);
            Localização.Latitude = PE[0];
            Localização.Longitude = PE[1];
            Localização.Inicio = PE[2];
            Localização.VelocidadeDePropagação = PE[3] * 111.12;
            Localização.NumTerminaisUtilizados = uti;
        }
        #endregion

        #region Classes e tipos 
     //   public enum TipoDeEvento { Indefinido, Salto, Sobrefrequência, Subfrequência };
        public enum TipoDeEvento { Indefinido, Salto, Desvio_de_Frequência };

        //coordenadas da localização do evento
        public struct CoordenadasDeEvento
        {
            public double Latitude;
            public double Longitude;
            public double VelocidadeDePropagação;
            public double Inicio;
            public int NumTerminaisUtilizados; //número de terminais utilizados na triangulação
            public bool Encontrada;
        }
        
        public class TerminaisEmEvento
        {
            #region Propriedades
            public EventosDetectados Pai { get; set; } //evento pai
            public int Index { get; set; }
            public DateTime Início { get; set; } //instante de detecção do eventos NESTE terminal
            public TimeSpan TempoDeAtraso
            {
                get
                {                    
                    return Início.Subtract(Pai.InstanteDeDetecção);
                }
                set
                {
                    Início = Pai.InstanteDeDetecção.Add(value);
                }
            } //tempo de atraso
            public TimeSpan Duração { get; set; }
            
            public Coordenadas Localização;
            #endregion

            #region Métodos
            
            //construtor
            public TerminaisEmEvento(int index)
            {
                Pai = new EventosDetectados();
                Index = index;
                Início = new DateTime();
                TempoDeAtraso = new TimeSpan();
                Duração = new TimeSpan(long.MaxValue);
                Localização.Latitude = 0;
                Localização.Longitude = 0;
            }
            #endregion

            #region Tipos
            public struct Coordenadas
            {
                public double Latitude {get; set;}
                public double Longitude { get; set; }
            }
            #endregion

        }
        #endregion
    }
    public struct ListaDeLimites
    {
        public double MaxMagnitude; //máxima magnitude do sinal de teste, acima da qual sinaliza a ocorrência de evento
        public TimeSpan TempoAbaixoFimdoEvento; //tempo o qual o sinal deve permanecer abaixo do limite para indicar que realmente terminou o evento
        public TimeSpan MaxTempoSalto; //máximo tempo que caracteriza evento do tipo salto
        public TimeSpan MaxDiferencaMesmoEventodeSalto; //máximo tempo entre diferentes terminais para que seja o mesmo evento do tipo salto
        public TimeSpan MaxDiferencaMesmoEventodeDesbalanço; //máximo tempo entre diferentes terminais para que seja o mesmo evento do tipo desbalanço

    }
}
