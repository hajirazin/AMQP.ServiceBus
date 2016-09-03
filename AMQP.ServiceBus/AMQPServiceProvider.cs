using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AMQP.ServiceBus
{
    public class TopicInfo
    {
        public string Id { get; set; }

        public long Time { get; set; }
    }

    public class SubscriptionInfo
    {
        public string Id { get; set; }

        public long Time { get; set; }
    }

    public abstract class AMQPServiceProvider
    {
        protected string SASKeyName { get; set; }
        protected string SASKeyValue { get; set; }
        protected string AuthScheme { get; set; }
        protected string BaseAddress { get; set; }
        protected string Token { get; set; }

        protected virtual string GetSASToken(string SASKeyName, string SASKeyValue)
        {
            TimeSpan fromEpochStart = DateTime.UtcNow - new DateTime(1970, 1, 1);
            string expiry = Convert.ToString((int)fromEpochStart.TotalSeconds + 3600);
            string stringToSign = WebUtility.UrlEncode(BaseAddress) + "\n" + expiry;
            HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(SASKeyValue));

            string signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));
            string sasToken = String.Format(CultureInfo.InvariantCulture, "sr={0}&sig={1}&se={2}&skn={3}",
                WebUtility.UrlEncode(BaseAddress), WebUtility.UrlEncode(signature), expiry, SASKeyName);
            return sasToken;
        }

        public virtual async Task CreateQueue(string queueId)
        {
            await Task.FromException(new NotImplementedException());
        }

        public async Task DeleteQueue(string queueId)
        {
            await DeleteResource(queueId);
        }

        public virtual async Task CreateTopic(string topicId)
        {
            await Task.FromException(new NotImplementedException());
        }

        public async Task DeleteTopic(string topicId)
        {
            await DeleteResource(topicId);
        }

        public async Task DeleteSubscription(string topicId, string subscriptionId)
        {
            await DeleteResource(topicId + "/Subscriptions/" + subscriptionId);
        }

        public virtual async Task CreateSubscription(string topicId, string subscriptionName, string lockDuration = "PT30S")
        {
            await Task.FromException(new NotImplementedException());
        }

        public virtual async Task<string> ReceiveAndDeleteMessage(string resourceId, string subscriptionId, int timeOut)
        {
            await Task.FromException(new NotImplementedException());
            return string.Empty;
        }

        public virtual async Task<string> PeekTopMessage(string resourceId, string subscriptionId, int timeOutSecond)
        {
            await Task.FromException(new NotImplementedException());
            return string.Empty;
        }

        public virtual async Task SendMessage(string queueId, string body, int timeToLive = 60 * 60 * 24 * 30)
        {
            await Task.FromException(new NotImplementedException());
        }

        public virtual async Task<string> GetResources(string resourceAddress)
        {
            await Task.FromException(new NotImplementedException());
            return string.Empty;
        }

        public virtual async Task DeleteResource(string resourceId)
        {
            await Task.FromException(new NotImplementedException());
        }

        public virtual async Task<List<SubscriptionInfo>> GetSubscriptions(string topicId)
        {
            await Task.FromException(new NotImplementedException());
            return new List<SubscriptionInfo>();
        }

        public virtual async Task<List<TopicInfo>> GetTopics()
        {
            await Task.FromException(new NotImplementedException());
            return new List<TopicInfo>();
        }

        public virtual async Task UnlockTopMessage(string unlockAddress)
        {
            await Task.FromException(new NotImplementedException());
        }
    }
}
