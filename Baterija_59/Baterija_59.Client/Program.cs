using Baterija_59.Client.IO;
using Baterija_59.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceModel;

namespace Baterija_59.Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Izaberite opciju:");
                Console.WriteLine("1 - Posalji jedan CSV fajl");
                Console.WriteLine("2 - Posalji sve CSV fajlove iz dataset foldera");
                Console.Write("Opcija: ");

                string option = Console.ReadLine();

                if (option == "1")
                {
                    SendOneCsvFile();
                }
                else if (option == "2")
                {
                    Console.WriteLine("Unesite putanju do dataset foldera:");
                    string datasetPath = Console.ReadLine();

                    if (!Directory.Exists(datasetPath))
                    {
                        Console.WriteLine("Dataset folder ne postoji.");
                        Console.ReadLine();
                        return;
                    }

                    SendDatasetFolder(datasetPath);
                }
                else
                {
                    Console.WriteLine("Nepoznata opcija.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Greska:");
                Console.WriteLine(ex.Message);
            }

            Console.WriteLine("Pritisni ENTER za izlaz.");
            Console.ReadLine();
        }

        private static void SendOneCsvFile()
        {
            Console.WriteLine("Unesite punu putanju do CSV fajla:");
            string csvPath = Console.ReadLine();

            if (!File.Exists(csvPath))
            {
                Console.WriteLine("Fajl ne postoji.");
                return;
            }

            Console.Write("Unesite BatteryId, npr. B01: ");
            string batteryId = Console.ReadLine();

            Console.Write("Unesite TestId, npr. Test_1: ");
            string testId = Console.ReadLine();

            Console.Write("Unesite SoC procenat, npr. 50: ");
            int socPercent = int.Parse(Console.ReadLine());

            SendCsvFile(csvPath, batteryId, testId, socPercent);
        }

        private static void SendDatasetFolder(string datasetPath)
        {
            int sentFiles = 0;

            string[] batteryFolders = Directory.GetDirectories(datasetPath);

            for (int i = 0; i < batteryFolders.Length; i++)
            {
                string batteryId = Path.GetFileName(batteryFolders[i]);

                if (!IsValidBatteryFolder(batteryId))
                {
                    continue;
                }

                string eisFolder = Path.Combine(batteryFolders[i], "EIS measurements");

                if (!Directory.Exists(eisFolder))
                {
                    eisFolder = Path.Combine(batteryFolders[i], "EIS Measurement");
                }

                if (!Directory.Exists(eisFolder))
                {
                    Console.WriteLine("Nije pronadjen EIS folder za bateriju: " + batteryId);
                    continue;
                }

                string[] testFolders = Directory.GetDirectories(eisFolder);

                for (int j = 0; j < testFolders.Length; j++)
                {
                    string testId = Path.GetFileName(testFolders[j]);

                    if (testId != "Test_1" && testId != "Test_2")
                    {
                        continue;
                    }

                    string[] csvFiles = Directory.GetFiles(testFolders[j], "*.csv", SearchOption.AllDirectories);

                    for (int k = 0; k < csvFiles.Length; k++)
                    {
                        int socPercent = ExtractSocFromFileName(csvFiles[k]);

                        if (socPercent == -1)
                        {
                            Console.WriteLine("Nije pronadjen SoC u nazivu fajla. Fajl se preskace: " + csvFiles[k]);
                            continue;
                        }

                        if (socPercent < 5 || socPercent > 100 || socPercent % 5 != 0)
                        {
                            Console.WriteLine("SoC nije validan. Fajl se preskace: " + csvFiles[k]);
                            continue;
                        }

                        bool success = SendCsvFile(csvFiles[k], batteryId, testId, socPercent);

                        if (success)
                        {
                            sentFiles++;
                        }
                    }
                }
            }

            Console.WriteLine("Ukupno uspesno poslatih CSV fajlova: " + sentFiles);
        }

        private static bool SendCsvFile(string csvPath, string batteryId, string testId, int socPercent)
        {
            ChannelFactory<IEisService> factory = null;
            IEisService proxy = null;

            try
            {
                if (!File.Exists(csvPath))
                {
                    Console.WriteLine("Fajl ne postoji: " + csvPath);
                    return false;
                }

                List<EisSample> samples;

                using (CsvDatasetReader reader = new CsvDatasetReader(csvPath))
                {
                    samples = reader.ReadAllSamples();
                }

                if (samples.Count == 0)
                {
                    Console.WriteLine("Nema ucitanih validnih redova iz CSV fajla: " + csvPath);
                    return false;
                }

                if (samples.Count != 28)
                {
                    Console.WriteLine("Upozorenje: Ocekivano je 28 redova, a ucitano je " + samples.Count);
                }

                EisMeta meta = new EisMeta
                {
                    BatteryId = batteryId,
                    TestId = testId,
                    SocPercent = socPercent,
                    FileName = Path.GetFileName(csvPath),
                    TotalRows = samples.Count
                };

                factory = new ChannelFactory<IEisService>("EisServiceEndpoint");
                proxy = factory.CreateChannel();

                Console.WriteLine("-----------------------------------");
                Console.WriteLine("Slanje fajla: " + csvPath);
                Console.WriteLine("BatteryId: " + batteryId);
                Console.WriteLine("TestId: " + testId);
                Console.WriteLine("SoC: " + socPercent);
                Console.WriteLine("Povezivanje sa EIS servisom...");

                AckResponse startResponse = proxy.StartSession(meta);
                Console.WriteLine("StartSession: " + startResponse.IsAck + " | " + startResponse.Message + " | " + startResponse.Status);

                for (int i = 0; i < samples.Count; i++)
                {
                    AckResponse pushResponse = proxy.PushSample(samples[i]);
                    Console.WriteLine("PushSample " + (i + 1) + ": " + pushResponse.IsAck + " | " + pushResponse.Message + " | " + pushResponse.Status);
                }

                AckResponse endResponse = proxy.EndSession();
                Console.WriteLine("EndSession: " + endResponse.IsAck + " | " + endResponse.Message + " | " + endResponse.Status);

                IClientChannel channel = proxy as IClientChannel;

                if (channel != null)
                {
                    channel.Close();
                }

                factory.Close();

                Console.WriteLine("Slanje CSV fajla je zavrseno uspesno.");
                return true;
            }
            catch (FaultException ex)
            {
                Console.WriteLine("WCF fault:");
                Console.WriteLine(ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Greska pri slanju CSV fajla:");
                Console.WriteLine(ex.Message);
                return false;
            }
            finally
            {
                IClientChannel channel = proxy as IClientChannel;

                if (channel != null && channel.State != CommunicationState.Closed)
                {
                    channel.Abort();
                }

                if (factory != null && factory.State != CommunicationState.Closed)
                {
                    factory.Abort();
                }
            }
        }

        private static bool IsValidBatteryFolder(string batteryId)
        {
            if (string.IsNullOrWhiteSpace(batteryId))
            {
                return false;
            }

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

        private static int ExtractSocFromFileName(string filePath)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            char[] separators = new char[] { '_', '-', ' ', '(', ')' };
            string[] parts = fileName.Split(separators, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i].ToUpper();

                if (part == "SOC" && i + 1 < parts.Length)
                {
                    int soc;

                    if (int.TryParse(parts[i + 1], out soc))
                    {
                        return soc;
                    }
                }

                if (part.StartsWith("SOC") && part.Length > 3)
                {
                    string numberText = part.Substring(3);
                    int soc;

                    if (int.TryParse(numberText, out soc))
                    {
                        return soc;
                    }
                }
            }

            return -1;
        }
    }
}