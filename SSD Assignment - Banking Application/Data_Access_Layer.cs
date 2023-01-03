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
    public class Data_Access_Layer
    {
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

            }

        }

        private string createHmac(string data)
        {

            byte[] dataBytes = Encoding.UTF8.GetBytes(data);

            // Convert the key to a byte array
            byte[] keyBytes = Encoding.UTF8.GetBytes(secretKey);

            // Create a new HMAC object using the specified algorithm and key
            HMAC hmac = HMAC.Create("HMACSHA256");
            hmac.Key = keyBytes;

            // Compute the HMAC of the data
            byte[] hmacBytes = hmac.ComputeHash(dataBytes);

            // Convert the HMAC to a hexadecimal string
            string hmacHex = BitConverter.ToString(hmacBytes).Replace("-", "");

            return hmacHex;
            
        }




        public void loadBankAccounts()
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
                    SqliteDataReader dr = command.ExecuteReader();
                    
                    while(dr.Read())
                    {

                        int accountType = dr.GetInt16(7);

                        if(accountType == Account_Type.Current_Account)
                        {
                            Current_Account ca = new Current_Account();
                            ca.accountNo = dr.GetString(0);
                            ca.name = dr.GetString(1);
                            ca.address_line_1 = dr.GetString(2);
                            ca.address_line_2 = dr.GetString(3);
                            ca.address_line_3 = dr.GetString(4);
                            ca.town = dr.GetString(5);
                            ca.balance = dr.GetDouble(6);
                            ca.overdraftAmount = dr.GetDouble(8);
                            accounts.Add(ca);
                        }
                        else
                        {
                            Savings_Account sa = new Savings_Account();
                            sa.accountNo = dr.GetString(0);
                            sa.name = dr.GetString(1);
                            sa.address_line_1 = dr.GetString(2);
                            sa.address_line_2 = dr.GetString(3);
                            sa.address_line_3 = dr.GetString(4);
                            sa.town = dr.GetString(5);
                            sa.balance = dr.GetDouble(6);
                            sa.interestRate = dr.GetDouble(9);
                            accounts.Add(sa);
                        }


                    }

                }

            }
        }

     
        public String addBankAccount(Bank_Account ba) 
        {
            


            if (ba.GetType() == typeof(Current_Account))
                ba = (Current_Account)ba;
            else
                ba = (Savings_Account)ba;

            accounts.Add(ba);

            using (var connection = getDatabaseConnection())
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "PRAGMA key = 'dd';";
                command.CommandText =
                @"
                    INSERT INTO Bank_Accounts VALUES(" +
                    "'" + ba.accountNo + "', " +
                    "'" + ba.name + "', " +
                    "'" + ba.address_line_1 + "', " +
                    "'" + ba.address_line_2 + "', " +
                    "'" + ba.address_line_3 + "', " +
                    "'" + ba.town + "', " +
                    ba.balance + ", " +
                    (ba.GetType() == typeof(Current_Account) ? 1 : 2) + ", " + "'" + createHmac(ba.accountNo) + "', ";

                if (ba.GetType() == typeof(Current_Account))
                {
                    Current_Account ca = (Current_Account)ba;
                    command.CommandText += ca.overdraftAmount + ", NULL)";
                }

                else
                {
                    Savings_Account sa = (Savings_Account)ba;
                    command.CommandText += "NULL," + sa.interestRate + ")";
                }

                // Bind the values to the parameters
                command.Parameters.AddWithValue("@accountNo", ba.accountNo);
                command.Parameters.AddWithValue("@name", ba.name);
                command.Parameters.AddWithValue("@address_line_1", ba.address_line_1);
                command.Parameters.AddWithValue("@address_line_2", ba.address_line_2);
                command.Parameters.AddWithValue("@address_line_3", ba.address_line_3);
                command.Parameters.AddWithValue("@town", ba.town);
                command.Parameters.AddWithValue("@balance", ba.balance);
                command.Parameters.AddWithValue("@balance", ba.balance);

                command.ExecuteNonQuery();

            }

            return ba.accountNo;

        }
        protected bool computeHmac(string data, string includedHmac)
        {
            string computedHmac;
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey)))
            {
                computedHmac = BitConverter.ToString(hmac.ComputeHash(Encoding.UTF8.GetBytes(data))).Replace("-","");
            }

            return includedHmac == computedHmac ? true: false;

        }

        private string getHmac(string accNo)
        {
            using (var connection = getDatabaseConnection())
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText =
                    @"SELECT hmac FROM Bank_Accounts WHERE accountNo = @accNo";
                command.Parameters.AddWithValue("@accNo", accNo);
                var reader = command.ExecuteReader();

                if (reader.Read())
                {
                    return reader["hmac"].ToString();
                }
            }

            return null;
        }

        public Bank_Account findBankAccountByAccNo(String accNo) 
        {

        
            foreach(Bank_Account ba in accounts)
            {
                
                if (ba.accountNo.Equals(accNo) )
                {
                    if (!computeHmac(ba.accountNo, getHmac(ba.accountNo)))
                    {
                        throw new Exception("this account was not created by this app");
                    }

                    return ba;
                }

            }

            return null ; 
        }

        public bool closeBankAccount(String accNo) 
        {

            Bank_Account toRemove = null;
            
            foreach (Bank_Account ba in accounts)
            {

                if (ba.accountNo.Equals(accNo))
                {
                    toRemove = ba;
                    break;
                }

            }

            if (toRemove == null)
                return false;
            else
            {
                accounts.Remove(toRemove);

                using (var connection = getDatabaseConnection())
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = "DELETE FROM Bank_Accounts WHERE accountNo = '" + toRemove.accountNo + "'";
                    command.ExecuteNonQuery();

                }

                return true;
            }

        }

        public bool lodge(String accNo, double amountToLodge)
        {

            Bank_Account toLodgeTo = null;

            foreach (Bank_Account ba in accounts)
            {

                if (ba.accountNo.Equals(accNo))
                {
                    ba.lodge(amountToLodge);
                    toLodgeTo = ba;
                    break;
                }

            }

            if (toLodgeTo == null)
                return false;
            else
            {

                using (var connection = getDatabaseConnection())
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = "UPDATE Bank_Accounts SET balance = " + toLodgeTo.balance + " WHERE accountNo = '" + toLodgeTo.accountNo + "'";
                    command.ExecuteNonQuery();

                }

                return true;
            }

        }

        public bool withdraw(String accNo, double amountToWithdraw)
        {

            Bank_Account toWithdrawFrom = null;
            bool result = false;

            foreach (Bank_Account ba in accounts)
            {

                if (ba.accountNo.Equals(accNo))
                {
                    result = ba.withdraw(amountToWithdraw);
                    toWithdrawFrom = ba;
                    break;
                }

            }

            if (toWithdrawFrom == null || result == false)
                return false;
            else
            {

                using (var connection = getDatabaseConnection())
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = "UPDATE Bank_Accounts SET balance = " + toWithdrawFrom.balance + " WHERE accountNo = '" + toWithdrawFrom.accountNo + "'";
                    command.ExecuteNonQuery();

                }

                return true;
            }

        }

    }
}
