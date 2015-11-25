using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Configuration;
using System.IO;
using System.IO.Compression;



namespace mvSqlExporter
{
    class DB
    {
        /// <summary>
        /// Exports all products from a DB into a CSV file for further upload.
        /// </summary>
        /// <returns></returns>
        public static void exportAllProducts(string sCsvFile)
        {
            Console.WriteLine("Extracting product data from the DB. " + DateTime.Now.ToString("s"));

            //Get data from the DB. The query is quite elaborate and had to be placed in an external file.
            SqlConnection connectionDB = new SqlConnection(ConfigurationManager.ConnectionStrings["DBSource"].ConnectionString);
            connectionDB.Open();
            SqlCommand commandGetAllProducts = new SqlCommand(AddCutoffDate( getSqlQuery("GetAllProducts")), connectionDB);
            commandGetAllProducts.CommandTimeout = 0;
            SqlDataReader dbReader = commandGetAllProducts.ExecuteReader();

            //load IDs of data processed before. The IDs are stored in files as a list in plain text
            Hashtable arSavedRowIDs = GetExportedIDs("DataIdx"); //Load the list of rows exported before
            Console.WriteLine("Loaded DataIdx. " + DateTime.Now.ToString("s"));

            var arExtractedRowIDs = new Hashtable(arSavedRowIDs.Count); //This collection will contain IDs extracted this time to keep the list current

            //Get the list of DB fields
            string sDbFields = ConfigurationManager.AppSettings.Get("DBFieldNames");
            string[] arDbFields = sDbFields.Split(new char[] { ',' });
            string sSkuName = GetSkuFieldName(arDbFields); //name of the field containing product SKU. It must be unique.

            ///Prepare the output CSV file
            StringBuilder CSV = new StringBuilder();
            CSV.AppendLine(sDbFields);  //add the header row

            //Loop through the recordset and read the data
            Console.WriteLine("Going through extracted data. " + DateTime.Now.ToString("s"));

            while (dbReader.Read())
            {
                //The row for the item is ready to be saved. It contains full item details. Additional rows will contain only some of the details.
                string sRecord = BuildCsvString(arDbFields, dbReader);

                //use a hash of the row to check if the data changed
                string sSKU = dbReader[sSkuName].ToString();
                string sHash = CalculateHash(sRecord, sSKU).ToString();
                if (!arSavedRowIDs.Contains(sHash))
                {
                    //add the record to the output
                    CSV.AppendLine(sRecord);
                }
                //save the id in a new array to update the list
                if (!arExtractedRowIDs.Contains(sHash))
                {
                    arExtractedRowIDs.Add(sHash, null);
                }
                else
                {
                    System.Diagnostics.EventLog.WriteEntry(Program.EventSourceName, "Duplicate hash " + sHash + " for: " + sRecord, System.Diagnostics.EventLogEntryType.Warning);
                }
            }

            connectionDB.Close(); //Don't need the DB any more.

            //report progress
            Console.WriteLine("Saving data. " + DateTime.Now.ToString("s"));

            //Save the data in a temp file with a unique name
            System.IO.File.WriteAllText(sCsvFile, CSV.ToString());
            CompressFile(sCsvFile);

            //Save Image IDs in a file to avoid exporting them again if they were deleted
            SaveExportedIDs(arExtractedRowIDs, "DataIdx");

        }

        /// <summary>
        /// Gets an SQL query from the file with the same name as the queryName+".sql"
        /// </summary>
        /// <param name="queryName"></param>
        /// <returns></returns>
        static string getSqlQuery(string queryName)
        {
            string path = System.IO.Directory.GetCurrentDirectory();
            path += "\\" + queryName + ".sql";
            if (!System.IO.File.Exists(path)) throw new Exception("Cannot find file: " + path);
            return System.IO.File.ReadAllText(path);
        }


        /// <summary>
        /// Builds a single CSV row using the header as the template and putting the values in the right cells
        /// </summary>
        /// <param name="dbValues"></param>
        /// <returns></returns>
        static string buildCSVRow(System.Collections.Specialized.NameValueCollection dbValues, bool UseDefaultValues = true)
        {
            //Prepare the header template
            string sHeader = ConfigurationManager.AppSettings.Get("DBFieldNames");
            string[] arHeader = sHeader.Split(new char[] { ',' });

            //Prepare the data row template with placeholders
            string[] arRow = (string[])Array.CreateInstance(typeof(string), arHeader.Length);

            //Loop through the columns and write the values
            string sOutputString = "";
            for (int i = 0; i < arHeader.Length; i++)
            {
                string sColumn = arHeader[i];
                string sValue = dbValues.Get(sColumn);
                if (sValue == null) sValue = ""; 
                if (sOutputString != "") sOutputString += ","; //Add a ,-separator between values, but not for the very first value
                sValue = sValue.Replace("\"", "\"\""); //Replace all single " with "" for CSV " escaping
                sValue = "\"" + sValue + "\""; ///Wrap the value in "" even if it's empty
                sOutputString += sValue; // Append to output
            }

            return sOutputString;

        }

  
        /// <summary>
        /// Image and row IDs are stored as a list in a file to avoid exporting them more than once
        /// </summary>
        /// <returns></returns>
        static Hashtable GetExportedIDs(string FileNameKey)
        {
            string sFilePath = ConfigurationManager.AppSettings.Get(FileNameKey);
            if (!File.Exists(sFilePath))
            {
                File.Create(sFilePath).Close(); //create a new one if it doesn't exist
            }

            //load the file
            StreamReader oFile = File.OpenText(sFilePath);
            Hashtable arValues = new Hashtable();

            //read the lines one at a time
            while (!oFile.EndOfStream)
            {
                string sLine = oFile.ReadLine(); // add only good lines

                //add the value to the array if it's not yet there
                if (sLine != null && sLine != "" && !arValues.Contains(sLine)) arValues.Add(sLine, null);
            }

            oFile.Close();

            return arValues;
        }



