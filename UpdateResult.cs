using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Joe.Business
{

    public class Result
    {
        public IList<ValidationWarning> Warnings { get; set; }
    }

    public class Result<TViewModel> : Result
    {
        public TViewModel ViewModel { get; internal set; }

        public Result(TViewModel viewModel)
        {
            ViewModel = viewModel;
            Warnings = new List<ValidationWarning>();
        }

    }

}
