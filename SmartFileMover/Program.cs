using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections;
using System.Diagnostics;
using System.Net.Mail;
using System.Xml;

namespace SmartFileMover
{
    //This program watches the downloaded folder and automatically movies files based on particular credentials
    class Program
    {
        static string _monitoredDir = "E:\\Torrents\\Downloaded\\";
        static string _tvFolder = "E:\\TV\\";
        static string _movieFolder = "E:\\Movies\\";
        static string _musicFolder = "E:\\GMusic\\";
        static string _videoFolder = "E:\\Videos\\web_gems\\";
        static string _videoTypes = ".mkv,.avi,.mp4,.mpeg,.m4v";
        static string _subTitlesTypes = ".srt,.sub";
        static string _musicTypes = ".mp3,.flac,.m4a,.ogg,.m4p,.wma";
        static string _logDir = "E:\\GDrive\\PCs\\MS1\\";
        static string _log = string.Empty;
        static string _installDir = "C:\\smartFileMover\\";
        static ArrayList movedFiles = new ArrayList();
        static ArrayList failedFiles = new ArrayList();

        static void Main(string[] args)
        {  
            //start timmer
            Stopwatch sWatch = new Stopwatch();
            sWatch.Start();

            //start log
            logCheck();
            _log += "Smart File Mover Started: v.1.1 build:12-20-2016" + Environment.NewLine;
            DateTime startTime = DateTime.Now;
            _log += startTime.ToString() + Environment.NewLine;

            //load settings from config file
            readFromConfigFile();

            #region do process
            try
            {                
                //get a list of all files in downloaded dir
                ArrayList monitoredFiles = new ArrayList(filesInMonitoredDir());

                //get a list of folders in tv folder
                ArrayList tvFoldersFound = new ArrayList(tvSubFolders());

                //filter and move for tv
                ArrayList possibleMovieFiles = new ArrayList(filterFilesForTV(monitoredFiles, tvFoldersFound));

                //filter and move for movies
                filterAndMoveToMovies(possibleMovieFiles);

                //move root only music to music favorites
                moveMusicToGmusic();                
            }
            catch (Exception ex)
            {
                _log += "A serious error has occurred: " + ex.Message + Environment.NewLine;
            }
            #endregion

            //email status
            sendEmail("All", movedFiles, failedFiles);

            //finish log
            sWatch.Stop();
            TimeSpan elapsed = sWatch.Elapsed;
            _log += "All Done! (" + elapsed.Seconds.ToString() + " seconds)" + Environment.NewLine
                + "--------------------------------------------------"
                + Environment.NewLine;
            
            //clean log per conditions
            logCheck();
            if (!string.IsNullOrEmpty(_log))
            {
                //write log
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(_logDir + "SmartFileMover_Log.txt", true))
                {
                    file.WriteLine(_log);
                }
            }
        }

        static void readFromConfigFile()
        {
            //read from xml.
            XmlDocument settings = new XmlDocument();
            try
            {
                settings.Load(_installDir + "smartfilemover.config");
                try
                {
                    //main settings
                    XmlNode XMLmonitoredDir = settings.DocumentElement["monitoredDir"];
                    XmlNode XMLtvfolder = settings.DocumentElement["tvfolder"];
                    XmlNode XMLmoviefolder = settings.DocumentElement["moviefolder"];
                    XmlNode XMLvideoTypes = settings.DocumentElement["videoTypes"];
                    XmlNode XMLsubTitlesTypes = settings.DocumentElement["subTitlesTypes"];
                    XmlNode XMLlogDir = settings.DocumentElement["logDir"];
                    //music settings
                    XmlNode XMLmusicTypes = settings.DocumentElement["musicTypes"];
                    XmlNode XMLmusicFolder = settings.DocumentElement["musicFolder"];

                    //monitored folder
                    _monitoredDir = XMLmonitoredDir.Attributes["path"].Value.ToString();
                    //tv folder
                    _tvFolder = XMLtvfolder.Attributes["path"].Value.ToString();
                    //movie folder
                    _movieFolder = XMLmoviefolder.Attributes["path"].Value.ToString();
                    //video types
                    _videoTypes = XMLvideoTypes.Attributes["types"].Value.ToString();
                    //subtitles types
                    _subTitlesTypes = XMLsubTitlesTypes.Attributes["types"].Value.ToString();
                    //log folder
                    _logDir = XMLlogDir.Attributes["path"].Value.ToString();
                    //music types
                    _musicTypes = XMLmusicTypes.Attributes["types"].Value.ToString();
                    //music folder
                    _musicFolder = XMLmusicFolder.Attributes["path"].Value.ToString();

                    _log += "Config File Loaded" + Environment.NewLine;
                }
                catch (Exception ex)
                {
                    _log += "Config File load failed:"
                        + Environment.NewLine + ex.Message
                        + Environment.NewLine + "Using Default Configuration"
                        + Environment.NewLine;
                }
            }
            catch(Exception ex)
            {
                _log += "No Config File, Using Default Configuration..." + Environment.NewLine;
                _log += ex.Message + Environment.NewLine;
            }
        }

        static ArrayList filesInMonitoredDir()
        {
            ArrayList allfiles = new ArrayList();
            bool filesfound = false;
            foreach (string fileName in Directory.GetFiles(_monitoredDir, "*.*", SearchOption.AllDirectories))
            {
                try
                {
                    FileInfo fi = new FileInfo(fileName);

                    if (!(fileName == _monitoredDir + "Thumbs.db") &&
                        _videoTypes.Contains(fi.Extension.ToLower()) &&
                        fi.Length > 40000000)
                    {
                        //40000000 = 40 MB
                        filesfound = true;
                        allfiles.Add(fileName);
                        _log += "Found: " + fileName + Environment.NewLine;
                    }
                }
                catch (Exception ex)
                {
                    //log it
                    _log += "Error: " + fileName + Environment.NewLine + ex.Message;
                    //just go to the next one
                    continue;
                }

            }

            if (!filesfound)
            {
                _log += "No Video Files Found :(" + Environment.NewLine;
            }

            return allfiles;
        }

