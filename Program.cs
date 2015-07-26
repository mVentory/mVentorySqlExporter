using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.IO;

namespace mvKudos
{

    class Program
    {

        public const string EventSourceName = "mvKudos";

        static void Main(string[] args)
        {
            //Check if paths exist
            string sCsvPath = ConfigurationManager.AppSettings.Get("FtpFolderPathCsv").TrimEnd(new char[] { '\\' }) + "\\";
            System.IO.Directory.CreateDirectory(sCsvPath);
            string sImgPath = ConfigurationManager.AppSettings.Get("FtpFolderPathImg").TrimEnd(new char[] { '\\' }) + "\\";
            System.IO.Directory.CreateDirectory(sImgPath);

            //Generate CSV file name
            string sCsvFile = sCsvPath + "mvKudos_" + DateTime.Now.ToString("s").Replace(":","") + ".csv";
            Console.WriteLine(ConfigurationManager.AppSettings.Get("WelcomeMsg"));
            Console.WriteLine("Saving into " + sCsvFile);

            mvKudos.DB.exportAllProducts(sCsvFile);

            Console.WriteLine("Done.");
            
            System.Threading.Thread.Sleep(5000);
        }

    }
}
