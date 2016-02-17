using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.IO;

namespace mVentoryGenericExporter
{

    class Program
    {

        static void Main(string[] args)
        {
            //Check if paths exist
            string CsvPath = System.IO.Directory.GetCurrentDirectory() + "\\csv";
            System.IO.Directory.CreateDirectory(CsvPath);

            //Save current time for reporting later
            DateTime now = DateTime.Now;

            //Get list of SQL queries
            string SqlPath = System.IO.Directory.GetCurrentDirectory() + "\\sql";
            string[] files = System.IO.Directory.GetFiles(SqlPath);
            if (files.Length == 0)
            {
                Log(Strings.msgNoSQL);
                System.Threading.Thread.Sleep(5000);
                return;
            }

            //Execute the extraction for every file
            for (int i = 0; i < files.Length; i++)
            {
                mVentoryGenericExporter.DB.exportAllProducts(files[i]);
                //Report the results
                string sResult = string.Format(Strings.msgDoneIn, DateTime.Now.Subtract(now).TotalMinutes.ToString(), files[i]);
                Log(sResult);
            }

            //Give the user a chance to read the output.
            System.Threading.Thread.Sleep(5000);
        }


        /// <summary>
        /// Log a msg to a file instead of the system log and output it to the screen
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
