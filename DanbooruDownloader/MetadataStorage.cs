using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DanbooruDownloader
{
    public class MetadataStorage : IDisposable
    {
        SqliteConnection connection;

        public MetadataStorage(string path)
        {
            string sourceDirectoryPath = Path.GetDirectoryName(path);

            if (!Directory.Exists(sourceDirectoryPath))
            {
                Directory.CreateDirectory(sourceDirectoryPath);
            }

            this.connection = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = path,
            }.ToString());

            this.connection.Open();
            this.TryCreateTable();
        }

        private void TryCreateTable()
        {
            using (SqliteTransaction transaction = this.connection.BeginTransaction())
            {
                SqliteCommand command = this.connection.CreateCommand();

                command.Transaction = transaction;
                command.CommandText = @"
CREATE TABLE IF NOT EXISTS Images(
    Id INTEGER NOT NULL PRIMARY KEY,
    Md5 TEXT,
	Extension TEXT,
	Tags TEXT,
	GeneralTagCount INTEGER
)";
                command.ExecuteNonQuery();

                transaction.Commit();
            }
        }

        public void InsertOrReplace(IEnumerable<Post> posts)
        {
            using (SqliteTransaction transaction = this.connection.BeginTransaction())
            {
                foreach (Post post in posts)
                {
                    SqliteCommand command = this.connection.CreateCommand();

                    command.CommandText = @"
INSERT OR REPLACE INTO Images (Id, Md5, Extension, Tags, GeneralTagCount) VALUES
(@Id, @Md5, @Extension, @Tags, @GeneralTagCount)";

                    command.Parameters.AddWithValue("@Id", post.Id);
                    command.Parameters.AddWithValue("@Md5", post.Md5);
                    command.Parameters.AddWithValue("@Extension", post.Extension);
                    command.Parameters.AddWithValue("@Tags", post.Tags);
                    command.Parameters.AddWithValue("@GeneralTagCount", post.GeneralTagCount);

                    command.ExecuteNonQuery();
                }

                transaction.Commit();
            }
        }

        public void Dispose()
        {
            this.connection.Dispose();
        }
    }
}
