using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinancialControl.Shared.CacheHolder;

public class CategorizerCache
{
    public List<CategoriaCache> Categorias { get; set; } = new();
    public List<AprendizadoCache> Aprendizados { get; set; } = new();
}
