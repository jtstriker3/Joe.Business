using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Joe.Business.Report
{
    public class PlotLine
    {
        public String Color { get; set; }
        public Double Value { get; set; }
        public int Width { get; set; }
        public DashStyle DashStyle { get; set; }
        public Label Label { get; set; }
    }
}
