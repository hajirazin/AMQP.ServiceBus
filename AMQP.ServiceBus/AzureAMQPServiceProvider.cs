using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.XPath;
using Windows.Web.Http;
using Windows.Web.Http.Headers;

namespace AMQP.ServiceBus
{
    public class AzureServiceProvider : AMQPServiceProvider
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ServiceNamespace">Azure service bus name space</param>
        /// <param name="SASKeyName">Name of service access key</param>
        /// <param name="SASKeyValue">Value of service access key</param>
        public AzureServiceProvider(string ServiceNamespace, string SASKeyName, string SASKeyValue)
        {
            // cache SAS key name and value
            this.SASKeyName = SASKeyName;
            this.SASKeyValue = SASKeyValue;

            // calculate the address
            this.BaseAddress = "https://" + ServiceNamespace + ".servicebus.windows.net/";
            this.Token = GetSASToken(SASKeyName, SASKeyValue);
            this.AuthScheme = "SharedAccessSignature";
        }

        public override async Task CreateQueue(string queueId)
        {
            // Prepare the body of the create queue request
            string putData = @"<entry xmlns=""http://www.w3.org/2005/Atom"">
                                  <title type=""text"">" + queueId + @"</title>
                                  <content type=""application/xml"">
                                    <QueueDescription xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.microsoft.com/netservices/2010/10/servicebus/connect"" />
                                  </content>
                                </entry>";

            // Create the URI of the new queue, note that this uses the HTTPS scheme
            string queueAddress = BaseAddress + queueId;

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, new Uri(queueAddress));
            request.Headers.Authorization = new HttpCredentialsHeaderValue(AuthScheme, Token);
            request.Content = new HttpStringContent(putData, Windows.Storage.Streams.UnicodeEncoding.Utf8);

            HttpClient httpClient = new HttpClient();
            HttpResponseMessage response = await httpClient.SendRequestAsync(request);

