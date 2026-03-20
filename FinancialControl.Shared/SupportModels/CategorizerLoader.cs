using FinancialControl.Shared.CacheHolder;
using FinancialControl.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace FinancialControl.Shared.SupportModels;
public class CategorizerLoader
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Constructor for CategorizerLoader that initializes the loader with a database context.
    /// </summary>
    /// <param name="context"></param>
    public CategorizerLoader(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Loads the categorizer cache by fetching categories and their associated rules,
    /// as well as learning data from the database.
    /// This method constructs a CategorizerCache object that can be used for
    /// efficient categorization of financial transactions based on the loaded data.
    /// </summary>
    /// <returns>
    /// Returns a Task that represents the asynchronous operation of loading the categorizer cache.
    /// </returns>
    public async Task<CategorizerCache> LoadAsync()
    {
        var categorias = await _context.Categorias
            .Include(c => c.Rules)
            .ToListAsync();

        var aprendizados = await _context.Set<LearningCategory>().ToListAsync();

        return new CategorizerCache
        {
            Categorias = categorias.Select(c => new CategoriaCache
            {
                Id = c.Id,
                Nome = c.Name,
                Regras = c.Rules.Select(r => new RegraCache
                {
                    Termo = Categorizer.CleanText(r.KeyWord),
                    Peso = r.weight
                }).ToList()
            }).ToList(),

            Aprendizados = aprendizados.Select(a => new AprendizadoCache
            {
                Descricao = a.CleanDescription,
                CategoriaId = a.CategoryId,
                Peso = a.Times
            }).ToList()
        };
    }
}
