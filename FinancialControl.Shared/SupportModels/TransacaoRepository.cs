using Dapper;
using FinancialControl.Shared.Interfaces;
using FinancialControl.Shared.Models;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

public class TransacaoRepository : ITransacaoRepository
{
    private readonly string _connectionString;

    public TransacaoRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    // ================================
    // INSERT COM GARANTIA DE CATEGORIA
    // ================================
    public async Task InsertAsync(Transacao t)
    {
        using var conexao = new MySqlConnection(_connectionString);
        await conexao.OpenAsync();

        t.CategoriaId = await GarantirCategoriaAsync(t.CategoriaId);

        string sql = @"INSERT INTO transacoes 
            (Data, Descricao, Valor, CategoriaId, Tipo, NomeOriginal)
            VALUES (@Data, @Descricao, @Valor, @CategoriaId, @Tipo, @NomeOriginal);";

        await conexao.ExecuteAsync(sql, t);
    }

    // ================================
    // GARANTIR CATEGORIA
    // ================================
    private async Task<int> GarantirCategoriaAsync(int categoriaId)
    {
        using var conexao = new MySqlConnection(_connectionString);

        var existe = await conexao.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM categorias WHERE Id = @Id",
            new { Id = categoriaId });

        if (existe > 0)
            return categoriaId;

        var categoriaExtra = await conexao.QueryFirstOrDefaultAsync<int?>(
            "SELECT Id FROM categorias WHERE Nome = 'Extra' LIMIT 1");

        if (!categoriaExtra.HasValue)
        {
            categoriaExtra = await conexao.ExecuteScalarAsync<int>(
                @"INSERT INTO categorias (Nome) VALUES ('Extra');
                  SELECT LAST_INSERT_ID();");
        }

        return categoriaExtra.Value;
    }

    // ================================
    // 🔥 NOVO: LISTAR COM NOME DA CATEGORIA
    // ================================
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

    // ================================
    // 🔥 NOVO: RESUMO DO DASHBOARD
    // ================================
    public async Task<(decimal entrada, decimal saida, decimal saldo)> GetResumoAsync()
    {
        using var conexao = new MySqlConnection(_connectionString);

        var entrada = await conexao.ExecuteScalarAsync<decimal>(
            "SELECT IFNULL(SUM(Valor),0) FROM transacoes WHERE Tipo = 'INCOME'");

        var saida = await conexao.ExecuteScalarAsync<decimal>(
            "SELECT IFNULL(SUM(Valor),0) FROM transacoes WHERE Tipo = 'EXPENSE'");

        var saldo = entrada - saida;

        return (entrada, saida, saldo);
    }

    // ================================
    // SEUS MÉTODOS (mantidos)
    // ================================
    public async Task<int> GetOrCreateCategoryIdAsync(string nome)
    {
        using var conexao = new MySqlConnection(_connectionString);

        var id = await conexao.QueryFirstOrDefaultAsync<int?>(
            "SELECT Id FROM categorias WHERE Nome = @Nome",
            new { Nome = nome });

        if (id.HasValue)
            return id.Value;

        var newId = await conexao.ExecuteScalarAsync<int>(
            @"INSERT INTO categorias (Nome) VALUES (@Nome);
              SELECT LAST_INSERT_ID();",
            new { Nome = nome });

        return newId;
    }

    public async Task<string> GetCategoriaNomeAsync(int categoriaId)
    {
        using var conexao = new MySqlConnection(_connectionString);

        var nome = await conexao.QueryFirstOrDefaultAsync<string>(
            "SELECT Nome FROM categorias WHERE Id = @Id",
            new { Id = categoriaId });

        return nome ?? "Extra";
    }

    public async Task<Dictionary<int, string>> GetCategoriasAsync()
    {
        using var conexao = new MySqlConnection(_connectionString);

        var categorias = await conexao.QueryAsync<(int Id, string Nome)>(
            "SELECT Id, Nome FROM categorias");

        return categorias.ToDictionary(c => c.Id, c => c.Nome);
    }
}