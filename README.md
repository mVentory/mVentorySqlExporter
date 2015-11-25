# mVentory Exporters

This is a collection of Windows applications for extracting product data from various customer databases.

## Remote file sync

File paths for the exported data can be configured in `app.config` file or `*app-name*.exe.config`. The defaults are set to:

* `C:\mvExport` - for `data-index.txt` and `image-index.txt`, but they can be confgured separately and reside anywhere else
* `C:\mvExport\remote\csv` - output directory for CSV files
* `C:\mvExport\remote\images` - output directory for images

`csv` and and `images` folders have to be separate because they need to be uploaded sequentially - images first, then the csv file. If the csv files comes first you may run into a problem of images being missing because they havn't been uploaded yet.

### winscp config

We use [winscp](https://winscp.net/eng/download.php) for uploading files to the web server.

##### 1. Create a .bat file to call winscp:

```
// delete all previous CSV and image files before exporting a new lot
del c:\mvExport\remote\csv\*.csv 
del c:\mvExport\remote\images\*.jpg
// run the export
c:\mvExport\mVentorySqlExporter.exe
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
synchronize remote "C:\mvExport\remote\images" "/mvexport/remote/images" -criteria=size
synchronize remote "C:\mvExport\remote\csv" "/mvexport/remote/csv" -criteria=size
close
exit 
```

##### 3. Configure a schedulled task to run a the export .bat file as often as you need. 

Make sure they do not run over each other - one must stop before a new one begins. The export task will be differential and should take seconds to run.

##### 4. Configure a schedulled tasks to delete `data-index.txt` once a day just before the last upload.

It will force the full data export and clear any possible inconsistencies accummulated during the day. It is a crude, but reliable way of keeping the website in sync with Kudos.

https://winscp.net/eng/docs/task_synchronize_full has more info on full directory sync and its parameters.
