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

## Remote file sync

File paths can be configured in `app.config` file or `mVentory Kudos Exporter.exe.config`. The defaults are set to:

* `C:\KudosExport` - for `data-index.txt` and `image-index.txt`, but they can be confgured separately and reside anywhere else
* `C:\KudosExport\remote\csv` - output directory for CSV files
* `C:\KudosExport\remote\images` - output directory for images

`csv` and and `images` folders have to be separate because they need to be uploaded sequentially - images first, then the csv file. If the csv files comes first you may run into a problem of images being missing because they havn't been uploaded yet.

### winscp config

We use [winscp](https://winscp.net/eng/download.php). 

##### 1. Create a .bat file to call winscp:

```
// delete all previous CSV and image files before exporting a new lot
del c:\kudosexport\remote\csv\*.csv 
del c:\kudosexport\remote\images\*.jpg
// run the export
c:\kudosexport\mventorykudosexporter.exe
// sync the exported files with the remote server (website)
cd C:\winscp\
winscp.com /script=sync-script.txt /log=ftp-sync-log.txt
```

##### 2. Create `sync-script.txt` file with instructions for winscp:

```
option batch continue
option confirm off
open ftp://login:pwd@my.website.com/
option transfer binary
synchronize remote "C:\kudosexport\remote\images" "/kudos/remote/images" -criteria=size
synchronize remote "C:\kudosexport\remote\csv" "/kudos/remote/csv" -criteria=size
close
exit 
```

##### 3. Configure a schedulled task to run a the export .bat file as often as you need. 

Make sure they do not run over each other - one must stop before a new one begins. The export task will be differential and should take seconds to run.

##### 4. Configure a schedulled tasks to delete `data-index.txt` once a day just before the last upload.

It will force the full data export and clear any possible inconsistencies accummulated during the day. It is a crude, but reliable way of keeping the website in sync with Kudos.

https://winscp.net/eng/docs/task_synchronize_full has more info on full directory sync and its parameters.
