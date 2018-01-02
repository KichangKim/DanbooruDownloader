using System;
using System.Collections.Generic;
using System.Text;

namespace DanbooruDownloader
{
    public class DataSourceInfoAttribute : Attribute
    {
        public string Name;
        public bool IsDefault;
    }
}
