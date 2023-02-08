﻿using MedFasee.Data;
using MedFasee.Equipment;
using MedFasee.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;

namespace MedFasee.Repository
{
    public class MeasurementHistorian : Database, IMeasurementDb
    {

        string Database { get; }
        readonly string connectionString;
        public MeasurementHistorian(string ip, int port, string user, string pass) : base(ip, user, pass)
        {
            connectionString = "http://" + ip + ":" + port + "/historian/timeseriesdata/read/historic/";
        }

        public Dictionary<Channel, ITimeSeries> QueryTerminalSeries(string Id, DateTime start, DateTime finish, List<Channel> measurements, int dataRate, int equipmentRate, bool downloadStat = false)
        {

            Dictionary<Channel, ITimeSeries> result;

            Dictionary<string, Channel> builtMeasurements = new Dictionary<string, Channel>();

            foreach(Channel channel in measurements)
                builtMeasurements[channel.Id.ToString()] = channel;

            if (downloadStat)
                builtMeasurements["MISSING"] = Channel.MISSING;

            string path = BuildPath(start, finish, GetChannels(measurements));

            try
            {
                using (var httpClient = new HttpClient())
                {
                    var queryTask = httpClient.GetStringAsync(path);
                    
                    result = ParseJson(ref queryTask, builtMeasurements, dataRate, downloadStat);

                }


            }
            catch (Exception e)
            {
                if(e.InnerException != null)
                {
                    if (e.InnerException is HttpRequestException)
                    {
                        if (e.InnerException.HResult == -2147467259)
                            throw new InvalidConnectionException(Ip);
                        else if (e.InnerException.HResult == -2146233088)
                            throw new InvalidQueryException(InvalidQueryException.BAD_HIST_QUERY);
                    }
                    if (e.InnerException is TaskCanceledException && e.InnerException.HResult == -2146233029)
                    {
                        throw new QueryTimeoutException();
                    }
                    else
                    {
                        throw new InvalidQueryException(e.InnerException.Message);
                    }
                }
                else
                    throw new InvalidQueryException(e.Message);
            }


            return result;

        }

        private string BuildPath(DateTime start, DateTime finish, string channels)
        {
            return connectionString + channels + "/" + start.ToString("yyyy-MM-ddTHH:mm:ss.fff") + "/" + finish.ToString("yyyy-MM-ddTHH:mm:ss.fff") + "/json";
        }

        private static string GetChannels(List<Channel> measurements)
        {
            string result = "";
            string prefix = "";
            foreach(Channel channel in measurements)
            {
                result += prefix;
                result += channel.Id;
                prefix = ",";
            }

            return result;
        }

        private static Dictionary<Channel, ITimeSeries> ParseJson(ref Task<string> query, Dictionary<string,Channel> measurements, double framesPerSecond, bool downloadStat)
        {
            query.Wait();

            


            if (query.Result == "{\"TimeSeriesDataPoints\":[]}")
                throw new InvalidQueryException(InvalidQueryException.EMPTY);

            Dictionary<Channel, ITimeSeries> series = new Dictionary<Channel, ITimeSeries>();
            bool oneValid = downloadStat;
            bool hasData = false;

            foreach (KeyValuePair<string, Channel> pair in measurements)
                series.Add(pair.Value, new TimeSeries());

            int rowSize = 0;
            int rowStart = 26;

            while (rowStart < query.Result.Length)
            {
                rowSize = query.Result.IndexOf("}", rowStart);
                rowSize = rowSize == -1 ? query.Result.Length-3 : rowSize; 
                
                string[] fields = query.Result.Substring(rowStart, rowSize-rowStart).Replace("\"", string.Empty)
                    .Replace("HistorianID:", string.Empty)
                    .Replace("Time:", string.Empty)
                    .Replace("Value:", string.Empty)
                    .Replace("Quality:", string.Empty).Split(',');

                DateTime measureTime = DateTime.Parse(fields[1]);

                double timeModulus = measureTime.Millisecond % (1000 / framesPerSecond);
                double timeModulusDiff = Math.Abs((1000 / framesPerSecond) - timeModulus);
                if (timeModulus < 2 || timeModulusDiff < 2)
                {
                    bool quality = fields[3] == "29";
                    
                    if ((quality || downloadStat) && double.TryParse(fields[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                    {
                        oneValid |= quality;

                        if (!hasData)
                            hasData = true;
                        double time = TimeUtils.OaDate(measureTime);
                        Channel key = measurements[fields[0]];
                        series[key].Add(time, value);

                        if (!quality && downloadStat)
                            series[Channel.MISSING].Add(time, 2);
                    }



                }

                rowStart = rowSize + 3;
            }


            if (!hasData)
                throw new InvalidQueryException(InvalidQueryException.EMPTY);
            if (!oneValid)
                throw new InvalidQueryException(InvalidQueryException.NO_VALID);

            return series;
        }
    }
}
