using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DanbooruDownloader.Sources
{
    public abstract class DownloadSource
    {
        public string Name { get; private set; }
        public bool IsDefault { get; private set; }

        public abstract Task Run(string query, string outputPath);

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
