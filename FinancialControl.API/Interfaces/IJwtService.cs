using FinancialControl.Shared.Models;

namespace FinancialControl.API.Interfaces;

public interface IJwtService
{
    string GenerateToken(Usuario user);
}
