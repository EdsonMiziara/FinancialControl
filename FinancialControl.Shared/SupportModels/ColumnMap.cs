using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinancialControl.ConsoleApp.SupportModels;

public class ColumnMap
{
    public int HeaderLine { get; set; }
    public int Date { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public int Type { get; set; }
    public int Category { get; set; }
    public int Description { get; set; }
    public int Value { get; set; }
}
