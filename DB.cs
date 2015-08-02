using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Configuration;
using System.IO;



namespace mvKudos
{
    class DB
    {
        /// <summary>
        /// Exports all products from a Counter Intelligence DB into a CSV file for further upload.
        /// </summary>
        /// <returns></returns>
        public static void exportAllProducts(string sCsvFile)
        {
            Console.WriteLine("Extracting product data from the DB. " + DateTime.Now.ToString("s"));

            //Get data from the DB. The query is quite elaborate and had to be placed in an external file.
            SqlConnection connectionKudos = new SqlConnection(ConfigurationManager.ConnectionStrings["Kudos"].ConnectionString);
            connectionKudos.Open();
            SqlCommand commandGetAllProducts = new SqlCommand(getSqlQuery("GetAllProducts"), connectionKudos);
            commandGetAllProducts.CommandTimeout = 0;
            SqlDataReader kudosReader = commandGetAllProducts.ExecuteReader();

            //prepare a reusable connection for retrieving images
            SqlConnection connectionImages = new SqlConnection(ConfigurationManager.ConnectionStrings["Kudos"].ConnectionString);
            connectionImages.Open();

            //pepare some constants
            string sUrlPathFormat = ConfigurationManager.AppSettings.Get("UrlPath");

            //load IDs of data processed before. The IDs are stored in files as a list in plain text
            List<long> arSavedImageIDs = GetExportedIDs("ImgIdx"); //Load the list of image files exported before
            Console.WriteLine("Loaded ImgIdx. " + DateTime.Now.ToString("s"));

            List<long> arSavedRowIDs = GetExportedIDs("DataIdx"); //Load the list of rows exported before
            Console.WriteLine("Loaded DataIdx. " + DateTime.Now.ToString("s"));

            var arExtractedRowIDs = new List<long>(); //This collection will contain IDs extracted this time to keep the list current
            //set initial capacity
            arExtractedRowIDs.Capacity = arSavedRowIDs.Count;

            ///Prepare the output CSV file
            var oCSV = new System.Collections.Specialized.NameValueCollection();
            StringBuilder CSV = new StringBuilder();
            string sRecord = ConfigurationManager.AppSettings.Get("TemplateAllProductsHeader"); //add the header row
            CSV.AppendLine(sRecord);

            //Loop through the recordset and read the data
            Console.WriteLine("Going through extracted data. " + DateTime.Now.ToString("s"));

            while (kudosReader.Read())
            {
                oCSV.Add("style", kudosReader["code"].ToString());
                string sSKU = kudosReader["id"].ToString();
                oCSV.Add("sku", sSKU);
                oCSV.Add("description", kudosReader["idescrfull"].ToString());
                oCSV.Add("short_description", kudosReader["idescrshort"].ToString());
                oCSV.Add("price", kudosReader["price"].ToString());
                oCSV.Add("color", kudosReader["color"].ToString());
                oCSV.Add("product_barcode_", kudosReader["lookupnum"].ToString());
                oCSV.Add("name", kudosReader["descr"].ToString());
                oCSV.Add("sizetm", kudosReader["sizetm"].ToString());
                oCSV.Add("brand", kudosReader["brand"].ToString());
                oCSV.Add("qty", kudosReader["quantity"].ToString());

                //Categories are separated by ; and may have a leading/trailing ;
                string sValue = kudosReader["category"].ToString();
                if (sValue != null) sValue = sValue.Trim(new char[] { ';' });
                oCSV.Add("_category", sValue);

                //branch stock data is separated by ; and may have a leading/trailing ;
                sValue = kudosReader["branches"].ToString();
                if (sValue != null) sValue = sValue.Trim(new char[] { ';' });
                oCSV.Add("branches", sValue);

                //Sizes get a weird . at the beginning in some products. We don't know what it means, if anything.
                sValue = kudosReader["size"].ToString();
                if (sValue != null) sValue = sValue.Trim(new char[] { '.' });
                oCSV.Add("size", sValue);

                // Get image details for the product
                string sImgIDs = kudosReader["imgids"].ToString();
                if (sImgIDs != null) sImgIDs = sImgIDs.Trim(new char[] { ';' });
                oCSV.Add("image", sImgIDs);

                //The row for the item is ready to be saved. It contains full item details. Additional rows will contain only some of the details.
                sRecord = buildCSVRow(oCSV);

                //use a hash of the row to check if the data changed
                long fHash = CalculateHash(sRecord, sSKU);
                if (!arSavedRowIDs.Contains(fHash))
                {
                    //get any missing images
                    RetrieveImages(connectionImages, arSavedImageIDs, sImgIDs);
                    //add the record to the output
                    CSV.AppendLine(sRecord);
                }
                //save the id in a new array to update the list
                if (!arExtractedRowIDs.Contains(fHash))
                {
                    arExtractedRowIDs.Add(fHash);
                }
                else
                {
                    System.Diagnostics.EventLog.WriteEntry(Program.EventSourceName, "Duplicate hash " + fHash.ToString() + " for: " + sRecord, System.Diagnostics.EventLogEntryType.Warning);
                }

                oCSV.Clear();

            }

            connectionKudos.Close(); //Don't need the DB any more.

            //report progress
            Console.WriteLine("Saving data. " + DateTime.Now.ToString("s"));

            //Save the data in a temp file with a unique name
            System.IO.File.WriteAllText(sCsvFile, CSV.ToString());

            //Save Image IDs in a file to avoid exporting them again if they were deleted
            SaveExportedIDs(arSavedImageIDs, "ImgIdx");
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
            string sHeader = ConfigurationManager.AppSettings.Get("TemplateAllProductsHeader");
            string[] arHeader = sHeader.Split(new char[] { ',' });

            //Prepare the data row template with placeholders
            string sRow = ConfigurationManager.AppSettings.Get("TemplateAllProductsLine");
            string[] arRow = sRow.Split(new char[] { ',' });


            //Loop through the columns and write the values
            string sOutputString = "";
            for (int i = 0; i < arHeader.Length; i++)
            {
                string sColumn = arHeader[i];
                string sValue = dbValues.Get(sColumn);
                if (UseDefaultValues && (sValue == "" || sValue == null) && arRow[i] != "") sValue = arRow[i]; ///use the default value from the row template if no value is provided
                if (sOutputString != "") sOutputString += ","; //Add a ,-separator between values, but not for the very first value
                if (sValue != null) sValue = sValue.Replace("\"", "\"\""); //Replace all single " with "" for CSV " escaping
                sValue = "\"" + sValue + "\""; ///Wrap the value in "" even if it's empty
                sOutputString += sValue; // Append to output
            }

            return sOutputString;

        }

