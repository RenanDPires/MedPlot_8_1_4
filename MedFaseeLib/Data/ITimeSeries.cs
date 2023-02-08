﻿using System;
using System.Collections.Generic;
using System.Text;

namespace MedFasee.Data
{
    public interface ITimeSeries
    {
        int Count { get; }

        void Add(double timestamp, double reading);
        double Timestamp(int position);
        double Reading(int position);

        double[] GetReadings();
        double[] GetTimestamps();
    }
}
