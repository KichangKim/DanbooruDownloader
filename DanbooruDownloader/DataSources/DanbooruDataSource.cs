using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;

namespace DanbooruDownloader.DataSources
{
    [DataSourceInfo(Name = "danbooru", IsDefault = true)]
    public class DanbooruDataSource : DataSource
    {
        protected override string GetDumpUrl(long startId)
        {
            string query = $"id:>={startId} order:id_asc";
            string urlEncodedQuery = WebUtility.UrlEncode(query);
            return $"https://danbooru.donmai.us/posts.json?tags={urlEncodedQuery}&page=1&limit=1000";
        }

        protected override string GetPostsUrl(string query, long page)
        {
            string urlEncodedQuery = WebUtility.UrlEncode(query);
            return $"https://danbooru.donmai.us/posts.json?tags={urlEncodedQuery}&page={page}&limit=1000";
        }

        protected override Post ToPost(JObject jsonObject)
        {
            Post post = new Post
            {
                Id = jsonObject.GetValue("id").ToString(),
                Md5 = jsonObject.GetValue("md5")?.ToString() ?? "",
                ImageUrl = jsonObject.GetValue("file_url")?.ToString() ?? "",
                Tags = jsonObject.GetValue("tag_string").ToString(),
                GeneralTagCount = jsonObject.GetValue("tag_count_general")?.ToObject<int>() ?? 0,
                Extension = jsonObject.GetValue("file_ext")?.ToString() ?? "",
                CreatedDate = DateTime.Parse(jsonObject.GetValue("created_at").ToString()),
                UpdatedDate = jsonObject.GetValue("updated_at") != null ? DateTime.Parse(jsonObject.GetValue("updated_at").ToString()) : DateTime.Parse(jsonObject.GetValue("created_at").ToString()),
                JsonString = jsonObject.ToString(),
                IsPending = jsonObject.GetValue("is_pending")?.ToObject<bool>() ?? false,
                IsDeleted = jsonObject.GetValue("is_deleted")?.ToObject<bool>() ?? false,
            };
            
            if (post.UpdatedDate < post.CreatedDate)
            {
                post.UpdatedDate = post.CreatedDate;
            }

            return post;
        }
    }
}
