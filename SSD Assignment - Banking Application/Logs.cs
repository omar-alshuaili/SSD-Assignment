using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSD_Assignment___Banking_Application
{
    public class Logs
    {
        private readonly static String databaseName = "Banking Database.db";
        public string logMess { get; set; }
        public string logInfo { get; set; }

        public Logs()
        {

        }

        public void saveLog(string logMess, string logInfo)
        {
            initialiseDatabase();

            using (var connection = getDatabaseConnection())
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText =
                @"
                    INSERT INTO Logs (message, info) VALUES (@logMess, @logInfo);
                ";

                command.Parameters.AddWithValue("@logMess", logMess);
                command.Parameters.AddWithValue("@logInfo", logInfo);

                command.ExecuteNonQuery();
            }
        }

        private void initialiseDatabase()
        {
            using (var connection = getDatabaseConnection())
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText =
                @"
                    CREATE TABLE IF NOT EXISTS Logs(    
                      message TEXT NOT NULL,
                      info TEXT NOT NULL
                    );
                ";

                command.ExecuteNonQuery();
            }
        }

        private SqliteConnection getDatabaseConnection()
        {
            String databaseConnectionString = new SqliteConnectionStringBuilder()
            {
                DataSource = Logs.databaseName,
                Mode = SqliteOpenMode.ReadWriteCreate
            }.ToString();

            return new SqliteConnection(databaseConnectionString);
        }
    }
}
