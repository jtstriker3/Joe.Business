using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Joe.Business
{
    public interface IEmail<T>
    {
        System.Collections.Generic.ICollection<String> BCC { get; set; }
        System.Collections.Generic.ICollection<String> CC { get; set; }
        System.Collections.Generic.ICollection<String> To { get; set; }
        T Model { get; set; }
    }

    public interface IEmailProvider
    {
        void SendMail<T>(IEmail<T> email);
    }
}
