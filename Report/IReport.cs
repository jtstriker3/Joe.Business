using Joe.Map;
using System;
namespace Joe.Business.Report
{
    public interface IReport
    {
        string Description { get; }
        System.Collections.Generic.IEnumerable<ReportFilter> Filters { get; set; }
        [ViewMapping(ReadOnly = true)]
        Type ListView { get; }
        [ViewMapping(ReadOnly = true)]
        System.Collections.Generic.IEnumerable<string> ListViewDisplayProperties { get; }
        [ViewMapping(ReadOnly = true)]
        Type Model { get; }
        [ViewMapping(ReadOnly = true)]
        string Name { get; set; }
        [ViewMapping(ReadOnly = true)]
        Type ReportRepo { get; }
        [ViewMapping(ReadOnly = true)]
        Type ReportView { get; }
        bool Single { get; }
        [ViewMapping(ReadOnly = true)]
        System.Collections.IEnumerable SingleChoices { get; set; }
        string SingleID { get; set; }
        String UiHint { get; }
        Boolean Chart { get; }
        String Group { get; }
        String SubGroup { get; }
        Boolean IsCustomRepository { get; }
    }
}
