using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Banking_Application
{
    public partial class Current_Account: Bank_Account
    {

        public double overdraftAmount;

        public Current_Account(): base()
        {

        }
        
        public Current_Account(String name, String address_line_1, String address_line_2, String address_line_3, String town, double balance, double overdraftAmount) : base(name, address_line_1, address_line_2, address_line_3, town, balance)
        {
            this.overdraftAmount = overdraftAmount;
        }

        public override bool withdraw(double amountToWithdraw)
        {
            Assembly assembly = Assembly.GetCallingAssembly();

            // Get the type of the calling class
            Type callingType = assembly.GetType(
            MethodBase.GetCurrentMethod().DeclaringType.FullName);

            // Check if the calling class is derived from Bank_Account or is in the same assembly as Bank_Account
            if (callingType.IsAssignableFrom(this.GetType()) ||
                callingType.Assembly == this.GetType().Assembly)
            {
                // The calling class is authorized, so allow the method to be invoked
                double avFunds = getAvailableFunds();

                if (avFunds >= amountToWithdraw)
                {
                    balance -= amountToWithdraw;
                    return true;
                }

                else
                    return false;
            }
            else
            {
                // The calling class is not authorized, so throw an exception
                throw new UnauthorizedAccessException();
            }

            

        }

        public override double getAvailableFunds()
        {
            return (base.balance + overdraftAmount);
        }

        public override String ToString()
        {

            return base.ToString() +
                "Account Type: Current Account\n" +
                "Overdraft Amount: " + overdraftAmount + "\n";

        }

    }
}
