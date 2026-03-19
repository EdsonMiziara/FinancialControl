using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinancialControl.Shared.CacheHolder;

public class CategoriaCache
{
    public int Id { get; set; }
    public string Nome { get; set; } // 🔥 obrigatório
    public List<RegraCache> Regras { get; set; }
}
