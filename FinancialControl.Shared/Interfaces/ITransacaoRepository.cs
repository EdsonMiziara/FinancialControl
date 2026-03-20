using FinancialControl.Shared.Models;

namespace FinancialControl.Shared.Interfaces;

public interface ITransacaoRepository
{
    Task InsertAsync(Transaction transacao);
    Task<bool> ExistsTransactionAsync(DateTime data, decimal valor, string descricao);
    Task<IEnumerable<dynamic>> GetAllAsync();
    Task<(decimal entrada, decimal saida, decimal saldo)> GetResumeAsync();

}