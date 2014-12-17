using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Joe.Business.Report
{
    public interface ICustomRepository<TFilters>
    {
        Object RunReport(TFilters filters);
    }
}
