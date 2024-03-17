Deletes all files recursively that are older than 'x' days on a given ftp remote.
It also removes empty directories.

Run via docker-compose:

``` version: "3.5"
services:
  ftpCleaner:
    image: 'ghcr.io/traffiqq/ftpcleaner:latest'
    restart: unless-stopped
    
    environment:
      FTP_HOST: "host.com"
      FTP_USER: "username"
      FTP_PASSWORD: "password"
      FTP_DIRECTORY: "/pathOnRemote"
      DRY_RUN: "Y" # set to "N" once tested that it deletes the correct stuff
      DeleteOlderThanXDays: "14"
      CycleTimeInHours: "24" # how many hours to wait between checks
      FTP_PORT: "21"
```
