# mVentoryGenericExporter

This application connects to an MS SQL database to extract any data as CSV.

## Installation

* Copy contents of `bin` folder to the destination. .Net 4.0 or later is required.

* Check App.config for default settings:
** connection string (change DB name, add login/pwd if need to)
* Place SQL scripts in `/sql` folder
* Run `mVentoryGenericExporter.exe` as a one-off and collect the files from `/csv` folder
* Run `upload.bat` to clean up files, run the exporter and upload them to the site in one go
** Configure login/pwd/url for the FTP access from the step above

The exporter goes through the list of SQL files and creates an CSV file for each of them with the data from the DB.