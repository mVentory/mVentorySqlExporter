using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NameMatcher
{
    class Program
    {
        // Constants - should be moved to a config file at some stage
        static string sProdList = "all-simple-prods.csv"; // CSV file with a list of products
        static string sFileList = "files.txt"; // a file with file names
        static string sProdFiles = "prod-with-files.csv"; // output file with a new column for matching file names
        static string sFilesReport = "files-report.csv"; // the full list of files with an additional column 

        // Image file locations
        static string sFilesSource = @"C:\Users\admin\Documents\mventory\customer data\harfords\harfords-prod-images"; // a folder with all the files
        static string sFilesMatched = "matched\\"; // the folder with matched images, relative

        // Arrays and Collections
        static System.Collections.Specialized.StringCollection arCsvLines = new System.Collections.Specialized.StringCollection();
        static System.Collections.Specialized.StringCollection arFileNames = new System.Collections.Specialized.StringCollection();
        static System.Collections.Specialized.StringCollection arFilesReport = new System.Collections.Specialized.StringCollection();



        static void Main(string[] args)
        {
            //Get paths to the files
            var sPath =System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)+"\\";
            sProdList = sPath + sProdList;
            sFileList = sPath + sFileList;
            sProdFiles = sPath + sProdFiles;
            sFilesReport = sPath + sFilesReport;
            sFilesMatched = sPath + sFilesMatched;

            //Check destination folder for images
            if (!System.IO.Directory.Exists(sFilesMatched)) System.IO.Directory.CreateDirectory(sFilesMatched);

            //load the CSV file
            foreach (string sLine in System.IO.File.ReadLines(sProdList))
            {

                if (sLine == null) continue; // ignore empty lines

                arCsvLines.Add(sLine + ",\""); //build the list with a separator for an additional col where the file names will go
            };

            //read the list of file names and try to find matches
            foreach (string sLine in System.IO.File.ReadLines(sFileList))
            {

                if (sLine == null) continue; // ignore empty lines


                string sku = System.IO.Path.GetFileNameWithoutExtension(sLine).Split('_')[0].ToLower(); // get product code part from the first column
                string sku6 = (sku.Length > 6) ? sku.Substring(0, 6) : sku; // some codes have suffixes not found in the CSV
                bool ismatch = false;

                // match line by line
                for (int i=0; i< arCsvLines.Count; i++)
                {
                    string sCsvLine = arCsvLines[i];
                    if (sCsvLine.ToLower().Contains(sku) || sCsvLine.ToLower().Contains(sku6))
                    {
                        //there is a match - add the file name to the CSV
                        arCsvLines[i] += sLine + "\n";
                        ismatch = true;
                    }
                }

                //copy the files to the appropriate folder
                foreach (string sSourceFileName in System.IO.Directory.GetFiles(sFilesSource, sLine, System.IO.SearchOption.AllDirectories))
                {
                    if (ismatch)
                    {
                        // move the file to matched folder
                        string fileTarget = sFilesMatched + sLine;
                        if (!System.IO.File.Exists(fileTarget))
                        {
                            System.IO.File.Move(sSourceFileName, fileTarget);
                        }
                        else 
                        { 
                            System.IO.File.Delete(sSourceFileName); // just in case the same file exists in more than one copy
                        }
                    }
                }
                
                //add the file name to the report 
                arFilesReport.Add(sLine + "," + ((ismatch) ? "1" : "0"));
            };

            // Close the open lines and remove trailing CR
            for (int i = 0; i < arCsvLines.Count; i++)
            {
                arCsvLines[i] = arCsvLines[i].TrimEnd('\n')+"\"";
            }



            // save the CSV with the file names
            System.IO.File.WriteAllLines(sProdFiles, arCsvLines.Cast<string>());

            // save the CSV with the file report - which files were used and which were not
            System.IO.File.WriteAllLines(sFilesReport, arFilesReport.Cast<string>());

        }
    }
}
