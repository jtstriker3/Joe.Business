using Joe.Business.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;

namespace Joe.Business.History
{
    public class History
    {
        [Key, Column(Order = 0)]
        public String Type { get; set; }
        [Key, Column(Order = 1)]
        public String ID { get; set; }
        [Key, Column(Order = 2)]
        public int Version { get; set; }
        public String Data { get; set; }
        public DateTime DateSaved { get; set; }
        public String UpdateByID { get; set; }

        [ForeignKey("UpdateByID")]
        public User UpdatedBy { get; set; }
    }
}
