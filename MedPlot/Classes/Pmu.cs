using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedPlot
{
    public class Pmu
    {
        #region Atributos

        #endregion

        #region Propriedades
        public double VoltLevel { get; set; }
        public string Area { get; set; }
        public string State { get; set; }
        public string Station { get; set; }
        public string IdName { get; set; }
        public int IdNumber { get; set; }
        public List<Phasor> Phasors { get; set; }
        public Freq Freqs { get; set; }
        public DFreq DFreqs { get; set; }
        #endregion

        public class Phasor
        {
            public string PName { get; set; }
            public string PType { get; set; }
            public string PPhase { get; set; }
            public int ModId { get; set; }
            public int AngId { get; set; }
            public int ChId { get; set; }
        }

        public class Freq
        {
            public string FName { get; set; }
            public int FId { get; set; }
        }

        public class DFreq
        {
            public string DFName { get; set; }
            public int DFId { get; set; }
        }
    }
}
