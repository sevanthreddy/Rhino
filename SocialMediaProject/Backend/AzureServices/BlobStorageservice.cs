using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

public class BlobStorageService
{
    private readonly BlobServiceClient _serviceClient;

    private readonly BlobContainerClient _container;
    private readonly BlobContainerClient _videoContainer;

    private readonly ILogger<BlobStorageService> _logger;


    public BlobStorageService(
        IConfiguration configuration,
        ILogger<BlobStorageService> logger)
    {
        var namespaceUrl =
            configuration["AZURE_BLOB_STORAGE_URL"];

        var containerName =
            configuration["AZURE_POST_IMAGES_CONTAINER"];

        var videoContainerName =
            configuration["AZURE_VIDEO_CONTAINER"];

        _logger = logger;


        // -----------------------------------------
        // Azure authentication
        // -----------------------------------------

        TokenCredential credential;

        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            == "Development")
        {
            _logger.LogInformation(
                "🔐 Development: Using Azure CLI credential"
            );

            credential = new AzureCliCredential();
        }
        else
        {
            _logger.LogInformation(
                "🔐 Production: Using Managed Identity"
            );

            credential = new ManagedIdentityCredential();
        }


        // -----------------------------------------
        // Blob Service Client
        // -----------------------------------------

        _serviceClient = new BlobServiceClient(
            new Uri(namespaceUrl!),
            credential
        );


        // -----------------------------------------
        // Containers
        // -----------------------------------------

        _container =
            _serviceClient.GetBlobContainerClient(
                containerName
            );

        _videoContainer =
            _serviceClient.GetBlobContainerClient(
                videoContainerName
            );
    }


    public async Task<string> UploadAsync(IFormFile file, string fileName)
    {
        _logger.LogInformation("Getting blob client for {FileName}", fileName);

        var blob = _container.GetBlobClient(fileName);
        _logger.LogInformation("Opening file stream");
        using var stream = file.OpenReadStream();


        var uploadOptions =
            new BlobUploadOptions
            {
                HttpHeaders =
                    new BlobHttpHeaders
                    {
                        ContentType =
                            file.ContentType
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


    public async Task<string> GenerateUploadSasAsync(
    string fileName,
    string contentType)
{
    try
    {
        _logger.LogInformation(
            "🎥 GenerateUploadSasAsync STARTED. FileName: {FileName}, ContentType: {ContentType}",
            fileName,
            contentType
        );


        // -----------------------------------------
        // 1. Get Blob Client
        // -----------------------------------------

        _logger.LogInformation(
            "🎥 Getting blob client from video container..."
        );

        var blob = _videoContainer.GetBlobClient(fileName);

        _logger.LogInformation(
            "✅ Blob client created. Blob URI: {BlobUri}",
            blob.Uri
        );


        // -----------------------------------------
        // 2. SAS expiration
        // -----------------------------------------

        var startsOn =
            DateTimeOffset.UtcNow.AddMinutes(-1);

        var expiresOn =
            DateTimeOffset.UtcNow.AddMinutes(15);

        _logger.LogInformation(
            "🎥 SAS time configured. Starts: {StartsOn}, Expires: {ExpiresOn}",
            startsOn,
            expiresOn
        );


        // -----------------------------------------
        // 3. Get User Delegation Key
        // -----------------------------------------

        _logger.LogInformation(
            "🔑 Requesting User Delegation Key from Azure..."
        );

        var delegationKey =
            await _serviceClient.GetUserDelegationKeyAsync(
                new BlobGetUserDelegationKeyOptions(expiresOn)
            );

        _logger.LogInformation(
            "✅ User Delegation Key received successfully."
        );


        // -----------------------------------------
        // 4. Build SAS
        // -----------------------------------------

        _logger.LogInformation(
            "🔐 Creating BlobSasBuilder..."
        );

        var sasBuilder =
            new BlobSasBuilder
            {
                BlobContainerName =
                    _videoContainer.Name,

                BlobName =
                    fileName,

                Resource = "b",

                StartsOn =
                    startsOn,

                ExpiresOn =
                    expiresOn,

                ContentType =
                    contentType
            };


        // -----------------------------------------
        // 5. Set permissions
        // -----------------------------------------

        _logger.LogInformation(
            "🔐 Setting SAS permissions: Create + Write"
        );

        sasBuilder.SetPermissions(
            BlobSasPermissions.Create |
            BlobSasPermissions.Write
        );


        // -----------------------------------------
        // 6. Generate SAS token
        // -----------------------------------------

        _logger.LogInformation(
            "🔐 Generating SAS query parameters..."
        );

        var sasToken =
            sasBuilder.ToSasQueryParameters(
                delegationKey.Value,
                _serviceClient.AccountName
            );

        _logger.LogInformation(
            "✅ SAS query parameters generated."
        );


        // -----------------------------------------
        // 7. Build final URL
        // -----------------------------------------

        var sasUrl =
            $"{blob.Uri}?{sasToken}";

        _logger.LogInformation(
            "✅ SAS URL generated successfully for {FileName}",
            fileName
        );

        return sasUrl;
    }
    catch (Exception ex)
    {
        _logger.LogError(
            ex,
            "💥 GenerateUploadSasAsync FAILED for {FileName}",
            fileName
        );

        throw;
    }
}

public async Task<string> GenerateReadSasAsync(string blobName)
{
    var blob = _videoContainer.GetBlobClient(blobName);

    var startsOn = DateTimeOffset.UtcNow.AddMinutes(-1);
    var expiresOn = DateTimeOffset.UtcNow.AddMinutes(15);

    var delegationKey =
        await _serviceClient.GetUserDelegationKeyAsync(
            new BlobGetUserDelegationKeyOptions(expiresOn)
        );

    var sasBuilder = new BlobSasBuilder
    {
        BlobContainerName = _videoContainer.Name,
        BlobName = blobName,
        Resource = "b",
        StartsOn = startsOn,
        ExpiresOn = expiresOn
    };

    sasBuilder.SetPermissions(
        BlobSasPermissions.Read
    );

    var sasToken =
        sasBuilder.ToSasQueryParameters(
            delegationKey.Value,
            _serviceClient.AccountName
        );

    return $"{blob.Uri}?{sasToken}";
}
}