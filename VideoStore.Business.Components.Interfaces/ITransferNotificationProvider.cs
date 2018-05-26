using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VideoStore.Business.Components.Interfaces
{
    public interface ITransferNotificationProvider
    {
        void NotifyTransferResult(bool pResult, string pDescription, string pOrderNumber);
    }
}
