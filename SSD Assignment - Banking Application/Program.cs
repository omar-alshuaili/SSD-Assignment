﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using System.IO;
using SSD_Assignment___Banking_Application;
using System.Configuration;
using System.Diagnostics;
namespace Banking_Application
{
    public class Program
    {
        private const int maxGarbage = 1000;

        private static Logs log = new Logs();
        private static Data_Access_Layer dal = Data_Access_Layer.getInstance();

        public static void Main(string[] args)
        {
            using (dal)
            {
                dal.LoadBankAccounts();
                bool running = true;

                do
                {
                    DisplayMemoryUsage();

                    DateTime date = DateTime.Now;
                    log.saveLog("Starting the application", date.ToString("HH:mm:ss dd/MM/yy"));

                    DisplayMenu();

                    string option = Console.ReadLine();

                    switch (option)
                    {
                        case "1":
                            CreateAccount(date);
                            break;
                        case "2":
                            CloseAccount(date);
                            break;
                        case "3":
                            ViewAccountInfo();
                            break;
                        case "4":
                            MakeLodgement(date);
                            break;
                        case "5":
                            MakeWithdrawal(date);
                            break;
                        case "6":
                            ExitApplication();
                            running = false;
                            break;
                        default:
                            Console.WriteLine("INVALID OPTION CHOSEN - PLEASE TRY AGAIN");
                            break;
                    }

                } while (running != false);
            }
        }

        private static void DisplayMemoryUsage()
        {
            Console.WriteLine("Memory used before collection:       {0:N0}", GC.GetTotalMemory(false));
        }

        private static void DisplayMenu()
        {
            Console.WriteLine("");
            Console.WriteLine("***Banking Application Menu***");
            Console.WriteLine("1. Add Bank Account");
            Console.WriteLine("2. Close Bank Account");
            Console.WriteLine("3. View Account Information");
            Console.WriteLine("4. Make Lodgement");
            Console.WriteLine("5. Make Withdrawal");
            Console.WriteLine("6. Exit");
            Console.WriteLine("CHOOSE OPTION:");
            
        }

        private static void CreateAccount(DateTime date)
        {
            //adding log 
            log.saveLog("trying to create an account", date.ToString("HH:mm:ss dd/MM/yy"));

            String accountType = "";
            int loopCount = 0;

            do
            {

                if (loopCount > 0)
                    Console.WriteLine("INVALID OPTION CHOSEN - PLEASE TRY AGAIN");

                Console.WriteLine("");
                Console.WriteLine("***Account Types***:");
                Console.WriteLine("1. Current Account.");
                Console.WriteLine("2. Savings Account.");
                Console.WriteLine("CHOOSE OPTION:");
                accountType = Console.ReadLine();

                loopCount++;

            } while (!(accountType.Equals("1") || accountType.Equals("2")));

            String name = "";
            loopCount = 0;

            do
            {

                if (loopCount > 0)
                    Console.WriteLine("INVALID NAME ENTERED - PLEASE TRY AGAIN");

                Console.WriteLine("Enter Name: ");
                name = Console.ReadLine();

                loopCount++;

            } while (name.Equals(""));

            String addressLine1 = "";
            loopCount = 0;

            do
            {

                if (loopCount > 0)
                    Console.WriteLine("INVALID ÀDDRESS LINE 1 ENTERED - PLEASE TRY AGAIN");

                Console.WriteLine("Enter Address Line 1: ");
                addressLine1 = Console.ReadLine();

                loopCount++;

            } while (addressLine1.Equals(""));

            Console.WriteLine("Enter Address Line 2: ");
            String addressLine2 = Console.ReadLine();

            Console.WriteLine("Enter Address Line 3: ");
            String addressLine3 = Console.ReadLine();

            String town = "";
            loopCount = 0;

            do
            {

                if (loopCount > 0)
                    Console.WriteLine("INVALID TOWN ENTERED - PLEASE TRY AGAIN");

                Console.WriteLine("Enter Town: ");
                town = Console.ReadLine();

                loopCount++;

            } while (town.Equals(""));

            double balance = -1;
            loopCount = 0;

            do
            {

                if (loopCount > 0)
                    Console.WriteLine("INVALID OPENING BALANCE ENTERED - PLEASE TRY AGAIN");

                Console.WriteLine("Enter Opening Balance: ");
                String balanceString = Console.ReadLine();

                try
                {
                    balance = Convert.ToDouble(balanceString);
                }

                catch
                {
                    loopCount++;
                }

            } while (balance < 0);

            Bank_Account ba;

            if (Convert.ToInt32(accountType) == Account_Type.Current_Account)
            {
                double overdraftAmount = -1;
                loopCount = 0;

                do
                {

                    if (loopCount > 0)
                        Console.WriteLine("INVALID OVERDRAFT AMOUNT ENTERED - PLEASE TRY AGAIN");

                    Console.WriteLine("Enter Overdraft Amount: ");
                    String overdraftAmountString = Console.ReadLine();

                    try
                    {
                        overdraftAmount = Convert.ToDouble(overdraftAmountString);
                    }

                    catch
                    {
                        loopCount++;
                    }

                } while (overdraftAmount < 0);

                ba = new Current_Account(name, addressLine1, addressLine2, addressLine3, town, balance, overdraftAmount);
            }

            else
            {

                double interestRate = -1;
                loopCount = 0;

                do
                {

                    if (loopCount > 0)
                        Console.WriteLine("INVALID INTEREST RATE ENTERED - PLEASE TRY AGAIN");

                    Console.WriteLine("Enter Interest Rate: ");
                    String interestRateString = Console.ReadLine();

                    try
                    {
                        interestRate = Convert.ToDouble(interestRateString);
                    }

                    catch
                    {
                        loopCount++;
                    }

                } while (interestRate < 0);

                ba = new Savings_Account(name, addressLine1, addressLine2, addressLine3, town, balance, interestRate);
            }

            String accNo = dal.AddBankAccount(ba);

            Console.WriteLine("New Account Number Is: " + accNo);

        }

