using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DanbooruDownloader.Utilities
{
    public static class PathUtility
    {
        public static void CreateDirectoryIfNotExists(params string[] paths)
        {
            foreach (string path in paths)
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
        }

        public static void ChangeFileTimestamp(string path, DateTime createTime, DateTime updateTime)
        {
            File.SetCreationTime(path, createTime);
            File.SetLastWriteTime(path, updateTime);
        }
    }
}
