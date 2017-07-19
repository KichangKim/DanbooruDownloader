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
    }
}
