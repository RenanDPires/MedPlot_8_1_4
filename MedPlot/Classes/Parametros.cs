using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MedPlot
{
    class Parametros
    {
        private static int taxa;
        private static string ip, usuario, senha, banco;

        public static int Taxa
        {
            get { return taxa; }
            set { taxa = value; }                        
        }
        public static string IP
        {
            get { return ip; }
            set { ip = value; }
        }
        public static string Usuario
        {
            get { return usuario; }
            set { usuario = value; }
        }
        public static string Senha
        {
            get { return senha; }
            set { senha = value; }
        }
        public static string Banco
        {
            get { return banco; }
            set { banco = value; }
        }
    }
}
