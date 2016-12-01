namespace HandmadeHeroVideoRecorder.ConsoleApp.Core.Infrastructure.Helpers
{
    using Constants;

    /// <summary>
    /// Generic methods to help dealing with the Hero Handmade Network website
    /// </summary>
    public static class HandmadeNetworkUrlHelper
    {
        /// <summary>
        /// Build the url of an epidoside of Hero Handmade Network
        /// </summary>
        /// <param name="day"></param>
        /// <returns></returns>
        public static string BuildDayEpisodeUrl(int day)
        {
            if (day <= 0)
                return null;

            string s = day.ToString();
            if (string.IsNullOrWhiteSpace(s))
                return null;

            if (s.Length > 3)
                return null;

            while (s.Length < 3)
                s = "0" + s;

            return HandmadeNetworkConstants.DayEpisodeBaseUrl + s;
        }
    }
}