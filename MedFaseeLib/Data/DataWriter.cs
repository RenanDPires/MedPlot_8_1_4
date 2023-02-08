using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using MedFasee.Equipment;
using MedFasee.Structure;
using MedFasee.Utils;

namespace MedFasee.Data
{
    public static class DataWriter
    {

        static readonly NumberFormatInfo NumberFormat = new NumberFormatInfo() { NumberDecimalSeparator = "."};

        static DataWriter()
        {
        }
        
        public static void WriteMedFasee(Query query, string folder)
        {
            foreach (Measurement measurement in query.Measurements)
                WriteMeasurementTerminal(measurement, folder);
        }

        public static void WriteMeasurementTerminal(Measurement measurement, string path)
        {
            List<Channel> channels = GetChannelsWithDefault(measurement.Series);
            int[] indexes = new int[channels.Count];
            int[] limits = new int[channels.Count];

            string lastValid = "";
            int skipped = 0;

            List<double> skippedSoc = new List<double>();
            double startSoc = TimeUtils.Soc(measurement.Start);
            double nextSoc = startSoc;
            decimal nextSocAsOA = new decimal(TimeUtils.OaDate(measurement.Start));
            decimal skipOA = 1000 * new decimal(TimeUtils.OA_MILLISECOND) / measurement.FramesPerSecond;

            int estimatedPhasors = (int)(Math.Round((measurement.Finish - measurement.Start).TotalSeconds) * measurement.FramesPerSecond);

            using (var fileStream = new FileStream(path+"/"+measurement.Terminal.Id+".txt", FileMode.Create))
            using (var streamWriter = new StreamWriter(fileStream))
            {
                #region Header
                streamWriter.Write("Terminal: ");
                streamWriter.WriteLine(measurement.Terminal.Id);
                streamWriter.Write("Tensão base: ");
                streamWriter.WriteLine(measurement.Terminal.VoltageLevel.ToString("0.00 kV"));
                streamWriter.Write("SOC inicial: ");
                streamWriter.WriteLine(TimeUtils.Soc(measurement.Start));
                streamWriter.Write("SOC final: ");
                streamWriter.WriteLine(TimeUtils.Soc(measurement.Finish));
                streamWriter.Write("Taxa: ");
                streamWriter.Write(measurement.FramesPerSecond);
                streamWriter.WriteLine(" fasores/s");
                streamWriter.WriteLine("Total de frames faltantes: " + FindNumberOfMissingData(estimatedPhasors,measurement.Series)); // Just a guess, afaik it isn't really used in practice.
                streamWriter.WriteLine();
                streamWriter.Write("Tempo_(SOC)    ");

                for(int i = 0; i < channels.Count; i++)
                {
                    limits[i] = measurement.Series[channels[i]].Count;
                    if (!channels[i].Equals(Channel.MISSING))
                        streamWriter.Write(channels[i].Name.PadLeft(17));
                }
                streamWriter.WriteLine("Faltante".PadLeft(13));

                #endregion



                for (int i = 0; i < estimatedPhasors; i++)
                {

                    nextSoc = startSoc + Math.Floor(1.0 * i / measurement.FramesPerSecond) + (1.0 * i % measurement.FramesPerSecond) / measurement.FramesPerSecond;

                    bool valid = true;
                    bool stat = false;
                    string tmpString = "";


                    for (int j = 0; j < channels.Count; j++)
                    {
                        Channel channel = channels[j];
                        int index = indexes[j];

                        valid &= indexes[j] < measurement.Series[channel].Count;

                        if (!valid)
                            continue;

                        double time = measurement.Series[channel].Timestamp(index);

                        if (valid && Math.Abs(decimal.ToDouble(nextSocAsOA) - time) < 3 * TimeUtils.OA_MILLISECOND)
                        {
                            if (channel.Equals(Channel.MISSING))
                            {
                                stat = measurement.Series[channel].Reading(index) == 2;
                                valid &= measurement.Series[channel].Reading(index) == 0;
                                valid &= stat;
                            }
                            else
                            {
                                tmpString += measurement.Series[channel].Reading(index).ToString("0.000000", NumberFormat).PadLeft(17);

                            }
                            indexes[j]++;
                        }
                        else
                        {
                            valid = false;
                            while (time < decimal.ToDouble(nextSocAsOA) && indexes[j] < (measurement.Series[channel].Count-1))
                            {
                                indexes[j]++;
                                index = indexes[j];
                                time = measurement.Series[channel].Timestamp(index);
                            }
                        }
                    }

                    if (valid)
                    {
                        for (int k = 0; k < skipped; k++)
                        {
                            streamWriter.Write(skippedSoc[k].ToString("0.0000", NumberFormat));
                            streamWriter.WriteLine((string.IsNullOrEmpty(lastValid) ? tmpString : lastValid) + (stat ? "d" : "*").PadLeft(11));
                        }
                        skipped = 0;
                        skippedSoc.Clear();

                        lastValid = tmpString;
                        streamWriter.Write(nextSoc.ToString("0.0000", NumberFormat));
                        streamWriter.WriteLine(tmpString);
                    }
                    else
                    {
                        skippedSoc.Add(nextSoc);
                        skipped++;

                        if (!string.IsNullOrEmpty(lastValid))
                        {
                            for (int k = 0; k < skipped; k++)
                            {
                                streamWriter.Write(skippedSoc[k].ToString("0.0000", NumberFormat));
                                streamWriter.WriteLine(lastValid + (stat ? "d" : "*").PadLeft(11));
                            }
                            skipped = 0;
                            skippedSoc.Clear();
                        }
                            
                    }

                    nextSocAsOA += skipOA;
                }



            }



            if (string.IsNullOrEmpty(lastValid))
                File.Delete(path + "/" + measurement.Terminal.Id + ".txt");

        }

        private static int FindNumberOfMissingData(int estimatedNumber, Dictionary<Channel, ITimeSeries> series)
        {
            int missing = 0;

            foreach (KeyValuePair<Channel, ITimeSeries> serie in series)
                if (estimatedNumber - serie.Value.Count > missing)
                    missing = estimatedNumber - serie.Value.Count;
            return missing;
        }

        private static List<Channel> GetChannelsWithDefault(Dictionary<Channel, ITimeSeries> series)
        {
            List<Channel> channels = new List<Channel>();

            foreach(Channel channel in Channel.DEFAULT_CHANNELS)
            {
                if (series.ContainsKey(channel))
                    channels.Add(channel);
            }

            foreach(Channel channel in series.Keys)
            {
                if (Array.IndexOf(Channel.DEFAULT_CHANNELS,channel) == -1)
                    channels.Add(channel);
            }

            return channels;
        }
    }
}
