using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

public static class Categorizer
{
    private static readonly Dictionary<string, string[]> DefaultCategoriesRules = new()
    {
        { "PRINCIPAL", new[] { "PORTABILIDADE", "MESMA TIT", "CRED TED", "CREDITO FORNEC", "IDCONTROLEENVIOTED", "RECEBIMENTO PIX EDSON ELIAS MIZIARA FILHO 285 946" } },
        { "IMPOSTOS", new[] { "PAGTO DAS", "RECEITA FEDERAL", "DARF", "SIMPLES NACIONAL", "TRIBUTOS FEDERAIS" } },
        { "TARIFAS BANCARIAS", new[] { "TAR ", "CESTA", "MANUTENCAO", "ANUIDADE" } },
        { "ENERGIA AGUA LUZ", new[] { "CEMIG", "CODEN", "SAAE", "ENERGISA", "CODAU" } },
        { "SOFTWARE INFRA", new[] { "AWS", "AZURE", "GOOGLE", "JETBRAINS", "GITHUB" } },
        { "PRO-LABORE PESSOAL", new[] { "TRANSF TITULARIDADE", "SAQUE", "CONTA SALARIO" } },
        { "EXTRA", new[] { "PIX", "TRANSFERENCIA", "TED", "DOC", "DEPOSITO" } },
        { "SAUDE", new[] { "DROGASIL1204 UBERABA BRA", "Uberaba SAO PAULO BRA", "TOTAL FARMA UBERABA BRA" } },
        { "COMIDA", new[] { "IFD", "TRIGALLE"} },
        { "MERCADO", new[] { "SUPERMERCADO", "MERCADO", "CARREFOUR", "PÃO DE AÇÚCAR", "ZEMA", "BRETAS", "GUARATO", "BAHAMAS", "SUPERMERCADOS BH" } },
        { "TRANSPORTE", new[] { "UBER", "99 ", "TÁXI", "METRÔ" } },
        { "LAZER", new[] { "NETFLIX", "SPOTIFY", "AMAZON PRIME", "DISNEY+" } }

    };

    // Este dicionário será preenchido com todos os textos já limpos e sem acentos
    private static readonly Dictionary<string, string[]> CategoriesRules;

    static Categorizer()
    {
        CategoriesRules = DefaultCategoriesRules.ToDictionary(
            kvp => CleanText(kvp.Key), // Limpa a chave (ex: ÁGUA -> AGUA)
            kvp => kvp.Value.Select(term => CleanText(term)).ToArray() // Limpa cada termo de busca
        );
    }

    public static string Identify(string description, decimal value)
    {
        if (string.IsNullOrWhiteSpace(description))
            return "A CLASSIFICAR";

        description = CleanText(description);

        var scores = new Dictionary<string, int>();

        foreach (var rule in CategoriesRules)
        {
            int score = 0;

            foreach (var term in rule.Value)
            {
                if (string.IsNullOrWhiteSpace(term)) continue;

                // Match exato (mais forte)
                if (Regex.IsMatch(description, $@"\b{Regex.Escape(term)}\b"))
                {
                    score += 3;

                    // bônus para termos grandes (mais específicos)
                    if (term.Length > 10)
                        score += 2;
                }
                // Match parcial (mais fraco)
                else if (description.Contains(term))
                {
                    score += 1;
                }
            }

            if (score > 0)
                scores[rule.Key] = score;
        }

        if (!scores.Any())
        {
            if (description.Contains("PIX") && value < 0)
                return "PIX ENVIADO (VERIFICAR)";

            return "EXTRA";
        }
        // pega a categoria com maior score
        var best = scores
            .OrderByDescending(x => x.Value)
            .First();

        return best.Key;
    }

    public static string RemoveAccents(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        // 1. Separa os caracteres base dos seus acentos (Ex: 'á' vira 'a' + '´')
        string normalizedString = text.Normalize(NormalizationForm.FormD);
        StringBuilder sb = new StringBuilder();

        foreach (char c in normalizedString)
        {
            // 2. Mantém apenas a letra base, ignorando a marca gráfica do acento
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        // 3. Remonta a string, joga para maiúsculo e remove espaços duplos/sobras
        string result = sb.ToString().Normalize(NormalizationForm.FormC).ToUpper();
        return Regex.Replace(result, @"\s+", " ").Trim();
    }

    public static string CleanText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        // 1. PRIMEIRO removemos os acentos (para garantir que letras acentuadas sejam salvas)
        string withoutAccents = RemoveAccents(text);

        // 2. AGORA substituímos qualquer coisa que NÃO seja A-Z, 0-9 ou espaço por espaço vazio.
        string noSpecials = Regex.Replace(withoutAccents, @"[^a-zA-Z0-9\s]", " ");

        // 3. Normaliza os espaços finais gerados pela substituição acima
        return Regex.Replace(noSpecials, @"\s+", " ").Trim();
    }
}