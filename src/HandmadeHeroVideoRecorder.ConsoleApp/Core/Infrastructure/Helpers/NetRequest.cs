namespace HandmadeHeroVideoRecorder.ConsoleApp.Core.Infrastructure.Helpers
{
    using System;
    using System.IO;
    using System.Net;
    using System.Threading.Tasks;

    /// <summary>
    /// Generic methods to help dealing with net requests
    /// </summary>
    internal static class NetRequestHelper
    {
        /// <summary>
        /// Get html content from an HTTP net request
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static async Task<string> TryGetUrlHtmlContent(string url)
        {
            HttpWebRequest req;

            try
            {
                req = HttpWebRequest.Create(url) as HttpWebRequest;
                req.Method = "GET";
                req.ContentType = "text/html";

                using (WebResponse wr = await req.GetResponseAsync())
                {
                    using (Stream respStream = wr.GetResponseStream())
                    {
                        using (StreamReader reader = new StreamReader(respStream))
                        {
                            return reader.ReadToEnd().Trim();
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                req = null;
            }
        }
    }
}