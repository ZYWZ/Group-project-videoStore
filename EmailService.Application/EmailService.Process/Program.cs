using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using Microsoft.Practices.Unity;
using Microsoft.Practices.Unity.Configuration;
using Microsoft.Practices.Unity.ServiceLocatorAdapter;
using Microsoft.Practices.ServiceLocation;
using System.Configuration;
using System.Messaging;

namespace EmailService.Process
{
    class Program
    {
        private static readonly String sPublishQueuePath = ".\\private$\\EmailTransacted";

        static void Main(string[] args)
        {
            EnsureQueueExists();
            ResolveDependencies();
            using (ServiceHost lHost = new ServiceHost(typeof(EmailService.Services.EmailService)))
            {
                lHost.Open();
                Console.WriteLine("Email Service Started");
                while (Console.ReadKey().Key != ConsoleKey.Q) ;
            }
        }

        private static void EnsureQueueExists()
        {
            // Create the transacted MSMQ queue if necessary.
            if (!MessageQueue.Exists(sPublishQueuePath))
                MessageQueue.Create(sPublishQueuePath, true);
        }

        private static void ResolveDependencies()
        {

            UnityContainer lContainer = new UnityContainer();
            UnityConfigurationSection lSection
                    = (UnityConfigurationSection)ConfigurationManager.GetSection("unity");
            lSection.Containers["containerOne"].Configure(lContainer);
            UnityServiceLocator locator = new UnityServiceLocator(lContainer);
            ServiceLocator.SetLocatorProvider(() => locator);
        }
    }
}
