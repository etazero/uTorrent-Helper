# uTorrent-Helper
*Compile with VS2010 or later*

A small windows utility originally, SmartFileMover, however now uTorrent Helper. In conjunction with Plex the utility is used to automatically move files from the "downloaded" folder (that is setup in uTorrent) to the respective folders that are libraries on your Plex Server, automatically determining if it is a movie, TV show, video, etc.

For it to work properly you must set the directories and file types in the .config file (you can also set the defaults in the .cs file) The app is hard coded to work from "C:\smartfilemover\" however this can be changed in the .cs file.

In order to determine TV shows you must have an empty folder of a TV show in the specified folder for the app to catalog it. The rest is automatic based on file sizes and other conditions.

This app works best when a scheduled task is created to run every 2 to 3 minutes.

This app also has the ability to email when files are moved, please update "sendEmail" method in the .cs file with your credentials to enable this feature. This will be a .config file field in the future.

An official installer will be coming soon
