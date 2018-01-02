using System;
using System.Collections.Generic;
using System.Text;

namespace DanbooruDownloader
{
    public static class DateTimeEx
    {
        public static DateTime FromUnixTimeStamp(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }
    }
}
