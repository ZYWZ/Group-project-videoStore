using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VideoStore.Business.Components.Interfaces;
using VideoStore.Business.Entities;
using System.Transactions;
using Microsoft.Practices.ServiceLocation;
using DeliveryCo.MessageTypes;
using VideoStore.Services;
using System.Threading;


namespace VideoStore.Business.Components
{
    public class OrderProvider : IOrderProvider
    {
        private bool TransferStatus;

        public IEmailProvider EmailProvider
        {
            get { return ServiceLocator.Current.GetInstance<IEmailProvider>(); }
        }

        public IUserProvider UserProvider
        {
            get { return ServiceLocator.Current.GetInstance<IUserProvider>(); }
        }

        public void SubmitOrder(Entities.Order pOrder)
        {
           
            using (TransactionScope lScope = new TransactionScope())
            {
                LoadMediaStocks(pOrder);
                MarkAppropriateUnchangedAssociations(pOrder);
                using (VideoStoreEntityModelContainer lContainer = new VideoStoreEntityModelContainer())
                {
                    try
                    {

                        pOrder.OrderNumber = Guid.NewGuid();
                        
                        if (pOrder.CheckStockLevels())
                        {
                            TransferFundsFromCustomer(UserProvider.ReadUserById(pOrder.Customer.Id).BankAccountNumber, pOrder.Total ?? 0.0, pOrder.OrderNumber.ToString());
                        }
                        else {
                            throw new Exception("Insufficient stock!");
                        }
                        
                        lContainer.Orders.ApplyChanges(pOrder);
         
                        lContainer.SaveChanges();
                        lScope.Complete();                     
                    }
                    catch (Exception lException)
                    {
                      //  SendOrderErrorMessage(pOrder, lException);
                        throw;
                    }              
                }
            }
            Thread.Sleep(TimeSpan.FromSeconds(3));
            Console.WriteLine(Status.bankInfoStatus);
            if (!Status.bankInfoStatus)
            {
              //  RollbackOrder(pOrder);
                throw new Exception("Insufficient account balance!");
            }
            else {
                UpdateOrderStock(pOrder);
            }
        }

        private void MarkAppropriateUnchangedAssociations(Order pOrder)
        {
            pOrder.Customer.MarkAsUnchanged();
            pOrder.Customer.LoginCredential.MarkAsUnchanged();
            foreach (OrderItem lOrder in pOrder.OrderItems)
            {
                lOrder.Media.Stocks.MarkAsUnchanged();
                lOrder.Media.MarkAsUnchanged();
            }
        }

        private void LoadMediaStocks(Order pOrder)
        {
            using (VideoStoreEntityModelContainer lContainer = new VideoStoreEntityModelContainer())
            {
                foreach (OrderItem lOrder in pOrder.OrderItems)
                {
                    lOrder.Media.Stocks = lContainer.Stocks.Where((pStock) => pStock.Media.Id == lOrder.Media.Id).FirstOrDefault();    
                }
            }
        }

        private void TransferFundsFromCustomer(int pCustomerAccountNumber, double pTotal, string pOrderNumber)
        {
            try
            {
                //ExternalServiceFactory.Instance.TransferService.Transfer(pTotal, pCustomerAccountNumber, RetrieveVideoStoreAccountNumber());
                BankTransferService.TransferServiceClient lClient = new BankTransferService.TransferServiceClient();
                lClient.Transfer(pTotal, pCustomerAccountNumber, RetrieveVideoStoreAccountNumber(), pOrderNumber);
            }
            catch(Exception e)
            {
                throw new Exception("Error Transferring funds for order."+e.Message);
            }
        }

        public Order GetOrderByOrderNumber(Guid pOrderNumber)
        {
            using (var lContainer = new VideoStoreEntityModelContainer())
            {
                return lContainer.Orders.Include("Customer").Include("OrderItems").Where((pOrder) => (pOrder.OrderNumber == pOrderNumber)).FirstOrDefault();
            }
        }


        private int RetrieveVideoStoreAccountNumber()
        {
            return 123;
        }

        private void RollbackOrder(Order pOrder)
        {
            using (TransactionScope lScope = new TransactionScope())
            using (VideoStoreEntityModelContainer lContainer = new VideoStoreEntityModelContainer())
            {
                if (pOrder != null)
                {
                    pOrder.RollbackStockLevels();
                    lContainer.Orders.ApplyChanges(pOrder);
                    lContainer.SaveChanges();
                }
                lScope.Complete();
            }
        }

        private void UpdateOrderStock(Order pOrder)
        {
            using (TransactionScope lScope = new TransactionScope())
            using (VideoStoreEntityModelContainer lContainer = new VideoStoreEntityModelContainer())
            {
                if (pOrder != null)
                {
                    LoadMediaStocks(pOrder);
                    MarkAppropriateUnchangedAssociations(pOrder);
                    pOrder.UpdateStockLevels();
                    lContainer.Orders.ApplyChanges(pOrder);
                    lContainer.SaveChanges();
                }
                lScope.Complete();
            }
        }

        public void setTransferStatus(bool Status)
        {
            TransferStatus = Status;
        }
        public bool getTransferStatus() { 
            
                return TransferStatus;
            
        }

    }
}
