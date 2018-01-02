using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DanbooruDownloader
{
    public static class FileEx
    {
        public static void ChangeFileTimestamp(string path, DateTime createTime, DateTime updateTime)
        {
            File.SetCreationTime(path, createTime);
            File.SetLastWriteTime(path, updateTime);
        }
    }
}
