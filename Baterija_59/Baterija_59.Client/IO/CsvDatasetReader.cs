using Baterija_59.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Baterija_59.Client.IO
{
    public class CsvDatasetReader : IDisposable
    {
        private StreamReader reader;
        private StreamWriter logWriter;
        private bool disposed;

        public CsvDatasetReader(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("CSV fajl nije pronadjen.", filePath);
            }

            reader = new StreamReader(filePath);

            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "invalid_rows_log.txt");
            logWriter = new StreamWriter(logPath, true);
        }

        public List<EisSample> ReadAllSamples()
        {
            ThrowIfDisposed();

            List<EisSample> samples = new List<EisSample>();

            string line;
            bool isFirstLine = true;
            int rowIndex = 0;
            int lineNumber = 0;

            while ((line = reader.ReadLine()) != null)
            {
                lineNumber++;

                if (isFirstLine)
                {
                    isFirstLine = false;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (rowIndex >= 28)
                {
                    LogInvalidLine(lineNumber, line, "Red viska. Ocekivano je 28 redova po CSV fajlu.");
                    continue;
                }

                string[] parts = line.Split(',');

                if (parts.Length < 6)
                {
                    LogInvalidLine(lineNumber, line, "Red nema dovoljan broj kolona.");
                    continue;
                }

                try
                {
                    EisSample sample = new EisSample();

                    sample.RowIndex = rowIndex;
                    sample.FrequencyHz = double.Parse(parts[0], CultureInfo.InvariantCulture);
                    sample.R_ohm = double.Parse(parts[1], CultureInfo.InvariantCulture);
                    sample.X_ohm = double.Parse(parts[2], CultureInfo.InvariantCulture);
                    sample.V = double.Parse(parts[3], CultureInfo.InvariantCulture);
                    sample.T_degC = double.Parse(parts[4], CultureInfo.InvariantCulture);
                    sample.Range_ohm = double.Parse(parts[5], CultureInfo.InvariantCulture);

                    samples.Add(sample);
                    rowIndex++;
                }
                catch (Exception)
                {
                    LogInvalidLine(lineNumber, line, "Greska pri parsiranju reda.");
                }
            }

            return samples;
        }

        private void LogInvalidLine(int lineNumber, string line, string reason)
        {
            Console.WriteLine("Nevalidan red " + lineNumber + ": " + reason);

            if (logWriter != null)
            {
                logWriter.WriteLine("Red " + lineNumber + " | " + reason + " | " + line);
                logWriter.Flush();
            }
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException("CsvDatasetReader");
            }
        }

        ~CsvDatasetReader()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (reader != null)
                    {
                        reader.Dispose();
                        reader = null;
                    }

                    if (logWriter != null)
                    {
                        logWriter.Dispose();
                        logWriter = null;
                    }
                }

                disposed = true;
            }
        }
    }
}