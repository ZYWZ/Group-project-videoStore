using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoStore.Business.Components.Interfaces;
using Microsoft.Practices.ServiceLocation;
using VideoStore.Business.Entities;
using DeliveryCo.MessageTypes;
using System.Transactions;
using VideoStore.Business.Components;
using VideoStore.Services;

namespace VideoStore.Business.Components
{
    class TransferNotificationProvider : ITransferNotificationProvider
    {
        public IEmailProvider EmailProvider
        {
            get { return ServiceLocator.Current.GetInstance<IEmailProvider>(); }
        }

        public void NotifyTransferResult(bool pResult, string pDescription, string pOrderNumber) {
            using (TransactionScope lScope = new TransactionScope())
            {
                Console.WriteLine(pDescription);
                using (VideoStoreEntityModelContainer lContainer = new VideoStoreEntityModelContainer())
                {
                    Order lOrder = ServiceLocator.Current.GetInstance<IOrderProvider>().GetOrderByOrderNumber(Guid.Parse(pOrderNumber));
                    LoadMediaStocks(lOrder);
                    MarkAppropriateUnchangedAssociations(lOrder);
                    Console.WriteLine(lOrder.OrderItems[0].Media.Stocks.Quantity);
                    Console.WriteLine(lOrder.OrderItems[0].Quantity);
                    if (lOrder != null)
                    {
                       
                            if (pResult)
                            {
                                SendOrderPlacedConfirmation(lOrder);
                                PlaceDeliveryForOrder(lOrder);
                            }
                            else
                            {
                                SendOrderErrorMessage(lOrder);
                                Console.WriteLine("Stocks have been rolled back");
                                lOrder.RollbackStockLevels();
                              
                            }
                       
                    }
                    lContainer.Orders.ApplyChanges(lOrder);
                    lContainer.SaveChanges();
                    lScope.Complete();
                }
            }
        }

        private void SendOrderPlacedConfirmation(Order pOrder)
        {
            EmailProvider.SendMessage(new EmailMessage()
            {
                ToAddress = pOrder.Customer.Email,
                Message = "Your order " + pOrder.OrderNumber + " has been placed"
            });
        }

        private void SendOrderErrorMessage(Order pOrder)
        {
            EmailProvider.SendMessage(new EmailMessage()
            {
                ToAddress = pOrder.Customer.Email,
                Message = "There is not enough account balance in your bank account for order: " + pOrder.OrderNumber
            });
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

        private void PlaceDeliveryForOrder(Order pOrder)
        {
            Guid identifier = Guid.NewGuid();
            Delivery lDelivery = new Delivery()
            {
                ExternalDeliveryIdentifier = identifier,
                DeliveryStatus = DeliveryStatus.Submitted,
                SourceAddress = "Video Store Address",
                DestinationAddress = pOrder.Customer.Address,
                Order = pOrder
            };

            DeliveryService.DeliveryServiceClient lClient = new DeliveryService.DeliveryServiceClient();
            lClient.SubmitDelivery(new DeliveryInfo()
            {
                OrderNumber = lDelivery.Order.OrderNumber.ToString(),
                SourceAddress = lDelivery.SourceAddress,
                DestinationAddress = lDelivery.DestinationAddress,
                DeliveryNotificationAddress = "net.tcp://localhost:9010/DeliveryNotificationService",
                DeliveryIdentifier = identifier
            });
            lDelivery.ExternalDeliveryIdentifier = identifier;
          //  Console.WriteLine("External : " + lDelivery.ExternalDeliveryIdentifier);
            pOrder.Delivery = lDelivery;

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

    }
    
}
