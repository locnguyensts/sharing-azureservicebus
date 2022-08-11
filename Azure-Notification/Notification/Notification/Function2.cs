using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using System.Collections.Generic;
using Notification.Services;

namespace Notification
{
    public class Function2
    {
        //Telegram bot
        private TelegramBotClient bot = new TelegramBotClient("5399836438:AAFFoPSojJk0uhYPRIGxs9l2EyDeXIUaTv0");

        //Slack bot
        private const string _randomChannel = "/services/T03TH6XFEDP/B03THG08CSD/e6urlHvIhxH5kHqQyOeqqmu1";


        private readonly ILogger<Function2> _logger;

        
        public Function2(ILogger<Function2> log)
        {
            _logger = log;
        }
        #region Topic
        [FunctionName("Function2")]
        public void Run([ServiceBusTrigger("%TopicSendName%", "AzureSubscription", Connection = "ConnectionString")] NotificationDTO input)
        {
            Console.WriteLine(input.notiName);
        }
        #endregion

        #region Telegram bot
        //Telegram bot
        [FunctionName("Telegram")]
        public async Task RunTele([ServiceBusTrigger("%TopicFilterLocation%", "LocationVN", Connection = "ConnectionString")] NotificationDTO input)
        {
            ChatId chat = new ChatId("-738921423");
            await bot.SendTextMessageAsync(chatId: chat, text: "My mentor is Tai Vo");
        }
        #endregion

        #region Slack bot
      
        //Slack bot
        [FunctionName("Slack")]
        public async Task SendMessageOnRandomAsync([ServiceBusTrigger("%TopicFilterLocation%", "LocationUSA", Connection = "ConnectionString")] NotificationDTO input)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://hooks.slack.com");
                input.notiName = "My mentor is Tai Map";
                var contentObject = new { text = input.notiName };
                var contentObjectJson = JsonSerializer.Serialize(contentObject);
                var content = new StringContent(contentObjectJson, Encoding.UTF8, "application/json");

                var result = await client.PostAsync(_randomChannel, content);
                var resultContent = await result.Content.ReadAsStringAsync();
                if (!result.IsSuccessStatusCode)
                {
                    throw new Exception("Task failed.");
                }
            };
        }
        #endregion
        public class NotificationDTO
        {
            public string notiName { get; set; }
        }

    }
}
