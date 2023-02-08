using MedFasee.Data;
using MedFasee.Equipment;
using MedFasee.Utils;
using System;
using System.Collections.Generic;

namespace MedFasee.Structure
{
    public class Measurement
    {
        public Terminal Terminal { get; private set; }
        public int FramesPerSecond { get; private set; }
        public Dictionary<Channel, ITimeSeries> Series { get; private set; }
        public DateTime Start { get; private set; }
        public DateTime Finish { get; private set; }

        public Measurement(Terminal terminal, DateTime start, DateTime finish, int framesPerSecond, Dictionary<Channel, ITimeSeries> series )
        {
            Terminal = terminal;
            Start = start;
            Finish = finish;
            FramesPerSecond = framesPerSecond;
            Series = series;
        }

        public static List<KeyValuePair<Channel, TimeSeries>> CalculatePositiveSequence(Measurement measurement, bool current = false)
        {
            Channel modulePhaseA = current ? Channel.CURRENT_A_MOD : Channel.VOLTAGE_A_MOD;
            Channel modulePhaseB = current ? Channel.CURRENT_B_MOD : Channel.VOLTAGE_B_MOD;
            Channel modulePhaseC = current ? Channel.CURRENT_C_MOD : Channel.VOLTAGE_C_MOD;
            Channel anglePhaseA = current ? Channel.CURRENT_A_ANG : Channel.VOLTAGE_A_ANG;
            Channel anglePhaseB = current ? Channel.CURRENT_B_ANG : Channel.VOLTAGE_B_ANG;
            Channel anglePhaseC = current ? Channel.CURRENT_C_ANG : Channel.VOLTAGE_C_ANG;

            List<KeyValuePair<Channel, TimeSeries>> result = new List<KeyValuePair<Channel, TimeSeries>>();

            int phaseAIndex = 0;
            int phaseBIndex = 0;
            int phaseCIndex = 0;

            ITimeSeries missing = measurement.Series.ContainsKey(Channel.MISSING) ?
                measurement.Series[Channel.MISSING] : null;

            List<double> timestampsModule = new List<double>();
            List<double> calculatedModule = new List<double>();
            List<double> timestampsAngle = new List<double>();
            List<double> calculatedAngle = new List<double>();
            int invalidToWrite = 0;

            while (phaseAIndex < measurement.Series[modulePhaseA].Count &&
                phaseBIndex < measurement.Series[modulePhaseB].Count &&
                phaseCIndex < measurement.Series[modulePhaseC].Count)
            {

                double phaseATime = measurement.Series[modulePhaseA].Timestamp(phaseAIndex);
                double phaseBTime = measurement.Series[modulePhaseB].Timestamp(phaseBIndex);
                double phaseCTime = measurement.Series[modulePhaseC].Timestamp(phaseCIndex);

                double maxTime = Math.Max(phaseATime, Math.Max(phaseBTime, phaseCTime));

                while (phaseATime < maxTime &&
                    Math.Abs(phaseATime - maxTime) > 3 * TimeUtils.OA_MILLISECOND &&
                    phaseAIndex < measurement.Series[modulePhaseA].Count)
                {
                    phaseAIndex++;
                    phaseATime = measurement.Series[modulePhaseA].Timestamp(phaseAIndex);
                }

                while (phaseBTime < maxTime &&
                    Math.Abs(phaseBTime - maxTime) > 3 * TimeUtils.OA_MILLISECOND &&
                    phaseBIndex < measurement.Series[modulePhaseB].Count)
                {
                    phaseBIndex++;
                    phaseBTime = measurement.Series[modulePhaseB].Timestamp(phaseBIndex);
                }

                while (phaseCTime < maxTime &&
                    Math.Abs(phaseCTime - maxTime) > 3 * TimeUtils.OA_MILLISECOND &&
                    phaseCIndex < measurement.Series[modulePhaseC].Count)
                {
                    phaseCIndex++;
                    phaseCTime = measurement.Series[modulePhaseC].Timestamp(phaseCIndex);
                }

                if (phaseAIndex >= measurement.Series[modulePhaseA].Count ||
                    phaseBIndex >= measurement.Series[modulePhaseB].Count ||
                    phaseCIndex >= measurement.Series[modulePhaseC].Count)
                    break;

                if (missing != null && missing.Reading(phaseAIndex) == 1)
                {
                    invalidToWrite++;

                    phaseAIndex++;
                    phaseBIndex++;
                    phaseCIndex++;
                    continue;
                }

                double phaseAReading = measurement.Series[modulePhaseA].Reading(phaseAIndex);
                double phaseBReading = measurement.Series[modulePhaseB].Reading(phaseBIndex);
                double phaseCReading = measurement.Series[modulePhaseC].Reading(phaseCIndex);

                double phaseAAngle = measurement.Series[anglePhaseA].Reading(phaseAIndex) * Math.PI / 180;
                double phaseBAngle = (measurement.Series[anglePhaseB].Reading(phaseBIndex) + 120) * Math.PI / 180;
                double phaseCAngle = (measurement.Series[anglePhaseC].Reading(phaseCIndex) + 240) * Math.PI / 180;


                double positiveSequenceAngle = Math.Atan2(phaseAReading * Math.Sin(phaseAAngle) +
                    phaseBReading * Math.Sin(phaseBAngle) +
                    phaseCReading * Math.Sin(phaseCAngle),
                    phaseAReading * Math.Cos(phaseAAngle) +
                    phaseBReading * Math.Cos(phaseBAngle) +
                    phaseCReading * Math.Cos(phaseCAngle)) * 180 / Math.PI;

                double positiveSequenceModule = Math.Sqrt(
                    Math.Pow(
                        phaseAReading * Math.Sin(phaseAAngle) +
                    phaseBReading * Math.Sin(phaseBAngle) +
                    phaseCReading * Math.Sin(phaseCAngle),
                        2) +
                    Math.Pow(
                    phaseAReading * Math.Cos(phaseAAngle) +
                    phaseBReading * Math.Cos(phaseBAngle) +
                    phaseCReading * Math.Cos(phaseCAngle),
                    2)) / 3;


                for (int j = 0; j < invalidToWrite; j++)
                {
                    double time = measurement.Series[modulePhaseA].Timestamp(phaseAIndex - invalidToWrite + j);
                    timestampsAngle.Add(time);
                    timestampsModule.Add(time);

                    if (calculatedModule.Count == 0)
                    {
                        calculatedAngle.Add(positiveSequenceAngle);
                        calculatedModule.Add(positiveSequenceModule);
                        continue;
                    }
                    calculatedAngle.Add(calculatedAngle[calculatedAngle.Count - 1]);
                    calculatedModule.Add(calculatedModule[calculatedModule.Count - 1]);


                }
                invalidToWrite = 0;

                timestampsAngle.Add(maxTime);
                timestampsModule.Add(maxTime);

                calculatedAngle.Add(positiveSequenceAngle);
                calculatedModule.Add(positiveSequenceModule);

                phaseAIndex++;
                phaseBIndex++;
                phaseCIndex++;
            }

            if (calculatedModule.Count != 0)
            {
                int initialSize = calculatedModule.Count;
                for (int j = 0; j < invalidToWrite; j++)
                {
                    double time = measurement.Series[modulePhaseA].Timestamp(initialSize + j);
                    timestampsAngle.Add(time);
                    timestampsModule.Add(time);

                    calculatedAngle.Add(calculatedAngle[calculatedAngle.Count - 1]);
                    calculatedModule.Add(calculatedModule[calculatedModule.Count - 1]);
                }
            }

            result.Add(new KeyValuePair<Channel, TimeSeries>(current ? Channel.CURRENT_POS_MOD : Channel.VOLTAGE_POS_MOD, new TimeSeries(timestampsModule, calculatedModule)));
            result.Add(new KeyValuePair<Channel, TimeSeries>(current ? Channel.CURRENT_POS_ANG : Channel.VOLTAGE_POS_ANG, new TimeSeries(timestampsAngle, calculatedAngle)));


            return result;
        }

