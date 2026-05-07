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
        private CsvSessionWriter csvWriter;

        public AckResponse StartSession(EisMeta meta)
        {
            ValidateMeta(meta);

            currentMeta = meta;
            lastRowIndex = -1;

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

            ValidateSample(sample);

            if (sample.RowIndex <= lastRowIndex)
            {
                ThrowValidationFault("RowIndex mora monotono da raste.", "RowIndex");
            }

            lastRowIndex = sample.RowIndex;

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
            Console.WriteLine("Zavrsena sesija za fajl: " + currentMeta.FileName);

            if (csvWriter != null)
            {
                csvWriter.Dispose();
                csvWriter = null;
            }

            currentMeta = null;
            lastRowIndex = -1;

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

            if (string.IsNullOrWhiteSpace(meta.TestId))
            {
                ThrowValidationFault("TestId je obavezan.", "TestId");
            }

            if (meta.SocPercent < 0 || meta.SocPercent > 100)
            {
                ThrowValidationFault("SoC mora biti između 0 i 100.", "SocPercent");
            }

            if (string.IsNullOrWhiteSpace(meta.FileName))
            {
                ThrowValidationFault("FileName je obavezan.", "FileName");
            }

            if (meta.TotalRows <= 0)
            {
                ThrowValidationFault("TotalRows mora biti pozitivan broj.", "TotalRows");
            }
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
        }

        private void ThrowValidationFault(string message, string fieldName)
        {
            ValidationFault fault = new ValidationFault(message, fieldName);
            throw new FaultException<ValidationFault>(fault, new FaultReason(message));
        }

    }
}
