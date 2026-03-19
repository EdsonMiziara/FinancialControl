using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinancialControl.Shared.Models;

public class RegraCategoria
{
    public int Id { get; set; }

    public int CategoriaId { get; set; }
    public Categoria Categoria { get; set; }

    public string PalavraChave { get; set; }
    public int Peso { get; set; }
}
