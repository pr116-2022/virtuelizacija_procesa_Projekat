using Baterija_59.Faults;
using Baterija_59.Models;
using System;
using System.ServiceModel;
using Baterija_59.IO;

namespace Baterija_59
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession)]
    public class EisService : IEisService
    {
        private EisMeta currentMeta;
        private int lastRowIndex = -1;
        private int receivedRows = 0;
        private CsvSessionWriter csvWriter;

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

            csvWriter = new CsvSessionWriter(meta);

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

                ThrowValidationFault(message, "TotalRows");
            }

            Console.WriteLine("Zavrsena sesija za fajl: " + currentMeta.FileName);

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

        private void ThrowValidationFault(string message, string fieldName)
        {
            ValidationFault fault = new ValidationFault(message, fieldName);
            throw new FaultException<ValidationFault>(fault, new FaultReason(message));
        }
    }
}