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
            ChannelFactory<IEisService> factory = null;
            IEisService proxy = null;

            try
            {
                Console.WriteLine("Unesite punu putanju do CSV fajla:");
                string csvPath = Console.ReadLine();

                if (!File.Exists(csvPath))
                {
                    Console.WriteLine("Fajl ne postoji.");
                    Console.ReadLine();
                    return;
                }

                Console.Write("Unesite BatteryId, npr. B01: ");
                string batteryId = Console.ReadLine();

                Console.Write("Unesite TestId, npr. Test_1: ");
                string testId = Console.ReadLine();

                Console.Write("Unesite SoC procenat, npr. 50: ");
                int socPercent = int.Parse(Console.ReadLine());

                List<EisSample> samples;

                using (CsvDatasetReader reader = new CsvDatasetReader(csvPath))
                {
                    samples = reader.ReadAllSamples();
                }

                if (samples.Count == 0)
                {
                    Console.WriteLine("Nema ucitanih validnih redova iz CSV fajla.");
                    Console.ReadLine();
                    return;
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

                ((IClientChannel)proxy).Close();
                factory.Close();

                Console.WriteLine("Slanje CSV fajla je zavrseno uspesno.");
            }
            catch (FaultException ex)
            {
                Console.WriteLine("WCF fault:");
                Console.WriteLine(ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Greska:");
                Console.WriteLine(ex.Message);
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

            Console.WriteLine("Pritisni ENTER za izlaz.");
            Console.ReadLine();
        }
    }
}