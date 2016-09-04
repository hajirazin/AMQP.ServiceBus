using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AMQP.ServiceBus.Sample
{
    class sample
    {
        public static string ServiceNamespace
        {
            get { return "your-service-namespace"; }
        }

        public static string SASKeyName
        {
            get
            {
                return "RootManageSharedAccessKey";
            }
        }

        public static string SASKeyValue
        {
            get
            {
                return "key-for-read-and-write-access";
            }
        }

        public async Task TestAMQPService()
        {
            AMQPServiceProvider provider = AMQPServiceProviderFactory.CreateServiceProvider(
                ServiceProviderType.Azure,
                ServiceNamespace,
                SASKeyName,
                SASKeyValue);

            string topicName = "MyTopic";
            string queueName = "MyQueue";
            string subName = "MySubscription";

            // create topic and its subscription
            await provider.CreateQueue(topicName);
            await provider.CreateSubscription(topicName, subName);

            // send mesage to topic
            await provider.SendMessage(topicName, "test message");
            // read and delete the message from subscription.
            var topicMessage = await provider.ReceiveAndDeleteMessage(topicName, subName);
            // delete topic
            await provider.DeleteTopic(topicName);

            // create queue
            await provider.CreateTopic(queueName);
            // send message to queue;
            await provider.SendMessage(queueName, string.Empty);
            // read and delete the message from queue
            var queueMessage = await provider.ReceiveAndDeleteMessage(queueName, string.Empty);
            // delete queue
            await provider.DeleteQueue(queueName);



        }
    }
}
