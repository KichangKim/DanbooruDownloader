using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
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

        protected abstract DateTime ToDateTime(string timeFromMetadata);
        
        public async Task Run(string query, string outputPath, int limit, bool recalculateHash)
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
                Console.WriteLine($"Downloading metadata ... (page {page})");
                JObject[] imageMetadatas = await this.GetPosts(query, page, limit);

                int currentIndex = 0;

                foreach (JObject imageMetadata in imageMetadatas)
                {
                    Console.WriteLine();
                    currentIndex++;
                    try
                    {
                        string id = imageMetadata.GetValue("id").ToString();
                        string tags = imageMetadata.GetValue(this.TagsPropertyName).ToString();
                        string md5Hash = imageMetadata.GetValue("md5").ToString().ToLower();
                        string imageUrl = this.ToCompleteImageUrl(imageMetadata.GetValue("file_url").ToString());
                        string imageFilePath = Path.Combine(outputPath, $"{md5Hash}.{imageMetadata.GetValue("file_ext")}");
                        string metadataFilePath = Path.Combine(metadataOutputPath, $"{md5Hash}-{this.Name}-{id}.json");
                        DateTime createDateTime = this.ToDateTime(imageMetadata.GetValue("created_at").ToString());
                        DateTime updateDateTime = this.ToDateTime(imageMetadata.GetValue("updated_at").ToString());

                        if (updateDateTime < createDateTime)
                        {
                            updateDateTime = createDateTime;
                        }

                        Console.WriteLine($"({currentIndex}/{imageMetadatas.Length}) (Page {page})");
                        Console.WriteLine($"Id     : {id}");
                        Console.WriteLine($"Create : {createDateTime}");
                        Console.WriteLine($"Update : {updateDateTime}");
                        Console.WriteLine($"MD5    : {md5Hash}");
                        Console.WriteLine($"Tags   : {tags}");

                        bool shouldDownload = false;

                        if (File.Exists(metadataFilePath) && this.IsMetadataUpdated(imageMetadata, metadataFilePath))
                        {
                            shouldDownload = true;
                        }

                        File.WriteAllText(metadataFilePath, imageMetadata.ToString());

                        if (!shouldDownload && !File.Exists(imageFilePath))
                        {
                            shouldDownload = true;
                        }

                        if (!shouldDownload && File.Exists(imageFilePath))
                        {
                            if (recalculateHash)
                            {
                                string storedMd5Hash = this.GetMd5Hash(imageFilePath);

                                if (md5Hash != storedMd5Hash)
                                {
                                    Console.WriteLine($"MD5 Hash is different. We'll download this. : {md5Hash} (new) /= {storedMd5Hash} (old)");
                                    shouldDownload = true;
                                }
                                else
                                {
                                    Console.WriteLine("MD5 hash is same. We'll skip this.");
                                }
                            }
                        }
                        
                        if (shouldDownload)
                        {
                            Console.WriteLine($"Downloading ... ");
                            await this.Download(imageUrl, imageFilePath);

                            string downloadedMd5Hash = this.GetMd5Hash(imageFilePath);

                            if (md5Hash != downloadedMd5Hash)
                            {
                                Console.WriteLine($"Difference MD5 hash between metadata and downloaded image. We'll delete this. : {md5Hash} /= {downloadedMd5Hash}");
                                File.Delete(imageFilePath);
                                continue;
                            }

                            
                            this.ChageFileTime(imageFilePath, createDateTime, updateDateTime);

                            totalDownloadCount++;
                        }
                        else
                        {
                            Console.WriteLine($"Skipping ...");
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
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

        string GetMd5Hash(string path)
        {
            using (MD5 md5 = MD5.Create())
            {
                using (FileStream stream = new FileStream(path, FileMode.Open))
                {
                    byte[] hashBytes = md5.ComputeHash(stream);

                    return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                }
            }
        }

        void ChageFileTime(string path, DateTime createTime, DateTime updateTime)
        {
            File.SetCreationTime(path, createTime);
            File.SetLastWriteTime(path, updateTime);
        }
    }
}
