namespace HandmadeHeroVideoRecorder.ConsoleApp
{
    using Core.Infrastructure.Constants;
    using Core.Infrastructure.Helpers;
    using HtmlAgilityPack;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using VideoLibraryNetCore;

    /// <summary>
    /// Little program to save on disk some Hero Handmade Network tutorials videos
    /// PLEASE WATCH THEM ON YOUTUBE AS YOU CAN, it's the correct way to follow this tutorials. This program just
    /// exists for your personal archives or to survive to a disconnected period
    /// All the rights are owned by Casey Muratori - @cmuratori
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            Program p = new Program();

            string cmd = string.Empty;
            while (cmd.ToLower() != "exit")
            {
                p.DisplayMenu();

                Console.Write("Enter your command :");
                cmd = Console.ReadLine();

                switch (cmd)
                {
                    case "1":
                        p.RecordDayVideo();
                        break;
                    case "2":
                        p.RecordWeekVideos();
                        break;
                    default:
                        Console.WriteLine("Unknowed command ...");
                        break;
                }
            }
        }

        /// <summary>
        /// Show menu
        /// </summary>
        private void DisplayMenu()
        {
            Console.WriteLine();
            Console.WriteLine("Hero Handmade video recorder");
            Console.WriteLine("MENU");
            Console.WriteLine();
            Console.WriteLine("Choose your command : ");
            Console.WriteLine();
            Console.WriteLine("1 - Save day's video");
            Console.WriteLine("2 - Save week's videos");
            Console.WriteLine();
            Console.Write("");
        }

        /// <summary>
        /// Record the video of a hero handmade network day tutorial
        /// </summary>
        async void RecordDayVideo()
        {
            // Get path
            Console.WriteLine("Where do you want to record this video ?");
            string outputDirectoryPath = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(outputDirectoryPath) || !Directory.Exists(outputDirectoryPath))
            {
                Console.WriteLine("An error occured");
                return;
            }

            // Get day
            Console.WriteLine("Which day to your want ? (number of the day)");
            string dayNumberString = Console.ReadLine();
            int dayNumber = 0;
            if (string.IsNullOrWhiteSpace(dayNumberString) || !Int32.TryParse(dayNumberString, out dayNumber) || dayNumber <= 0)
            {
                Console.WriteLine("An error occured");
                return;
            }

            string html = null, videoTitle = null, videoLink = null, youtubeUrl = null;
            HtmlDocument doc = new HtmlDocument();
            HtmlNodeCollection nodes = null;
            HtmlDocument tmpDoc = new HtmlDocument();

            try
            {
                WriteCreditsFiles(outputDirectoryPath);

                // Browse episode guide to get video url 

                html = await NetRequestHelper.TryGetUrlHtmlContent(HandmadeNetworkConstants.EpisodeGuideUrl);

                if (string.IsNullOrWhiteSpace(html))
                {
                    Console.WriteLine("An error occured");
                    return;
                }

                doc.LoadHtml(html);

                if (doc == null)
                {
                    Console.WriteLine("An error occured");
                    return;
                }

                nodes = doc.DocumentNode.SelectNodes("//div[@class='description']/*");
                if (nodes == null || nodes.Count == 0)
                {
                    Console.WriteLine("An error occured");
                    return;
                }

                foreach (HtmlNode nd in nodes)
                {
                    if (nd.Name != "ul")
                        continue;

                    if (string.IsNullOrWhiteSpace(nd.InnerHtml))
                        continue;

                    tmpDoc = new HtmlDocument();
                    tmpDoc.LoadHtml(nd.InnerHtml.Replace("\n", null));

                    if (tmpDoc.DocumentNode.ChildNodes.First().SelectNodes("//li") == null)
                        continue;

                    if (tmpDoc.DocumentNode.ChildNodes.First().SelectNodes("//li").Count == 0)
                        continue;

                    bool theGoodOne = false;
                    bool keepBrowsingDays = true;

                    for (int i = 0; i < tmpDoc.DocumentNode.ChildNodes.First().SelectNodes("//li").Count; i++)
                    {
                        if (!string.IsNullOrWhiteSpace(tmpDoc.DocumentNode.ChildNodes.First().SelectNodes("//li")[i].InnerText) && tmpDoc.DocumentNode.ChildNodes.First().SelectNodes("//li")[i].InnerText.ToLower().Contains("day"))
                        {
                            if (!keepBrowsingDays)
                                continue;

                            string[] dayArr2 = tmpDoc.DocumentNode.ChildNodes.First().SelectNodes("//li")[i].InnerText.Split(new char[] { '-', ':' }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (string s in dayArr2)
                            {
                                if (s.ToLower().Contains("day") && s.ToLower().Replace("day", null).Replace(" ", null).Length == 3)
                                {
                                    int _i = 0;
                                    if (Int32.TryParse(s.ToLower().Replace("day", null), out _i) && _i == dayNumber)
                                    {
                                        videoLink = HandmadeNetworkUrlHelper.BuildDayEpisodeUrl(_i);
                                        theGoodOne = true;
                                    }
                                }
                                else if (theGoodOne)
                                {
                                    videoTitle = s;
                                    keepBrowsingDays = false;
                                }
                            }
                        }
                    }
                }

                // Checks

                if (string.IsNullOrWhiteSpace(videoLink))
                    throw new Exception();

                // Get youtube video url

                youtubeUrl = await GetDayYouTubeVideoUrl(videoLink);

                if (string.IsNullOrWhiteSpace(youtubeUrl))
                    throw new Exception();

                // Download video

                if (SaveVideoOnDisk(youtubeUrl, outputDirectoryPath))
                    Console.WriteLine("Video saved");
                else
                    Console.WriteLine("On error occurred");
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occured");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                html = null;
                doc = null;
                nodes = null;
                tmpDoc = null;
                videoTitle = null;
                videoLink = null;
                youtubeUrl = null;
            }
        }

        /// <summary>
        /// Save all videos of a week
        /// </summary>
        async void RecordWeekVideos()
        {
            // Get path
            Console.WriteLine("Where do you want to record this video ?");
            string outputDirectoryPath = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(outputDirectoryPath) || !Directory.Exists(outputDirectoryPath))
            {
                Console.WriteLine("An error occured");
                return;
            }

            // Get day
            Console.WriteLine("Which week to your want ? (number of the week)");
            string weekNumberString = Console.ReadLine();
            int weekNumber = 0;
            if (string.IsNullOrWhiteSpace(weekNumberString) || !Int32.TryParse(weekNumberString, out weekNumber) || weekNumber <= 0)
            {
                Console.WriteLine("An error occured");
                return;
            }

            string html = null, weekTitle = null;
            HtmlDocument doc = new HtmlDocument();
            HtmlNodeCollection nodes = null;
            HtmlDocument tmpDoc = new HtmlDocument();

            try
            {
                WriteCreditsFiles(outputDirectoryPath);

                // Browse episode guide to get video url 

                html = await NetRequestHelper.TryGetUrlHtmlContent(HandmadeNetworkConstants.EpisodeGuideUrl);

                if (string.IsNullOrWhiteSpace(html))
                {
                    Console.WriteLine("An error occured");
                    return;
                }

                doc.LoadHtml(html);

                if (doc == null)
                {
                    Console.WriteLine("An error occured");
                    return;
                }

                nodes = doc.DocumentNode.SelectNodes("//div[@class='description']/*");
                if (nodes == null || nodes.Count == 0)
                {
                    Console.WriteLine("An error occured");
                    return;
                }

                bool curWeek = false;

                foreach (HtmlNode nd in nodes)
                {
                    if (nd.Name == "h3")
                    {
                        if (string.IsNullOrWhiteSpace(nd.InnerText))
                            continue;

                        if (!nd.InnerText.ToLower().Contains("week"))
                            continue;

                        if (nd.InnerText.Contains(':'))
                        {
                            if (weekNumber == Convert.ToInt32(nd.InnerText.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries)[0].ToLower().Replace("week ", null)))
                            {
                                weekTitle = nd.InnerText.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries)[1];
                                curWeek = true;
                            }
                        }
                        else
                        {
                            if (weekNumber == Convert.ToInt32(nd.InnerText.ToLower().Replace("week ", null)))
                            {
                                curWeek = true;
                                weekTitle = string.Empty;
                            }
                        }
                    }
                    else if (curWeek && nd.Name == "ul")
                    {
                        tmpDoc = new HtmlDocument();
                        tmpDoc.LoadHtml(nd.InnerHtml.Replace("\n", null));

                        for (int i = 0; i < tmpDoc.DocumentNode.ChildNodes.First().SelectNodes("//li").Count; i++)
                        {
                            string _dayVideoLink = null, _dayVideoTitle = null, _dayYouTubeUrl = null;

                            string[] dayArr2 = tmpDoc.DocumentNode.ChildNodes.First().SelectNodes("//li")[i].InnerText.Split(new char[] { '-', ':' }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (string s in dayArr2)
                            {
                                if (s.ToLower().Contains("day") && s.ToLower().Replace("day", null).Replace(" ", null).Length == 3)
                                {
                                    int _i = 0;
                                    if (Int32.TryParse(s.ToLower().Replace("day", null), out _i))
                                    {
                                        _dayVideoLink = HandmadeNetworkUrlHelper.BuildDayEpisodeUrl(_i);
                                    }
                                }
                                else
                                {
                                    _dayVideoTitle = s;
                                }
                            }

                            _dayYouTubeUrl = await GetDayYouTubeVideoUrl(_dayVideoLink);
                            SaveVideoOnDisk(_dayYouTubeUrl, outputDirectoryPath);

                            _dayVideoLink = null;
                            _dayVideoTitle = null;
                            _dayYouTubeUrl = null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occured");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                html = null;
                doc = null;
                nodes = null;
                tmpDoc = null;
                weekTitle = null;
            }
        }

        #region Internals

        /// <summary>
        /// Get the youtube url of a video from his hero handmade network episode page url
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        async Task<string> GetDayYouTubeVideoUrl(string url)
        {
            string html = null, videoId = null;
            HtmlDocument doc = new HtmlDocument();

            try
            {
                html = await NetRequestHelper.TryGetUrlHtmlContent(url);
                doc.LoadHtml(html);

                videoId = doc.DocumentNode.SelectSingleNode("//div[@id='player-wrapper']/div[@id='player']").Attributes["data-video-id"].Value;
                if (string.IsNullOrWhiteSpace(videoId))
                    return null;

                return YouTubeConstants.VideoBaseUrl + videoId;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                html = null;
                doc = null;
                videoId = null;
            }
        }

        /// <summary>
        /// Save YouTube video
        /// </summary>
        /// <param name="url">video YouTube url</param>
        /// <param name="path">absolute path of where save this video</param>
        /// <returns>operation completed or not</returns>
        bool SaveVideoOnDisk(string url, string path)
        {
            YouTube youTube = YouTube.Default;
            YouTubeVideo video = null;

            try
            {
                video = youTube.GetVideo(url);
                if (File.Exists(Path.Combine(path, video.FullName)))
                {
                    Console.WriteLine("The file '" + Path.Combine(path, video.FullName) + "' already exists. Operation canceled.");
                    return false;
                }
                File.WriteAllBytes(Path.Combine(path, video.FullName), video.GetBytes());
                return true;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                youTube = null;
                video = null;
            }
        }

        /// <summary>
        /// Write files explained about the rights, the credits and some other stuff about Casey Muratori
        /// </summary>
        /// <param name="path">absolute path of directory to write files in</param>
        void WriteCreditsFiles(string path)
        {
            using (StreamWriter sw = File.CreateText(Path.Combine(path, "copyright.txt")))
            {
                sw.WriteLine("This project is under the licence 'Handmade Hero License'");
                sw.WriteLine("Property rights owner : Casey Muratori");
                sw.WriteLine("Casey Muratori's twitter account : @cmuratori");
                sw.WriteLine("The original url of the project is https://handmadehero.org/ (" + HandmadeNetworkConstants.HomepageUrl + " on Handmade Network). You also can follow the project's twitter account : @handmade_hero");
            }

            using (StreamWriter sw = File.CreateText(Path.Combine(path, "readme.txt")))
            {
                sw.WriteLine("The only purpose of this little executable is to allow you to download some videos if you want to continue learning C++ in holidays or to download all the videos for your personal archives but PLEASE WATCH THEM ON THE OFFICIAL NETWORKS as long as you can because it's the way that give some money to @cmuratori and he's deserves it so much !");
                sw.WriteLine();
                sw.WriteLine("If you like this videos, you can contribute to this amazing work on Patreon : https://patreon.com/cmuratori");
            }

            using (StreamWriter sw = File.CreateText(Path.Combine(path, "references.txt")))
            {
                sw.WriteLine("----------------------");
                sw.WriteLine("Casey Muratori");
                sw.WriteLine("----------------------");
                sw.WriteLine("https://patreon.com/cmuratori");
                sw.WriteLine("@cmuratori");
                sw.WriteLine();
                sw.WriteLine();
                sw.WriteLine("----------------------");
                sw.WriteLine("Handmade Hero");
                sw.WriteLine("----------------------");
                sw.WriteLine("https://handmadehero.org/");
                sw.WriteLine("@handmade_hero");
                sw.WriteLine("https://www.twitch.tv/handmade_hero");
                sw.WriteLine();
                sw.WriteLine();
                sw.WriteLine("----------------------");
                sw.WriteLine("Handmade Network");
                sw.WriteLine("----------------------");
                sw.WriteLine(HandmadeNetworkConstants.HomepageUrl);
            }
        }

        #endregion
    }
}