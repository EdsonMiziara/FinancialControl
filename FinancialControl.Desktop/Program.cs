using FinancialControl.Shared;
using FinancialControl.Shared.Services;
using FinancialControl.Shared.SupportModels;
using FinancialControl.Shared.SupportModels.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace FinancialControl.Desktop;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        //registra encoding antes de tudo
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        ApplicationConfiguration.Initialize();

        // Configuração (appsettings.json)
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        // DbContext (MySQL)
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
            .Options;

        var dbContext = new AppDbContext(options);

        // Carrega cache 
        var loader = new CategorizerLoader(dbContext);
        var cache = loader.LoadAsync().GetAwaiter().GetResult();

        // Services
        var categorizer = new CategorizerService(cache, dbContext);
        var categoryRepository = new CategoryRepository(configuration);
        var transactionRepository = new TransactionRepository(configuration : configuration, categoryRepository : categoryRepository);
        var fileService = new FileService(transactionRepository, categorizer, categoryRepository);

        Application.Run(new MainForm(dbContext, fileService, categorizer));
    }
}