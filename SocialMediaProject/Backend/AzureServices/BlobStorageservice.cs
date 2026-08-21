using Azure.Storage.Blobs;

public class BlobStorageService
{
    private readonly BlobContainerClient _container;

    public BlobStorageService(IConfiguration configuration)
    {
        var connectionString =
    configuration["AZURE_BLOB_CONNECTION_STRING"];

        var containerName =
            configuration["AZURE_POST_IMAGES_CONTAINER"];

        _container = new BlobContainerClient(
            connectionString,
            containerName
        );
    }

    public async Task<string> UploadAsync(
        IFormFile file,
        string fileName)
    {
        await _container.CreateIfNotExistsAsync();

        var blob = _container.GetBlobClient(fileName);

        using var stream = file.OpenReadStream();

        await blob.UploadAsync(
            stream,
            overwrite: true
        );

        return blob.Uri.ToString();
    }
}