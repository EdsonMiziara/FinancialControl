namespace FinancialControl.API.Models;

public class JwtSettings
{
    public string Key { get; set; }
    public int ExpirationHours { get; set; }
}
