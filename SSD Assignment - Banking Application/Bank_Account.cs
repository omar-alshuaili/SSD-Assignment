using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using SSD_Assignment___Banking_Application;
using System.Reflection;
using System.Security.Permissions;
namespace Banking_Application
{
    public abstract class Bank_Account
    {
        public String accountNo;
        public String name;
        public String address_line_1;
        public String address_line_2;
        public String address_line_3;
        public String town;
        public double balance;



        public String keyBase64;
        public Bank_Account()
        {

        }
        public Bank_Account(String name, String address_line_1, String address_line_2, String address_line_3, String town, double balance)
        {
            using (Aes aesAlgorithm = Aes.Create())
            {



                aesAlgorithm.KeySize = 128;
                aesAlgorithm.GenerateKey();
                keyBase64 = Convert.ToBase64String(aesAlgorithm.Key);
                this.accountNo = System.Guid.NewGuid().ToString();
                this.name = EncryptData.EncryptString(keyBase64, name);
                this.address_line_1 = EncryptData.EncryptString(keyBase64, address_line_1); ;
                this.address_line_2 = EncryptData.EncryptString(keyBase64, address_line_2); ;
                this.address_line_3 = EncryptData.EncryptString(keyBase64, address_line_3); ;
                this.town = EncryptData.EncryptString(keyBase64, town);
                this.balance = balance;



                Console.WriteLine("Key: {0}", keyBase64);



            }
        }
        public void lodge(double amountIn)
        {
            // Get the assembly of the calling class
            Assembly assembly = Assembly.GetCallingAssembly();

            // Get the type of the calling class
            Type callingType = assembly.GetType(
            MethodBase.GetCurrentMethod().DeclaringType.FullName);

            // Check if the calling class is derived from Bank_Account or is in the same assembly as Bank_Account
            if (callingType.IsAssignableFrom(this.GetType()) ||
                callingType.Assembly == this.GetType().Assembly)
            {
                // The calling class is authorized, so allow the method to be invoked
                balance += amountIn;
            }
            else
            {
                // The calling class is not authorized, so throw an exception
                throw new UnauthorizedAccessException();
            }

        }
        public abstract bool withdraw(double amountToWithdraw);
        public abstract double getAvailableFunds();
        public override String ToString()
        {



            return "\nAccount No: " + accountNo + "\n" +
            "Name: " + EncryptData.DecryptString(keyBase64, name) + "\n" +
            "Address Line 1: " + EncryptData.DecryptString(keyBase64, address_line_1) + "\n" +
            "Address Line 2: " + EncryptData.DecryptString(keyBase64, address_line_2) + "\n" +
            "Address Line 3: " + EncryptData.DecryptString(keyBase64, address_line_3) + "\n" +
            "Town: " + EncryptData.DecryptString(keyBase64, town) + "\n" +
            "Balance: " + balance + "\n";



        }
    }
}