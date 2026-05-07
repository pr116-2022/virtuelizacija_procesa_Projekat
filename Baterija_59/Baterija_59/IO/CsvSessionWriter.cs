using Baterija_59.Models;
using System;
using System.Globalization;
using System.IO;

namespace Baterija_59.IO
{
    public class CsvSessionWriter : IDisposable
    {
        private StreamWriter sessionWriter;
        private StreamWriter rejectsWriter;
        private bool disposed;

        public string SessionFilePath { get; private set; }
        public string RejectsFilePath { get; private set; }

        public CsvSessionWriter(EisMeta meta)
        {
            if (meta == null)
            {
                throw new ArgumentNullException(nameof(meta));
            }

            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

            string sessionDirectory = Path.Combine(
                baseDirectory,
                "Data",
                meta.BatteryId,
                meta.TestId,
                meta.SocPercent.ToString()
            );

            Directory.CreateDirectory(sessionDirectory);

            SessionFilePath = Path.Combine(sessionDirectory, "session.csv");
            RejectsFilePath = Path.Combine(sessionDirectory, "rejects.csv");

            sessionWriter = new StreamWriter(SessionFilePath, false);
            rejectsWriter = new StreamWriter(RejectsFilePath, false);

            sessionWriter.WriteLine("RowIndex,FrequencyHz,R_ohm,X_ohm,V,T_degC,Range_ohm");
            rejectsWriter.WriteLine("RowIndex,Reason");
        }

        public void WriteSample(EisSample sample)
        {
            ThrowIfDisposed();

            string line = string.Join(",",
                sample.RowIndex.ToString(CultureInfo.InvariantCulture),
                sample.FrequencyHz.ToString(CultureInfo.InvariantCulture),
                sample.R_ohm.ToString(CultureInfo.InvariantCulture),
                sample.X_ohm.ToString(CultureInfo.InvariantCulture),
                sample.V.ToString(CultureInfo.InvariantCulture),
                sample.T_degC.ToString(CultureInfo.InvariantCulture),
                sample.Range_ohm.ToString(CultureInfo.InvariantCulture)
            );

            sessionWriter.WriteLine(line);
            sessionWriter.Flush();
        }

        public void WriteReject(int rowIndex, string reason)
        {
            ThrowIfDisposed();

            string safeReason = reason == null ? "" : reason.Replace(",", ";");

            rejectsWriter.WriteLine(
                rowIndex.ToString(CultureInfo.InvariantCulture) + "," + safeReason
            );

            rejectsWriter.Flush();
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(CsvSessionWriter));
            }
        }

        public void Dispose()
        {
            if (!disposed)
            {
                if (sessionWriter != null)
                {
                    sessionWriter.Dispose();
                    sessionWriter = null;
                }

                if (rejectsWriter != null)
                {
                    rejectsWriter.Dispose();
                    rejectsWriter = null;
                }

                disposed = true;
            }
        }
    }
}