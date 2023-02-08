using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MedPlot
{
    class OpcaoGrafico
    {
        private static int opcao;
        private static int pu = 0;

        public static int Opcao
        {
            get { return opcao; }
            set { opcao = value; }
        }

        public static int Pu
        {
            get { return pu; }
            set { pu = value; }
        }
    }
}
