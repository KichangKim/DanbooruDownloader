using Newtonsoft.Json.Linq;
using NLog;
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

namespace DanbooruDownloader
{
    public abstract class DataSource : IDisposable
    {
        static Logger logger = LogManager.GetCurrentClassLogger();

        class NotRetryableException : Exception { }

        public string Name { get; private set; }
        public bool IsDefault { get; private set; }

        protected abstract string GetDumpUrl(long startId);
        protected abstract string GetPostsUrl(string query, long page);

        protected abstract Post ToPost(JObject jsonObject);
            
        public async Task Run(DataSourceContext context)
        {
            context.Initialize();

            this.ActionByMode(context, tagAction: c =>
            {
                DirectoryEx.CreateDirectoryIfNotExists(
                    c.OutputPath,
                    c.MetadataRootFolderPath,
                    c.MetadataJsonFolderPath,
                    c.TemporaryFolderPath
                );
            },
            dumpAction: c =>
            {
                DirectoryEx.CreateDirectoryIfNotExists(
                    c.OutputPath,
                    c.MetadataRootFolderPath,
                    c.TemporaryFolderPath
                );
            });
            
            using (MetadataStorage metadataStorage = new MetadataStorage(Path.Combine(context.MetadataRootFolderPath, $"{this.Name}.sqlite")))
            {
                long emptyPageCount = 0;

                while (true)
                {
                    string postsUrl = this.GetCurrentPostsUrl(context);

                    JObject[] postsAsJson = null;

                    for (int metadataRetryCount = 0; metadataRetryCount < context.RetryCount; metadataRetryCount++)
                    {
                        try
                        {
                            this.ActionByMode(context,
                                tagAction: c =>
                                {
                                    logger.Info($"Downloading metadata (page {c.CurrentPageForTag}) ...");
                                },
                                dumpAction: c =>
                                {
                                    logger.Info($"Downloading metadata (Id {c.CurrentIdForDump} ~) ...");
                                });

                            postsAsJson = await this.GetJsonObjectsFromUrl(postsUrl);

                            if (postsAsJson == null)
                            {
                                throw new InvalidDataException();
                            }
                        }
                        catch (Exception e)
                        {
                            logger.Error(e, "An error occured.");
                            await Task.Delay(context.RetryDelay);
                            continue;
                        }

                        break;
                    }

                    if (postsAsJson.Length == 0)
                    {
                        emptyPageCount++;

                        if (emptyPageCount >= context.EmptyPageCountThreshold)
                        {
                            logger.Info($"There are multiple empty posts. We'll exit program.");
                            break;
                        }

                        await Task.Delay(context.RetryDelay);
                        continue;
                    }
                    else
                    {
                        emptyPageCount = 0;
                    }

                    Post[] posts = postsAsJson.Select(p => this.ToPost(p)).ToArray();

                    if (posts.Length > 0)
                    {
                        logger.Info($"Checking {posts.Length} posts ...");
                    }
                    else
                    {
                        logger.Info("Empty posts.");
                    }

                    Parallel.ForEach(posts, post =>
                    {
                        if (string.IsNullOrEmpty(post.Md5))
                        {
                            logger.Debug($"Skip for empty MD5 : Id={post.Id}");
                            return;
                        }

                        if (string.IsNullOrEmpty(post.ImageUrl))
                        {
                            logger.Debug($"Skip for empty image URL : Id={post.Id}");
                            return;
                        }

                        if (post.IsPending || post.IsDeleted)
                        {
                            return;
                        }
                        
                        post.IsValid = true;

                        string metadataPath = Path.Combine(this.GetMetadataBaseFolderPath(context, post), this.GetMetadataJsonFileName(context, post));
                        
                        try
                        {
                            if (File.Exists(metadataPath))
                            {
                                Post storedPost = this.ToPost(JObject.Parse(File.ReadAllText(metadataPath)));

                                if (post.UpdatedDate > storedPost.UpdatedDate)
                                {
                                    post.ShouldSaveMetadata = true;
                                    post.ShouldUpdateImage = true;
                                }
                            }
                            else
                            {
                                post.ShouldSaveMetadata = true;
                                post.ShouldUpdateImage = true;
                            }
                        }
                        catch (Exception e)
                        {
                            logger.Error(e, $"An error occured when parsing metadata : Id={post.Id}, MD5={post.Md5}");
                            post.ShouldSaveMetadata = true;
                        }

                        string storedImagePath = Path.Combine(this.GetImageBaseFolderPath(context, post), this.GetImageFileName(context, post));

                        if (!File.Exists(storedImagePath))
                        {
                            post.ShouldDownloadImage = true;
                            post.ShouldUpdateImage = true;
                            return;
                        }

                        if (!context.IgnoreHashCalculation)
                        {
                            string storedImageMd5 = this.GetMd5Hash(storedImagePath);

                            if (post.Md5 != storedImageMd5)
                            {
                                post.ShouldDownloadImage = true;
                                post.ShouldUpdateImage = true;
                                logger.Warn($"MD5 is different : Id={post.Id}, {post.Md5} (new) != {storedImageMd5} (stored)");
                                return;
                            }
                        }
                    });

                    int shouldDownloadCount = posts.Where(p => p.ShouldDownloadImage).Count();
                    int shouldUpdateCount = posts.Where(p => p.ShouldUpdateImage).Count();
                    int pendingCount = posts.Where(p => p.IsPending).Count();

                    if (posts.Length > 0)
                    {
                        if (shouldDownloadCount > 0 || shouldUpdateCount > 0)
                        {
                            logger.Info($"{shouldUpdateCount} of {posts.Length} posts are updated. Downloading {shouldDownloadCount} posts ...");
                        }
                        else
                        {
                            logger.Info("There are no updated posts.");
                        }

                        if (pendingCount > 0)
                        {
                            logger.Info($"{pendingCount} posts are pending.");
                        }
                    }

                    Post lastPost = null;

                    foreach (Post post in posts)
                    {
                        if (post.ShouldDownloadImage || post.ShouldSaveMetadata || post.ShouldUpdateImage)
                        {
                            string imageFileName = this.GetImageFileName(context, post);
                            string imageBaseFolderPath = this.GetImageBaseFolderPath(context, post);
                            string imageTemporaryPath = Path.Combine(context.TemporaryFolderPath, imageFileName);
                            string imageCommitPath = Path.Combine(imageBaseFolderPath, imageFileName);
                            string metadataPath = Path.Combine(this.GetMetadataBaseFolderPath(context, post), this.GetMetadataJsonFileName(context, post));

                            DirectoryEx.CreateDirectoryIfNotExists(imageBaseFolderPath);

                            int retryCount = 0;

                            while (true)
                            {
                                try
                                {
                                    if (post.ShouldDownloadImage)
                                    {
                                        logger.Info($"Downloading post {post.Id} ...");
                                        await this.Download(post.ImageUrl, imageTemporaryPath);
                                        string downloadedMd5 = this.GetMd5Hash(imageTemporaryPath);

                                        if (downloadedMd5 != post.Md5)
                                        {
                                            logger.Warn($"MD5 hash of downloaded image is different : Id={post.Id}, {post.Md5} (metadata) != {downloadedMd5} (downloaded)");
                                            try
                                            {
                                                File.Delete(imageTemporaryPath);
                                            }
                                            finally { }

                                            try
                                            {
                                                File.Delete(metadataPath);
                                            }
                                            finally { }
                                            throw new Exception();
                                        }
                                        
                                        File.Delete(imageCommitPath);
                                        File.Move(imageTemporaryPath, imageCommitPath);
                                    }

                                    if (post.ShouldDownloadImage || post.ShouldUpdateImage)
                                    {
                                        FileEx.ChangeFileTimestamp(imageCommitPath, post.CreatedDate, post.UpdatedDate);
                                    }

                                    if (post.ShouldSaveMetadata)
                                    {
                                        File.WriteAllText(metadataPath, post.JsonString);
                                    }
                                    
                                    break;
                                }
                                catch (NotRetryableException)
                                {
                                    logger.Error($"Can't retryable exception was occured : Id={post.Id}");
                                    post.IsValid = false;
                                    break;
                                }
                                catch (Exception e)
                                {
                                    logger.Error(e, $"An error occured when downloading. We'll retry it : Id={post.Id}");
                                    retryCount++;

                                    if (retryCount < context.RetryCount)
                                    {
                                        await Task.Delay(context.RetryDelay);
                                        continue;
                                    }
                                    else
                                    {
                                        post.IsValid = false;
                                        break;
                                    }
                                }
                            }
                        }

                        lastPost = post;
                    }

                    if (posts.Length > 0)
                    {
                        logger.Info("Updating database ...");
                        metadataStorage.InsertOrReplace(posts.Where(p => p.IsValid));
                    }

                    this.UpdateContextToNext(context, lastPost);
                }
            }

            try
            {
                Directory.Delete(context.TemporaryFolderPath, true);
                logger.Info("All process are complete.");
            }
            catch (Exception e)
            {
                logger.Error(e, "An error occured.");
            }
        }

