using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DanbooruDownloader.Sources
{
    [SourceMetadata("danbooru", true)]
    class DanbooruSource : DownloadSource
    {
        public override async Task Run(string query, string outputPath)
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
                int downloadedCount = 0;

                JObject[] imageMetadatas = await this.GetImages(query, page);
                
                foreach (JObject imageMetadata in imageMetadatas)
                {
                    try
                    {
                        string id = imageMetadata.GetValue("data-id").ToString();
                        string tags = imageMetadata.GetValue("data-tags").ToString();
                        string imageUrl = $"http://danbooru.donmai.us{imageMetadata.GetValue("data-file-url")}";
                        string imageFilePath = Path.Combine(outputPath, $"{id}.{imageMetadata.GetValue("data-file-ext")}");
                        string metadataFilePath = Path.Combine(metadataOutputPath, $"{id}.json");

                        if (!File.Exists(imageFilePath))
                        {
                            Console.WriteLine($"Page {page} : Downloading {id} {tags}...");
                            await this.Download(imageUrl, imageFilePath);
                            File.WriteAllText(metadataFilePath, imageMetadata.ToString());
                            downloadedCount++;
                            await Task.Delay(1000);
                        }
                        else
                        {
                            Console.WriteLine($"Page {page} : Skipping {id} ...");
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }

                totalDownloadCount += downloadedCount;

                if (downloadedCount > 0)
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

        async Task<JObject[]> GetImages(string query, int page)
        {
            HtmlWeb web = new HtmlWeb();
            HtmlDocument document = await web.LoadFromWebAsync(this.GetUrl(query, page));

            return document.DocumentNode.Descendants("article").Select(node =>
            {
                JObject jsonObject = new JObject();

                foreach (HtmlAttribute attribute in node.Attributes)
                {
                    if (attribute.Name.StartsWith("data-"))
                    {
                        jsonObject.Add(attribute.Name, attribute.Value);
                    }
                }

                return jsonObject;
            })
            .ToArray();
        }

        string GetUrl(string query, int page)
        {
            return $"https://danbooru.donmai.us/posts?tags={WebUtility.UrlEncode(query)}&page={page}";
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
    }
}