        public static TimeSeries CalculateFrequency(Measurement measurement, double systemFrequency)
        {

            if(!measurement.Series.TryGetValue(Channel.VOLTAGE_POS_ANG, out ITimeSeries positiveSequenceAngles))
                positiveSequenceAngles = CalculatePositiveSequence(measurement).Find(keypair => keypair.Key == Channel.VOLTAGE_POS_ANG).Value;


            List<double> timestamps = new List<double>();
            List<double> frequencies = new List<double>();

            ITimeSeries missing = measurement.Series.ContainsKey(Channel.MISSING) ? measurement.Series[Channel.MISSING] : null;
            double maxTimeDiff = TimeUtils.OA_MILLISECOND * 1000 / measurement.FramesPerSecond;
            int invalidToWrite = 0;

            for (int i = 1; i < positiveSequenceAngles.Count; i++)
            {
                double timeDiff = positiveSequenceAngles.Timestamp(i) - positiveSequenceAngles.Timestamp(i - 1);

                bool timeError = timeDiff > 1.1 * maxTimeDiff;

                if (timeError || (missing != null && (missing.Reading(i) == 1 || missing.Reading(i - 1) == 1)))
                {
                    invalidToWrite++;
                    continue;
                }

                double diff = positiveSequenceAngles.Reading(i) - positiveSequenceAngles.Reading(i - 1);

                if (diff > 180)
                    diff = diff - 360;
                else if (diff < -180)
                    diff = diff + 360;

                double freq = diff / (360 / measurement.FramesPerSecond) + systemFrequency;

                for (int j = 0; j < invalidToWrite; j++)
                {
                    timestamps.Add(positiveSequenceAngles.Timestamp(i - invalidToWrite + j));
                    if (frequencies.Count == 0)
                    {
                        frequencies.Add(freq);
                        continue;
                    }
                    frequencies.Add(frequencies[frequencies.Count-1]);
                }
                invalidToWrite = 0;

                frequencies.Add(freq);
                timestamps.Add(positiveSequenceAngles.Timestamp(i));
            }

            if(frequencies.Count != 0)
            {
                int initialSize = frequencies.Count;
                for (int j = 0; j < invalidToWrite; j++)
                {
                    frequencies.Add(frequencies[frequencies.Count - 1]);

                    timestamps.Add(positiveSequenceAngles.Timestamp(initialSize + j));
                }

                frequencies.Insert(0, frequencies[0]);
                timestamps.Insert(0, positiveSequenceAngles.Timestamp(0));
            }

            return new TimeSeries(timestamps, frequencies);
        }

