using FinancialControl.Shared.Models;

namespace FinancialControl.Shared.Interfaces;

public interface ITransacaoRepository
{
    Task InsertAsync(Transaction transacao);
    Task<bool> ExistsTransactionAsync(DateTime date, decimal value, string description);
    Task<IEnumerable<dynamic>> GetAllAsync();
    Task<(decimal income, decimal expense, decimal balance)> GetResumeAsync();

}