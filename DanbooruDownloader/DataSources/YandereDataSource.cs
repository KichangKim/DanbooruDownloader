using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace DanbooruDownloader.DataSources
{
    [DataSourceInfo(Name = "yandere")]
    public class YandereDataSource : DataSource
    {
        protected override string GetDumpUrl(long startId)
        {
            throw new NotImplementedException();
        }

        protected override string GetPostsUrl(string query, long page)
        {
            throw new NotImplementedException();
        }

        protected override Post ToPost(JObject jsonObject)
        {
            throw new NotImplementedException();
        }
    }
}
