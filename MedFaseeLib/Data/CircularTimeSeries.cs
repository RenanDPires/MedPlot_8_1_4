using System;
using System.Collections.Generic;

namespace MedFasee.Data
{
    public class CircularTimeSeries : ITimeSeries
    {
        public int MaximumSize { get; set; } 
        private List<double> Readings { get; set; }
        private List<double> Timestamps { get; set; }
        public int Count => Readings.Count;

        public CircularTimeSeries(int maximumSize = 3600)
        {
            if (maximumSize <= 0)
                throw new ArgumentException("Size must be a positive and non-zero number!", nameof(maximumSize));

            MaximumSize = maximumSize;

            Readings = new List<double>(maximumSize + 500);
            Timestamps = new List<double>(maximumSize + 500);
        }

        public CircularTimeSeries(int maximumSize, List<double> readings, List<double> timestamp)
        {
            if (maximumSize <= 0)
                throw new ArgumentException("Size must be a positive and non-zero number!", nameof(maximumSize));
            if (readings == null)
                throw new ArgumentNullException(nameof(readings), "The reading vector can't be null!");
            if (timestamp == null)
                throw new ArgumentNullException(nameof(timestamp), "The timestamp vector can't be null!");
            if (readings.Count != timestamp.Count)
                throw new ArgumentException("The readings and timestamp arrays must have same length!");
            if (maximumSize < readings.Count)
                maximumSize = readings.Count;

            MaximumSize = maximumSize;
            Readings = readings;
            Timestamps = timestamp;
        }

        public void Add(double timestamp, double reading)
        {
            if(Timestamps.Count == MaximumSize)
            {
                Timestamps.RemoveAt(0);
                Readings.RemoveAt(0);
            }
            Timestamps.Add(timestamp);
            Readings.Add(reading);
        }

        public double[] GetReadings()
        {
            return Readings.ToArray();
        }

        public double[] GetTimestamps()
        {
            return Timestamps.ToArray();
        }

        public double Timestamp(int position)
        {
            return Timestamps[position];
        }

        public double Reading(int position)
        {
            return Readings[position];
        }


    }
}
