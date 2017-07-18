using System;
using System.Collections.Generic;
using System.Text;

namespace DanbooruDownloader
{
    class SourceMetadataAttribute : Attribute
    {
        public string Name { get; private set; }
        public bool IsDefault { get; private set; }

        public SourceMetadataAttribute(string name, bool isDefault = false)
        {
            this.Name = name;
            this.IsDefault = isDefault;
        }
    }
}
