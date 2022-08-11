using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Notification.Services
{
    public interface ISlackMessageSender
    {
        Task SendMessageOnRandomAsync(string text);
    }
}
