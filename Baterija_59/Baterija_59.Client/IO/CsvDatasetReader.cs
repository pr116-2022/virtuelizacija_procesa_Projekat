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
        private bool disposed;

        public CsvDatasetReader(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("CSV fajl nije pronađen.", filePath);
            }

            reader = new StreamReader(filePath);
        }

        public List<EisSample> ReadAllSamples()
        {
            ThrowIfDisposed();

            List<EisSample> samples = new List<EisSample>();

            string line;

            bool isFirstLine = true;
            int rowIndex = 0;

            while ((line = reader.ReadLine()) != null)
            {
                if (isFirstLine)
                {
                    isFirstLine = false;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                string[] parts = line.Split(',');

                if (parts.Length < 6)
                {
                    Console.WriteLine("Nevalidan red: " + line);
                    continue;
                }

                try
                {
                    EisSample sample = new EisSample
                    {
                        RowIndex = rowIndex,
                        FrequencyHz = double.Parse(parts[0], CultureInfo.InvariantCulture),
                        R_ohm = double.Parse(parts[1], CultureInfo.InvariantCulture),
                        X_ohm = double.Parse(parts[2], CultureInfo.InvariantCulture),
                        V = double.Parse(parts[3], CultureInfo.InvariantCulture),
                        T_degC = double.Parse(parts[4], CultureInfo.InvariantCulture),
                        Range_ohm = double.Parse(parts[5], CultureInfo.InvariantCulture)
                    };

                    samples.Add(sample);

                    rowIndex++;
                }
                catch (Exception)
                {
                    Console.WriteLine("Greška pri parsiranju reda: " + line);
                }
            }

            return samples;
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(CsvDatasetReader));
            }
        }

        public void Dispose()
        {
            if (!disposed)
            {
                if (reader != null)
                {
                    reader.Dispose();
                    reader = null;
                }

                disposed = true;
            }
        }
    }
}