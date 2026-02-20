using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

public static class Categorizer
{
    private static readonly Dictionary<string, string[]> DefaultCategoriesRules = new()
    {
        { "PRINCIPAL", new[] { "PORTABILIDADE", "MESMA TIT", "CRED TED", "CREDITO FORNEC" } },
        { "IMPOSTOS", new[] { "PAGTO DAS", "RECEITA FEDERAL", "DARF", "SIMPLES NACIONAL", "TRIBUTOS FEDERAIS" } },
        { "TARIFAS BANCARIAS", new[] { "TAR ", "CESTA", "MANUTENCAO", "ANUIDADE" } },
        { "ENERGIA AGUA LUZ", new[] { "CEMIG", "CODEN", "SAAE", "ENERGISA", "CODAU" } },
        { "SOFTWARE INFRA", new[] { "AWS", "AZURE", "GOOGLE", "JETBRAINS", "GITHUB" } },
        { "PRO-LABORE PESSOAL", new[] { "TRANSF TITULARIDADE", "SAQUE", "CONTA SALARIO" } },
        { "EXTRA", new[] { "PIX", "TRANSFERENCIA", "TED", "DOC", "DEPOSITO" } }
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
        if (string.IsNullOrWhiteSpace(description)) return "A CLASSIFICAR";

        // Limpa a descrição do extrato (remove acentos e o caractere estranho )
        string cleanDesc = CleanText(description);

        // O loop respeita a ordem do dicionário. PRINCIPAL vem primeiro.
        foreach (var rule in CategoriesRules)
        {
            if (rule.Value.Any(term => cleanDesc.Contains(term)))
            {
                return rule.Key;
            }
        }

        // Regra de segurança para PIX enviado que não entrou em nenhuma categoria acima
        if (cleanDesc.Contains("PIX") && value < 0) return "PIX ENVIADO (VERIFICAR)";

        return "EXTRA";
    }

    public static string RemoveAccents(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        // 2. Remove acentos
        string normalizedString = text.Normalize(NormalizationForm.FormD);
        StringBuilder sb = new StringBuilder();

        foreach (char c in normalizedString)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        // 3. Normaliza espaços (remove espaços duplos e sobras nas pontas)
        string result = sb.ToString().Normalize(NormalizationForm.FormC).ToUpper();
        return Regex.Replace(result, @"\s+", " ").Trim();
    }
    public static string CleanText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        // 1. Substitui qualquer caractere que não seja letra ou número por espaço (resolve o )
        string noSpecials = Regex.Replace(text, @"[^\w\s]", " ");

        // 2. Remove acentos
        string normalizedString = noSpecials.Normalize(NormalizationForm.FormD);
        StringBuilder sb = new StringBuilder();

        foreach (char c in normalizedString)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        // 3. Normaliza espaços (remove espaços duplos e sobras nas pontas)
        string result = sb.ToString().Normalize(NormalizationForm.FormC).ToUpper();
        return Regex.Replace(result, @"\s+", " ").Trim();
    }
}