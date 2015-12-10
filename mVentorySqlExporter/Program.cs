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
            string sCsvFile = sCsvPath + "mvSqlExp_" + DateTime.Now.ToString("s").Replace(":", "") + ".csv";
            Console.WriteLine(Strings.msgWelcome);
            Log(Strings.msgSavingInto + " " + sCsvFile);

            //Save current time for reporting later
            DateTime now = DateTime.Now;

            //Execute the extraction
            mvSqlExporter.DB.exportAllProducts(sCsvFile);

            //Report the results
            string sResult = string.Format(Strings.msgDoneIn, DateTime.Now.Subtract(now).TotalMinutes.ToString());
            Log(sResult);

            System.Threading.Thread.Sleep(5000);
        }


        /// <summary>
        /// Log a msg to a file instead of 
        /// </summary>
        /// <param name="Msg"></param>
        public static void Log(string Msg)
        {
            //Get file name / path
            string sFileName = new Uri(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).TrimEnd(new char[] { '\\' }) + "\\" + ConfigurationManager.AppSettings.Get("LogFile")).LocalPath;

            //Write to the screen
            Console.WriteLine(Msg);

            //Check if the file exists
            if (!File.Exists(sFileName)) File.Create(sFileName).Close();

            //write and close
            StreamWriter w = File.AppendText(sFileName);
            w.WriteLine("{0} {1} {2}", DateTime.Now.ToLongDateString(), DateTime.Now.ToLongTimeString(), Msg);
            w.Flush();
            w.Close();
        }

    }
}
