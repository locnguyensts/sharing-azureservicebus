using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using AzureServiceBus.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace AzureServiceBus.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static ServiceBusSender queueSender;
        private static ServiceBusClient client;
        private static ServiceBusSender topicSender;
        private static ServiceBusSender topicLocationSender;
        private static ServiceBusReceiver recev;
        private static ServiceBusAdministrationClient adminClient;
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };
        private static string QUEUE_NAME = "firebase-notification-dead";
        private static string TOPIC_MULTI = "topic-firebase-notification";
        private static string TOPIC_FILTER = "topic-filter-location";
        private static string DEAD_LETTER_PATH = "$deadletterqueue";
        private static string CON_STRING = "Endpoint=sb://locnguyen.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=CSW9BtGtRHmDzRKO6iqRJ/blPaoJr0K9er53pjvzdYU=";
        static string FormatDeadLetterPath() => $"{QUEUE_NAME}/{DEAD_LETTER_PATH}";
        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            client = new ServiceBusClient(CON_STRING);
            queueSender = client.CreateSender("firebase-notification-dead");
            recev = client.CreateReceiver("firebase-notification-dead");
            topicSender = client.CreateSender("topic-firebase-notification-dead");
            topicLocationSender = client.CreateSender("topic-filter-location");
            _logger = logger;
        }

        [HttpPost("SendQueueNoti")]
        public async Task SendQueueNoti(NotificationDTO input)
        {
            //Queue
            var msgBody = JsonConvert.SerializeObject(input);

            await queueSender.SendMessageAsync(new ServiceBusMessage(msgBody));

        }

        [HttpPost("ResubmitDeadLetter")]
        public async Task ResubmitDeadLetter()
        {
            try
            {


                // the received message is a different type as it contains some service set properties
                //ServiceBusReceivedMessage receivedMessage = await recev.ReceiveMessageAsync();
                // receive the dead lettered message with receiver scoped to the dead letter queue.
                ServiceBusReceiver dlqReceiver = client.CreateReceiver(QUEUE_NAME, new ServiceBusReceiverOptions
                {
                    SubQueue = SubQueue.DeadLetter,
                    ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete
                }); ;
                ServiceBusReceivedMessage dlqMessage = await dlqReceiver.ReceiveMessageAsync();

                //using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

                var resubmitMessage = new ServiceBusMessage(dlqMessage);
                await queueSender.SendMessageAsync(resubmitMessage);
                //throw new Exception("aa"); // - to prove the transaction
                //  await recev.CompleteMessageAsync(receivedMessage);
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        [HttpPost("SendTopicNoti")]
        public async Task SendTopicNoti(NotificationDTO input)
        {
            //Topic
            var msgBody = JsonConvert.SerializeObject(input);

            await topicSender.SendMessageAsync(new ServiceBusMessage(msgBody));
        }

        [HttpPost("CreateSubscription")]
        public async Task CreateSubscription(TopicDTO input)
        {
            adminClient = new ServiceBusAdministrationClient(CON_STRING);
            //Create Subscription
            await adminClient.CreateSubscriptionAsync(
                       new CreateSubscriptionOptions(TOPIC_FILTER, $"Location{input.location}"),
                       new CreateRuleOptions("ByLocation", new SqlRuleFilter($"location={input.location}")));
        }

        [HttpPost("SendLocation")]
        public async Task SendMessagesLocationToTopicAsync()
        {
            // Create the clients that we'll use for sending and processing messages.
            client = new ServiceBusClient(CON_STRING);
            topicLocationSender = client.CreateSender(TOPIC_FILTER);

            Console.WriteLine("\nSending orders to topic.");

            // Now we can start sending orders.
            await Task.WhenAll(
                SendLocationMessage(new LocationDTO()),
                SendLocationMessage(new LocationDTO { Location = "VN", Quantity = 5, Priority = "low" }),
                SendLocationMessage(new LocationDTO { Location = "USA", Quantity = 10, Priority = "high" }),
                SendLocationMessage(new LocationDTO { Location = "USA", Quantity = 9, Priority = "low" }),
                SendLocationMessage(new LocationDTO { Location = "CAN", Quantity = 5, Priority = "low" }),
                SendLocationMessage(new LocationDTO { Location = "CAN", Quantity = 8, Priority = "high" }),
                SendLocationMessage(new LocationDTO { Location = "CAN", Quantity = 8, Priority = "high" })
                );

            Console.WriteLine("All messages sent.");
        }
        private async Task SendLocationMessage(LocationDTO location)
        {
            var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(location)))
            {
                CorrelationId = location.Priority,
                Subject = location.Location,
                ApplicationProperties =
                {
                    { "location", location.Location },
                    { "quantity", location.Quantity },
                    { "priority", location.Priority }
                }
            };
            await topicLocationSender.SendMessageAsync(message);
        }
        class LocationDTO
        {
            public string Location
            {
                get;
                set;
            }

            public int Quantity
            {
                get;
                set;
            }

            public string Priority
            {
                get;
                set;
            }
        }
    }
}
