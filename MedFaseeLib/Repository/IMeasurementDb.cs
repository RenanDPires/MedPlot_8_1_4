using MedFasee.Data;
using MedFasee.Equipment;
using System;
using System.Collections.Generic;


namespace MedFasee.Repository
{
    public interface IMeasurementDb
    {

        Dictionary<Channel, ITimeSeries> QueryTerminalSeries(string Id, DateTime start, DateTime finish, List<Channel> Measurements, int dataRate, int equipmentRate, bool downloadStat);

    }
}
