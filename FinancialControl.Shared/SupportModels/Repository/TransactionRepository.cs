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

        string sql = @"INSERT INTO transactions 
            (Date, Description, Value, CategoryId, Type, OriginalName)
            VALUES (@Date, @Description, @Value, @CategoryId, @Type, @OriginalName);";

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
            t.Date,
            t.Value,
            t.Description,
            t.Type,
            c.Name AS Category,
            t.OriginalName
        FROM transactions t
        LEFT JOIN categories c ON c.Id = t.CategoryId
        ORDER BY t.Date DESC;
        ";

        return await conexao.QueryAsync(sql);
    }
    /// <summary>
    /// Calculates the financial summary: total income, total expenses and balance (income - expenses)
    /// </summary>
    /// <param name="income"></param>
    /// <param name="expense"></param>
    /// <param name="balance"></param>
    /// <returns>
    /// Returns a tuple with the following values: entrada (total income), saida (total expenses) and saldo (balance)
    /// </returns>

    public async Task<(decimal income, decimal expense, decimal balance)> GetResumeAsync()
    {
        using var conection = new MySqlConnection(_connectionString);

        var income = await conection.ExecuteScalarAsync<decimal>(
            "SELECT IFNULL(SUM(Valor),0) FROM transactions WHERE Tipo = 'INCOME'");

        var expense = await conection.ExecuteScalarAsync<decimal>(
            "SELECT IFNULL(SUM(Valor),0) FROM transactions WHERE Tipo = 'EXPENSE'");

        var saldo = income - expense;

        return (income, expense, saldo);
    }

    /// <summary>
    /// Verify if a transaction with the same date,
    /// value and description already exists (ignoring case sensitivity).
    /// This is used to avoid importing duplicate transactions from OFX files.
    /// </summary>
    /// <param name="date"></param>
    /// <param name="value"></param>
    /// <param name="description"></param>
    /// <returns>
    /// Returns true if a transaction with the same date, value and description already exists; otherwise, false.
    /// </returns>
    
    public async Task<bool> ExistsTransactionAsync(DateTime date, decimal value, string description)
    {
        using var conexao = new MySqlConnection(_connectionString);

        var existe = await conexao.ExecuteScalarAsync<int>(
            @"SELECT COUNT(1) 
          FROM transactions 
          WHERE Date = @Date 
          AND Value = @Value
          AND LOWER(Description) = LOWER(@Description)",
            new
            {
                Description = description,
                Value = value,
                Date = date
            });

        return existe > 0;
    }
}