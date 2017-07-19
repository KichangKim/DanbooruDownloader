using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DanbooruDownloader.Sources
{
    public abstract class DownloadSource
    {
        public string Name { get; private set; }
        public bool IsDefault { get; private set; }

        protected abstract string PostsApiUrl{get;}

        protected abstract string TagsPropertyName { get; }

        protected abstract string ToCompleteImageUrl(string imageUrlFromMetadata);
        
        public async Task Run(string query, string outputPath, int limit)
        {
            string metadataOutputPath = Path.Combine(outputPath, "metadata");

            if (!Directory.Exists(metadataOutputPath))
            {
                Directory.CreateDirectory(metadataOutputPath);
            }

            int page = 1;
            int totalDownloadCount = 0;
            while (true)
            {
                JObject[] imageMetadatas = await this.GetPosts(query, page, limit);

                int currentIndex = 1;

                foreach (JObject imageMetadata in imageMetadatas)
                {
                    try
                    {
                        string id = imageMetadata.GetValue("id").ToString();
                        string tags = imageMetadata.GetValue(this.TagsPropertyName).ToString();
                        string imageUrl = this.ToCompleteImageUrl(imageMetadata.GetValue("file_url").ToString());
                        string imageFilePath = Path.Combine(outputPath, $"{id}.{imageMetadata.GetValue("file_ext")}");
                        string metadataFilePath = Path.Combine(metadataOutputPath, $"{id}.json");

                        if (!File.Exists(imageFilePath) || !File.Exists(metadataFilePath) || this.IsMetadataUpdated(imageMetadata, metadataFilePath))
                        {
                            Console.WriteLine($"Downloading {id} ... (Page {page}, {currentIndex}/{imageMetadatas.Length}){Environment.NewLine}{tags}");
                            await this.Download(imageUrl, imageFilePath);
                            File.WriteAllText(metadataFilePath, imageMetadata.ToString());
                            totalDownloadCount++;
                            await Task.Delay(1000);
                        }
                        else
                        {
                            Console.WriteLine($"Skipping {id} ... (Page {page}, {currentIndex}/{imageMetadatas.Length})");
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                    currentIndex++;
                }

                if (imageMetadatas.Length > 0)
                {
                    await Task.Delay(1000);
                    page++;
                }
                else
                {
                    break;
                }
            }

            Console.WriteLine($"Total {totalDownloadCount} images are downloaded.");
        }

        private bool IsMetadataUpdated(JObject imageMetadata, string metadataFilePath)
        {
            JObject storedJsonObject = JObject.Parse(File.ReadAllText(metadataFilePath));

            string storedUpdateTime = storedJsonObject.GetValue("updated_at")?.ToString();
            string updateTime = imageMetadata.GetValue("updated_at")?.ToString();

            if (string.IsNullOrEmpty(storedUpdateTime) || string.IsNullOrEmpty(updateTime) || storedUpdateTime != updateTime)
            {
                return true;
            }

            return false;
        }

        async Task<JObject[]> GetPosts(string query, int page, int limit)
        {
            using (HttpClient client = new HttpClient())
            {
                string jsonString = await client.GetStringAsync(this.GetPostsUrl(query, page, limit));

                JArray jsonArray = JArray.Parse(jsonString);

                return jsonArray.Cast<JObject>().ToArray();
            }
        }

        string GetPostsUrl(string query, int page, int limit)
        {
            return $"{this.PostsApiUrl}?page={page}&limit={limit}&tags={WebUtility.UrlEncode(query)}";
        }

        async Task Download(string uri, string path)
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);

                response.EnsureSuccessStatusCode();
                
                using (FileStream fileStream = File.Create(path))
                {
                    using (Stream httpStream = await response.Content.ReadAsStreamAsync())
                    {
                        httpStream.CopyTo(fileStream);
                        fileStream.Flush();
                    }
                }
            }
        }
        
        public static IEnumerable<DownloadSource> GetAllSources()
        {
            List<DownloadSource> sources = new List<DownloadSource>();

            foreach (Type type in Assembly.GetEntryAssembly().GetTypes().Where(type =>
            {
                TypeInfo info = type.GetTypeInfo();
                return info.IsClass && !info.IsAbstract && info.IsSubclassOf(typeof(DownloadSource));
            }))
            {
                TypeInfo info = type.GetTypeInfo();

                SourceMetadataAttribute attribute = info.GetCustomAttribute<SourceMetadataAttribute>();

                if (attribute != null)
                {
                    if (Activator.CreateInstance(type) is DownloadSource command)
                    {
                        command.Name = attribute.Name;
                        command.IsDefault = attribute.IsDefault;
                        sources.Add(command);
                    }
                }
            }

            return sources;
        }
    }
}
