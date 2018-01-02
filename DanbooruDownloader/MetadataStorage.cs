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
CREATE TABLE IF NOT EXISTS images(
    id INTEGER NOT NULL PRIMARY KEY,
    md5 TEXT,
	extension TEXT,
	tags TEXT,
	created TEXT,
	updated TEXT,
	json TEXT
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
INSERT OR REPLACE INTO Images (id, md5, extension, tags, created, updated, json) VALUES
($id, $md5, $extension, $tags, $created, $updated, $json)";

                    command.Parameters.AddWithValue("$id", post.Id);
                    command.Parameters.AddWithValue("$md5", post.Md5);
                    command.Parameters.AddWithValue("$extension", post.Extension);
                    command.Parameters.AddWithValue("$tags", post.Tags);
                    command.Parameters.AddWithValue("$created", post.CreatedDate);
                    command.Parameters.AddWithValue("$updated", post.UpdatedDate);
                    command.Parameters.AddWithValue("$json", post.JsonString);

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