        static ArrayList tvSubFolders()
        {
            ArrayList subfolders = new ArrayList();

            if (_log.Contains("No Video Files Found :("))
            {
                return subfolders;
            }

            _log += "--------------------------------------------------" + Environment.NewLine;
            _log += "List of TV folders: ";            
            DirectoryInfo di = new DirectoryInfo(_tvFolder);
            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                subfolders.Add(dir.Name);
                _log += dir + ",";
                
                //special conditions
                if (dir.Name.ToLower().Contains(" and "))
                {
                    //if dir name also contains "and" put a specal string to symbol an "and"
                    subfolders.Add("+and," + dir.Name);
                }
                if (dir.Name.ToLower().Contains("'"))
                {
                    subfolders.Add("*aps," + dir.Name);
                }
                if (dir.Name.ToLower().Contains("the"))
                {
                    subfolders.Add("*article," + dir.Name);
                }
                if (dir.Name.ToLower().Contains("."))
                {
                    subfolders.Add("*dot," + dir.Name);
                }
                if (dir.Name.ToLower().Contains("&"))
                {
                    subfolders.Add("*apers," + dir.Name);
                }
                if (dir.Name.ToLower().Contains(":"))
                {
                    subfolders.Add("*colon," + dir.Name);
                }
                if (dir.Name.ToLower().Contains("-"))
                {
                    subfolders.Add("*dash," + dir.Name);
                }
            }
            _log += Environment.NewLine;
            _log += "--------------------------------------------------" + Environment.NewLine;
           
            IComparer lenComp = new StringLenthCompare();

            subfolders.Sort(lenComp);
            
