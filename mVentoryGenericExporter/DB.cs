using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Configuration;
using System.IO;
using System.IO.Compression;



namespace mVentoryGenericExporter
{
    class DB
    {
        /// <summary>
        /// Exports all products from a DB into a CSV file for further upload.
        /// </summary>
        /// <returns></returns>
        public static void exportAllProducts(string sSqlFile)
        {
            Console.WriteLine("Extracting product data from the DB. " + DateTime.Now.ToString("s"));

            //Get data from the DB. The query is quite elaborate and had to be placed in an external file.
            SqlConnection connectionDB = new SqlConnection(ConfigurationManager.ConnectionStrings["DBSource"].ConnectionString);
            connectionDB.Open();
            SqlCommand commandGetAllProducts = new SqlCommand((System.IO.File.ReadAllText(sSqlFile)), connectionDB);
            commandGetAllProducts.CommandTimeout = 0;
            SqlDataReader dbReader = commandGetAllProducts.ExecuteReader();

            ///Prepare the output CSV file
            StringBuilder CSV = new StringBuilder();
            CSV.AppendLine(getListOfFields(dbReader));  //add the header row

            //Loop through the recordset and read the data
            Program.Log("Going through extracted data.");

            while (dbReader.Read())
            {
                //add the record to the output
                string sRecord = BuildCsvString(dbReader);
                CSV.AppendLine(sRecord);
            }

            connectionDB.Close(); //Don't need the DB any more.

            //report progress
            Program.Log("Saving data.");

            //Save the data in a temp file with a unique name
            string sCsvFile = System.IO.Directory.GetCurrentDirectory() + "\\csv\\" + System.IO.Path.GetFileNameWithoutExtension(sSqlFile) + ".csv";

            System.IO.File.WriteAllText(sCsvFile, CSV.ToString());
            CompressFile(sCsvFile);

        }

        /// <summary>
        /// Gets an SQL query from the file
        /// </summary>
        /// <param name="queryName"></param>
        /// <returns></returns>
        static string getSqlQuery(string queryFile)
        {
            return System.IO.File.ReadAllText(queryFile);
        }

        /// <summary>
        /// Return the list of DB field names as a CSV string
        /// </summary>
        /// <param name="dbReader"></param>
        /// <returns></returns>
        static string getListOfFields(SqlDataReader dbReader)
        {

            //get the 1st value
            string fields = "\"" + dbReader.GetName(0) + "\"";

            //get the rest of the values
            for (int i = 1; i < dbReader.FieldCount; i++)
            {
                fields += "," + "\"" + dbReader.GetName(i) + "\"";
            }

            return fields;
        }


        /// <summary>
        /// Builds a single CSV row
        /// </summary>
        /// <param name="dbValues"></param>
        /// <returns></returns>
        static string buildCSVRow(System.Collections.Specialized.StringCollection dbValues)
        {

            //Loop through the columns and write the values
            string sOutputString = "";
            foreach (string sValue in dbValues)
            {
                string sV = sValue; //create a copy

                if (sV == null) sV = "";
                if (sOutputString != "") sOutputString += ","; //Add a ,-separator between values, but not for the very first value
                sV = sV.Replace("\"", "\"\""); //Replace all single " with "" for CSV " escaping
                sV = "\"" + sV + "\""; ///Wrap the value in "" even if it's empty
                sOutputString += sV; // Append to output
            }

            return sOutputString;

        }


        /// <summary>
        /// Build and return a single string of CSV values for every field.
        /// </summary>
        /// <param name="arCsvColumnsAll"></param>
        /// <param name="arDbFields"></param>
        /// <param name="dbReader"></param>
        /// <returns></returns>
        static string BuildCsvString(SqlDataReader dbReader)
        {

            var oCSV = new System.Collections.Specialized.StringCollection(); //name-value collection of the output

            //Loop through the list of DB fields and extract what needs to be extracted
            for (int i = 0; i < dbReader.FieldCount; i++)
            {

                string sValue = dbReader[i].ToString(); //Get the actual value

                if (sValue != "") //Clean up the value, if any. More clean up functionality will be added as needed.
                {
                    sValue = sValue.Trim();
                }

                oCSV.Add(sValue); //Add to the collection of values for this row
            }

            //return a properly formatted CSV row
            return buildCSVRow(oCSV);
        }



        /// <summary>
        /// Compress the CSV file into filename.csv.gz.
        /// </summary>
        /// <param name="sCsvFile"></param>
        static void CompressFile(string sCsvFile)
        {
            //Uplifted from MSDN.

            FileStream sourceFileStream = File.OpenRead(sCsvFile);
            FileStream destFileStream = File.Create(sCsvFile + ".gz");

            GZipStream compressingStream = new GZipStream(destFileStream,
                CompressionMode.Compress);

            byte[] bytes = new byte[2048];
            int bytesRead;
            while ((bytesRead = sourceFileStream.Read(bytes, 0, bytes.Length)) != 0)
            {
                compressingStream.Write(bytes, 0, bytesRead);
            }

            sourceFileStream.Close();
            compressingStream.Close();
            destFileStream.Close();

            //delete the source file
            try
            {
                System.IO.File.Delete(sCsvFile);
            }
            catch { }
        }
    }
}
