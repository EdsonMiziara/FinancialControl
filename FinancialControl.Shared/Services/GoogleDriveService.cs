using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;

namespace FinancialControl.Shared.Services;

public class GoogleDriveService
{
    private readonly DriveService _service;
    private const string ExcelMimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    /// <summary>
    /// Constructor for GoogleDriveService that initializes the service using the provided credentials JSON content.
    /// It creates a GoogleCredential from the JSON string and initializes the DriveService with the appropriate scope for file access.
    /// </summary>
    /// <param name="credentialsJsonContent"></param>

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

    /// <summary>
    /// Downloads a specific file from Google Drive by its ID and saves it to the specified local path.
    /// If the file does not exist or the download fails, an exception will be thrown.
    /// </summary>
    /// <param name="fileId"></param>
    /// <param name="localPath"></param>
    /// <returns>
    /// Returns a Task representing the asynchronous operation. The caller can await this task to ensure the download is complete before proceeding.
    /// </returns>
    
    public async Task DownloadFileByIdAsync(string fileId, string localPath)
    {
        var request = _service.Files.Get(fileId);
        using var stream = new FileStream(localPath, FileMode.Create);
        await request.DownloadAsync(stream);
    }

    /// <summary>
    /// Lists all .ofx files in a specific Google Drive folder and downloads them to a local directory.
    /// </summary>
    /// <param name="folderId"></param>
    /// <param name="localFolder"></param>
    /// <returns>
    /// Returns a Task representing the asynchronous operation. The caller can await this task to ensure all files are downloaded before proceeding.
    /// </returns>
    
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

    /// <summary>
    /// Updates the content of an existing file in Google Drive by its ID with the content from a local file.
    /// </summary>
    /// <param name="localPath"></param>
    /// <param name="fileId"></param>
    /// <returns>
    /// Returns a Task representing the asynchronous operation. The caller can await this task to ensure the file update is complete before proceeding.
    /// </returns>
    
    public async Task UpdateFileAsync(string localPath, string fileId)
    {
        using var stream = new FileStream(localPath, FileMode.Open);
        var updateRequest = _service.Files.Update(new Google.Apis.Drive.v3.Data.File(), fileId, stream, ExcelMimeType);
        await updateRequest.UploadAsync();
    }
}