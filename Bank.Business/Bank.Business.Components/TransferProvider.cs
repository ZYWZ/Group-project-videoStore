using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bank.Business.Components.Interfaces;
using Bank.Business.Entities;
using System.Transactions;
using Bank.Services.Interfaces;

namespace Bank.Business.Components
{
    public class TransferProvider : ITransferProvider
    {


        public void Transfer(double pAmount, int pFromAcctNumber, int pToAcctNumber, string pOrderNumber)
        {
            using (TransactionScope lScope = new TransactionScope())
            using (BankEntityModelContainer lContainer = new BankEntityModelContainer())
            {
               
                try
                {
                    Account lFromAcct = GetAccountFromNumber(pFromAcctNumber);
                    Account lToAcct = GetAccountFromNumber(pToAcctNumber);
                    if (lFromAcct.Withdraw(pAmount))
                    {
                        lToAcct.Deposit(pAmount);
                        lContainer.Attach(lFromAcct);
                        lContainer.Attach(lToAcct);
                        lContainer.ObjectStateManager.ChangeObjectState(lFromAcct, System.Data.EntityState.Modified);
                        lContainer.ObjectStateManager.ChangeObjectState(lToAcct, System.Data.EntityState.Modified);
                        lContainer.SaveChanges();

                        Console.WriteLine("Transfered sucessfully! This payment cost : " + pAmount);
                        TransferNotificationService.ITransferNotificationService lClient = new TransferNotificationService.TransferNotificationServiceClient();
                        lClient.NotifyTransferResult(true, "Transfer successful! The amount is " + pAmount, pOrderNumber);
                    }
                    else {
                        Console.WriteLine("Transfered failed!");
                        TransferNotificationService.ITransferNotificationService lClient = new TransferNotificationService.TransferNotificationServiceClient();
                        lClient.NotifyTransferResult(false, "Transfer failed! Please check your bank account balance!", pOrderNumber);
                    }
                    lScope.Complete();

                }
                catch (Exception lException)
                {
                    Console.WriteLine("Error occured while transferring money:  " + lException.Message);     
                    throw;
                }
                finally
                {
                    lScope.Dispose();
                }
            }
        }

        private Account GetAccountFromNumber(int pToAcctNumber)
        {
            using (BankEntityModelContainer lContainer = new BankEntityModelContainer())
            {
                return lContainer.Accounts.Where((pAcct) => (pAcct.AccountNumber == pToAcctNumber)).FirstOrDefault();
            }
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // Log the exception, display it, etc
            Console.WriteLine((e.ExceptionObject as Exception).Message);
        }
    }
}
