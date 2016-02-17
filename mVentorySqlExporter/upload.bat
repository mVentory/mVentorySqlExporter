del C:\mVentory\KudosExport\remote\csv\*.* /Q

cd C:\mVentory\
mVentorySqlExporter.exe

winscp.com /script=sync-script.txt /log=ftp-sync-log.txt