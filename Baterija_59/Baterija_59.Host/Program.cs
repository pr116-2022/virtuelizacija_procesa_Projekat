using System;
using System.ServiceModel;

namespace Baterija_59.Host
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ServiceHost host = null;

            try
            {
                host = new ServiceHost(typeof(EisService));

                host.Open();

                Console.WriteLine("EIS servis je pokrenut.");
                Console.WriteLine("Pritisni ENTER za gasenje.");

                Console.ReadLine();

                host.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Greska:");
                Console.WriteLine(ex.ToString());

                if (host != null)
                {
                    host.Abort();
                }

                Console.WriteLine();
                Console.WriteLine("Pritisni ENTER za izlaz.");
                Console.ReadLine();
            }
        }
    }
}