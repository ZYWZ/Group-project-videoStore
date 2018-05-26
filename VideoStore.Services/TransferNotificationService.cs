using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoStore.Services.Interfaces;
using VideoStore.Business.Components.Interfaces;
using Microsoft.Practices.ServiceLocation;

namespace VideoStore.Services
{
    class TransferNotificationService : ITransferNotificationService
    {
        public ITransferNotificationProvider Provider
        {
            get
            {
                return ServiceLocator.Current.GetInstance<ITransferNotificationProvider>();
            }
        }

        public void NotifyTransferResult(bool pResult, String pDescription, string pOrderNumber) {
            Provider.NotifyTransferResult(pResult, pDescription, pOrderNumber);
        }
    }
}
