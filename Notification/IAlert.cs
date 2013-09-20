using System;
namespace Joe.Business.Notification
{
    public interface IAlert
    {
        DateTime AlertDate { get; set; }
        int ID { get; set; }
        string Message { get; set; }
        bool Read { get; set; }
    }
}