        /// <summary>
        ///Image IDs are stored as a list in a file to avoid exporting them more than once
        /// </summary>
        /// <param name="arSavedIDs"></param>
        static void SaveExportedIDs(Hashtable arSavedIDs, string FileNameKey)
        {
            string sFilePath = ConfigurationManager.AppSettings.Get(FileNameKey);

            //load the file
            StreamWriter oFile = File.CreateText(sFilePath);

            //write the lines one at a time
            foreach (String sLine in arSavedIDs.Keys) oFile.WriteLine(sLine);

            //save the data
            oFile.Flush();
            oFile.Close();
        }


        /// <summary>
        /// Calculate a quick hash consisting of SKU as the whole and sRecord.GetHashCode as one long integer
        /// </summary>
        /// <param name="sRecord"></param>
        /// <param name="sSKU"></param>
        /// <returns></returns>
        static long CalculateHash(string sRecord, string sSKU)
        {
            long iHash = Math.Abs(sRecord.GetHashCode());
            long iSKU = 0;
            sSKU = sSKU.PadRight(18, '0');
            long.TryParse(sSKU, out iSKU);
            iHash = iSKU + iHash;
            return iHash;
        }


        /// <summary>
        /// Build and return a single string of CSV values.
        /// </summary>
        /// <param name="arCsvColumnsAll"></param>
        /// <param name="arDbFields"></param>
        /// <param name="dbReader"></param>
        /// <returns></returns>
        static string BuildCsvString(string[] arDbFields, SqlDataReader dbReader)
        {
            var oCSV = new System.Collections.Specialized.NameValueCollection(); //name-value collection of the output

            //Loop through the list of DB fields and extract what needs to be extracted
            for (int iDbFieldIdx = 0; iDbFieldIdx < arDbFields.Length; iDbFieldIdx++)
            {
                string sDbFieldName = arDbFields[iDbFieldIdx]; //Get the name of the correspondin DB field name

                //Check if the field is present
                try 
                {
                    dbReader.GetOrdinal(sDbFieldName);
                }
                catch
                {
                    Console.WriteLine(Strings.msgMissingField, sDbFieldName);
                    Console.ReadKey();
                    throw new Exception(string.Format(Strings.msgMissingField, sDbFieldName));
                }

                string sValue = dbReader[sDbFieldName].ToString(); //Get the actual value

                if (sValue != "") //Clean up the value, if any. More clean up functionality will be added as needed.
                {
                    sValue = sValue.Trim();
                }

                oCSV.Add(sDbFieldName, sValue); //Add to the collection of values for this row
            }

            //return a properly formatted CSV row
            return buildCSVRow(oCSV);
        }


        /// <summary>
        /// Insert cutoff date into the SQL query if possible
        /// </summary>
        /// <param name="sSqlQuery"></param>
        /// <returns></returns>
        static string AddCutoffDate(string sSqlQuery)
        {
            //check if the query contains a placeholder for the cutoff date
            if (!sSqlQuery.Contains("{0}")) return sSqlQuery;
            
            //the cut off date is in the config file
            string sCutOffDate = ConfigurationManager.AppSettings.Get("CutoffDate");

            //but it is optional, so may need to insert a dummy
            if (sCutOffDate == "") sCutOffDate = "2000-01-01T00:00:00";

            //replace the value
            sSqlQuery = string.Format(getSqlQuery("GetAllProducts"), sCutOffDate);

            return sSqlQuery;

        }

        /// <summary>
        /// Read SKU field name from the config and check if it's valid
        /// </summary>
        /// <param name="arDbFields"></param>
        /// <returns></returns>
        static string GetSkuFieldName(string[] arDbFields)
        {
            string sSkuName = ConfigurationManager.AppSettings.Get("DBSKU"); //name of the field containing product SKU. It must be unique.

            //check if the value is present in the list
            if (arDbFields.Contains(sSkuName)) return sSkuName;

            //The value is either missing or misconfigured
            throw new Exception(string.Format(Strings.msgWrongSKU, sSkuName));
       
        }

        /// <summary>
        /// Compress the CSV file into filename.csv.gz.
        /// </summary>
        /// <param name="sCsvFile"></param>
        static void CompressFile(string sCsvFile)
        {
             //Uplifted from MSDN.

            FileStream sourceFileStream = File.OpenRead(sCsvFile);
            FileStream destFileStream = File.Create(sCsvFile+".gz");

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
        }
    }
}
