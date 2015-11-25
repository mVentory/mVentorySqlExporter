# mVentory-Kudos-Exporter

This application connects directly to Kudos Counter Intelligence database to extract product data, including internet information and product photos.

## Installation

* Copy contents of `bin` folder to the destination. .Net 4.0 or later is required.

* Run `mvKudosEventLogSource.reg` with admin rights to add a logging event source

* Check App.config for default settings:
** connection string (change DB name, add login/pwd if need to)
** default output file names and locations
* Run `mVentory Kudos Exporter.exe` as a one-off
* Configure a cron job to run `mVentory Kudos Exporter.exe` on a schedule

## Data caching

The app creates 2 plain text files for indexing extracted data: 
* `data-index.txt` with hashes for every record from a previously submitted CSV file
* `image-index.txt` with id's of images extracted earlier

Both files are created automatically at runtime. If a file is deleted all the data will be extracted.

If an image exists in the destination folder (where the exporter puts them, not some other remote location they are uploaded later) it will not be extracted again.

Extracted images can be deleted from the local folder specified in `FtpFolderPathImg` if their IDs are retained in `ImgIdx` file. See `App.config` for details.

#### Hash format

`data-index.txt` has 19-digit integers produced by padding the SKU with zero's on the right. It produces a long integer with the SKU on the left and the record hash on the right. E.g.

```
4867050000086000534
4867070001991624918
```

## Performance

The very first run will extract everything, including images. It may tie up the server for several minutes. Subsequent runs take about 1 minute per 100,000 rows.