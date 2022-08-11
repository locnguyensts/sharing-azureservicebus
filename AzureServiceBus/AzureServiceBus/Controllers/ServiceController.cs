using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureServiceBus.DTO;
using Newtonsoft.Json;

namespace AzureServiceBus.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ServiceController : ControllerBase
    {
        private static ServiceBusSender notificationSender;
        private static ServiceBusClient client;

        private readonly ILogger<ServiceController> _logger;

        public ServiceController(ILogger<ServiceController> logger)
        {
            client = new ServiceBusClient("Endpoint=sb://locnguyen.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=CSW9BtGtRHmDzRKO6iqRJ/blPaoJr0K9er53pjvzdYU=");
            notificationSender = client.CreateSender("firebase-notification-dead");
            _logger = logger;
        }

        public async Task sendNotification (NotificationDTO input)
        {
            input.notiName = "loc.nguyen";
            var msgBody = JsonConvert.SerializeObject(input);

            await notificationSender.SendMessageAsync(new ServiceBusMessage(msgBody));
        }
    }
}
