using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace Joe.Business
{
    public interface IManyToMany
    {
        [Required(ErrorMessage = "A Valid Entry is Required for this Field, Please Select from the Prompted List.")]
        [Range(1, 9999999999999, ErrorMessage = "A Valid Entry is Required for this Field, Please Select from the Prompted List.")]
        int ID1 { get; set; }
        [Required(ErrorMessage = "A Valid Entry is Required for this Field, Please Select from the Prompted List.")]
        [Range(1, 9999999999999, ErrorMessage = "A Valid Entry is Required for this Field, Please Select from the Prompted List.")]
        int ID2 { get; set; }
        String Name1 { get; set; }
        String Name2 { get; set; }
        Boolean Included { get; set; }
    }
}
