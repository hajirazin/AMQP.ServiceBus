using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AMQP.ServiceBus
{
    public enum ServiceProviderType
    {
        Azure = 0,
    }

    public class AMQPServiceProviderFactory
    {
        public static AMQPServiceProvider CreateServiceProvider(
            ServiceProviderType type,
            string ServiceNamespace,
            string SASKeyName,
            string SASKeyValue)
        {
            switch (type)
            {
                case ServiceProviderType.Azure:
                    return new AzureServiceProvider(
                        ServiceNamespace,
                        SASKeyName,
                        SASKeyValue);
                default:
                    return null;
            }
        }
    }
}
