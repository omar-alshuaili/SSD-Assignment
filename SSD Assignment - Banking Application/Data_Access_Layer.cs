using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using System.Data.SQLite;
using System.Security.Cryptography;
using System.Xml;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using Microsoft.Extensions.Logging;
using System.Configuration;
using System.Security.Cryptography;


namespace Banking_Application
{
    public class Data_Access_Layer: IDisposable
    {

        private bool disposed = false;

        

        private List<Bank_Account> accounts;
        private  readonly static String databaseName = "Banking Database.db";

        private static Data_Access_Layer instance;

        public ILogger<Data_Access_Layer> _logger;

        protected string secretKey = ConfigurationManager.AppSettings["secretKey"];

        private Data_Access_Layer(ILogger<Data_Access_Layer> logger)
        {
            accounts = new List<Bank_Account>();
            _logger = logger;
        }


        public static Data_Access_Layer getInstance()
        {
            if (instance == null)
            {
                ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
                {
                    builder
                        .AddFilter("Microsoft", LogLevel.Warning)
                        .AddFilter("System", LogLevel.Warning)
                        .AddFile("Logs/log.txt");
                });

                ILogger<Data_Access_Layer> logger = loggerFactory.CreateLogger<Data_Access_Layer>();
                instance = new Data_Access_Layer(logger);
            }
            return instance;
        }



        private SqliteConnection getDatabaseConnection()
        {

            String databaseConnectionString = new SqliteConnectionStringBuilder()
            {
                DataSource = Data_Access_Layer.databaseName,
                Mode = SqliteOpenMode.ReadWriteCreate
            }.ToString();

            SqliteConnection connection = new SqliteConnection(databaseConnectionString);
            SqliteCommand command = connection.CreateCommand();
            // Encrypt the database file with the specified password
            connection.Open();
            command.ExecuteNonQuery();

            return connection;

        }



