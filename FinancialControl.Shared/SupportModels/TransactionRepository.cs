using Dapper;
using FinancialControl.Shared.Interfaces;
using FinancialControl.Shared.Models;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

public class TransactionRepository : ITransacaoRepository
{
    private readonly string _connectionString;
    private readonly ICategoryRepository _categoryRepository;

    /// <summary>
    /// Constructor for TransactionRepository that initializes the repository with a database connection string
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="categoryRepository"></param>
    public TransactionRepository(IConfiguration configuration, ICategoryRepository categoryRepository)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
        _categoryRepository = categoryRepository;
    }
    /// <summary>
    /// Insert transaction in Db, ensuring the category exists (if not, it will be created)
    /// </summary>
    /// <param name="t"></param>

    public async Task InsertAsync(Transaction t)
    {
        using var conexao = new MySqlConnection(_connectionString);
        await conexao.OpenAsync();

        t.CategoryId = await _categoryRepository.EnsureCategoryAsync(t.CategoryId);

        string sql = @"INSERT INTO transacoes 
            (Data, Descricao, Valor, CategoriaId, Tipo, NomeOriginal)
            VALUES (@Date, @Description, @Value, @CategoryId, @Tipe, @OriginalName);";

        await conexao.ExecuteAsync(sql, t);
    }

    /// <summary>
    /// Lits all transaction with category name, ordered by date desc. 
    /// </summary>
    /// <returns>
    /// It returns a dynamic object with the following properties: 
    /// id, data, valor, descricao, tipo, categoria (category name) and nomeOriginal
    /// </returns>

    public async Task<IEnumerable<dynamic>> GetAllAsync()
    {
        using var conexao = new MySqlConnection(_connectionString);

        string sql = @"
        SELECT 
            t.Id,
            t.Data,
            t.Valor,
            t.Descricao,
            t.Tipo,
            c.Nome AS Categoria,
            t.NomeOriginal
        FROM transacoes t
        LEFT JOIN categorias c ON c.Id = t.CategoriaId
        ORDER BY t.Data DESC;
        ";

        return await conexao.QueryAsync(sql);
    }
    /// <summary>
    /// Calculates the financial summary: total income, total expenses and balance (income - expenses)
    /// </summary>
    /// <returns>
    /// Returns a tuple with the following values: entrada (total income), saida (total expenses) and saldo (balance)
    /// </returns>
    
    public async Task<(decimal entrada, decimal saida, decimal saldo)> GetResumeAsync()
    {
        using var conexao = new MySqlConnection(_connectionString);

        var entrada = await conexao.ExecuteScalarAsync<decimal>(
            "SELECT IFNULL(SUM(Valor),0) FROM transacoes WHERE Tipo = 'INCOME'");

        var saida = await conexao.ExecuteScalarAsync<decimal>(
            "SELECT IFNULL(SUM(Valor),0) FROM transacoes WHERE Tipo = 'EXPENSE'");

        var saldo = entrada - saida;

        return (entrada, saida, saldo);
    }

    /// <summary>
    /// Verify if a transaction with the same date,
    /// value and description already exists (ignoring case sensitivity).
    /// This is used to avoid importing duplicate transactions from OFX files.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="valor"></param>
    /// <param name="descricao"></param>
    /// <returns>
    /// Returns true if a transaction with the same date, value and description already exists; otherwise, false.
    /// </returns>
    
    public async Task<bool> ExistsTransactionAsync(DateTime data, decimal valor, string descricao)
    {
        using var conexao = new MySqlConnection(_connectionString);

        var existe = await conexao.ExecuteScalarAsync<int>(
            @"SELECT COUNT(1) 
          FROM transacoes 
          WHERE Data = @Data 
          AND Valor = @Valor 
          AND LOWER(Descricao) = LOWER(@Descricao)",
            new
            {
                Data = data,
                Valor = valor,
                Descricao = descricao
            });

        return existe > 0;
    }
}