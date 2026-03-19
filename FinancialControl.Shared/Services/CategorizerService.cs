using FinancialControl.Shared.CacheHolder;
using FinancialControl.Shared.Models;
using FuzzySharp;
using Microsoft.EntityFrameworkCore;

public class CategorizerService
{
    private readonly CategorizerCache _cache;
    private readonly AppDbContext _context;

    public CategorizerService(CategorizerCache cache, AppDbContext context)
    {
        _cache = cache;
        _context = context;
    }

    public int Identify(string description, decimal value)
    {
        if (string.IsNullOrWhiteSpace(description))
            return 0;

        description = Categorizer.CleanText(description);

        var scores = new Dictionary<int, double>();

        // 1. MATCH DIRETO
        foreach (var categoria in _cache.Categorias)
        {
            double score = 0;

            foreach (var regra in categoria.Regras)
            {
                if (description.Contains(regra.Termo))
                {
                    score += 2 * regra.Peso;
                }
            }

            if (score > 0)
                scores[categoria.Id] = score;
        }

        // 2. FUZZY
        if (!scores.Any())
        {
            foreach (var categoria in _cache.Categorias)
            {
                double score = 0;

                foreach (var regra in categoria.Regras)
                {
                    int similarity = Fuzz.PartialRatio(description, regra.Termo);

                    if (similarity > 85)
                        score += 3 * regra.Peso;
                }

                if (score > 0)
                    scores[categoria.Id] = score;
            }
        }

        // 3. APRENDIZADO (PRIORIDADE)
        foreach (var aprendizado in _cache.Aprendizados)
        {
            int similarity = Fuzz.PartialRatio(description, aprendizado.Descricao);

            if (similarity > 90)
            {
                if (!scores.ContainsKey(aprendizado.CategoriaId))
                    scores[aprendizado.CategoriaId] = 0;

                scores[aprendizado.CategoriaId] += 10 * aprendizado.Peso;
            }
        }

        if (!scores.Any())
        {
            return 0; // categoria padrão
        }

        return scores.OrderByDescending(x => x.Value).First().Key;
    }
    public async Task LearnAsync(string descricao, int categoriaId)
    {
        var limpa = Categorizer.CleanText(descricao);

        var existente = await _context.Set<AprendizadoCategoria>()
            .FirstOrDefaultAsync(x => x.DescricaoLimpa == limpa);

        if (existente != null)
        {
            existente.Vezes++;
            existente.CategoriaId = categoriaId;
        }
        else
        {
            existente = new AprendizadoCategoria
            {
                Descricao = descricao,
                DescricaoLimpa = limpa,
                CategoriaId = categoriaId,
                Vezes = 1
            };

            _context.Add(existente);
        }

        await _context.SaveChangesAsync();

        _cache.Aprendizados.Add(new AprendizadoCache
        {
            Descricao = limpa,
            CategoriaId = categoriaId,
            Peso = existente.Vezes
        });
    }
}
