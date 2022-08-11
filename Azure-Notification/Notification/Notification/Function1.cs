using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Threading;
using Microsoft.ServiceBus.Messaging;
using Microsoft.Azure.WebJobs.ServiceBus;
using Azure.Messaging.ServiceBus;
using System.Text;

namespace Notification
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";

            return new OkObjectResult(responseMessage);
        }

        //[FunctionName("QueueSender")]
        //public static void QueueSender(
        //[ServiceBusTrigger("%ConnectionSendName%", Connection = "ConnectionString", AutoCompleteMessages = true )] NotificationDTO input, ServiceBusMessageActions msgActions)
        //{
        //    //Thread.Sleep(TimeSpan.FromSeconds(30));
        //    await msgActions.DeadLetterMessageAsync(msg);
        //    Console.WriteLine(input.notiName);
        //}
        [FunctionName("QueueSender")]
        public static async Task QueueSender(
        [ServiceBusTrigger("%ConnectionSendName%", Connection = "ConnectionString")] ServiceBusReceivedMessage msgs, ServiceBusMessageActions messageActions)
        {
            //var msgBody = JsonConvert.SerializeObject(msgs);
            var notifications = JsonConvert.DeserializeObject<NotificationDTO>(Encoding.UTF8.GetString(msgs.Body));
            await messageActions.CompleteMessageAsync(msgs);
            Thread.Sleep(TimeSpan.FromSeconds(30));
            // Console.WriteLine(input.notiName);
        }
        public class NotificationDTO
        {
            public string notiName { get; set; }
        }
    }
}