        public static TimeSeries CalculateRocof(Measurement measurement)
        {
            TimeSeries series = new TimeSeries();


            if (!measurement.Series.TryGetValue(Channel.FREQ, out ITimeSeries frequencies))
                frequencies = CalculatePositiveSequence(measurement).Find(keypair => keypair.Key == Channel.VOLTAGE_POS_ANG).Value;


            List<double> timestamps = new List<double>();
            List<double> rocof = new List<double>();

            ITimeSeries missing = measurement.Series.ContainsKey(Channel.MISSING) ? measurement.Series[Channel.MISSING] : null;
            double maxTimeDiff = TimeUtils.OA_MILLISECOND * 1000 / measurement.FramesPerSecond;
            int invalidToWrite = 0;

            for (int i = 2; i < frequencies.Count; i++)
            {
                double timeDiff = frequencies.Timestamp(i) - frequencies.Timestamp(i - 1);

                bool timeError = timeDiff > 1.1 * maxTimeDiff;

                if (timeError || (missing != null && (missing.Reading(i) == 1 || missing.Reading(i - 1) == 1)))
                {
                    invalidToWrite++;
                    continue;
                }

                double rate = (frequencies.Reading(i) - frequencies.Reading(i - 1)) * measurement.FramesPerSecond;

                for (int j = 0; j < invalidToWrite; j++)
                {
                    timestamps.Add(frequencies.Timestamp(i - invalidToWrite + j));
                    if (rocof.Count == 0)
                    {
                        rocof.Add(rate);
                        continue;
                    }
                    rocof.Add(rocof[rocof.Count - 1]);
                }
                invalidToWrite = 0;

                rocof.Add(rate);
                timestamps.Add(frequencies.Timestamp(i));
            }

            if (rocof.Count != 0)
            {
                int initialSize = rocof.Count;
                for (int j = 0; j < invalidToWrite; j++)
                {
                    rocof.Add(rocof[rocof.Count - 1]);

                    timestamps.Add(frequencies.Timestamp(initialSize + j));
                }

                rocof.Insert(0, rocof[0]);
                rocof.Insert(0, rocof[0]);
                timestamps.Insert(0, frequencies.Timestamp(1));
                timestamps.Insert(0, frequencies.Timestamp(0));


            }

            return new TimeSeries(timestamps, rocof);

        }

