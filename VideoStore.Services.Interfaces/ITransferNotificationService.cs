using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;

namespace VideoStore.Services.Interfaces
{
    public enum TransferInfoStatus { Transfered, Failed }

    [ServiceContract]
    public interface ITransferNotificationService
    {
        [OperationContract(IsOneWay = true)]
        void NotifyTransferResult(bool pResult, String pDescription, string pOrderNumber);
    }
}
