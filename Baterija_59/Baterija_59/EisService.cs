using Baterija_59.Events;
using Baterija_59.Faults;
using Baterija_59.IO;
using Baterija_59.Models;
using System;
using System.Configuration;
using System.ServiceModel;

namespace Baterija_59
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession)]
    public class EisService : IEisService
    {
        private EisMeta currentMeta;
        private int lastRowIndex = -1;
        private int receivedRows = 0;
        private CsvSessionWriter csvWriter;

        private EisSample previousSample;
        private double previousZ;
        private double zSum = 0;
        private int zCount = 0;

        private double vThreshold;
        private double zThreshold;
        private double runningMeanPercent;

        public event EventHandler<TransferEventArgs> OnTransferStarted;
        public event EventHandler<SampleReceivedEventArgs> OnSampleReceived;
        public event EventHandler<TransferEventArgs> OnTransferCompleted;
        public event EventHandler<WarningEventArgs> OnWarningRaised;

        public EisService()
        {
            OnTransferStarted += LogTransferStarted;
            OnSampleReceived += LogSampleReceived;
            OnTransferCompleted += LogTransferCompleted;
            OnWarningRaised += LogWarningRaised;
            vThreshold = ReadDoubleSetting("V_threshold", 0.2);
            zThreshold = ReadDoubleSetting("Z_threshold", 0.1);
            runningMeanPercent = ReadDoubleSetting("RunningMeanPercent", 25);

        }

        public AckResponse StartSession(EisMeta meta)
        {
            if (currentMeta != null)
            {
                ThrowValidationFault("Sesija je vec pokrenuta.", "Session");
            }

            ValidateMeta(meta);

            currentMeta = meta;
            lastRowIndex = -1;
            receivedRows = 0;

            previousSample = null;
            previousZ = 0;
            zSum = 0;
            zCount = 0;

            csvWriter = new CsvSessionWriter(meta);

            RaiseTransferStarted(meta);

            Console.WriteLine("Pokrenuta sesija:");
            Console.WriteLine("BatteryId: " + meta.BatteryId);
            Console.WriteLine("TestId: " + meta.TestId);
            Console.WriteLine("SoC: " + meta.SocPercent);
            Console.WriteLine("FileName: " + meta.FileName);
            Console.WriteLine("TotalRows: " + meta.TotalRows);

            return new AckResponse(
                true,
                "Sesija je uspesno pokrenuta.",
                TransferStatus.IN_PROGRESS
            );
        }

        public AckResponse PushSample(EisSample sample)
        {
            if (currentMeta == null)
            {
                ThrowValidationFault("Sesija nije pokrenuta. Prvo pozvati StartSession.", "Session");
            }

            if (csvWriter == null)
            {
                ThrowValidationFault("CSV writer nije otvoren.", "CsvSessionWriter");
            }

            ValidateSample(sample);

            if (sample.RowIndex <= lastRowIndex)
            {
                ThrowValidationFault("RowIndex mora monotono da raste.", "RowIndex");
            }

            lastRowIndex = sample.RowIndex;
            receivedRows++;

            csvWriter.WriteSample(sample);

            AnalyzeSample(sample);

            RaiseSampleReceived(sample);

            Console.WriteLine("Primljen sample RowIndex: " + sample.RowIndex);

            return new AckResponse(
                true,
                "Uzorak je primljen.",
                TransferStatus.IN_PROGRESS
            );
        }

        public AckResponse EndSession()
        {
            if (currentMeta == null)
            {
                ThrowValidationFault("Ne postoji aktivna sesija.", "Session");
            }

            if (receivedRows != currentMeta.TotalRows)
            {
                string message = "Broj primljenih redova se ne poklapa sa TotalRows. Primljeno: "
                    + receivedRows + ", ocekivano: " + currentMeta.TotalRows;

                CloseWriter();
                currentMeta = null;
                lastRowIndex = -1;
                receivedRows = 0;

                previousSample = null;
                previousZ = 0;
                zSum = 0;
                zCount = 0;

                ThrowValidationFault(message, "TotalRows");
            }

            Console.WriteLine("Zavrsena sesija za fajl: " + currentMeta.FileName);

            RaiseTransferCompleted(currentMeta);

            CloseWriter();

            currentMeta = null;
            lastRowIndex = -1;
            receivedRows = 0;

            return new AckResponse(
                true,
                "Sesija je uspesno zavrsena.",
                TransferStatus.COMPLETED
            );
        }

        private void ValidateMeta(EisMeta meta)
        {
            if (meta == null)
            {
                ThrowValidationFault("Meta podaci nisu prosledjeni.", "EisMeta");
            }

            if (string.IsNullOrWhiteSpace(meta.BatteryId))
            {
                ThrowValidationFault("BatteryId je obavezan.", "BatteryId");
            }

            if (!IsValidBatteryId(meta.BatteryId))
            {
                ThrowValidationFault("BatteryId mora biti u opsegu B01 do B11.", "BatteryId");
            }

            if (string.IsNullOrWhiteSpace(meta.TestId))
            {
                ThrowValidationFault("TestId je obavezan.", "TestId");
            }

            if (meta.TestId != "Test_1" && meta.TestId != "Test_2")
            {
                ThrowValidationFault("TestId mora biti Test_1 ili Test_2.", "TestId");
            }

            if (meta.SocPercent < 5 || meta.SocPercent > 100 || meta.SocPercent % 5 != 0)
            {
                ThrowValidationFault("SoC mora biti 5, 10, 15, ..., 100.", "SocPercent");
            }

            if (string.IsNullOrWhiteSpace(meta.FileName))
            {
                ThrowValidationFault("FileName je obavezan.", "FileName");
            }

            if (meta.TotalRows <= 0)
            {
                ThrowValidationFault("TotalRows mora biti pozitivan broj.", "TotalRows");
            }

            if (meta.TotalRows > 28)
            {
                ThrowValidationFault("TotalRows ne sme biti veci od 28 za EIS CSV fajl.", "TotalRows");
            }
        }

        private bool IsValidBatteryId(string batteryId)
        {
            if (batteryId.Length != 3)
            {
                return false;
            }

            if (batteryId[0] != 'B')
            {
                return false;
            }

            int number;

            if (!int.TryParse(batteryId.Substring(1), out number))
            {
                return false;
            }

            if (number < 1 || number > 11)
            {
                return false;
            }

            return true;
        }

        private void ValidateSample(EisSample sample)
        {
            if (sample == null)
            {
                ThrowValidationFault("Sample nije prosledjen.", "EisSample");
            }

            if (sample.RowIndex < 0)
            {
                ThrowValidationFault("RowIndex ne sme biti negativan.", "RowIndex");
            }

            if (sample.FrequencyHz <= 0)
            {
                ThrowValidationFault("FrequencyHz mora biti veci od 0.", "FrequencyHz");
            }

            if (double.IsNaN(sample.R_ohm) || double.IsInfinity(sample.R_ohm))
            {
                ThrowValidationFault("R_ohm nije validna vrednost.", "R_ohm");
            }

            if (double.IsNaN(sample.X_ohm) || double.IsInfinity(sample.X_ohm))
            {
                ThrowValidationFault("X_ohm nije validna vrednost.", "X_ohm");
            }

            if (double.IsNaN(sample.V) || double.IsInfinity(sample.V))
            {
                ThrowValidationFault("V nije validna vrednost.", "V");
            }

            if (double.IsNaN(sample.T_degC) || double.IsInfinity(sample.T_degC))
            {
                ThrowValidationFault("T_degC nije validna vrednost.", "T_degC");
            }

            if (double.IsNaN(sample.Range_ohm) || double.IsInfinity(sample.Range_ohm))
            {
                ThrowValidationFault("Range_ohm nije validna vrednost.", "Range_ohm");
            }
        }

        private void CloseWriter()
        {
            if (csvWriter != null)
            {
                csvWriter.Dispose();
                csvWriter = null;
            }
        }

        private void RaiseTransferStarted(EisMeta meta)
        {
            OnTransferStarted?.Invoke(this, new TransferEventArgs(meta));
        }

        private void RaiseSampleReceived(EisSample sample)
        {
            OnSampleReceived?.Invoke(this, new SampleReceivedEventArgs(currentMeta, sample));
        }

        private void RaiseTransferCompleted(EisMeta meta)
        {
            OnTransferCompleted?.Invoke(this, new TransferEventArgs(meta));
        }

        private void RaiseWarning(EisSample sample, string warningType, string message)
        {
            OnWarningRaised?.Invoke(this, new WarningEventArgs(currentMeta, sample, warningType, message));
        }

        private void LogTransferStarted(object sender, TransferEventArgs e)
        {
            Console.WriteLine("[EVENT] Transfer started: " + e.Meta.FileName);
        }

        private void LogSampleReceived(object sender, SampleReceivedEventArgs e)
        {
            Console.WriteLine("[EVENT] Sample received: RowIndex=" + e.Sample.RowIndex);
        }

        private void LogTransferCompleted(object sender, TransferEventArgs e)
        {
            Console.WriteLine("[EVENT] Transfer completed: " + e.Meta.FileName);
        }

        private void LogWarningRaised(object sender, WarningEventArgs e)
        {
            Console.WriteLine("[WARNING] " + e.WarningType + " | " + e.Message);
        }

        private void AnalyzeSample(EisSample sample)
        {
            double currentZ = Math.Sqrt(sample.R_ohm * sample.R_ohm + sample.X_ohm * sample.X_ohm);

            if (previousSample != null)
            {
                double deltaV = sample.V - previousSample.V;

                if (Math.Abs(deltaV) > vThreshold)
                {
                    string direction = deltaV > 0 ? "iznad ocekivanog" : "ispod ocekivanog";

                    RaiseWarning(
                        sample,
                        "VoltageSpike",
                        "Detektovana nagla promena napona. DeltaV=" + deltaV + ", smer=" + direction
                    );
                }

                double deltaZ = currentZ - previousZ;

                if (Math.Abs(deltaZ) > zThreshold)
                {
                    string direction = deltaZ > 0 ? "iznad ocekivanog" : "ispod ocekivanog";

                    RaiseWarning(
                        sample,
                        "ImpedanceJump",
                        "Detektovana nagla promena impedanse. DeltaZ=" + deltaZ + ", smer=" + direction
                    );
                }
            }

            if (zCount > 0)
            {
                double runningMean = zSum / zCount;

                double lowerLimit = runningMean * (1 - runningMeanPercent / 100.0);
                double upperLimit = runningMean * (1 + runningMeanPercent / 100.0);

                if (currentZ < lowerLimit || currentZ > upperLimit)
                {
                    string direction = currentZ > upperLimit ? "iznad ocekivane vrednosti" : "ispod ocekivane vrednosti";

                    RaiseWarning(
                        sample,
                        "OutOfBandWarning",
                        "Impedansa odstupa od tekuceg proseka. Z=" + currentZ +
                        ", prosek=" + runningMean +
                        ", smer=" + direction
                    );
                }
            }

            zSum += currentZ;
            zCount++;

            previousSample = sample;
            previousZ = currentZ;
        }

        private double ReadDoubleSetting(string key, double defaultValue)
        {
            string value = ConfigurationManager.AppSettings[key];

            double result;

            if (double.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out result))
            {
                return result;
            }

            return defaultValue;
        }


        private void ThrowValidationFault(string message, string fieldName)
        {
            ValidationFault fault = new ValidationFault(message, fieldName);
            throw new FaultException<ValidationFault>(fault, new FaultReason(message));
        }
    }
}