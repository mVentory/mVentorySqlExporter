using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mvKudos
{

    class Program
    {

        public const string EventSourceName = "mvKudos";

        static void Main(string[] args)
        {
            string sCsvFile = System.IO.Path.GetTempPath() + "mvKudos_" + Guid.NewGuid().ToString().Substring(0, 5) + ".csv";
            Console.WriteLine(System.Configuration.ConfigurationManager.AppSettings.Get("WelcomeMsg"));
            Console.WriteLine("Saving into " + sCsvFile);

            mvKudos.DB.exportAllProducts(sCsvFile);

            Console.WriteLine("Done.");
            
            System.Threading.Thread.Sleep(5000);
        }

    }
}
