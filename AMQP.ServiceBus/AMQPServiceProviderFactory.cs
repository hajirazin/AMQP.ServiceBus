using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AMQP.ServiceBus
{
    public enum AMQPServiceProviderType
    {
        Azure = 0,
        AWS = 1,
    }

    class AMPQServiceProviderFactory
    {
        public AMPQServiceProviderFactory()
        {
        }

        public AMQPServiceProvider CreateServiceProvider(
            AMQPServiceProviderType type,
            string ServiceNamespace,
            string SASKeyName,
            string SASKeyValue)
        {
            switch (type)
            {
                case AMQPServiceProviderType.Azure:
                    return new AzureAMQPServiceProvider(
                        ServiceNamespace,
                        SASKeyName,
                        SASKeyValue);
                default:
                    return null;
            }
        }
    }
}
