using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using MedFasee.Equipment;
using MedFasee.Structure;
using MedFasee.Utils;

namespace MedFasee.Data
{
    public static class DataReader
    {

        static readonly NumberFormatInfo NumberFormat = new NumberFormatInfo();

        static DataReader()
        {
            NumberFormat.NumberDecimalSeparator = ".";
        }


        public static Query ReadMedFasee(string id, SystemData system, string folder)
        {

            Query query = new Query(id, system, new List<Measurement>());

            DirectoryInfo directoryInfo = new DirectoryInfo(folder);

            if (!directoryInfo.Exists)
                throw new ArgumentException("The supplied query path did not exist.");

            FileInfo[] files = directoryInfo.GetFiles("*.txt");

            foreach (Terminal terminal in system.Terminals)
                if(File.Exists(folder + "/" + terminal.Id + ".txt"))
                    query.Measurements.Add(ReadMeasurementTerminal(terminal, folder + "/" + terminal.Id + ".txt"));


            return query;
        }

        public static Measurement ReadMeasurementTerminal(Terminal terminal, string path)
        {

            Dictionary<Channel, ITimeSeries> readings = new Dictionary<Channel, ITimeSeries>();

            DateTime start;
            DateTime finish;
            int rate;

            char[] defaultSeparator = new char[] { ' ' };
                using (var fileStream = new FileStream(path, FileMode.Open))
                using (var streamReader = new StreamReader(fileStream))
                {
                    streamReader.ReadLine();
                    streamReader.ReadLine();

                    start = TimeUtils.FromSoc((long)double.Parse(streamReader.ReadLine().Split(defaultSeparator)[2]), 0, 1);
                    finish = TimeUtils.FromSoc((long)double.Parse(streamReader.ReadLine().Split(defaultSeparator)[2]), 0, 1);
                    rate = int.Parse(streamReader.ReadLine().Split(defaultSeparator)[1]);

                    double startSoc = TimeUtils.OaDate(start);
                    double skipSoc = 1000.0 * TimeUtils.OA_MILLISECOND / rate;

                    string line = streamReader.ReadLine();
                    while (!line.StartsWith("Tempo_(SOC)"))
                        line = streamReader.ReadLine();

                    string[] header = line.Split(defaultSeparator, StringSplitOptions.RemoveEmptyEntries);
                    Dictionary<int, Channel> channels = GetChannelsFromHeader(header, terminal.Channels);

                    foreach (Channel channel in channels.Values)
                        readings[channel] = new TimeSeries();
                    readings[Channel.MISSING] = new TimeSeries();

                    while (!streamReader.EndOfStream)
                    {
                        line = streamReader.ReadLine();
                        string[] values = line.Split(defaultSeparator, StringSplitOptions.RemoveEmptyEntries);

                        for (int i = 1; i < values.Length; i++)
                        {
                            if (channels.ContainsKey(i))
                                readings[channels[i]].Add(startSoc, double.Parse(values[i], NumberFormat));

                        }
                        int missingValue = 0;

                        if (values[values.Length - 1] == "*")
                            missingValue = 1;
                        else if (values[values.Length - 1] == "d")
                            missingValue = 2;

                        readings[Channel.MISSING].Add(startSoc, missingValue);

                        startSoc += skipSoc;
                    }

                }

            return new Measurement(terminal, start, finish, rate, readings);

        }

        private static Dictionary<int, Channel> GetChannelsFromHeader(string[] header, List<Channel> keys)
        {
            Dictionary<int, Channel> channels = new Dictionary<int, Channel>();


            foreach(Channel channel in keys)
            {
                int position = Array.IndexOf(header, channel.Name);
                if (position != -1)
                    channels[position] = channel;
            }

            foreach(Channel channel in Channel.DEFAULT_CHANNELS)
            {
                int position = Array.IndexOf(header, channel.Name);
                if (position != -1 && !channels.ContainsKey(position))
                      channels[position] = keys.Find(ch => ch.Equals(channel)) ?? channel;
            }

            return channels;
        }
    }
}
