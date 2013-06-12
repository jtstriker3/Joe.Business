using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Joe.Business
{
    public class Email<T> : IEmail<T>
    {
        public ICollection<String> To { get; set; }
        public ICollection<String> CC { get; set; }
        public ICollection<String> BCC { get; set; }
        public T Model { get; set; }

        public Email()
        {
            To = new List<String>();
            CC = new List<String>();
            BCC = new List<String>();
        }
    }
}