        public static List<KeyValuePair<Channel, TimeSeries>> CalculatePowers(Measurement measurement)
        {
            List<KeyValuePair<Channel, TimeSeries>> result = new List<KeyValuePair<Channel, TimeSeries>>();

            int voltageAIndex = 0;
            int voltageBIndex = 0;
            int voltageCIndex = 0;
            int currentAIndex = 0;
            int currentBIndex = 0;
            int currentCIndex = 0;

            ITimeSeries missing = measurement.Series.ContainsKey(Channel.MISSING) ?
                measurement.Series[Channel.MISSING] : null;

            List<double> timestampsActive = new List<double>();
            List<double> calculatedActive = new List<double>();
            List<double> timestampsReactive = new List<double>();
            List<double> calculatedReactive = new List<double>();
            int invalidToWrite = 0;

            while (voltageAIndex < measurement.Series[Channel.VOLTAGE_A_MOD].Count &&
                voltageBIndex < measurement.Series[Channel.VOLTAGE_B_MOD].Count &&
                voltageCIndex < measurement.Series[Channel.VOLTAGE_C_MOD].Count &&
                currentAIndex < measurement.Series[Channel.CURRENT_A_MOD].Count &&
                currentBIndex < measurement.Series[Channel.CURRENT_B_MOD].Count &&
                currentCIndex < measurement.Series[Channel.CURRENT_C_MOD].Count)
            {

                double voltageATime = measurement.Series[Channel.VOLTAGE_A_MOD].Timestamp(voltageAIndex);
                double voltageBTime = measurement.Series[Channel.VOLTAGE_B_MOD].Timestamp(voltageBIndex);
                double voltageCTime = measurement.Series[Channel.VOLTAGE_C_MOD].Timestamp(voltageCIndex);
                double currentATime = measurement.Series[Channel.CURRENT_A_MOD].Timestamp(currentAIndex);
                double currentBTime = measurement.Series[Channel.CURRENT_B_MOD].Timestamp(currentBIndex);
                double currentCTime = measurement.Series[Channel.CURRENT_C_MOD].Timestamp(currentCIndex);

                double maxTime = Math.Max(voltageATime, Math.Max(voltageBTime, voltageCTime));
                maxTime = Math.Max(Math.Max(maxTime, currentATime), Math.Max(currentBTime, currentCTime));

                while (voltageATime < maxTime &&
                    Math.Abs(voltageATime - maxTime) > 3 * TimeUtils.OA_MILLISECOND &&
                    voltageAIndex < measurement.Series[Channel.VOLTAGE_A_MOD].Count)
                {
                    voltageAIndex++;
                    voltageATime = measurement.Series[Channel.VOLTAGE_A_MOD].Timestamp(voltageAIndex);
                }

                while (voltageBTime < maxTime &&
                    Math.Abs(voltageBTime - maxTime) > 3 * TimeUtils.OA_MILLISECOND &&
                    voltageBIndex < measurement.Series[Channel.VOLTAGE_B_MOD].Count)
                {
                    voltageBIndex++;
                    voltageBTime = measurement.Series[Channel.VOLTAGE_B_MOD].Timestamp(voltageBIndex);
                }

                while (voltageCTime < maxTime &&
                    Math.Abs(voltageCTime - maxTime) > 3 * TimeUtils.OA_MILLISECOND &&
                    voltageCIndex < measurement.Series[Channel.VOLTAGE_C_MOD].Count)
                {
                    voltageCIndex++;
                    voltageCTime = measurement.Series[Channel.VOLTAGE_C_MOD].Timestamp(voltageCIndex);
                }




                while (currentATime < maxTime &&
                    Math.Abs(currentATime - maxTime) > 3 * TimeUtils.OA_MILLISECOND &&
                    currentAIndex < measurement.Series[Channel.CURRENT_A_MOD].Count)
                {
                    currentAIndex++;
                    currentATime = measurement.Series[Channel.CURRENT_A_MOD].Timestamp(currentAIndex);
                }

                while (currentBTime < maxTime &&
                    Math.Abs(currentBTime - maxTime) > 3 * TimeUtils.OA_MILLISECOND &&
                    currentBIndex < measurement.Series[Channel.CURRENT_B_MOD].Count)
                {
                    currentBIndex++;
                    currentBTime = measurement.Series[Channel.CURRENT_B_MOD].Timestamp(currentBIndex);
                }

                while (currentCTime < maxTime &&
                    Math.Abs(currentCTime - maxTime) > 3 * TimeUtils.OA_MILLISECOND &&
                    currentCIndex < measurement.Series[Channel.CURRENT_C_MOD].Count)
                {
                    currentCIndex++;
                    currentCTime = measurement.Series[Channel.CURRENT_C_MOD].Timestamp(currentCIndex);
                }




                if (voltageAIndex >= measurement.Series[Channel.VOLTAGE_A_MOD].Count ||
                    voltageBIndex >= measurement.Series[Channel.VOLTAGE_B_MOD].Count ||
                    voltageCIndex >= measurement.Series[Channel.VOLTAGE_C_MOD].Count ||
                    currentAIndex >= measurement.Series[Channel.CURRENT_A_MOD].Count ||
                    currentBIndex >= measurement.Series[Channel.CURRENT_B_MOD].Count ||
                    currentCIndex >= measurement.Series[Channel.CURRENT_C_MOD].Count)
                    break;

                if (missing != null && missing.Reading(voltageAIndex) == 1)
                {
                    invalidToWrite++;

                    voltageAIndex++;
                    voltageBIndex++;
                    voltageCIndex++;

                    currentAIndex++;
                    currentBIndex++;
                    currentCIndex++;
                    continue;
                }

                double phaseAReading = measurement.Series[Channel.VOLTAGE_A_MOD].Reading(voltageAIndex) * measurement.Series[Channel.CURRENT_A_MOD].Reading(currentAIndex);
                double phaseBReading = measurement.Series[Channel.VOLTAGE_B_MOD].Reading(voltageBIndex) * measurement.Series[Channel.CURRENT_B_MOD].Reading(currentBIndex);
                double phaseCReading = measurement.Series[Channel.VOLTAGE_C_MOD].Reading(voltageCIndex) * measurement.Series[Channel.CURRENT_C_MOD].Reading(currentCIndex);

                double phaseAAngle = (measurement.Series[Channel.VOLTAGE_A_ANG].Reading(voltageAIndex) - measurement.Series[Channel.CURRENT_A_ANG].Reading(currentAIndex)) * Math.PI / 180;
                double phaseBAngle = (measurement.Series[Channel.VOLTAGE_B_ANG].Reading(voltageBIndex) - measurement.Series[Channel.CURRENT_B_ANG].Reading(currentBIndex)) * Math.PI / 180;
                double phaseCAngle = (measurement.Series[Channel.VOLTAGE_C_ANG].Reading(voltageCIndex) - measurement.Series[Channel.CURRENT_C_ANG].Reading(currentCIndex)) * Math.PI / 180;


                double activePower = phaseAReading * Math.Cos(phaseAAngle) + phaseBReading * Math.Cos(phaseBAngle) + phaseCReading * Math.Cos(phaseCAngle);

                double reactivePower = phaseAReading * Math.Sin(phaseAAngle) + phaseBReading * Math.Sin(phaseBAngle) + phaseCReading * Math.Sin(phaseCAngle);


                for (int j = 0; j < invalidToWrite; j++)
                {
                    double time = measurement.Series[Channel.VOLTAGE_A_MOD].Timestamp(voltageAIndex - invalidToWrite + j);
                    timestampsActive.Add(time);
                    timestampsReactive.Add(time);

                    if (calculatedActive.Count == 0)
                    {
                        calculatedActive.Add(activePower);
                        calculatedReactive.Add(reactivePower);
                        continue;
                    }
                    calculatedActive.Add(calculatedActive[calculatedActive.Count - 1]);
                    calculatedReactive.Add(calculatedReactive[calculatedReactive.Count - 1]);


                }
                invalidToWrite = 0;

                timestampsActive.Add(maxTime);
                timestampsReactive.Add(maxTime);

                calculatedActive.Add(activePower);
                calculatedReactive.Add(reactivePower);

                voltageAIndex++;
                voltageBIndex++;
                voltageCIndex++;
                currentAIndex++;
                currentBIndex++;
                currentCIndex++;
            }

            if (calculatedActive.Count != 0)
            {
                int initialSize = calculatedActive.Count;
                for (int j = 0; j < invalidToWrite; j++)
                {
                    double time = measurement.Series[Channel.VOLTAGE_A_MOD].Timestamp(initialSize + j);
                    timestampsActive.Add(time);
                    timestampsReactive.Add(time);

                    calculatedActive.Add(calculatedActive[calculatedActive.Count - 1]);
                    calculatedReactive.Add(calculatedReactive[calculatedReactive.Count - 1]);
                }
            }

            result.Add(new KeyValuePair<Channel, TimeSeries>(Channel.ACTIVE_POWER, new TimeSeries(timestampsActive, calculatedActive)));
            result.Add(new KeyValuePair<Channel, TimeSeries>(Channel.REACTIVE_POWER, new TimeSeries(timestampsReactive, calculatedReactive)));


            return result;

        }

