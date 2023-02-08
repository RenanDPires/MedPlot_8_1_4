using MedFasee.Structure;
using System;
using System.IO;
using System.Linq;

namespace MedFasee.Data
{
    public class ComtradeWriter
    {
        private static readonly string FOLDER_NAME = "COMTRADE";
        private static readonly int SCALE_MAX = 32767;

        public ComtradeWriter() { }

        public void Write(Query query, string path, ComtradeRevision revision = ComtradeRevision.R1999)
        {
            string comtradePath = path + Path.DirectorySeparatorChar + FOLDER_NAME;
            Directory.CreateDirectory(comtradePath);

            foreach(var measurement in query.Measurements)
            {
                WriteConfig(comtradePath, query.System.NominalFrequency, measurement, revision);
            }
        }

        private void WriteConfig(string path, int nominalFrequency, Measurement measurement, ComtradeRevision revision)
        {
            if(revision == ComtradeRevision.R1999)
            {
                using (var fileStream = new FileStream(path + Path.DirectorySeparatorChar + measurement.Terminal.Id + ".cfg", FileMode.Create))
                using (var streamWriter = new StreamWriter(fileStream))
                {
                    int analogChannels = CountAnalogChannels(measurement);
                    int digitalChannels = CountDigitalChannels(measurement);

                    streamWriter.WriteLine("{0},medfasee,1999", measurement.Terminal.DisplayName);
                    streamWriter.WriteLine("{0},{1}A,{2}D",  analogChannels + digitalChannels, analogChannels, digitalChannels);

                    int count = 1;
                    foreach (var reading in measurement.Series)
                    {
                        double[] factors = FindComtradeFactors(reading.Value);
                        string quantity = "";
                        string unit = "";
                        string type = "";
                        string phase = Equipment.Channel.GetPhaseString(reading.Key.Phase);

                        switch (reading.Key.Quantity)
                        {
                            case Equipment.ChannelQuantity.VOLTAGE:
                                quantity = "V";
                                unit = "V";
                                break;
                            case Equipment.ChannelQuantity.CURRENT:
                                quantity = "I";
                                unit = "A";
                                break;
                            case Equipment.ChannelQuantity.FREQUENCY:
                                quantity = "VFreq";
                                unit = "Hz";
                                type = ",+";
                                break;
                            default:
                                break;
                        }

                        switch (reading.Key.Value)
                        {
                            case Equipment.ChannelValueType.ABSOLUTE:
                                type = string.Format(" Mag RMS,{0}m",phase);
                                break;
                            case Equipment.ChannelValueType.ANGLE:
                                type = string.Format(" Phi,{0}a",phase);
                                unit = "DEG";
                                break;
                            default:
                                break;
                        }
                        
                        string factorA = Convert.ToString(factors[0], new System.Globalization.CultureInfo("en-US"));
                        string factorB = Convert.ToString(factors[1], new System.Globalization.CultureInfo("en-US"));
                        streamWriter.WriteLine("{0},{1}{2}{3},,{4},{5},{6},0.0,{7},{8},1.0,1.0,P", count, quantity, phase.ToLower(), type, unit, factorA, factorB, -SCALE_MAX, SCALE_MAX);

                        count++;
                    }
                    streamWriter.WriteLine("{0}.0", nominalFrequency);
                    streamWriter.WriteLine("1");
                    streamWriter.WriteLine("{0},{1}", measurement.FramesPerSecond, measurement.Series.First().Value.Count);
                    streamWriter.WriteLine(measurement.Start.ToString("dd/MM/yyyy,hh:mm:ss.ffffff"));
                    streamWriter.WriteLine(measurement.Start.ToString("dd/MM/yyyy,hh:mm:ss.ffffff"));
                    streamWriter.WriteLine("BINARY");
                    streamWriter.WriteLine("1");

                    streamWriter.Flush();
                }
            }
        }

        private double[] FindComtradeFactors(ITimeSeries series)
        {
            double max = series.GetReadings().Max();
            double min = series.GetReadings().Min();


            return new double[] { (max-min)/(2/SCALE_MAX), (max + min)/2 };
        }

        private int CountAnalogChannels(Measurement measurement)
        {
            return (from reading in measurement.Series
                    where
                            reading.Key.Quantity == Equipment.ChannelQuantity.CURRENT ||
                            reading.Key.Quantity == Equipment.ChannelQuantity.VOLTAGE ||
                            reading.Key.Quantity == Equipment.ChannelQuantity.FREQUENCY
                    select reading).Count();
        }

        private int CountDigitalChannels(Measurement measurement)
        {
            // TODO: Implement digital counting for digital channels. 
            return 0;
        }
    }
}