            return subfolders;
        }

        static ArrayList filterFilesForTV(ArrayList filesFound, ArrayList tvFolders)
        {
            bool fileMoved = false; //a control for later
            bool isError = false; //a control for later
            bool isTVfile = false; //a control for later
            string subTitleDest = string.Empty;
            string subTitleDir = string.Empty;
            string cleanedFileName = string.Empty;
            string cleanedFileName1 = string.Empty;
            string smartTVdir = string.Empty;
            string origTVdir = string.Empty;

            ArrayList possibleMovieFiles = new ArrayList();

            foreach (string fullFileName in filesFound)
            {
                FileInfo fi = new FileInfo(fullFileName);
                if(_videoTypes.Contains(fi.Extension.ToLower()))
                {                    
                    //file is a video
                    //see if file name is in tv folders list
                    foreach(string tvDir in tvFolders)
                    {
                        fileMoved = false;
                        isError = false;
                        //remove ugly space subsitutes
                        cleanedFileName1 = fi.Name.Replace(".", " ").Replace("-", " ").Replace("_", " ").Replace("!","");
                        cleanedFileName = cleanedFileName1.Replace("  ", " "); //double spaces                        
                        //use smartTVdir to match if containing special words/char....
                        specChrCheck(tvDir, out smartTVdir, out origTVdir);                        
                        //finally check against cleaned file name...                        
                        if (cleanedFileName.ToLower().Contains(smartTVdir))
                        {
                            #region does match on a tv folder
                            isTVfile = true;
                            subTitleDir = fi.DirectoryName;
                            #region file belongs to a tv folder directory                            
                            _log += "Match for TV found: " + fi.Name
                                + " Belongs to TV Folder: " + origTVdir + Environment.NewLine;
                            //file belogs to this dir so now find out which season
                            foreach (string seasonNumber in tvfolderSeasonsTemp())
                            {
                                if (fi.Name.ToLower().Contains(seasonNumber))
                                {
                                    #region file belongs to a season sub folder
                                    //right season found!
                                    //get the season naming convention
                                    string seasonfolder = getSeasonfolder(seasonNumber);
                                    _log += fi.Name + " Belongs to: " + seasonfolder + "(" + seasonNumber + ") Folder." 
                                        + Environment.NewLine;
                                    //check to see if season folder exist
                                    string fullTVshowPath = _tvFolder + origTVdir + "\\" + seasonfolder + "\\";
                                    if (Directory.Exists(fullTVshowPath))
                                    {
                                        _log += fullTVshowPath + " Folder exists... moving file... " + fi.Name + Environment.NewLine;
                                        //it exist!
                                        //move file from downloads to file path above
                                        try
                                        {
                                            fi.MoveTo(fullTVshowPath + fi.Name);
                                            _log += "File Move Successful!" + Environment.NewLine;
                                            fileMoved = true;
                                            subTitleDest = fullTVshowPath;
                                            movedFiles.Add(fi.Name);
                                        }
                                        catch (Exception ex)
                                        {
                                            isError = true;
                                            _log += "!! Failed to move " + fi.Name + " to " + fullTVshowPath + " !!" 
                                                + Environment.NewLine
                                                + ex.Message
                                                + Environment.NewLine;
                                            failedFiles.Add(fi.Name + " reason: " + ex.Message);
                                        }
                                    }
                                    else //dir does not exit
                                    {
                                        _log += fullTVshowPath + " Folder DOES NOT exist... Creating Dir..." + Environment.NewLine;
                                        Directory.CreateDirectory(fullTVshowPath);
                                        //move file from downloads to file path above
                                        try
                                        {
                                            fi.MoveTo(fullTVshowPath + fi.Name);
                                            _log += "File Move Successful!" + Environment.NewLine;
                                            fileMoved = true;
                                            subTitleDest = fullTVshowPath;
                                            movedFiles.Add(fi.Name);
                                        }
                                        catch (Exception ex)
                                        {
                                            isError = true;
                                            _log += "!! Failed to move " + fi.Name + " to " + fullTVshowPath + " !!" 
                                                + Environment.NewLine
                                                + ex.Message
                                                + Environment.NewLine;
                                            failedFiles.Add(fi.Name + " reason: " + ex.Message);
                                        }
                                    }
                                    #endregion
                                    break;
                                }
                            }
                            //after the foreach seasonNumber
                            if ((!fileMoved) && (!isError))
                            {
                                //no season info found
                                _log += "No Season info found for " + fi.Name + Environment.NewLine;
                                //move to root directory
                                string rootTVshowPath = _tvFolder + origTVdir + "\\";
                                _log += "Moving " + fi.Name + " to root dir: " + rootTVshowPath + Environment.NewLine;
                                try
                                {
                                    fi.MoveTo(rootTVshowPath + fi.Name);
                                    _log += "File(" + fi.Name + ") Move Successful!" + Environment.NewLine;
                                    fileMoved = true;
                                    subTitleDest = rootTVshowPath;
                                    movedFiles.Add(fi.Name);

                                }
                                catch (Exception ex)
                                {
                                    isError = true;
                                    _log += "!! Failed to move " + fi.Name + " to " + rootTVshowPath + " !!" 
                                        + Environment.NewLine
                                        + ex.Message
                                        + Environment.NewLine;
                                    failedFiles.Add(fi.Name + " reason: " + ex.Message);
                                }
                            }
                            #endregion
                            //find subtitles
                            //look in directory of the file
                            if (!(subTitleDir + "\\" == _monitoredDir) &&
                                !(string.IsNullOrEmpty(subTitleDest)))
                            {
                                _log += "Looking for subtitles in " + subTitleDir + Environment.NewLine;
                                bool foundSubs = false;
                                foreach (string file in Directory.GetFiles(subTitleDir, "*.*", SearchOption.AllDirectories))
                                {
                                    FileInfo subfile = new FileInfo(file);
                                    if (_subTitlesTypes.Contains(subfile.Extension.ToLower()))
                                    {
                                        #region subtitles found
                                        _log += "TV Subtitles found!" + Environment.NewLine;
                                        foundSubs = true;
                                        string simpleParentName = fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length);
                                        _log += "Renaming and moving " + subfile.Name + " to " + simpleParentName + Environment.NewLine;
                                        try
                                        {
                                            subfile.MoveTo(subTitleDest + simpleParentName + subfile.Extension);
                                            _log += simpleParentName + subfile.Extension + " Move Successful to " + subTitleDest + Environment.NewLine;
                                        }
                                        catch (Exception ex)
                                        {
                                            isError = true;
                                            _log += "Failed to move " + simpleParentName + subfile.Extension + " to " + subTitleDest
                                                + Environment.NewLine
                                                + ex.Message
                                                + Environment.NewLine;
                                        }
                                        #endregion
                                    }
                                }
                                //after foreach for subfiles
                                if (!foundSubs)
                                {
                                    _log += "No TV Subtitles found :(" + Environment.NewLine;
                                }
                            }
                            break;
                            #endregion
                        }
                        else //doesn't belong in TV folder might be a movie, mark it for now
                        {
                            isTVfile = false;                            
                        }
                    }
                    //after trying to find file in tv folders
                    if (!isTVfile)
                    {
                        _log += "'" + cleanedFileName.ToLower() + "' does not contain any TV info..." + Environment.NewLine;
                        possibleMovieFiles.Add(fi);
                    }
                }
            }            
            return possibleMovieFiles;
        }

        static void filterAndMoveToMovies(ArrayList filesFromTVFilter)
        {
            bool fileMoved = false; //a control for later
            bool isError = false; //a control for later

            foreach (FileInfo fi in filesFromTVFilter)
            {
                //file is a video
                //is it large enough? minium 300000000 bytes (300 MB)                  
                if (fi.Length > 300000000)
                {
                    #region if it is large enough
                    _log += "Movie found: " + fi.Name + " Size: " + fi.Length.ToString() + Environment.NewLine;

                    //look for subtitles
                    //look in directory of the file
                    if (!(fi.DirectoryName + "\\" == _monitoredDir))
                    {
                        _log += "Looking for subtitles in " + fi.DirectoryName + Environment.NewLine;
                        bool foundSubs = false;
                        foreach (string file in Directory.GetFiles(fi.DirectoryName, "*.*", SearchOption.AllDirectories))
                        {
                            FileInfo subfile = new FileInfo(file);
                            if (_subTitlesTypes.Contains(subfile.Extension.ToLower()))
                            {
                                #region subtitles found
                                _log += "Subtitles found!" + Environment.NewLine;
                                foundSubs = true;
                                string simpleParentName = fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length);
                                _log += "Renaming and moving " + subfile.Name + " to " + simpleParentName + Environment.NewLine;
                                try
                                {
                                    subfile.MoveTo(_movieFolder + simpleParentName + subfile.Extension);
                                    _log += simpleParentName + subfile.Extension + " Move Successful..." + Environment.NewLine;
                                }
                                catch (Exception ex)
                                {
                                    isError = true;
                                    _log += "Failed to move " + simpleParentName + subfile.Extension
                                        + Environment.NewLine
                                        + ex.Message
                                        + Environment.NewLine;
                                }
                                #endregion
                            }
                        }
                        //after foreach for subfiles
                        if (!foundSubs)
                        {
                            _log += "No Subtitles found :(" + Environment.NewLine;
                        }
                    }

                    //no other checks needed
                    //move file to movie folder
                    _log += "Moving " + fi.Name + " to Dir: " + _movieFolder + Environment.NewLine;
                    try
                    {
                        fi.MoveTo(_movieFolder + fi.Name);
                        _log += "File Move Successful!" + Environment.NewLine;
                        fileMoved = true;
                        movedFiles.Add(fi.Name);
                    }
                    catch (Exception ex)
                    {
                        isError = true;
                        _log += "!! Failed to move " + fi.Name
                            + Environment.NewLine
                            + ex.Message
                            + Environment.NewLine;
                        failedFiles.Add(fi.Name + " reason: " + ex.Message);
                    }
                    #endregion
                }
                else
                {
                    #region must be a video?
                    _log += fi.Name + " is not a movie, must be a video." + Environment.NewLine;
                    //get video folder
                    string vidFolder = _videoFolder + DateTime.Now.Year.ToString();
                    if (Directory.Exists(vidFolder))
                    {
                        //move file to video folder
                        _log += "Moving " + fi.Name + " to Dir: " + vidFolder + Environment.NewLine;
                        try
                        {
                            fi.MoveTo(vidFolder + "\\" + fi.Name);
                            _log += "File Move Successful!" + Environment.NewLine;
                            fileMoved = true;
                            movedFiles.Add(fi.Name);
                        }
                        catch (Exception ex)
                        {
                            isError = true;
                            _log += "!! Failed to move " + fi.Name
                                + Environment.NewLine
                                + ex.Message
                                + Environment.NewLine;
                            failedFiles.Add(fi.Name + " reason: " + ex.Message);
                        }
                    }
                    else //doesnt exist
                    {
                        string newVidFolder = _videoFolder + DateTime.Now.Year.ToString();
                        try
                        {
                            //make dir
                            Directory.CreateDirectory(newVidFolder);
                            //now move file into new dir
                            fi.MoveTo(newVidFolder + "\\" + fi.Name);
                            _log += "File Move Successful!" + Environment.NewLine;
                            fileMoved = true;
                            movedFiles.Add(fi.Name);
                        }
                        catch (Exception ex)
                        {
                            isError = true;
                            _log += "!! Failed to move " + fi.Name
                                + Environment.NewLine
                                + ex.Message
                                + Environment.NewLine;
                            failedFiles.Add(fi.Name + " reason: " + ex.Message);
                        }
                    }
                    #endregion
                }
            }
        }

        static void moveMusicToGmusic()
        {
            bool musicFound = false;
            //move a music file in the root dir only.            
            foreach (string fullFileName in Directory.GetFiles(_monitoredDir)) //search in root only
            {
                FileInfo fi = new FileInfo(fullFileName);
                if (_musicTypes.Contains(fi.Extension.ToLower()))
                {
                    //file is a music file.
                    musicFound = true;
                    _log += "Music file found: " + fi.Name + Environment.NewLine;
                    _log += "Moving " + fi.Name + " to " + _musicFolder + Environment.NewLine;
                    try
                    {
                        fi.MoveTo(_musicFolder + fi.Name);
                        _log += "File Move Successful!";
                        movedFiles.Add(fi.Name);
                    }
                    catch (Exception ex)
                    {
                        _log += "!! Failed to move " + fi.Name + Environment.NewLine
                            + Environment.NewLine
                            + ex.Message
                            + Environment.NewLine;
                    }
                }
            }

            //after foreach in direcotry.
            if (!musicFound)
            {
                _log += "No Music Files Found :(" + Environment.NewLine;
            }
        }

        static void logCheck()
        {
            //check and see if log contains "No Files Found :("
            if (
                (_log.Contains("No Video Files Found :(")) && 
                !(_log.Contains("A serious error has occurred")) &&
                (_log.Contains("No Music Files Found :("))
                )
            {
                _log = "";
            }
            //check to see if log is larger than 150000 bytes (150kb)
            try
            {
                FileInfo fi = new FileInfo(_logDir + "SmartFileMover_Log.txt");
                if (fi.Length > 150000)
                {
                    fi.Delete();
                }
            }
            catch
            { }
        }

        static bool sendEmail(string movieOrTv, ArrayList movedFiles, ArrayList failedFiles)
        {
            bool emailSent = false;
            string fileNames = string.Empty;
            string failedfileNames = string.Empty; 

            //null check
            if (movedFiles.Count <= 0)
            {
                _log += "Nothing to email :(" + Environment.NewLine;
                return false;
            }

            //setup client
            SmtpClient _eClient = new SmtpClient();
            _eClient.Host = "smtp.mail.yahoo.com";
            _eClient.Port = 587; //or 465, 25 
            _eClient.EnableSsl = true;
            _eClient.DeliveryMethod = SmtpDeliveryMethod.Network;
            _eClient.Credentials = new System.Net.NetworkCredential("youremail@email.com", "");

            //to/from/subject
            MailMessage email = new MailMessage();
            MailAddress sender = new MailAddress("youremail@email.com", "Smart File Mover");
            email.To.Add("sentto@email.com");
            email.From = sender;
            email.Subject = "SmartFileMover Status Update";

            //setup message
            if (movieOrTv == "TV")
            {
                movieOrTv = "TV show";
            }
            else
            {
                movieOrTv = "Movie";
            }

            //transfer log to var
            string logForEmail = _log;

            foreach (string file in movedFiles)
            {
                fileNames += "<li>" + file + "</li>";
            }

            foreach (string file in failedFiles)
            {
                failedfileNames += "<li>" + file + "</li>";
            }

            string body = string.Format(
@"<h2>The following files are ready</h2>: 
<br />
<ul>
<strong>{0}</strong>
</ul>
<hr/>
<h2>These Files Failed: :(</h2>: 
<br />
<ul>
{1}
</ul>", fileNames,failedfileNames);
            email.Body = body;
            email.IsBodyHtml = true;

            //send
            try
            {
                _eClient.Send(email);
                _log += "Email Sent!" + Environment.NewLine;
                emailSent = true;
            }
            catch (Exception ex)
            {
                emailSent = false;
                _log += "!! Email Failed: " + ex.Message + Environment.NewLine;
            }
            return emailSent;
        }

        #region other methods
        static ArrayList tvfolderSeasonsTemp()
        {
            ArrayList seasonsTemplate = new ArrayList();

            seasonsTemplate.Add("s20");
            seasonsTemplate.Add("season20");
            seasonsTemplate.Add("season 20");
            seasonsTemplate.Add("20x");
            seasonsTemplate.Add("s19");
            seasonsTemplate.Add("season 19");
            seasonsTemplate.Add("season19");
            seasonsTemplate.Add("19x");
            seasonsTemplate.Add("s18");
            seasonsTemplate.Add("season 18");
            seasonsTemplate.Add("season18");
            seasonsTemplate.Add("18x");
            seasonsTemplate.Add("s17");
            seasonsTemplate.Add("season 17");
            seasonsTemplate.Add("season17");
            seasonsTemplate.Add("17x");
            seasonsTemplate.Add("s16");
            seasonsTemplate.Add("season 16");
            seasonsTemplate.Add("season16");
            seasonsTemplate.Add("16x");
            seasonsTemplate.Add("s15");
            seasonsTemplate.Add("season 15");
            seasonsTemplate.Add("season15");
            seasonsTemplate.Add("15x");
            seasonsTemplate.Add("s14");
            seasonsTemplate.Add("season 14");
            seasonsTemplate.Add("season14");
            seasonsTemplate.Add("14x");
            seasonsTemplate.Add("s13");
            seasonsTemplate.Add("season 13");
            seasonsTemplate.Add("season13");
            seasonsTemplate.Add("13x");
            seasonsTemplate.Add("s12");
            seasonsTemplate.Add("season 12");
            seasonsTemplate.Add("season12");
            seasonsTemplate.Add("12x");
            seasonsTemplate.Add("s11");
            seasonsTemplate.Add("season 11");
            seasonsTemplate.Add("season11");
            seasonsTemplate.Add("11x");
            seasonsTemplate.Add("s10");
            seasonsTemplate.Add("season 10");
            seasonsTemplate.Add("season10");
            seasonsTemplate.Add("10x");
            seasonsTemplate.Add("s09");
            seasonsTemplate.Add("s9e");
            seasonsTemplate.Add("season 9");
            seasonsTemplate.Add("season9");
            seasonsTemplate.Add("season 09");
            seasonsTemplate.Add("season09");            
            seasonsTemplate.Add("09x");
            seasonsTemplate.Add("9x");
            seasonsTemplate.Add("8x");
            seasonsTemplate.Add("08x");
            seasonsTemplate.Add("season08");
            seasonsTemplate.Add("season 08");
            seasonsTemplate.Add("season8");
            seasonsTemplate.Add("season 8");
            seasonsTemplate.Add("s8e");
            seasonsTemplate.Add("s08");
            seasonsTemplate.Add("s07");
            seasonsTemplate.Add("s7e");
            seasonsTemplate.Add("season 7");
            seasonsTemplate.Add("season7");
            seasonsTemplate.Add("season 07");
            seasonsTemplate.Add("season07");
            seasonsTemplate.Add("7x");
            seasonsTemplate.Add("07x");
            seasonsTemplate.Add("s06");
            seasonsTemplate.Add("s6e");
            seasonsTemplate.Add("season 6");
            seasonsTemplate.Add("season6");
            seasonsTemplate.Add("6x");
            seasonsTemplate.Add("season06");
            seasonsTemplate.Add("season 06");
            seasonsTemplate.Add("season 05");
            seasonsTemplate.Add("season05");
            seasonsTemplate.Add("06x");
            seasonsTemplate.Add("5x");
            seasonsTemplate.Add("s5e");
            seasonsTemplate.Add("s05");
            seasonsTemplate.Add("season5");
            seasonsTemplate.Add("season 5");
            seasonsTemplate.Add("05x");
            seasonsTemplate.Add("s04");
            seasonsTemplate.Add("s4e");
            seasonsTemplate.Add("season 4");
            seasonsTemplate.Add("season4");
            seasonsTemplate.Add("4x");
            seasonsTemplate.Add("season04");
            seasonsTemplate.Add("season 04");
            seasonsTemplate.Add("04x");
            seasonsTemplate.Add("s03");
            seasonsTemplate.Add("s3e");
            seasonsTemplate.Add("3x");
            seasonsTemplate.Add("03x");
            seasonsTemplate.Add("season03");
            seasonsTemplate.Add("season 03");
            seasonsTemplate.Add("season 3");
            seasonsTemplate.Add("season3");
            seasonsTemplate.Add("s02");
            seasonsTemplate.Add("s2e");
            seasonsTemplate.Add("season 2");
            seasonsTemplate.Add("season2");
            seasonsTemplate.Add("season 02");
            seasonsTemplate.Add("season02");
            seasonsTemplate.Add("2x");
            seasonsTemplate.Add("02x");
            seasonsTemplate.Add("s01");
            seasonsTemplate.Add("s1e");
            seasonsTemplate.Add("season 1");
            seasonsTemplate.Add("season1");
            seasonsTemplate.Add("season 01");
            seasonsTemplate.Add("season01");
            seasonsTemplate.Add("1x");
            seasonsTemplate.Add("01x");

            #region season that are "see" format or 104 = season 1 ep 4 (goes up to season 9 up to 30 ep per season)
            seasonsTemplate.Add("101");
            seasonsTemplate.Add("102");
            seasonsTemplate.Add("103");
            seasonsTemplate.Add("104");
            seasonsTemplate.Add("105");
            seasonsTemplate.Add("106");
            seasonsTemplate.Add("107");
            seasonsTemplate.Add("108");
            seasonsTemplate.Add("109");
            seasonsTemplate.Add("110");
            seasonsTemplate.Add("111");
            seasonsTemplate.Add("112");
            seasonsTemplate.Add("113");
            seasonsTemplate.Add("114");
            seasonsTemplate.Add("115");
            seasonsTemplate.Add("116");
            seasonsTemplate.Add("117");
            seasonsTemplate.Add("118");
            seasonsTemplate.Add("119");
            seasonsTemplate.Add("120");
            seasonsTemplate.Add("121");
            seasonsTemplate.Add("122");
            seasonsTemplate.Add("123");
            seasonsTemplate.Add("124");
            seasonsTemplate.Add("125");
            seasonsTemplate.Add("126");
            seasonsTemplate.Add("127");
            seasonsTemplate.Add("128");
            seasonsTemplate.Add("129");
            seasonsTemplate.Add("130");
            seasonsTemplate.Add("201");
            seasonsTemplate.Add("202");
            seasonsTemplate.Add("203");
            seasonsTemplate.Add("204");
            seasonsTemplate.Add("205");
            seasonsTemplate.Add("206");
            seasonsTemplate.Add("207");
            seasonsTemplate.Add("208");
            seasonsTemplate.Add("209");
            seasonsTemplate.Add("210");
            seasonsTemplate.Add("211");
            seasonsTemplate.Add("212");
            seasonsTemplate.Add("213");
            seasonsTemplate.Add("214");
            seasonsTemplate.Add("215");
            seasonsTemplate.Add("216");
            seasonsTemplate.Add("217");
            seasonsTemplate.Add("218");
            seasonsTemplate.Add("219");
            seasonsTemplate.Add("220");
            seasonsTemplate.Add("221");
            seasonsTemplate.Add("222");
            seasonsTemplate.Add("223");
            seasonsTemplate.Add("224");
            seasonsTemplate.Add("225");
            seasonsTemplate.Add("226");
            seasonsTemplate.Add("227");
            seasonsTemplate.Add("228");
            seasonsTemplate.Add("229");
            seasonsTemplate.Add("230");
            seasonsTemplate.Add("301");
            seasonsTemplate.Add("302");
            seasonsTemplate.Add("303");
            seasonsTemplate.Add("304");
            seasonsTemplate.Add("305");
            seasonsTemplate.Add("306");
            seasonsTemplate.Add("307");
            seasonsTemplate.Add("308");
            seasonsTemplate.Add("309");
            seasonsTemplate.Add("310");
            seasonsTemplate.Add("311");
            seasonsTemplate.Add("312");
            seasonsTemplate.Add("313");
            seasonsTemplate.Add("314");
            seasonsTemplate.Add("315");
            seasonsTemplate.Add("316");
            seasonsTemplate.Add("317");
            seasonsTemplate.Add("318");
            seasonsTemplate.Add("319");
            seasonsTemplate.Add("320");
            seasonsTemplate.Add("321");
            seasonsTemplate.Add("322");
            seasonsTemplate.Add("323");
            seasonsTemplate.Add("324");
            seasonsTemplate.Add("325");
            seasonsTemplate.Add("326");
            seasonsTemplate.Add("327");
            seasonsTemplate.Add("328");
            seasonsTemplate.Add("329");
            seasonsTemplate.Add("330");
            seasonsTemplate.Add("401");
            seasonsTemplate.Add("402");
            seasonsTemplate.Add("403");
            seasonsTemplate.Add("404");
            seasonsTemplate.Add("405");
            seasonsTemplate.Add("406");
            seasonsTemplate.Add("407");
            seasonsTemplate.Add("408");
            seasonsTemplate.Add("409");
            seasonsTemplate.Add("410");
            seasonsTemplate.Add("411");
            seasonsTemplate.Add("412");
            seasonsTemplate.Add("413");
            seasonsTemplate.Add("414");
            seasonsTemplate.Add("415");
            seasonsTemplate.Add("416");
            seasonsTemplate.Add("417");
            seasonsTemplate.Add("418");
            seasonsTemplate.Add("419");
            seasonsTemplate.Add("420");
            seasonsTemplate.Add("421");
            seasonsTemplate.Add("422");
            seasonsTemplate.Add("423");
            seasonsTemplate.Add("424");
            seasonsTemplate.Add("425");
            seasonsTemplate.Add("426");
            seasonsTemplate.Add("427");
            seasonsTemplate.Add("428");
            seasonsTemplate.Add("429");
            seasonsTemplate.Add("430");
            seasonsTemplate.Add("501");
            seasonsTemplate.Add("502");
            seasonsTemplate.Add("503");
            seasonsTemplate.Add("504");
            seasonsTemplate.Add("505");
            seasonsTemplate.Add("506");
            seasonsTemplate.Add("507");
            seasonsTemplate.Add("508");
            seasonsTemplate.Add("509");
            seasonsTemplate.Add("510");
            seasonsTemplate.Add("511");
            seasonsTemplate.Add("512");
            seasonsTemplate.Add("513");
            seasonsTemplate.Add("514");
            seasonsTemplate.Add("515");
            seasonsTemplate.Add("516");
            seasonsTemplate.Add("517");
            seasonsTemplate.Add("518");
            seasonsTemplate.Add("519");
            seasonsTemplate.Add("520");
            seasonsTemplate.Add("521");
            seasonsTemplate.Add("522");
            seasonsTemplate.Add("523");
            seasonsTemplate.Add("524");
            seasonsTemplate.Add("525");
            seasonsTemplate.Add("526");
            seasonsTemplate.Add("527");
            seasonsTemplate.Add("528");
            seasonsTemplate.Add("529");
            seasonsTemplate.Add("530");
            seasonsTemplate.Add("601");
            seasonsTemplate.Add("602");
            seasonsTemplate.Add("603");
            seasonsTemplate.Add("604");
            seasonsTemplate.Add("605");
            seasonsTemplate.Add("606");
            seasonsTemplate.Add("607");
            seasonsTemplate.Add("608");
            seasonsTemplate.Add("609");
            seasonsTemplate.Add("610");
            seasonsTemplate.Add("611");
            seasonsTemplate.Add("612");
            seasonsTemplate.Add("613");
            seasonsTemplate.Add("614");
            seasonsTemplate.Add("615");
            seasonsTemplate.Add("616");
            seasonsTemplate.Add("617");
            seasonsTemplate.Add("618");
            seasonsTemplate.Add("619");
            seasonsTemplate.Add("620");
            seasonsTemplate.Add("621");
            seasonsTemplate.Add("622");
            seasonsTemplate.Add("623");
            seasonsTemplate.Add("624");
            seasonsTemplate.Add("625");
            seasonsTemplate.Add("626");
            seasonsTemplate.Add("627");
            seasonsTemplate.Add("628");
            seasonsTemplate.Add("629");
            seasonsTemplate.Add("630");
            seasonsTemplate.Add("701");
            seasonsTemplate.Add("702");
            seasonsTemplate.Add("703");
            seasonsTemplate.Add("704");
            seasonsTemplate.Add("705");
            seasonsTemplate.Add("706");
            seasonsTemplate.Add("707");
            seasonsTemplate.Add("708");
            seasonsTemplate.Add("709");
            seasonsTemplate.Add("710");
            seasonsTemplate.Add("711");
            seasonsTemplate.Add("712");
            seasonsTemplate.Add("713");
            seasonsTemplate.Add("714");
            seasonsTemplate.Add("715");
            seasonsTemplate.Add("716");
            seasonsTemplate.Add("717");
            seasonsTemplate.Add("718");
            seasonsTemplate.Add("719");
            seasonsTemplate.Add("720");
            seasonsTemplate.Add("721");
            seasonsTemplate.Add("722");
            seasonsTemplate.Add("723");
            seasonsTemplate.Add("724");
            seasonsTemplate.Add("725");
            seasonsTemplate.Add("726");
            seasonsTemplate.Add("727");
            seasonsTemplate.Add("728");
            seasonsTemplate.Add("729");
            seasonsTemplate.Add("730");
            seasonsTemplate.Add("801");
            seasonsTemplate.Add("802");
            seasonsTemplate.Add("803");
            seasonsTemplate.Add("804");
            seasonsTemplate.Add("805");
            seasonsTemplate.Add("806");
            seasonsTemplate.Add("807");
            seasonsTemplate.Add("808");
            seasonsTemplate.Add("809");
            seasonsTemplate.Add("810");
            seasonsTemplate.Add("811");
            seasonsTemplate.Add("812");
            seasonsTemplate.Add("813");
            seasonsTemplate.Add("814");
            seasonsTemplate.Add("815");
            seasonsTemplate.Add("816");
            seasonsTemplate.Add("817");
            seasonsTemplate.Add("818");
            seasonsTemplate.Add("819");
            seasonsTemplate.Add("820");
            seasonsTemplate.Add("821");
            seasonsTemplate.Add("822");
            seasonsTemplate.Add("823");
            seasonsTemplate.Add("824");
            seasonsTemplate.Add("825");
            seasonsTemplate.Add("826");
            seasonsTemplate.Add("827");
            seasonsTemplate.Add("828");
            seasonsTemplate.Add("829");
            seasonsTemplate.Add("830");
            seasonsTemplate.Add("901");
            seasonsTemplate.Add("902");
            seasonsTemplate.Add("903");
            seasonsTemplate.Add("904");
            seasonsTemplate.Add("905");
            seasonsTemplate.Add("906");
            seasonsTemplate.Add("907");
            seasonsTemplate.Add("908");
            seasonsTemplate.Add("909");
            seasonsTemplate.Add("910");
            seasonsTemplate.Add("911");
            seasonsTemplate.Add("912");
            seasonsTemplate.Add("913");
            seasonsTemplate.Add("914");
            seasonsTemplate.Add("915");
            seasonsTemplate.Add("916");
            seasonsTemplate.Add("917");
            seasonsTemplate.Add("918");
            seasonsTemplate.Add("919");
            seasonsTemplate.Add("920");
            seasonsTemplate.Add("921");
            seasonsTemplate.Add("922");
            seasonsTemplate.Add("923");
            seasonsTemplate.Add("924");
            seasonsTemplate.Add("925");
            seasonsTemplate.Add("926");
            seasonsTemplate.Add("927");
            seasonsTemplate.Add("928");
            seasonsTemplate.Add("929");
            seasonsTemplate.Add("930");
            #endregion

            return seasonsTemplate;
        }

        static string getSeasonfolder(string SeasonNumber)
        {
            string seasonfolder = string.Empty;

            switch (SeasonNumber)
            {
                case "s20":
                case "s20e":
                case "season 20":
                case "season20":
                case "20x":
                    seasonfolder = "Season 20";
                    break;
                case "s19":
                case "s19e":
                case "season 19":
                case "season19":
                case "19x":
                    seasonfolder = "Season 19";
                    break;
                case "s18":
                case "s18e":
                case "season 18":
                case "season18":
                case "18x":
                    seasonfolder = "Season 18";
                    break;
                case "s17":
                case "s17e":
                case "season 17":
                case "season17":
                case "17x":
                    seasonfolder = "Season 17";
                    break;
                case "s16":
                case "s16e":
                case "season 16":
                case "season16":
                case "16x":
                    seasonfolder = "Season 16";
                    break;
                case "s15":
                case "s15e":
                case "season 15":
                case "season15":
                case "15x":
                    seasonfolder = "Season 15";
                    break;
                case "s14":
                case "s14e":
                case "season 14":
                case "season14":
                case "14x":
                    seasonfolder = "Season 14";
                    break;
                case "s13":
                case "s13e":
                case "season 13":
                case "season13":
                case "13x":
                    seasonfolder = "Season 13";
                    break;
                case "s12":
                case "s12e":
                case "season 12":
                case "season12":
                case "12x":
                    seasonfolder = "Season 12";
                    break;
                case "s11":
                case "s11e":
                case "season 11":
                case "season11":
                case "11x":
                    seasonfolder = "Season 11";
                    break;
                case "s10":
                case "s10e":
                case "season 10":
                case "season10":
                case "10x":
                    seasonfolder = "Season 10";
                    break;
                case "s9e":
                case "s09":
                case "09x":
                case "9x":
                case "season 9":
                case "season9":
                case "season 09":
                case "season09":
                case "901":
                case "902":
                case "903":
                case "904":
                case "905":
                case "906":
                case "907":
                case "908":
                case "909":
                case "910":
                case "911":
                case "912":
                case "913":
                case "914":
                case "915":
                case "916":
                case "917":
                case "918":
                case "919":
                case "920":
                case "921":
                case "922":
                case "923":
                case "924":
                case "925":
                case "926":
                case "927":
                case "928":
                case "929":
                case "930":
                    seasonfolder = "Season 9";
                    break;
                case "s8e":
                case "s08":
                case "08x":
                case "8x":
                case "season 8":
                case "season8":
                case "season 08":
                case "season08":
                case "801":
                case "802":
                case "803":
                case "804":
                case "805":
                case "806":
                case "807":
                case "808":
                case "809":
                case "810":
                case "811":
                case "812":
                case "813":
                case "814":
                case "815":
                case "816":
                case "817":
                case "818":
                case "819":
                case "820":
                case "821":
                case "822":
                case "823":
                case "824":
                case "825":
                case "826":
                case "827":
                case "828":
                case "829":
                case "830":
                    seasonfolder = "Season 8";
                    break;
                case "s7e":
                case "s07":
                case "07x":
                case "7x":
                case "season 7":
                case "season7":
                case "season 07":
                case "season07":
                case "701":
                case "702":
                case "703":
                case "704":
                case "705":
                case "706":
                case "707":
                case "708":
                case "709":
                case "710":
                case "711":
                case "712":
                case "713":
                case "714":
                case "715":
                case "716":
                case "717":
                case "718":
                case "719":
                case "720":
                case "721":
                case "722":
                case "723":
                case "724":
                case "725":
                case "726":
                case "727":
                case "728":
                case "729":
                case "730":
                    seasonfolder = "Season 7";
                    break;
                case "s6e":
                case "s06":
                case "06x":
                case "6x":
                case "season 6":
                case "season6":
                case "season 06":
                case "season06":
                case "601":
                case "602":
                case "603":
                case "604":
                case "605":
                case "606":
                case "607":
                case "608":
                case "609":
                case "610":
                case "611":
                case "612":
                case "613":
                case "614":
                case "615":
                case "616":
                case "617":
                case "618":
                case "619":
                case "620":
                case "621":
                case "622":
                case "623":
                case "624":
                case "625":
                case "626":
                case "627":
                case "628":
                case "629":
                case "630":
                    seasonfolder = "Season 6";
                    break;
                case "s5e":
                case "s05":
                case "05x":
                case "5x":
                case "season 5":
                case "season5":
                case "season 05":
                case "season05":
                case "501":
                case "502":
                case "503":
                case "504":
                case "505":
                case "506":
                case "507":
                case "508":
                case "509":
                case "510":
                case "511":
                case "512":
                case "513":
                case "514":
                case "515":
                case "516":
                case "517":
                case "518":
                case "519":
                case "520":
                case "521":
                case "522":
                case "523":
                case "524":
                case "525":
                case "526":
                case "527":
                case "528":
                case "529":
                case "530":
                    seasonfolder = "Season 5";
                    break;
                case "s4e":
                case "s04":
                case "04x":
                case "4x":
                case "season 4":
                case "season4":
                case "season 04":
                case "season04":
                case "401":
                case "402":
                case "403":
                case "404":
                case "405":
                case "406":
                case "407":
                case "408":
                case "409":
                case "410":
                case "411":
                case "412":
                case "413":
                case "414":
                case "415":
                case "416":
                case "417":
                case "418":
                case "419":
                case "420":
                case "421":
                case "422":
                case "423":
                case "424":
                case "425":
                case "426":
                case "427":
                case "428":
                case "429":
                case "430":
                    seasonfolder = "Season 4";
                    break;
                case "s3e":
                case "s03":
                case "03x":
                case "3x":
                case "season 3":
                case "season3":
                case "season 03":
                case "season03":
                case "301":
                case "302":
                case "303":
                case "304":
                case "305":
                case "306":
                case "307":
                case "308":
                case "309":
                case "310":
                case "311":
                case "312":
                case "313":
                case "314":
                case "315":
                case "316":
                case "317":
                case "318":
                case "319":
                case "320":
                case "321":
                case "322":
                case "323":
                case "324":
                case "325":
                case "326":
                case "327":
                case "328":
                case "329":
                case "330":
                    seasonfolder = "Season 3";
                    break;
                case "s2e":
                case "s02":
                case "02x":
                case "2x":
                case "season 2":
                case "season2":
                case "season 02":
                case "season02":
                case "201":
                case "202":
                case "203":
                case "204":
                case "205":
                case "206":
                case "207":
                case "208":
                case "209":
                case "210":
                case "211":
                case "212":
                case "213":
                case "214":
                case "215":
                case "216":
                case "217":
                case "218":
                case "219":
                case "220":
                case "221":
                case "222":
                case "223":
                case "224":
                case "225":
                case "226":
                case "227":
                case "228":
                case "229":
                case "230":
                    seasonfolder = "Season 2";
                    break;
                case "s1e":
                case "s01":
                case "01x":
                case "1x":
                case "season 1":
                case "season1":
                case "season 01":
                case "season01":
                case "101":
                case "102":
                case "103":
                case "104":
                case "105":
                case "106":
                case "107":
                case "108":
                case "109":
                case "110":
                case "111":
                case "112":
                case "113":
                case "114":
                case "115":
                case "116":
                case "117":
                case "118":
                case "119":
                case "120":
                case "121":
                case "122":
                case "123":
                case "124":
                case "125":
                case "126":
                case "127":
                case "128":
                case "129":
                case "130":
                    seasonfolder = "Season 1";
                    break;
                default:
                    seasonfolder = "Unknown Season";
                    break;
            }

            return seasonfolder;
        }

        static void specChrCheck(string tvDir, out string smartTVdir, out string origTVdir)
        {
            smartTVdir = string.Empty;
            origTVdir = string.Empty;

            smartTVdir = tvDir.ToLower();
            origTVdir = tvDir.ToLower(); //var to keep origional name without key.... 
            //contains "and"
            if (tvDir.ToLower().Contains("+and"))
            {
                //it has an "and"... remove it and take out key
                smartTVdir = smartTVdir.Replace(" and ", " ").Replace("+and,", "");
                //take out key to attempt moving the file to the right dir.
                origTVdir = tvDir.ToLower().Replace("+and,", "");

                _log += "Smart name matching: '" + origTVdir + "' to '" + smartTVdir + "'" + Environment.NewLine;
            }
            //contains apersand
            if (tvDir.ToLower().Contains("*aps"))
            {
                //it has an "spec char"... remove it and take out key
                smartTVdir = smartTVdir.Replace("'", "").Replace("*aps,", "");
                //take out key to attempt moving the file to the right dir.
                origTVdir = tvDir.ToLower().Replace("*aps,", "");

                _log += "Smart name matching: '" + origTVdir + "' to '" + smartTVdir + "'" + Environment.NewLine;
            }
            //contains "the"
            if (tvDir.ToLower().Contains("*article"))
            {
                //it has an "spec char"... remove it and take out key
                smartTVdir = smartTVdir.Replace("the ", "").Replace("*article,", "");
                //take out key to attempt moving the file to the right dir.
                origTVdir = tvDir.ToLower().Replace("*article,", "");

                _log += "Smart name matching: '" + origTVdir + "' to '" + smartTVdir + "'" + Environment.NewLine;
            }
            //contains "." (dot)
            if (tvDir.ToLower().Contains("*dot"))
            {
                //it has an "spec char"... remove it and take out key
                smartTVdir = smartTVdir.Replace(".", "").Replace("*dot,", "");
                //take out key to attempt moving the file to the right dir.
                origTVdir = tvDir.ToLower().Replace("*dot,", "");

                _log += "Smart name matching: '" + origTVdir + "' to '" + smartTVdir + "'" + Environment.NewLine;
            }
            //contains "&" (anpersand)
            if (tvDir.ToLower().Contains("*apers"))
            {
                //it has an "spec char"... remove it and take out key
                smartTVdir = smartTVdir.Replace(" & ", " ").Replace("*apers,", "");
                //take out key to attempt moving the file to the right dir.
                origTVdir = tvDir.ToLower().Replace("*apers,", "");

                _log += "Smart name matching: '" + origTVdir + "' to '" + smartTVdir + "'" + Environment.NewLine;
            }
            //contains ":" (colon)
            if (tvDir.ToLower().Contains("*colon"))
            {
                //it has an "spec char"... remove it and take out key
                smartTVdir = smartTVdir.Replace(":", "").Replace("*colon,", "");
                //take out key to attempt moving the file to the right dir.
                origTVdir = tvDir.ToLower().Replace("*colon,", "");

                _log += "Smart name matching: '" + origTVdir + "' to '" + smartTVdir + "'" + Environment.NewLine;
            }
            //contains "-" (dash)
            if (tvDir.ToLower().Contains("*dash"))
            {
                //it has an "spec char"... remove it and take out key
                smartTVdir = smartTVdir.Replace("-", " ").Replace("*dash,", "");
                //take out key to attempt moving the file to the right dir.
                origTVdir = tvDir.ToLower().Replace("*dash,", "");

                _log += "Smart name matching: '" + origTVdir + "' to '" + smartTVdir + "'" + Environment.NewLine;
            }
        }
        #endregion
    }
}

/// <summary>
/// Sort an array by the lenth of the name instead of alphabeticly decending - longest first
/// </summary>
public class StringLenthCompare : IComparer
{
    // Calls CaseInsensitiveComparer.Compare with the parameters reversed.
    int IComparer.Compare(Object x, Object y)
    {
        return ((new CaseInsensitiveComparer()).Compare(y.ToString().Length, x.ToString().Length));
    }

}
