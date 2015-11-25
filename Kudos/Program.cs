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

            //Save current time for reporting later
            DateTime now = DateTime.Now;

            //Execute the extraction
            mvKudos.DB.exportAllProducts(sCsvFile);

            //Report the results
            string sResult = "Done in " + DateTime.Now.Subtract(now).TotalMinutes.ToString() + " minutes.";
            Console.WriteLine(sResult);
            System.Diagnostics.EventLog.WriteEntry(Program.EventSourceName, sResult, System.Diagnostics.EventLogEntryType.Information);

            System.Threading.Thread.Sleep(5000);
        }

    }
}