        private void UpdateContextToNext(DataSourceContext context, Post lastPost)
        {
            this.ActionByMode(context,
                tagAction: c =>
                {
                    c.CurrentPageForTag++;
                },
                dumpAction: c =>
                 {
                     if (lastPost != null)
                     {
                         c.CurrentIdForDump = long.Parse(lastPost.Id) + 1;

                         try
                         {
                             File.WriteAllText(Path.Combine(c.MetadataRootFolderPath, "last_post.json"), lastPost.JsonString);
                         }
                         finally { }
                     }
                     else
                     {
                         c.CurrentIdForDump++;
                     }
                 });

            try
            {
                File.WriteAllText(Path.Combine(context.MetadataRootFolderPath, "last_context.txt"), context.ToString());
            }
            finally { }
        }

        void ActionByMode(DataSourceContext context, Action<DataSourceContext> tagAction, Action<DataSourceContext> dumpAction)
        {
            switch (context.Mode)
            {
                case DataSourceMode.Tag:
                    {
                        tagAction?.Invoke(context);
                    }
                    break;
                case DataSourceMode.Dump:
                    {
                        dumpAction?.Invoke(context);
                    }
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        T FunctionByMode<T>(DataSourceContext context, Func<DataSourceContext, T> tagFunction, Func<DataSourceContext, T> dumpFunction)
        {
            switch (context.Mode)
            {
                case DataSourceMode.Tag:
                    {
                        return tagFunction.Invoke(context);
                    }
                case DataSourceMode.Dump:
                    {
                        return dumpFunction.Invoke(context);
                    }
                default:
                    throw new NotSupportedException();
            }
        }
        
        public string GetCurrentPostsUrl(DataSourceContext context)
        {
            return this.FunctionByMode(context,
                tagFunction: c =>
                 {
                     return this.GetPostsUrl(c.QueryForTag, c.CurrentPageForTag);
                 },
                dumpFunction: c =>
                {
                    return this.GetDumpUrl(c.CurrentIdForDump);
                });
        }

        public string GetMetadataJsonFileName(DataSourceContext context, Post post)
        {
            return this.FunctionByMode(context,
                tagFunction: c =>
                 {
                     return $"{post.Md5}-{this.Name}-{post.Id}.json";
                 },
                dumpFunction: c =>
                 {
                     return $"{post.Md5}-{this.Name}.json";
                 });
        }

        public string GetMetadataBaseFolderPath(DataSourceContext context, Post post)
        {
            return this.FunctionByMode(context,
                tagFunction: c =>
                {
                    return c.MetadataJsonFolderPath;
                },
                dumpFunction: c =>
                {
                    return Path.Combine(c.OutputPath, post.Md5.Substring(0, 2));
                });
        }

        public string GetImageFileName(DataSourceContext context, Post post)
        {
            return $"{post.Md5}.{post.Extension}";
        }

        public string GetImageBaseFolderPath(DataSourceContext context, Post post)
        {
            return this.FunctionByMode(context,
                tagFunction: c =>
                {
                    return c.OutputPath;
                },
                dumpFunction: c =>
                {
                    return Path.Combine(c.OutputPath, post.Md5.Substring(0, 2));
                });
        }

        async Task<JObject[]> GetJsonObjectsFromUrl(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                string jsonString = await client.GetStringAsync(url);

                JArray jsonArray = JArray.Parse(jsonString);

                return jsonArray.Cast<JObject>().ToArray();
            }
        }

        async Task Download(string uri, string path)
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);

                switch (response.StatusCode)
                {
                    case HttpStatusCode.Forbidden:
                    case HttpStatusCode.NotFound:
                        throw new NotRetryableException();
                }

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

        public static IDictionary<string, DataSource> GetAllAsDictionary()
        {
            Dictionary<string, DataSource> providers = new Dictionary<string, DataSource>();

            foreach (Type type in Assembly.GetEntryAssembly().GetTypes().Where(type =>
            {
                TypeInfo info = type.GetTypeInfo();
                return info.IsClass && !info.IsAbstract && info.IsSubclassOf(typeof(DataSource));
            }))
            {
                DataSourceInfoAttribute providerInfo = type.GetCustomAttributes(
                typeof(DataSourceInfoAttribute), true
                ).FirstOrDefault() as DataSourceInfoAttribute;

                if (providerInfo != null)
                {
                    if (Activator.CreateInstance(type) is DataSource provider)
                    {
                        provider.Name = providerInfo.Name;
                        provider.IsDefault = providerInfo.IsDefault;

                        providers.Add(providerInfo.Name, provider);
                    }
                }
            }

            return providers;
        }

        public void Dispose()
        {
        }
    }
}
