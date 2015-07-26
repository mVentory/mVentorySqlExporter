using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Configuration;
using System.IO;
using System.IO.Compression;



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
            ///Get data from the DB. The query is quite elaborate and had to be placed in an external file.
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

            //Load the list of files exported before
            System.Collections.Specialized.StringCollection arSavedImageIDs = GetExportedImageIDs();

            ///Prepare the output CSV file
            var oCSV = new System.Collections.Specialized.NameValueCollection();
            StringBuilder CSV = new StringBuilder();
            string sRecord = ConfigurationManager.AppSettings.Get("TemplateAllProductsHeader"); //add the header row
            CSV.AppendLine(sRecord);

            ///Loop through the recordset and read the data
            while (kudosReader.Read())
            {
                oCSV.Add("style", kudosReader["code"].ToString());
                oCSV.Add("sku", kudosReader["id"].ToString());
                oCSV.Add("description", kudosReader["idescrfull"].ToString());
                oCSV.Add("short_description", kudosReader["idescrshort"].ToString());
                oCSV.Add("price", kudosReader["price"].ToString());
                oCSV.Add("color", kudosReader["color"].ToString());
                oCSV.Add("product_barcode_", kudosReader["lookupnum"].ToString());
                oCSV.Add("name", kudosReader["descr"].ToString());
                oCSV.Add("sizetm", kudosReader["sizetm"].ToString());

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
                
               ///Set in-stock depending on the value of qty
                Int64 iQty = ToIntSafe(kudosReader["quantity"].ToString());
                oCSV.Add("qty", iQty.ToString());
                if (iQty > 0) { oCSV.Add("is_in_stock", "1"); } else { oCSV.Add("is_in_stock", "0"); }

                /// Get image details for the product
                Int64 iStyleID = ToIntSafe(kudosReader["styleid"].ToString());
                Int64 iColorID = ToIntSafe(kudosReader["colorid"].ToString());
                string[] sFileNames = RetrieveImages(iStyleID, iColorID, connectionImages, arSavedImageIDs);

                //save file names as ;-separate list
                string sImageNames = "";
                if (sFileNames != null && sFileNames.Length > 0) 
                {
                    for (int i = 0; i < sFileNames.Length; i++)
                    {
                        sImageNames += sFileNames[i] + ';';
                    }
                    sImageNames = sImageNames.Trim(new char[] { ';' });
                    oCSV.Add("image", sImageNames); 
                }
                //The row for the item is ready to be saved. It contains full item details. Additional rows will contain only some of the details.
                sRecord = buildCSVRow(oCSV);
                CSV.AppendLine(sRecord);
                oCSV.Clear();

            }

            connectionKudos.Close(); //Don't need the DB any more.

            //zip up the CSV file
            CreateCsvArchive(sCsvFile, CSV);

            //Save IDs in a file to avoid exporting them again if they were deleted
            SaveExportedImageIDs(arSavedImageIDs);

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
        /// Converts any string to an int or returns 0 if the value is invalid.
        /// </summary>
        /// <param name="Val"></param>
        /// <returns></returns>
        static Int64 ToIntSafe(string Val)
        {
            try
            {
                return Convert.ToInt64(Val);
            }
            catch (Exception)
            {
                System.Diagnostics.EventLog.WriteEntry(Program.EventSourceName, "Cannot convert value to integer: " + Val);
                return 0;
            }

        }

        /// <summary>
        /// Converts any string to a decimal or returns 0 if the value is invalid.
        /// </summary>
        /// <param name="Val"></param>
        /// <returns></returns>
        static Decimal ToDecSafe(string Val)
        {
            try
            {
                return Convert.ToDecimal(Val);
            }
            catch (Exception)
            {
                System.Diagnostics.EventLog.WriteEntry(Program.EventSourceName, "Cannot convert value to Decimal: " + Val);
                return 0;
            }

        }


        /// <summary>
        /// Builds a single CSV row using the header as the template and putting the values in the right cells
        /// </summary>
        /// <param name="dbValues"></param>
        /// <returns></returns>
        static string buildCSVRow(System.Collections.Specialized.NameValueCollection dbValues, bool UseDefaultValues = true)
        {
            ///Prepare the header template
            string sHeader = ConfigurationManager.AppSettings.Get("TemplateAllProductsHeader");
            string[] arHeader = sHeader.Split(new char[] { ',' });

            ///Prepare the data row template with placeholders
            string sRow = ConfigurationManager.AppSettings.Get("TemplateAllProductsLine");
            string[] arRow = sRow.Split(new char[] { ',' });


            ///Loop through the columns and write the values
            string sOutputString = "";
            for (int i = 0; i < arHeader.Length; i++)
            {
                string sColumn = arHeader[i];
                string sValue = dbValues.Get(sColumn);
                if (UseDefaultValues && (sValue == "" || sValue == null) && arRow[i] != "") sValue = arRow[i]; ///use the default value from the row template if no value is provided
                if (sOutputString != "") sOutputString += ","; ///Add a ,-separator between values, but not for the very first value
                if (sValue != null) sValue = sValue.Replace("\"", "\"\""); ///Replace all single " with "" for CSV " escaping
                sValue = "\"" + sValue + "\""; ///Wrap the value in "" even if it's empty
                sOutputString += sValue; /// Append to output
            }

            return sOutputString;

        }

        /// <summary>
        /// Returns a list of file names saved to the FTP folder for the given style and color ids
        /// </summary>
        /// <param name="iStyleID"></param>
        /// <param name="iColorID"></param>
        /// <returns></returns>
        static string[] RetrieveImages(Int64 iStyleID, Int64 iColorID, SqlConnection connectionImages, System.Collections.Specialized.StringCollection arSavedImageIDs)
        {
            if (iStyleID == 0) return null; //No point doing anything if the style ID doesn;t exist

            var oListOfFiles = new System.Collections.Specialized.StringCollection();

            //Prepare SQL query
            SqlCommand commandGetProductImages = new SqlCommand("select * from [Stock Image] i, [Stock Image Map] m where i.ID=m.[Stock Image ID] and m.[Stock Style ID]=@sid and m.[Stock Colour ID]=@cid", connectionImages);
            commandGetProductImages.Parameters.Add(new SqlParameter("sid", iStyleID));
            commandGetProductImages.Parameters.Add(new SqlParameter("cid", iColorID));
            commandGetProductImages.CommandTimeout = 0;
            SqlDataReader kudosReader = commandGetProductImages.ExecuteReader();

            //Read the images from the stream and save as files with the image ID as the file name
            while (kudosReader.Read())
            {
                //prepare file name and path
                string sFileID = kudosReader["id"].ToString();
                string sFileName = sFileID + ".jpg";
                string sFilePath = ConfigurationManager.AppSettings.Get("FtpFolderPathImg").TrimEnd(new char[] { '\\' }) + "\\" + sFileName;
                //Write binary data into the file if it doesn't exist
                if (!System.IO.File.Exists(sFilePath) && !arSavedImageIDs.Contains(sFileID))
                {
                    byte[] oFileData = (byte[])kudosReader.GetValue(kudosReader.GetOrdinal("Image"));
                    if (oFileData != null && oFileData.Length > 0)
                    {
                        System.IO.File.WriteAllBytes(sFilePath, oFileData); //only save the file if there is data to save
                        arSavedImageIDs.Add(sFileID); //save the file ID, so that we can remove the file after the tranfer
                    }
                    else
                    {
                        //reset the name for an empty file and log the problem
                        System.Diagnostics.EventLog.WriteEntry(Program.EventSourceName, "Encountered an empty file for: " + sFileName);
                        sFileName = "";
                    }
                }
                else if (!arSavedImageIDs.Contains(sFileID)) // the file may have been exported, but is not in the list - just add it there
                {
                    arSavedImageIDs.Add(sFileID);
                }


                //add the file name to the list of files to be returned back to the caller. They come in the sequence order with the main image first.
                if (sFileName != "") oListOfFiles.Add(ConfigurationManager.AppSettings.Get("FilePrefix") + sFileName);
            }

            kudosReader.Close();

            //Push the collection into an array
            string[] sOut = new string[oListOfFiles.Count];
            oListOfFiles.CopyTo(sOut, 0);
            return sOut;

        }

        /// <summary>
        /// Image IDs are stored as a list in a file to avoid exporting them more than once
        /// </summary>
        /// <returns></returns>
        static System.Collections.Specialized.StringCollection GetExportedImageIDs ()
        {
            string sFilePath = ConfigurationManager.AppSettings.Get("ImgIdx");
            if (!File.Exists(sFilePath))
            {
                   File.Create(sFilePath).Close(); //create a new one if it doesn't exist
            }
            //load the file
            StreamReader oFile = File.OpenText(sFilePath);
            System.Collections.Specialized.StringCollection arSavedImageIDs = new System.Collections.Specialized.StringCollection();

            //read the lines one at a time
            while (!oFile.EndOfStream)
            {
                string sLine = oFile.ReadLine(); // add only good lines
                if (sLine!=null && sLine!="" && !arSavedImageIDs.Contains(sLine)) arSavedImageIDs.Add(sLine);
            }

            oFile.Close();
            return arSavedImageIDs;
        }

        /// <summary>
        ///Image IDs are stored as a list in a file to avoid exporting them more than once
        /// </summary>
        /// <param name="arSavedImageIDs"></param>
        static void SaveExportedImageIDs(System.Collections.Specialized.StringCollection arSavedImageIDs)
        {
            string sFilePath = ConfigurationManager.AppSettings.Get("ImgIdx");

            //load the file
            StreamWriter oFile = File.CreateText(sFilePath);

            //write the lines one at a time
            foreach (string sLine in arSavedImageIDs) oFile.WriteLine(sLine);

            //save the data
            oFile.Flush();
            oFile.Close();
        }

        /// <summary>
        /// Put the CSV data into a .zip file
        /// </summary>
        /// <param name="sCsvFile"></param>
        /// <param name="CSV"></param>
        static void CreateCsvArchive(string sCsvFile, StringBuilder CSV)
        {
            using (var zipStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
                {
                    var sZippedCsvFile = archive.CreateEntry(Path.GetFileName(sCsvFile));

                    using (var entryStream = sZippedCsvFile.Open())
                    using (var streamWriter = new StreamWriter(entryStream))
                    {
                        streamWriter.Write(CSV.ToString());
                    }
                }

                using (var fileStream = new FileStream(sCsvFile + ".zip", FileMode.Create))
                {
                    zipStream.Seek(0, SeekOrigin.Begin);
                    zipStream.CopyTo(fileStream);
                    fileStream.Close();
                }
            }

        }

    }
}
