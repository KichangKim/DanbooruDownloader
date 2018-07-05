using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;

namespace DanbooruDownloader.DataSources
{
    [DataSourceInfo(Name = "yandere")]
    public class YandereDataSource : DataSource
    {
        protected override string GetDumpUrl(long startId)
        {
            string query = $"id:>={startId} order:id";
            string urlEncodedQuery = WebUtility.UrlEncode(query);
            return $"https://yande.re/post.json?tags={urlEncodedQuery}&page=1&limit=1000";
        }

        protected override string GetPostsUrl(string query, long page)
        {
            string urlEncodedQuery = WebUtility.UrlEncode(query);

            return $"https://yande.re/post.json?tags={urlEncodedQuery}&page={page}&limit=1000";
        }

        protected override Post ToPost(JObject jsonObject)
        {
            Post post = new Post
            {
                Id = jsonObject.GetValue("id").ToString(),
                Md5 = jsonObject.GetValue("md5")?.ToString() ?? "",
                ImageUrl = jsonObject.GetValue("file_url")?.ToString(),
                Tags = jsonObject.GetValue("tags").ToString(),
                Extension = jsonObject.GetValue("file_ext")?.ToString() ?? "",
                CreatedDate = DateTimeEx.FromUnixTimeStamp(double.Parse(jsonObject.GetValue("created_at").ToString())),
                UpdatedDate = jsonObject.GetValue("updated_at") != null ? DateTimeEx.FromUnixTimeStamp(double.Parse(jsonObject.GetValue("updated_at").ToString())) : DateTimeEx.FromUnixTimeStamp(double.Parse(jsonObject.GetValue("created_at").ToString())),
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
