using FinancialControl.Shared.CacheHolder;
using FinancialControl.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace FinancialControl.Shared.SupportModels;
public class CategorizerLoader
{
    private readonly AppDbContext _context;

    public CategorizerLoader(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CategorizerCache> LoadAsync()
    {
        var categorias = await _context.Categorias
            .Include(c => c.Regras)
            .ToListAsync();

        var aprendizados = await _context.Set<AprendizadoCategoria>().ToListAsync();

        return new CategorizerCache
        {
            Categorias = categorias.Select(c => new CategoriaCache
            {
                Id = c.Id,
                Nome = c.Nome,
                Regras = c.Regras.Select(r => new RegraCache
                {
                    Termo = Categorizer.CleanText(r.PalavraChave),
                    Peso = r.Peso
                }).ToList()
            }).ToList(),

            Aprendizados = aprendizados.Select(a => new AprendizadoCache
            {
                Descricao = a.DescricaoLimpa,
                CategoriaId = a.CategoriaId,
                Peso = a.Vezes
            }).ToList()
        };
    }
}
