# mVentorySqlExporter

This application connects to an MS SQL database to extract product data.

## Installation

* Copy contents of `bin` folder to the destination. .Net 4.0 or later is required.

* Run `mvSqlExporterEventLogSource.reg` with admin rights to add a logging event source

* Check App.config for default settings:
** connection string (change DB name, add login/pwd if need to)
** default output file names and locations
* Run `mVentorySqlExporter.exe` as a one-off
* Configure a cron job to run `mVentorySqlExporter.exe` on a schedule

## Data caching

The app creates a plain text files for indexing extracted data: 
* `data-index.txt` with hashes for every record from a previously submitted CSV file

The file is created automatically at runtime. If a file is deleted all the data will be extracted from the DB.

#### Hash format

`data-index.txt` has 18-digit integers produced by padding the SKU with zero's on the right. It produces a long integer with the SKU on the left and the record hash on the right. E.g.

```
486705000086000534
486707001991624918
```

## Performance

The very first run will extract everything, including images. It may tie up the server for several minutes. Subsequent runs take about 1 minute per 100,000 rows.