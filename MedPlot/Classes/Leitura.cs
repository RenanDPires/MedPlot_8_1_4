using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;

namespace MedPlot
{
    class Leitura
    {
        // Atributos
        Int64 tl;

        public double[] v_med, i_med, freqArq, dFreqArq;
        //public double[] tempo;
        public string[] faltante;
        public int totalFaltantes;

        public void Le_Arquivos(string nomeCompleto, string nomeArquivo, Int64 totalLinhas, List<string> voltPhases, List<string> currPhases)
        {
            tl = totalLinhas;

            // Monta os vetores com os dados de acordo com a quantidade de canais para tensões e correntes
            if (voltPhases.Count != 0)
            {
                v_med = new double[2 * voltPhases.Count * tl];
            }
            if (currPhases.Count != 0)
            {
                i_med = new double[2 * currPhases.Count * tl];
            }
            
            faltante = new string[tl];
            //tempo = new double[tl];

            // cria uma nova instância de um objeto StreamReader
            StreamReader arquivo = new StreamReader(nomeCompleto + "\\" + nomeArquivo);
            string texto = ""; // variável que armazena o texto inteiro do arquivo
            // Array de strings, vai conter as palavras do texto quebrado.
            string[] palavras;

            // Separador de palavras, no caso do nosso arquivo é o espaço em branco!
            string[] separador = { " " };

            // Inicializa o vetor de strings "palavras"
            string aux = "lixo";
            palavras = aux.Split(separador, StringSplitOptions.RemoveEmptyEntries);

            // Enquanto não chegar ao cabeçalho das colunas, pula as linhas
            while (palavras[0] != ("Tempo_(SOC)"))
            {
                texto = arquivo.ReadLine();
                palavras = texto.Split(separador, StringSplitOptions.RemoveEmptyEntries);
                if (palavras.Length == 0)
                {
                    aux = "lixo";
                    palavras = aux.Split(separador, StringSplitOptions.RemoveEmptyEntries);
                }
            }

            // Variável associada à existência ou não da coluna com valores de freq.
            int colFreq = 0; // 0 -> não existe ; 1 -> existe
            if (palavras.Length > 2 * (currPhases.Count + voltPhases.Count) + 2 && palavras[2 * (currPhases.Count + voltPhases.Count) + 1] == "Frequência")
            {
                colFreq = 1;
                freqArq = new double[tl];
            }
            // Variável associada à existência ou não da coluna com valores de variação de freq.
            int colDFreq = 0; // 0 -> não existe ; 1 -> existe
            if (palavras.Length > 2 * (currPhases.Count + voltPhases.Count) + 3 && palavras[2 * (currPhases.Count + voltPhases.Count) + 2] == "Delta_Freq")
            {
                colDFreq = 1;
                dFreqArq = new double[tl];
            }
            // LEITURA DOS DADOS, após o cabeçalho!!
            for (int i = 0; i < totalLinhas; i++)
            {
                texto = arquivo.ReadLine();
                palavras = texto.Split(separador, StringSplitOptions.RemoveEmptyEntries);
                //tempo[i] = double.Parse(palavras[0]);
                for (int m = 0; m < 2 * voltPhases.Count; m++)
                {
                    palavras[m + 1] = palavras[m + 1].Replace(",", ".");
                    v_med[i + m * tl] = double.Parse(palavras[m + 1], CultureInfo.InvariantCulture);
                }
                for (int n = 0; n < 2 * currPhases.Count; n++)
                {
                    palavras[n + (2 * voltPhases.Count) + 1] = palavras[n + (2 * voltPhases.Count) + 1].Replace(",", ".");
                    i_med[i + n * tl] = double.Parse(palavras[n + (2 * voltPhases.Count) + 1], CultureInfo.InvariantCulture);
                }
                // Se houver uma coluna com as frequências
                if (colFreq == 1)
                {
                    palavras[2 * (currPhases.Count + voltPhases.Count) + 1] = palavras[2 * (currPhases.Count + voltPhases.Count) + 1].Replace(",", ".");
                    freqArq[i] = double.Parse(palavras[2 * (currPhases.Count + voltPhases.Count) + 1], CultureInfo.InvariantCulture);
                }
                // Se houver uma coluna com as variações de frequências
                if (colDFreq == 1)
                {
                    palavras[2 * (currPhases.Count + voltPhases.Count) + colFreq + colDFreq] = palavras[2 * (currPhases.Count + voltPhases.Count) + colFreq + colDFreq].Replace(",", ".");
                    dFreqArq[i] = double.Parse(palavras[2 * (currPhases.Count + voltPhases.Count) + colFreq + colDFreq], CultureInfo.InvariantCulture);
                }
                // Se existir marcação na coluna de faltante
                if (palavras.Length == (2 * (currPhases.Count + voltPhases.Count) + colFreq + colDFreq + 2))
                {
                    faltante[i] = palavras[palavras.Length - 1];
                    totalFaltantes++;
                }
            }
            arquivo.Close();
        }
    }
}
