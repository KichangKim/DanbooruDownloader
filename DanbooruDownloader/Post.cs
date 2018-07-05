using System;
using System.Collections.Generic;
using System.Text;

namespace DanbooruDownloader
{
    public class Post
    {
        public string Id;
        public string Extension;
        public string Tags;
        public string Md5;
        public DateTime CreatedDate;
        public DateTime UpdatedDate;
        public string JsonString;
        public string ImageUrl;

        public bool IsPending;
        public bool IsDeleted;

        public bool ShouldDownloadImage;
        public bool ShouldUpdateImage;
        public bool ShouldSaveMetadata;
        public bool IsValid;
    }
}
