using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DanbooruDownloader.Utilities
{
    public static class DanbooruUtility
    {
        public static string GetPostsUrl(long startId)
        {
            string query = $"id:>={startId} order:id_asc";
            string urlEncodedQuery = WebUtility.UrlEncode(query);
            return $"https://danbooru.donmai.us/posts.json?tags={urlEncodedQuery}&page=1&limit=1000";
        }

        public static async Task<JObject[]> GetPosts(long startId)
        {
            using (HttpClient client = new HttpClient())
            {
                string url = GetPostsUrl(startId);
                string jsonString = await client.GetStringAsync(url);

                JArray jsonArray = JArray.Parse(jsonString);

                return jsonArray.Cast<JObject>().ToArray();
            }
        }
    }
}
