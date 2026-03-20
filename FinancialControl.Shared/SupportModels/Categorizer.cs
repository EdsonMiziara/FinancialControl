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

    private static readonly Dictionary<string, string[]> CategoriesRules;

    /// <summary>
    /// Constructor for the Categorizer class that initializes the category rules 
    /// by cleaning the text of both the category keys and their associated search terms.
    /// </summary>
    static Categorizer()
    {
        CategoriesRules = DefaultCategoriesRules.ToDictionary(
            kvp => CleanText(kvp.Key), // Limpa texto da chave 
            kvp => kvp.Value.Select(term => CleanText(term)).ToArray() // Limpa cada texto de termo de busca
        );
    }


    /// <summary>
    /// Categorize a descption based on predefined rules.
    /// It uses a scoring system where exact matches score higher than partial matches,
    /// and longer terms (more specific) score more. If no category matches,
    /// it checks for "PIX" in the description with a negative value to classify as "PIX ENVIADO (VERIFICAR)",
    /// otherwise it defaults to "EXTRA".
    /// </summary>
    /// <param name="description"></param>
    /// <param name="value"></param>
    /// <returns>
    /// Returns the identified category as a string. If the description is empty or null, it returns "A CLASSIFICAR".
    /// </returns>
    
    public static string Identify(string description, decimal value)
    {
        if (string.IsNullOrWhiteSpace(description))
            return "A CLASSIFICAR";

        description = CleanText(description);

        //Scorificação para cada categoria: match exato vale mais que match parcial, e termos mais específicos (maiores) valem mais
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

    /// <summary>
    /// Remove acentos e caracteres especiais de uma string, mantendo apenas letras (A-Z), números (0-9) e espaços.
    /// </summary>
    /// <param name="text"></param>
    /// <returns>
    /// Returns a cleaned version of the input string with accents removed,
    /// converted to uppercase, and with multiple spaces normalized to a single space.
    /// </returns>

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
    /// <summary>
    /// Clean Text Removing accents, special characters and normalizing spaces.
    /// </summary>
    /// <param name="text"></param>
    /// <returns>
    /// Returns a cleaned version of the input string with accents removed, special characters replaced by spaces,
    /// </returns>

    public static string CleanText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        // 1. PRIMEIRO removemos os acentos 
        string withoutAccents = RemoveAccents(text);

        // 2. substitui qualquer coisa que NÃO seja A-Z, 0-9 ou espaço por espaço vazio.
        string noSpecials = Regex.Replace(withoutAccents, @"[^a-zA-Z0-9\s]", " ");

        // 3. Normaliza os espaços finais gerados pela substituição acima
        return Regex.Replace(noSpecials, @"\s+", " ").Trim();
    }
}