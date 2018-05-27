using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DeliveryCo.Business.Components.Interfaces;
using System.Transactions;
using DeliveryCo.Business.Entities;
using System.Threading;
using DeliveryCo.Services.Interfaces;
using VideoStore.Business.Entities;
using VideoStore.Services.Interfaces;

namespace DeliveryCo.Business.Components
{
    public class DeliveryProvider : IDeliveryProvider
    {
        public void SubmitDelivery(DeliveryCo.Business.Entities.DeliveryInfo pDeliveryInfo)
        {
            using(TransactionScope lScope = new TransactionScope())
            using(DeliveryDataModelContainer lContainer = new DeliveryDataModelContainer())
            {
                try
                {
                    //pDeliveryInfo.DeliveryIdentifier = Guid.NewGuid();
                    pDeliveryInfo.Status = 0;
                    lContainer.DeliveryInfoes.AddObject(pDeliveryInfo);
                    lContainer.SaveChanges();
                    ThreadPool.QueueUserWorkItem(new WaitCallback((pObj) => ScheduleDelivery(pDeliveryInfo)));
                    lScope.Complete();
                }
                catch (Exception lException)
                {
                    Console.WriteLine("Error occured while delivering:  " + lException.Message);
                    DeliveryNotificationService.DeliveryNotificationServiceClient lClient = new DeliveryNotificationService.DeliveryNotificationServiceClient();
                    lClient.NotifyDeliveryCompletion(pDeliveryInfo.DeliveryIdentifier, DeliveryInfoStatus.Failed);
                    throw;
                }
            }
            //return pDeliveryInfo.DeliveryIdentifier;
        }

        private void ScheduleDelivery(DeliveryInfo pDeliveryInfo)
        {
           // Thread.Sleep(TimeSpan.FromSeconds(3));

            Console.WriteLine("Delivering to" + pDeliveryInfo.DestinationAddress);
         //   Console.WriteLine("DeliverID: " + pDeliveryInfo.DeliveryIdentifier);
            Thread.Sleep(3000);
            DeliveryNotificationService.DeliveryNotificationServiceClient lClient = new DeliveryNotificationService.DeliveryNotificationServiceClient();
            lClient.NotifyDeliveryCompletion(pDeliveryInfo.DeliveryIdentifier, DeliveryInfoStatus.Delivered);
        }
    }
}
