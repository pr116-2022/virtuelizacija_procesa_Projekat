using Baterija_59.Models;
using System;
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
                factory = new ChannelFactory<IEisService>("EisServiceEndpoint");
                proxy = factory.CreateChannel();

                Console.WriteLine("Povezivanje sa EIS servisom...");

                EisMeta meta = new EisMeta
                {
                    BatteryId = "B01",
                    TestId = "Test_1",
                    SocPercent = 50,
                    FileName = "B01_Test_1_SOC_50.csv",
                    TotalRows = 2
                };

                AckResponse startResponse = proxy.StartSession(meta);
                Console.WriteLine($"StartSession: {startResponse.IsAck} | {startResponse.Message} | {startResponse.Status}");

                EisSample sample1 = new EisSample
                {
                    RowIndex = 0,
                    FrequencyHz = 1000,
                    R_ohm = 0.12,
                    X_ohm = 0.03,
                    V = 3.7,
                    T_degC = 25,
                    Range_ohm = 1
                };

                AckResponse pushResponse1 = proxy.PushSample(sample1);
                Console.WriteLine($"PushSample 1: {pushResponse1.IsAck} | {pushResponse1.Message} | {pushResponse1.Status}");

                EisSample sample2 = new EisSample
                {
                    RowIndex = 1,
                    FrequencyHz = 500,
                    R_ohm = 0.13,
                    X_ohm = 0.04,
                    V = 3.9,
                    T_degC = 25,
                    Range_ohm = 1
                };

                AckResponse pushResponse2 = proxy.PushSample(sample2);
                Console.WriteLine($"PushSample 2: {pushResponse2.IsAck} | {pushResponse2.Message} | {pushResponse2.Status}");

                AckResponse endResponse = proxy.EndSession();
                Console.WriteLine($"EndSession: {endResponse.IsAck} | {endResponse.Message} | {endResponse.Status}");

                ((IClientChannel)proxy).Close();
                factory.Close();

                Console.WriteLine("Test zavrsen uspesno.");
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
                if (proxy is IClientChannel channel && channel.State != CommunicationState.Closed)
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