        public static bool CanCalculateRocof(Measurement measurement)
        {
            return CanCalculateFrequency(measurement) || measurement.Series.ContainsKey(Channel.FREQ);
        }

        public static bool CanCalculateFrequency(Measurement measurement) => CanCalculateSequences(measurement);

        public static bool CanCalculateSequences(Measurement measurement, bool current = false)
        {
            if (current)
                return measurement.Series.ContainsKey(Channel.CURRENT_A_MOD) &&
                    measurement.Series.ContainsKey(Channel.CURRENT_A_ANG) &&
                    measurement.Series.ContainsKey(Channel.CURRENT_B_MOD) &&
                    measurement.Series.ContainsKey(Channel.CURRENT_B_ANG) &&
                    measurement.Series.ContainsKey(Channel.CURRENT_C_MOD) &&
                    measurement.Series.ContainsKey(Channel.CURRENT_C_ANG);
            else
                return measurement.Series.ContainsKey(Channel.VOLTAGE_A_MOD) &&
                    measurement.Series.ContainsKey(Channel.VOLTAGE_A_ANG) &&
                    measurement.Series.ContainsKey(Channel.VOLTAGE_B_MOD) &&
                    measurement.Series.ContainsKey(Channel.VOLTAGE_B_ANG) &&
                    measurement.Series.ContainsKey(Channel.VOLTAGE_C_MOD) &&
                    measurement.Series.ContainsKey(Channel.VOLTAGE_C_ANG);

        }

        public static bool CanCalculatePowers(Measurement measurement) => CanCalculateSequences(measurement) && CanCalculateSequences(measurement, true);



    }
}