            if (response.StatusCode != Windows.Web.Http.HttpStatusCode.Created)
            {
                throw new Exception(string.Format("Http request [{0}] failed with status code {1}", request.RequestUri, response.StatusCode));
            }
        }

        /// <summary>
        /// Sends a message to the "queueId" queue, given the name and the value to queue or topic
        /// </summary>
        /// <param name="queueId"></param>
        /// <param name="body">message body</param>
        /// <param name="timeToLive">message expiration time in seconds</param>
        /// <returns></returns>
        public override async Task SendMessage(string queueId, string body, int timeToLive = 60 * 60 * 24 * 30)
        {
            string fullAddress = BaseAddress + queueId + "/messages" + "?timeout=60&api-version=2013-08 ";

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, new Uri(fullAddress));
            request.Headers.Authorization = new HttpCredentialsHeaderValue(AuthScheme, Token);

            // Add brokered message properties “TimeToLive” and “Label”.
            request.Headers.Add("BrokerProperties", string.Format("{ \"TimeToLive\":{0}, \"Label\":\"M1\"}", timeToLive));

            // Add custom properties “Priority” and “Customer”.
            request.Headers.Add("Priority", "High");
            request.Headers.Add("Customer", "AzureServiceBusClient");

            request.Content = new HttpStringContent(body, Windows.Storage.Streams.UnicodeEncoding.Utf8);

            HttpClient httpClient = new HttpClient();
            HttpResponseMessage response = await httpClient.SendRequestAsync(request);

            if (response.StatusCode != Windows.Web.Http.HttpStatusCode.Created)
            {
                throw new Exception(string.Format("Http request [{0}] failed with status code {1}", request.RequestUri, response.StatusCode));
            }
        }

        /// <summary>
        /// Receives and deletes the next message from the given resource (queue, topic, or subscriptionId)
        /// using the resourceId and an HTTP DELETE request.
        /// </summary>
        /// <param name="resourceId"></param>
        /// <param name="subscriptionId"></param>
        /// <param name="timeOutSecond"></param>
        /// <returns></returns>
        public override async Task<string> ReceiveAndDeleteMessage(string resourceId, string subscriptionId, int timeOut = 15)
        {
            string queueAddress;
            if (string.IsNullOrWhiteSpace(subscriptionId))
            {
                queueAddress = BaseAddress + resourceId + "/messages/head";
            }
            else
            {
                queueAddress = BaseAddress + resourceId + "/subscriptions/" + subscriptionId + "/messages/head";
            }
            queueAddress += string.Format("?timeout={0}", timeOut);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, new Uri(queueAddress));
            request.Headers.Authorization = new HttpCredentialsHeaderValue(AuthScheme, Token);

            HttpClient httpClient = new HttpClient();
            HttpResponseMessage response = await httpClient.SendRequestAsync(request);

            if (response.StatusCode != Windows.Web.Http.HttpStatusCode.Ok)
            {
                throw new Exception(string.Format("Http request [{0}] failed with status code {1}", request.RequestUri, response.StatusCode));
            }
            else
            {
                // only needed in case of encoding problem.
                //string message = await result.Content.ReadAsStringAsync();
                //string parsedString = Regex.Unescape(message);
                //byte[] isoBites = Encoding.GetEncoding("ISO-8859-1").GetBytes(parsedString);
                //return Encoding.UTF8.GetString(isoBites, 0, isoBites.Length);

                return await response.Content.ReadAsStringAsync();
            }
        }

        /// <summary>
        /// Unlock the top message
        /// </summary>
        /// <param name="unlockAddress">message unlock key returned by "PeekTopMessage"</param>
        /// <returns></returns>
        public override async Task UnlockTopMessage(string unlockAddress)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, new Uri(unlockAddress));
            request.Headers.Authorization = new HttpCredentialsHeaderValue(AuthScheme, Token);

            HttpClient httpClient = new HttpClient();
            HttpResponseMessage response = await httpClient.SendRequestAsync(request);

            if (response.StatusCode != Windows.Web.Http.HttpStatusCode.Ok)
            {
                throw new Exception(string.Format("Http request [{0}] failed with status code {1}", request.RequestUri, response.StatusCode));
            }
        }

        /// <summary>
        /// Peek and lock the top message
        /// </summary>
        /// <param name="resourceId">name of topic or queue</param>
        /// <param name="subscriptionId">subscription Id</param>
        /// <param name="timeOutSecond">timeout</param>
        /// <returns>the lock key used to unlock the message</returns>
        public override async Task<string> PeekTopMessage(string resourceId, string subscriptionId, int timeOut = 15)
        {
            string queueAddress;
            if (string.IsNullOrWhiteSpace(subscriptionId))
            {
                queueAddress = BaseAddress + resourceId + "/messages/head";
            }
            else
            {
                queueAddress = BaseAddress + resourceId + "/subscriptions/" + subscriptionId + "/messages/head";
            }
            queueAddress += string.Format("?timeout={0}", timeOut);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, new Uri(queueAddress));
            request.Headers.Authorization = new HttpCredentialsHeaderValue(AuthScheme, Token);

            HttpClient httpClient = new HttpClient();
            HttpResponseMessage response = await httpClient.SendRequestAsync(request);

            string message = string.Empty;
            if (response.StatusCode != Windows.Web.Http.HttpStatusCode.Created)
            {
                throw new Exception(string.Format("Http request [{0}] failed with status code {1}", request.RequestUri, response.StatusCode));
            }
            else
            {
                message = await response.Content.ReadAsStringAsync();

                // must unlock it here
                await UnlockTopMessage(response.Headers["Location"].ToString());
            }

            return message;
        }

        // Using an HTTP PUT request to create topic
        public override async Task CreateTopic(string topicId)
        {
            // Prepare the body of the create queue request
            var body = @"<entry xmlns=""http://www.w3.org/2005/Atom"">
                                  <title type=""text"">" + topicId + @"</title>
                                  <content type=""application/xml"">
                                    <TopicDescription xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.microsoft.com/netservices/2010/10/servicebus/connect"" />
                                  </content>
                                </entry>";

            string topicAddress = BaseAddress + topicId;

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, new Uri(topicAddress));
            request.Headers.Authorization = new HttpCredentialsHeaderValue(AuthScheme, Token);
            request.Content = new HttpStringContent(body, Windows.Storage.Streams.UnicodeEncoding.Utf8);

            HttpClient httpClient = new HttpClient();
            HttpResponseMessage response = await httpClient.SendRequestAsync(request);

            if (response.StatusCode != Windows.Web.Http.HttpStatusCode.Created)
            {
                throw new Exception(string.Format("Http request [{0}] failed with status code {1}", request.RequestUri, response.StatusCode));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="topicId"></param>
        /// <param name="subscriptionName"></param>
        /// <param name="lockDuration">Duration of lock, checkout: http://www.threeten.org/articles/duration.html </param>
        /// <returns></returns>
        public override async Task CreateSubscription(string topicId, string subscriptionName, string lockDuration = "PT30S")
        {
            // Prepare the body of the create queue request
            var putData = @"<entry xmlns=""http://www.w3.org/2005/Atom"">
                                  <title type=""text"">" + subscriptionName + @"</title>
                                  <content type=""application/xml"">
                                    <SubscriptionDescription xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.microsoft.com/netservices/2010/10/servicebus/connect"" >
                                        <LockDuration>" + lockDuration + @"</LockDuration>
                                        <MaxDeliveryCount>9999999</MaxDeliveryCount>
                                    </SubscriptionDescription>
                                  </content>
                                </entry>";

            var subscriptionAddress = BaseAddress + topicId + "/Subscriptions/" + subscriptionName;

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, new Uri(subscriptionAddress));
            request.Headers.Authorization = new HttpCredentialsHeaderValue(AuthScheme, Token);
            request.Content = new HttpStringContent(putData, Windows.Storage.Streams.UnicodeEncoding.Utf8);

            HttpClient httpClient = new HttpClient();
            HttpResponseMessage response = await httpClient.SendRequestAsync(request);

            if (response.StatusCode != Windows.Web.Http.HttpStatusCode.Created)
            {
                throw new Exception(string.Format("Http request [{0}] failed with status code {1}", request.RequestUri, response.StatusCode));
            }
        }

        public override async Task<List<SubscriptionInfo>> GetSubscriptions(string topicId)
        {
            List<SubscriptionInfo> subscriptions = new List<SubscriptionInfo>();
            string subscriptionListXML = await GetResources(topicId + "/Subscriptions/");

            subscriptionListXML = subscriptionListXML.Replace(" xmlns=\"", " whocares=\"");

            XPathDocument xpd = new XPathDocument(new System.IO.StringReader(subscriptionListXML));

            var navigator = xpd.CreateNavigator();
            navigator.MoveToRoot();

            var it = navigator.Select("//entry");

            while (it.MoveNext())
            {
                SubscriptionInfo subscriptionId = new SubscriptionInfo();
                subscriptionId.Id = it.Current.SelectSingleNode("./title").InnerXml;
                DateTime time = DateTime.Parse(it.Current.SelectSingleNode("./published").InnerXml);
                subscriptionId.Time = (long)time.ToFileTimeUtc();

                subscriptions.Add(subscriptionId);
            }

            return subscriptions;
        }

        public override async Task<List<TopicInfo>> GetTopics()
        {
            List<TopicInfo> topics = new List<TopicInfo>();
            string topicListXML = await GetResources("$Resources/Topics");
            topicListXML = topicListXML.Replace(" xmlns=\"", " whocares=\"");

            XPathDocument xpd = new XPathDocument(new System.IO.StringReader(topicListXML));

            var navigator = xpd.CreateNavigator();
            navigator.MoveToRoot();

            var it = navigator.Select("//entry");

            while (it.MoveNext())
            {
                TopicInfo topic = new TopicInfo();
                topic.Id = it.Current.SelectSingleNode("./title").InnerXml;
                DateTime time = DateTime.Parse(it.Current.SelectSingleNode("./published").InnerXml);
                topic.Time = (long)time.ToFileTimeUtc();

                topics.Add(topic);
            }

            return topics;
        }

        public override async Task DeleteResource(string resourceId)
        {
            string requestAddress = BaseAddress + resourceId;

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, new Uri(requestAddress));
            request.Headers.Authorization = new HttpCredentialsHeaderValue(AuthScheme, Token);

            HttpClient httpClient = new HttpClient();

            HttpResponseMessage response = await httpClient.SendRequestAsync(request);

            if (response.StatusCode != Windows.Web.Http.HttpStatusCode.Ok)
            {
                throw new Exception(string.Format("Http request [{0}] failed with status code {1}", request.RequestUri, response.StatusCode));
            }
        }

        public override async Task<string> GetResources(string resourceId)
        {
            string requestAddress = BaseAddress + resourceId;


            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, new Uri(requestAddress));
            request.Headers.Authorization = new HttpCredentialsHeaderValue(AuthScheme, Token);
            request.Headers.IfModifiedSince = DateTimeOffset.Now;

            HttpClient httpClient = new HttpClient();
            HttpResponseMessage response = await httpClient.SendRequestAsync(request);

            if (response.StatusCode != Windows.Web.Http.HttpStatusCode.Ok)
            {
                throw new Exception(string.Format("Http request [{0}] failed with status code {1}", request.RequestUri, response.StatusCode));
            }
            else
            {
                return await response.Content.ReadAsStringAsync();
            }
        }
    }
}