        private static void CloseAccount(DateTime date)
        {

            //adding log 
            log.saveLog("trying to close an account", date.ToString("HH:mm:ss dd/MM/yy"));
            Console.WriteLine("Enter Account Number: ");
            string accNo = Console.ReadLine();

            //adding log
            log.saveLog("searching for account " + accNo + "to delete", date.ToString("HH:mm:ss dd/MM/yy"));

            Bank_Account ba = dal.FindBankAccountByAccNo(accNo);

            if (ba is null)
            {
                Console.WriteLine("Account Does Not Exist");
            }
            else
            {
                Console.WriteLine(ba.ToString());

                String ans = "";

                do
                {

                    Console.WriteLine("Proceed With Delection (Y/N)?");
                    ans = Console.ReadLine();

                    switch (ans)
                    {
                        case "Y":
                        case "y":
                            dal.CloseBankAccount(accNo);
                            //adding log
                            log.saveLog("account" + accNo + "was deleted", date.ToString("HH:mm:ss dd/MM/yy"));
                            break;
                        case "N":
                        case "n":
                            break;
                        default:
                            Console.WriteLine("INVALID OPTION CHOSEN - PLEASE TRY AGAIN");
                            break;
                    }
                } while (!(ans.Equals("Y") || ans.Equals("y") || ans.Equals("N") || ans.Equals("n")));
            }
        }

        private static void ViewAccountInfo()
        {
            int loopCount = 0;
            Console.WriteLine("Enter Account Number: ");
            string accNo = Console.ReadLine();

            Bank_Account ba = dal.FindBankAccountByAccNo(accNo);

            if (ba is null)
            {
                Console.WriteLine("Account Does Not Exist");
            }
            else
            {
                double amountToLodge = -1;
                loopCount = 0;

                do
                {

                    if (loopCount > 0)
                        Console.WriteLine("INVALID AMOUNT ENTERED - PLEASE TRY AGAIN");

                    Console.WriteLine("Enter Amount To Lodge: ");
                    String amountToLodgeString = Console.ReadLine();

                    try
                    {
                        amountToLodge = Convert.ToDouble(amountToLodgeString);
                    }

                    catch
                    {
                        loopCount++;
                    }

                } while (amountToLodge < 0);

                dal.Lodge(accNo, amountToLodge);
            }

        }

        private static void MakeLodgement(DateTime date)
        {
           
            int loopCount = 0;

            Console.WriteLine("Enter Account Number: ");
            string accNo = Console.ReadLine();

            Bank_Account ba = dal.FindBankAccountByAccNo(accNo);

            if (ba is null)
            {
                Console.WriteLine("Account Does Not Exist");
            }
            else
            {
                double amountToLodge = -1;
                loopCount = 0;

                do
                {

                    if (loopCount > 0)
                        Console.WriteLine("INVALID AMOUNT ENTERED - PLEASE TRY AGAIN");

                    Console.WriteLine("Enter Amount To Lodge: ");
                    String amountToLodgeString = Console.ReadLine();

                    try
                    {
                        amountToLodge = Convert.ToDouble(amountToLodgeString);
                    }

                    catch
                    {
                        loopCount++;
                    }

                } while (amountToLodge < 0);

                dal.Lodge(accNo, amountToLodge);
                //adding log
                log.saveLog("account" + accNo + "made lodge" + amountToLodge, date.ToString("HH:mm:ss dd/MM/yy"));

            }
        }

        private static void MakeWithdrawal(DateTime date)
        {
            int loopCount = 0;


            Console.WriteLine("Enter Account Number: ");
            string accNo = Console.ReadLine();

            Bank_Account ba = dal.FindBankAccountByAccNo(accNo);

            if (ba is null)
            {
                Console.WriteLine("Account Does Not Exist");
            }
            else
            {
                double amountToWithdraw = -1;
                loopCount = 0;

                do
                {

                    if (loopCount > 0)
                        Console.WriteLine("INVALID AMOUNT ENTERED - PLEASE TRY AGAIN");

                    Console.WriteLine("Enter Amount To Withdraw (€" + ba.getAvailableFunds() + " Available): ");
                    String amountToWithdrawString = Console.ReadLine();

                    try
                    {
                        //adding log
                        log.saveLog("account" + accNo + "made Withdraw" + amountToWithdraw, date.ToString("HH:mm:ss dd/MM/yy"));
                        amountToWithdraw = Convert.ToDouble(amountToWithdrawString);
                    }

                    catch
                    {
                        loopCount++;
                    }

                } while (amountToWithdraw < 0);

                bool withdrawalOK = dal.Withdraw(accNo, amountToWithdraw);

                if (withdrawalOK == false)
                {

                    Console.WriteLine("Insufficient Funds Available.");
                }
                log.saveLog($"Account {accNo} made a withdrawal of {amountToWithdraw}", date.ToString("HH:mm:ss dd/MM/yy"));

            }

        }

        private static void ExitApplication()
        {
            log = null;
            GC.Collect();
            Console.WriteLine("Memory used after full collection:   {0:N0}", GC.GetTotalMemory(true));
            Environment.Exit(0);
        }
    }
}
