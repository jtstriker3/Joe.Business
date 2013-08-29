using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;

namespace Joe.Business.Resource
{
    public class Resource : Joe.Business.Resource.IResource
    {
        [Key, Column(Order = 0)]
        public String Name { get; set; }
        [Key, Column(Order = 1)]
        public String Culture { get; set; }
        [Key, Column(Order = 2)]
        public String Type { get; set; }
        public String Value { get; set; }
    }
}
