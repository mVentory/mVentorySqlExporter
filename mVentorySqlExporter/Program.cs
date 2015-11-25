using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.IO;

namespace mvSqlExporter
{

    class Program
    {

        public const string EventSourceName = "mvSqlExporter";

        static void Main(string[] args)
        {
            //Check if paths exist
            string sCsvPath = ConfigurationManager.AppSettings.Get("FtpFolderPathCsv").TrimEnd(new char[] { '\\' }) + "\\";
            System.IO.Directory.CreateDirectory(sCsvPath);

            //Generate CSV file name
            string sCsvFile = sCsvPath + "mvSqlExp_" + DateTime.Now.ToString("s").Replace(":","") + ".csv";
            Console.WriteLine(Strings.msgWelcome);
            Console.WriteLine(Strings.msgSavingInto, sCsvFile);

            //Save current time for reporting later
            DateTime now = DateTime.Now;

            //Execute the extraction
            mvSqlExporter.DB.exportAllProducts(sCsvFile);

            //Report the results
            string sResult = string.Format(Strings.msgDoneIn, DateTime.Now.Subtract(now).TotalMinutes.ToString());
            Console.WriteLine(sResult);
            System.Diagnostics.EventLog.WriteEntry(Program.EventSourceName, sResult, System.Diagnostics.EventLogEntryType.Information);

            System.Threading.Thread.Sleep(5000);
        }

    }
}
