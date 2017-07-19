using System;

namespace DanbooruDownloader.Sources
{
    [SourceMetadata("danbooru", true)]
    class DanbooruSource : DownloadSource
    {
        protected override string PostsApiUrl => "https://danbooru.donmai.us/posts.json";

        protected override string TagsPropertyName => "tag_string";

        protected override string ToCompleteImageUrl(string imageUrlFromMetadata)
        {
            return $"https://danbooru.donmai.us{imageUrlFromMetadata}";
        }
    }
}
