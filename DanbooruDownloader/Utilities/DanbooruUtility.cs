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
            string urlEncodedUsername = WebUtility.UrlEncode(username);
            string urlEncodedApikey = WebUtility.UrlEncode(apikey);
            string urlstring = $"tags={urlEncodedQuery}&limit=1000&page=1&api_key={urlEncodedApikey}&login={urlEncodedUsername}";
            return $"https://danbooru.donmai.us/posts.json?{urlstring}";
        }

        public static async Task<JObject[]> GetPosts(long startId, string username, string apikey)
        {
            using (HttpClient client = new HttpClient())
            {
                string url = GetPostsUrl(startId, username, apikey);
				client.DefaultRequestHeaders.Add("User-Agent", "C# App");
                string jsonString = await client.GetStringAsync(url);

                JArray jsonArray = JArray.Parse(jsonString);

                return jsonArray.Cast<JObject>().ToArray();
            }
        }
    }
}