        /// <summary>
        /// Returns a list of file names saved to the FTP folder for the given style and color ids
        /// </summary>
        /// <param name="iStyleID"></param>
        /// <param name="iColorID"></param>
        /// <returns></returns>
        static void RetrieveImages(SqlConnection connectionImages, List<long> arSavedImageIDs, string sImgIDs)
        {
            if (sImgIDs==null || sImgIDs=="") return; //No point doing anything if there is not enough data

            //get list of images as an array to check if we already have those images, one by one
            string[] arImageNames = sImgIDs.ToLower().Split(new char[] {';'});
            foreach (string sImageName in arImageNames)
            {
                //get the file id
                long fFileID = 0;
                long.TryParse(sImageName.Replace(".jpg",""), out fFileID); //all images are .jpg or so we assume
                if (fFileID <= 0) //sanity check
                {
                    System.Diagnostics.EventLog.WriteEntry(Program.EventSourceName, "Wrong image file ID: " + sImageName, System.Diagnostics.EventLogEntryType.Warning);
                    continue;
                }


                string sFilePath = ConfigurationManager.AppSettings.Get("FtpFolderPathImg").TrimEnd(new char[] { '\\' }) + "\\" + sImageName;

                //check if the image has already been extracted (either the file exists on the disk or it was indexed)
                if (!arSavedImageIDs.Contains(fFileID) && !System.IO.File.Exists(sFilePath))
                {
                    //Prepare SQL query
                    SqlCommand commandGetProductImages = new SqlCommand("select * from [Stock Image] where ID=@id", connectionImages);
                    commandGetProductImages.Parameters.Add(new SqlParameter("id", ((int)fFileID).ToString()));
                    commandGetProductImages.CommandTimeout = 0;
                    SqlDataReader kudosReader = commandGetProductImages.ExecuteReader();

                    //Read the images from the stream and save as files with the image ID as the file name
                    while (kudosReader.Read())
                    {
                        //Write binary data into the file if it doesn't exist
                            byte[] oFileData = (byte[])kudosReader.GetValue(kudosReader.GetOrdinal("Image"));
                            if (oFileData != null && oFileData.Length > 0)
                            {
                                System.IO.File.WriteAllBytes(sFilePath, oFileData); //only save the file if there is data to save
                            }
                            else
                            {
                                //log the problem
                                System.Diagnostics.EventLog.WriteEntry(Program.EventSourceName, "Encountered an empty file for: " + sImageName, System.Diagnostics.EventLogEntryType.Warning);
                            }
                    }
                    kudosReader.Close();
                }

                //add the file name the index for future reference
                if (!arSavedImageIDs.Contains(fFileID))
                {
                    if (arSavedImageIDs.Count == arSavedImageIDs.Capacity) arSavedImageIDs.Capacity += 100; //bump the capacity to ease memalloc
                    arSavedImageIDs.Add(fFileID);
                }
            }
        }

        /// <summary>
        /// Image and row IDs are stored as a list in a file to avoid exporting them more than once
        /// </summary>
        /// <returns></returns>
        static List<long> GetExportedIDs(string FileNameKey)
        {
            string sFilePath = ConfigurationManager.AppSettings.Get(FileNameKey);
            if (!File.Exists(sFilePath))
            {
                File.Create(sFilePath).Close(); //create a new one if it doesn't exist
            }

            //load the file
            StreamReader oFile = File.OpenText(sFilePath);
            List<long> arValues = new List<long>();

            //read the lines one at a time
            while (!oFile.EndOfStream)
            {
                string sLine = oFile.ReadLine(); // add only good lines
                //convert to float for performance
                long fValue = 0;
                long.TryParse(sLine, out fValue);

                //add the value to the array if it's not yet there
                if (sLine != null && sLine != "" && !arValues.Contains(fValue)) arValues.Add(fValue);

                //bump the capacity if it's at the limit
                if (arValues.Count == arValues.Capacity) arValues.Capacity += 1000;

            }

            oFile.Close();

            //sort the array
            arValues.TrimExcess();
            arValues.Sort();

            return arValues;
        }



        /// <summary>
        ///Image IDs are stored as a list in a file to avoid exporting them more than once
        /// </summary>
        /// <param name="arSavedIDs"></param>
        static void SaveExportedIDs(List<long> arSavedIDs, string FileNameKey)
        {
            string sFilePath = ConfigurationManager.AppSettings.Get(FileNameKey);
            arSavedIDs.TrimExcess(); //not sure it makes any difference

            //load the file
            StreamWriter oFile = File.CreateText(sFilePath);

            //write the lines one at a time
            foreach (long sLine in arSavedIDs) oFile.WriteLine(sLine);

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
            sSKU = sSKU.PadRight(19, '0');
            long.TryParse(sSKU, out iSKU);
            iHash = iSKU + iHash;
            return iHash;
        }

    }
}
