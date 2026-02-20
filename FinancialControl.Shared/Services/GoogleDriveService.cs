using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;

namespace FinancialControl.Shared.Services;

public class GoogleDriveService
{
    private readonly DriveService _service;
    private const string ExcelMimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public GoogleDriveService(string credentialsJsonContent)
    {
        // No Azure, passaremos o conteúdo do JSON diretamente como string
        var credential = GoogleCredential.FromJson(credentialsJsonContent)
                                        .CreateScoped(DriveService.Scope.DriveFile);

        _service = new DriveService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = "FinancialControlAutomation"
        });
    }

    // Faz o download de um arquivo específico pelo ID
    public async Task DownloadFileByIdAsync(string fileId, string localPath)
    {
        var request = _service.Files.Get(fileId);
        using var stream = new FileStream(localPath, FileMode.Create);
        await request.DownloadAsync(stream);
    }

    // Lista e baixa todos os arquivos .ofx de uma pasta específica
    public async Task DownloadOfxFilesFromFolderAsync(string folderId, string localFolder)
    {
        if (!Directory.Exists(localFolder)) Directory.CreateDirectory(localFolder);

        var request = _service.Files.List();
        request.Q = $"'{folderId}' in parents and name contains '.ofx' and trashed = false";

        var result = await request.ExecuteAsync();

        foreach (var file in result.Files)
        {
            var localFilePath = Path.Combine(localFolder, file.Name);
            await DownloadFileByIdAsync(file.Id, localFilePath);
        }
    }

    // Atualiza o conteúdo de um arquivo existente no Drive
    public async Task UpdateFileAsync(string localPath, string fileId)
    {
        using var stream = new FileStream(localPath, FileMode.Open);
        var updateRequest = _service.Files.Update(new Google.Apis.Drive.v3.Data.File(), fileId, stream, ExcelMimeType);
        await updateRequest.UploadAsync();
    }
}