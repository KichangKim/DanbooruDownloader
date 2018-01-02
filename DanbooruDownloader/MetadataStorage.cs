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
INSERT OR REPLACE INTO Images (Id, Extension, Tags, Created, Updated, Json) VALUES
($Id, $Extension, $Tags, $Created, $Updated, $Json)";

                    command.Parameters.AddWithValue("$Id", post.Id);
                    command.Parameters.AddWithValue("$Extension", post.Extension);
                    command.Parameters.AddWithValue("$Tags", post.Tags);
                    command.Parameters.AddWithValue("$Created", post.CreatedDate);
                    command.Parameters.AddWithValue("$Updated", post.UpdatedDate);
                    command.Parameters.AddWithValue("$Json", post.JsonString);

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
