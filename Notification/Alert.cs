using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace Joe.Business.Notification
{
    public class Alert : Joe.Business.Notification.IAlert
    {
        public int ID { get; set; }
        public DateTime AlertDate { get; set; }
        [Required]
        public String Message { get; set; }
        public Boolean Read { get; set; }
        public String UserID { get; set; }
        public virtual User User { get; set; }
        public String ShortMessage { get; set; }
    }
}
