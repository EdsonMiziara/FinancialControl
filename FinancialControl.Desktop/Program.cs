using FinancialControl.Shared;
using FinancialControl.Shared.Services;
using FinancialControl.Shared.SupportModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace FinancialControl.Desktop;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        // 🔥 IMPORTANTE: registrar encoding ANTES de tudo
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        ApplicationConfiguration.Initialize();

        // 🔧 Configuração (appsettings.json)
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        // 🔧 DbContext (MySQL)
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
            .Options;

        var dbContext = new AppDbContext(options);

        // 🔥 Carregar cache (IMPORTANTE)
        var loader = new CategorizerLoader(dbContext);
        var cache = loader.LoadAsync().GetAwaiter().GetResult();

        // 🔧 Services
        var categorizer = new CategorizerService(cache, dbContext);
        var repository = new TransacaoRepository(configuration);
        var fileService = new FileService(repository, categorizer);

        // 🚀 Rodar aplicação
        Application.Run(new MainForm(dbContext, fileService, categorizer));
    }
}