        private void initialiseDatabase()
        {
            using (var connection = getDatabaseConnection())
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText =
                    @"
            CREATE TABLE IF NOT EXISTS Bank_Accounts(    
                accountNo TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                address_line_1 TEXT,
                address_line_2 TEXT,
                address_line_3 TEXT,
                town TEXT NOT NULL,
                balance REAL NOT NULL,
                accountType INTEGER NOT NULL,
                hmac TEXT,
                overdraftAmount REAL,
                interestRate REAL
            ) WITHOUT ROWID
            ";
                command.ExecuteNonQuery();

                command.CommandText = "PRAGMA key = 'SSD project';";
                command.ExecuteNonQuery();
                command.CommandText = "PRAGMA cipher_compatibility = 4;";
                command.ExecuteNonQuery();
                command.CommandText = "PRAGMA kdf_iter = 64000;";
                command.ExecuteNonQuery();
                command.CommandText = "PRAGMA cipher_page_size = 4096;";
                command.ExecuteNonQuery();
                command.CommandText = "PRAGMA cipher_hmac_algorithm = HMAC_SHA256;";
                command.ExecuteNonQuery();
                command.CommandText = "PRAGMA cipher_kdf_algorithm = PBKDF2_HMAC_SHA512;";
                command.ExecuteNonQuery();
                command.CommandText = "PRAGMA cipher_plaintext_header_size = 0;";
                command.ExecuteNonQuery();
                command.CommandText = "PRAGMA cipher_salt_length = 16;";
                command.ExecuteNonQuery();
            }
        }


        private string CreateHmac(string data)
        {
            using (HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey)))
            {
                byte[] dataBytes = Encoding.UTF8.GetBytes(data);
                byte[] hmacBytes = hmac.ComputeHash(dataBytes);
                string hmacHex = BitConverter.ToString(hmacBytes).Replace("-", "");
                return hmacHex;
            }
        }





        public void LoadBankAccounts()
        {
            if (!File.Exists(Data_Access_Layer.databaseName))
                initialiseDatabase();
            else
            {
                using (var connection = getDatabaseConnection())
                {
                    connection.Open();

                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT * FROM Bank_Accounts";
                    using (SqliteDataReader dr = command.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            int accountType = dr.GetInt16(7);

                            Bank_Account ba;
                            if (accountType == Account_Type.Current_Account)
                            {
                                Current_Account ca = new Current_Account();
                                ca.overdraftAmount = dr.GetDouble(8);
                                ba = ca;
                            }
                            else
                            {
                                Savings_Account sa = new Savings_Account();
                                sa.interestRate = dr.GetDouble(9);
                                ba = sa;
                            }

                            ba.accountNo = dr.GetString(0);
                            ba.name = dr.GetString(1);
                            ba.address_line_1 = dr.GetString(2);
                            ba.address_line_2 = dr.GetString(3);
                            ba.address_line_3 = dr.GetString(4);
                            ba.town = dr.GetString(5);
                            ba.balance = dr.GetDouble(6);

                            accounts.Add(ba);
                        }
                    }
                }
            }
        }


        public string AddBankAccount(Bank_Account ba)
        {
            accounts.Add(ba);

            using (var connection = getDatabaseConnection())
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"
            INSERT INTO Bank_Accounts 
            VALUES (
                @accountNo,
                @name,
                @address_line_1,
                @address_line_2,
                @address_line_3,
                @town,
                @balance,
                @accountType,
                @hmac,
                @overdraftAmount,
                @interestRate
            )";

                command.Parameters.AddWithValue("@accountNo", ba.accountNo);
                command.Parameters.AddWithValue("@name", ba.name);
                command.Parameters.AddWithValue("@address_line_1", ba.address_line_1);
                command.Parameters.AddWithValue("@address_line_2", ba.address_line_2);
                command.Parameters.AddWithValue("@address_line_3", ba.address_line_3);
                command.Parameters.AddWithValue("@town", ba.town);
                command.Parameters.AddWithValue("@balance", ba.balance);
                command.Parameters.AddWithValue("@accountType", (ba is Current_Account) ? 1 : 2);
                command.Parameters.AddWithValue("@hmac", CreateHmac(ba.accountNo));

                if (ba is Current_Account)
                {
                    Current_Account ca = (Current_Account)ba;
                    command.Parameters.AddWithValue("@overdraftAmount", ca.overdraftAmount);
                    command.Parameters.AddWithValue("@interestRate", DBNull.Value);
                }
                else if (ba is Savings_Account)
                {
                    Savings_Account sa = (Savings_Account)ba;
                    command.Parameters.AddWithValue("@overdraftAmount", DBNull.Value);
                    command.Parameters.AddWithValue("@interestRate", sa.interestRate);
                }

                command.ExecuteNonQuery();
            }

            return ba.accountNo;
        }

        protected bool ComputeHmac(string data, string includedHmac)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey)))
            {
                byte[] dataBytes = Encoding.UTF8.GetBytes(data);
                byte[] computedHmacBytes = hmac.ComputeHash(dataBytes);
                string computedHmac = BitConverter.ToString(computedHmacBytes).Replace("-", "");

                return string.Equals(includedHmac, computedHmac);
            }
        }


        private string GetHmac(string accNo)
        {
            using (var connection = getDatabaseConnection())
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT hmac FROM Bank_Accounts WHERE accountNo = @accNo";
                command.Parameters.AddWithValue("@accNo", accNo);
                string hmac = command.ExecuteScalar()?.ToString();

                return hmac;
            }
        }

        public Bank_Account FindBankAccountByAccNo(string accNo)
        {
            using (var connection = getDatabaseConnection())
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM Bank_Accounts WHERE accountNo = @accNo";
                command.Parameters.AddWithValue("@accNo", accNo);
                var reader = command.ExecuteReader();

                if (reader.Read())
                {
                    int accountType = reader.GetInt32(7);

                    if (accountType == Account_Type.Current_Account)
                    {
                        Current_Account ca = new Current_Account();
                        ca.accountNo = reader.GetString(0);
                        ca.name = reader.GetString(1);
                        ca.address_line_1 = reader.GetString(2);
                        ca.address_line_2 = reader.GetString(3);
                        ca.address_line_3 = reader.GetString(4);
                        ca.town = reader.GetString(5);
                        ca.balance = reader.GetDouble(6);
                        ca.overdraftAmount = reader.GetDouble(8);
                        return ca;
                    }
                    else
                    {
                        Savings_Account sa = new Savings_Account();
                        sa.accountNo = reader.GetString(0);
                        sa.name = reader.GetString(1);
                        sa.address_line_1 = reader.GetString(2);
                        sa.address_line_2 = reader.GetString(3);
                        sa.address_line_3 = reader.GetString(4);
                        sa.town = reader.GetString(5);
                        sa.balance = reader.GetDouble(6);
                        sa.interestRate = reader.GetDouble(9);
                        return sa;
                    }
                }
            }

            return null;
        }
        public bool CloseBankAccount(string accNo)
        {
            using (var connection = getDatabaseConnection())
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM Bank_Accounts WHERE accountNo = @accNo";
                command.Parameters.AddWithValue("@accNo", accNo);
                int rowsAffected = command.ExecuteNonQuery();

                if (rowsAffected > 0)
                {
                    accounts.RemoveAll(ba => ba.accountNo.Equals(accNo));
                    return true;
                }
            }

            return false;
        }

        public bool Lodge(string accNo, double amountToLodge)
        {
            Bank_Account toLodgeTo = accounts.FirstOrDefault(ba => ba.accountNo.Equals(accNo));

            if (toLodgeTo == null)
                return false;

            toLodgeTo.lodge(amountToLodge);

            using (var connection = getDatabaseConnection())
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "UPDATE Bank_Accounts SET balance = @balance WHERE accountNo = @accNo";
                command.Parameters.AddWithValue("@balance", toLodgeTo.balance);
                command.Parameters.AddWithValue("@accNo", accNo);
                int rowsAffected = command.ExecuteNonQuery();

                return rowsAffected > 0;
            }
        }

        public bool Withdraw(string accNo, double amountToWithdraw)
        {
            Bank_Account toWithdrawFrom = accounts.FirstOrDefault(ba => ba.accountNo.Equals(accNo));

            if (toWithdrawFrom == null)
                return false;

            bool result = toWithdrawFrom.withdraw(amountToWithdraw);

            if (!result)
                return false;

            using (var connection = getDatabaseConnection())
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "UPDATE Bank_Accounts SET balance = @balance WHERE accountNo = @accNo";
                command.Parameters.AddWithValue("@balance", toWithdrawFrom.balance);
                command.Parameters.AddWithValue("@accNo", accNo);
                int rowsAffected = command.ExecuteNonQuery();

                return rowsAffected > 0;
            }
        }


        // Implement IDisposable.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources.
                    // If you have any managed resource to dispose add them here.
                    // If you're using ADO.NET, you might want to close connections to the database, etc.
                }

                // Dispose unmanaged resources.
                disposed = true;
            }
        }

        ~Data_Access_Layer()
        {
            Dispose(false);
        }

    }
}
