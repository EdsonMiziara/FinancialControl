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
        { "EXTRA", new[] { "PIX", "TRANSFERENCIA", "TED", "DOC", "DEPOSITO" } },
        { "SAUDE", new[] { "DROGASIL", "Uberaba SAO PAULO BRA" } },
        { "COMIDA", new[] { "IFD", "TRIGALLE"} },
        { "MERCADO", new[] { "SUPERMERCADO", "MERCADO", "CARREFOUR", "PÃO DE AÇÚCAR", "ZEMA", "BRETAS", "GUARATO", "BAHAMAS" } },
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
        if (string.IsNullOrWhiteSpace(description)) return "A CLASSIFICAR";

        // Limpa a descrição do extrato (remove acentos e o caractere estranho )

        // O loop respeita a ordem do dicionário. PRINCIPAL vem primeiro.
        foreach (var rule in CategoriesRules)
        {
            if (rule.Value.Any(term => Regex.IsMatch(description, $@"\b{term}\b")))
            {
                return rule.Key;
            }
        }

        // Regra de segurança para PIX enviado que não entrou em nenhuma categoria acima
        if (description.Contains("PIX") && value < 0) return "PIX ENVIADO (VERIFICAR)";

        return "EXTRA";
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