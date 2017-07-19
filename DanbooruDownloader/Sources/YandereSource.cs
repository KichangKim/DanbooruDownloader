using System;
using System.Collections.Generic;
using System.Text;

namespace DanbooruDownloader.Sources
{
    [SourceMetadata("yandere")]
    class YandereSource : DownloadSource
    {
        protected override string PostsApiUrl => "https://yande.re/post.json";

        protected override string TagsPropertyName => "tags";

        protected override string ToCompleteImageUrl(string imageUrlFromMetadata)
        {
            return imageUrlFromMetadata;
        }

        protected override DateTime ToDateTime(string timeFromMetadata)
        {
            return this.UnixTimeStampToDateTime(double.Parse(timeFromMetadata));
        }

        DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }
    }
}
