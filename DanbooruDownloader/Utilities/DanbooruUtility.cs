
using CloudflareSolverRe;
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
        public static string GetPostsUrl(long startId, string username, string apikey)
        {
            string query = $"id:>={startId} order:id_asc";
            string urlEncodedQuery = WebUtility.UrlEncode(query);
            return $"https://danbooru.donmai.us/posts.json?tags={urlEncodedQuery}&page=1&limit=1000&login={WebUtility.UrlEncode(username)}&api_key={WebUtility.UrlEncode(apikey)}";
        }

        public static async Task<JObject[]> GetPosts(long startId, string username, string apikey)
        {
            string url = GetPostsUrl(startId, username, apikey);
            var target = new Uri(url);
            var handler = new ClearanceHandler
            {
                MaxTries = 3,
                ClearanceDelay = 3000
            };

            using (HttpClient client = new HttpClient(handler))
            {
                string jsonString = await client.GetStringAsync(target);

                JArray jsonArray = JArray.Parse(jsonString);

                return jsonArray.Cast<JObject>().ToArray();
            }
        }
    }
}
