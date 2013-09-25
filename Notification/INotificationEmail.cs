﻿using System;
namespace Joe.Business.Notification
{
    public interface INotificationEmail
    {
        int ID { get; set; }
        string Message { get; set; }
        string ShortMessage { get; set; }
        string Template { get; set; }
    }
}
