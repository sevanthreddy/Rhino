using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

public class BlobStorageService
{
    private readonly BlobContainerClient _container;
        private readonly ILogger<BlobStorageService> _logger;


    public BlobStorageService(IConfiguration configuration,ILogger<BlobStorageService> logger)
    {
        var namespaceUrl =
            configuration["AZURE_BLOB_STORAGE_URL"];

        var containerName =
            configuration["AZURE_POST_IMAGES_CONTAINER"];

        var serviceClient = new BlobServiceClient(
            new Uri(namespaceUrl!),
            new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ExcludeManagedIdentityCredential = true
            })
        );
        _logger=logger;

        _container = serviceClient.GetBlobContainerClient(containerName);
    }

   public async Task<string> UploadAsync(IFormFile file, string fileName)
{
    _logger.LogInformation("Getting blob client for {FileName}", fileName);
    var blob = _container.GetBlobClient(fileName);

    _logger.LogInformation("Opening file stream");
    using var stream = file.OpenReadStream();

    var uploadOptions = new BlobUploadOptions
    {
        HttpHeaders = new BlobHttpHeaders
        {
            ContentType = file.ContentType
        }
    };

    _logger.LogInformation("Starting Azure upload");

    try
    {
        await blob.UploadAsync(stream, uploadOptions);
        _logger.LogInformation("Upload completed: {BlobUri}", blob.Uri);
        return blob.Uri.ToString();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Upload failed for {FileName}", fileName);
        throw;
    }
}


}