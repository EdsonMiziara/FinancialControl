namespace FinancialControl.Lib;

public class Transacao
{
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; }
    public string Tipe { get; set; }
    public string Category { get; set; }

}
