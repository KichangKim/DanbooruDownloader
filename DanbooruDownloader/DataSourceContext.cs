using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DanbooruDownloader
{
    public class DataSourceContext
    {
        public string SourceName;
        public string OutputPath;
        public bool IgnoreHashCalculation;
        public DataSourceMode Mode;

        public string QueryForTag;
        public long StartPageForTag;
        public long CurrentPageForTag;

        public long StartIdForDump;
        public long CurrentIdForDump;

        public readonly int RetryCount = 5;
        public readonly int RetryDelay = 10000;
        public readonly int EmptyPageCountThreshold = 2;
        public readonly HashSet<string> AllowedExtensions = new HashSet<string>()
        {
            "jpeg",
            "jpg",
            "png",
            "gif",
        };

        public string TemporaryFolderPath
        {
            get
            {
                return Path.Combine(this.OutputPath, "_temp");
            }
        }

        public string MetadataRootFolderPath
        {
            get
            {
                return Path.Combine(this.OutputPath, "_metadata");
            }
        }

        public string MetadataJsonFolderPath
        {
            get
            {
                return Path.Combine(this.OutputPath, "_metadata", "json");
            }
        }

        public void Initialize()
        {
            this.CurrentIdForDump = this.StartIdForDump;
            this.CurrentPageForTag = this.StartPageForTag;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            
            builder.AppendLine($"{nameof(this.SourceName)} : {this.SourceName}");
            builder.AppendLine($"{nameof(this.IgnoreHashCalculation)} : {this.IgnoreHashCalculation}");
            builder.AppendLine($"{nameof(this.Mode)} : {this.Mode}");
            builder.AppendLine($"{nameof(this.QueryForTag)} : {this.QueryForTag}");
            builder.AppendLine($"{nameof(this.StartPageForTag)} : {this.StartPageForTag}");
            builder.AppendLine($"{nameof(this.CurrentPageForTag)} : {this.CurrentPageForTag}");
            builder.AppendLine($"{nameof(this.StartIdForDump)} : {this.StartIdForDump}");
            builder.AppendLine($"{nameof(this.CurrentIdForDump)} : {this.CurrentIdForDump}");

            return builder.ToString();
        }
    }
}
