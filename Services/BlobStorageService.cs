using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SnappersRepairShop.Shared.Models;
using SnappersRepairShop.Data;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Retry;

namespace SnappersRepairShop.Services;

/// <summary>
/// Service for managing photo uploads to Azure Blob Storage
/// Handles full-size images, thumbnail generation, secure URL generation, and metadata storage
/// Includes retry logic for transient failures
/// </summary>
public class BlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;
    private readonly int _thumbnailWidth;
    private readonly int _thumbnailHeight;
    private readonly ILogger<BlobStorageService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AsyncRetryPolicy _retryPolicy;
    private const int MaxRetries = 3;
    private const int MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB

    public BlobStorageService(
        IConfiguration configuration,
        ILogger<BlobStorageService> logger,
        IServiceScopeFactory scopeFactory)
    {
        var connectionString = configuration["AzureBlobStorage:ConnectionString"]
            ?? throw new InvalidOperationException("Azure Blob Storage connection string not configured");

        _containerName = configuration["AzureBlobStorage:ContainerName"] ?? "job-photos";
        _thumbnailWidth = int.Parse(configuration["AzureBlobStorage:ThumbnailWidth"] ?? "300");
        _thumbnailHeight = int.Parse(configuration["AzureBlobStorage:ThumbnailHeight"] ?? "300");

        _blobServiceClient = new BlobServiceClient(connectionString);
        _logger = logger;
        _scopeFactory = scopeFactory;

        // Configure retry policy for transient failures
        _retryPolicy = Policy
            .Handle<Azure.RequestFailedException>(ex =>
                ex.Status == 408 || // Request Timeout
                ex.Status == 429 || // Too Many Requests
                ex.Status == 500 || // Internal Server Error
                ex.Status == 502 || // Bad Gateway
                ex.Status == 503 || // Service Unavailable
                ex.Status == 504)   // Gateway Timeout
            .WaitAndRetryAsync(
                MaxRetries,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(exception,
                        "Retry {RetryCount} of {MaxRetries} after {Delay}s due to: {Message}",
                        retryCount, MaxRetries, timeSpan.TotalSeconds, exception.Message);
                });
    }

    /// <summary>
    /// Ensures the blob container exists with private access
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            await _retryPolicy.ExecuteAsync(async () =>
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);
            });
            _logger.LogInformation("Blob container '{ContainerName}' initialized", _containerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing blob container");
            throw;
        }
    }

    /// <summary>
    /// Validates file size and type
    /// </summary>
    private void ValidateFile(Stream fileStream, string fileName)
    {
        if (fileStream.Length > MaxFileSizeBytes)
        {
            throw new InvalidOperationException($"File size exceeds maximum allowed size of {MaxFileSizeBytes / 1024 / 1024}MB");
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };

        if (!allowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException($"File type '{extension}' is not supported. Allowed types: {string.Join(", ", allowedExtensions)}");
        }
    }

    /// <summary>
    /// Uploads a photo and generates a thumbnail (legacy method - use UploadJobPhotoAsync for new code)
    /// Returns tuple of (originalBlobName, thumbnailBlobName)
    /// </summary>
    public async Task<(string OriginalBlobName, string ThumbnailBlobName)> UploadPhotoAsync(
        Stream photoStream,
        string fileName,
        string contentType,
        IProgress<double>? progress = null)
    {
        try
        {
            ValidateFile(photoStream, fileName);

            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);

            // Generate unique blob names
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var uniqueId = Guid.NewGuid().ToString("N")[..8];
            var extension = Path.GetExtension(fileName);
            var baseName = $"{timestamp}_{uniqueId}";
            var originalBlobName = $"{baseName}{extension}";
            var thumbnailBlobName = $"{baseName}_thumb{extension}";

            // Upload original photo with retry
            await _retryPolicy.ExecuteAsync(async () =>
            {
                var originalBlobClient = containerClient.GetBlobClient(originalBlobName);
                var blobHttpHeaders = new BlobHttpHeaders { ContentType = contentType };

                photoStream.Position = 0;
                await originalBlobClient.UploadAsync(photoStream, new BlobUploadOptions
                {
                    HttpHeaders = blobHttpHeaders
                });
            });

            progress?.Report(50);

            // Generate and upload thumbnail with retry
            await _retryPolicy.ExecuteAsync(async () =>
            {
                photoStream.Position = 0;
                using var image = await Image.LoadAsync(photoStream);
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(_thumbnailWidth, _thumbnailHeight),
                    Mode = ResizeMode.Max
                }));

                using var thumbnailStream = new MemoryStream();
                await image.SaveAsync(thumbnailStream, new JpegEncoder { Quality = 85 });
                thumbnailStream.Position = 0;

                var thumbnailBlobClient = containerClient.GetBlobClient(thumbnailBlobName);
                await thumbnailBlobClient.UploadAsync(thumbnailStream, new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders { ContentType = "image/jpeg" }
                });
            });

            progress?.Report(100);

            _logger.LogInformation("Uploaded photo: {OriginalBlobName}, thumbnail: {ThumbnailBlobName}",
                originalBlobName, thumbnailBlobName);

            return (originalBlobName, thumbnailBlobName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading photo: {FileName}", fileName);
            throw;
        }
    }

    /// <summary>
    /// Uploads a job photo with metadata storage in the database
    /// Returns the JobPhoto entity with ID populated
    /// </summary>
    public async Task<JobPhoto> UploadJobPhotoAsync(
        int workOrderId,
        Stream photoStream,
        string fileName,
        string contentType,
        string? caption = null,
        string? uploadedByUserId = null,
        IProgress<double>? progress = null)
    {
        try
        {
            ValidateFile(photoStream, fileName);

            // Upload to blob storage
            var (originalBlobName, thumbnailBlobName) = await UploadPhotoAsync(
                photoStream, fileName, contentType, progress);

            // Store metadata in database
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var jobPhoto = new JobPhoto
            {
                WorkOrderId = workOrderId,
                FileName = fileName,
                BlobName = originalBlobName,
                ThumbnailBlobName = thumbnailBlobName,
                ContentType = contentType,
                FileSizeBytes = photoStream.Length,
                Caption = caption,
                UploadedByUserId = uploadedByUserId,
                UploadedDate = DateTime.UtcNow
            };

            context.JobPhotos.Add(jobPhoto);
            await context.SaveChangesAsync();

            _logger.LogInformation("Job photo saved: ID={JobPhotoId}, WorkOrder={WorkOrderId}, Blob={BlobName}",
                jobPhoto.JobPhotoId, workOrderId, originalBlobName);

            return jobPhoto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading job photo for WorkOrder {WorkOrderId}: {FileName}",
                workOrderId, fileName);
            throw;
        }
    }

    /// <summary>
    /// Uploads multiple job photos with progress reporting
    /// Returns list of JobPhoto entities
    /// </summary>
    public async Task<List<JobPhoto>> UploadMultipleJobPhotosAsync(
        int workOrderId,
        IEnumerable<(Stream Stream, string FileName, string ContentType, string? Caption)> files,
        string? uploadedByUserId = null,
        IProgress<double>? overallProgress = null)
    {
        var results = new List<JobPhoto>();
        var fileList = files.ToList();
        var totalFiles = fileList.Count;
        var completedFiles = 0;

        try
        {
            foreach (var (stream, fileName, contentType, caption) in fileList)
            {
                var fileProgress = new Progress<double>(percent =>
                {
                    var overallPercent = (completedFiles * 100 + percent) / totalFiles;
                    overallProgress?.Report(overallPercent);
                });

                var jobPhoto = await UploadJobPhotoAsync(
                    workOrderId, stream, fileName, contentType, caption, uploadedByUserId, fileProgress);

                results.Add(jobPhoto);
                completedFiles++;
                overallProgress?.Report((completedFiles * 100.0) / totalFiles);
            }

            _logger.LogInformation("Uploaded {Count} photos for WorkOrder {WorkOrderId}",
                results.Count, workOrderId);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading multiple photos for WorkOrder {WorkOrderId}. Uploaded {Count} of {Total}",
                workOrderId, completedFiles, totalFiles);
            throw;
        }
    }

    /// <summary>
    /// Generates a time-limited SAS URL for secure access to a blob
    /// Default expiration: 1 hour
    /// Permissions: Read-only (suitable for all roles including Technicians)
    /// </summary>
    public async Task<string> GetSecureUrlAsync(string blobName, TimeSpan? expiresIn = null, bool allowWrite = false)
    {
        try
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                var blobClient = containerClient.GetBlobClient(blobName);

                // Check if blob exists
                if (!await blobClient.ExistsAsync())
                {
                    throw new FileNotFoundException($"Blob not found: {blobName}");
                }

                // Generate SAS token with appropriate permissions
                var sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = _containerName,
                    BlobName = blobName,
                    Resource = "b",
                    StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
                    ExpiresOn = DateTimeOffset.UtcNow.Add(expiresIn ?? TimeSpan.FromHours(1))
                };

                // Read-only by default (safe for Technicians)
                // Only allow write if explicitly requested (Admin operations)
                sasBuilder.SetPermissions(allowWrite ? BlobSasPermissions.Read | BlobSasPermissions.Write : BlobSasPermissions.Read);

                var sasUri = blobClient.GenerateSasUri(sasBuilder);
                return sasUri.ToString();
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating secure URL for blob: {BlobName}", blobName);
            throw;
        }
    }

    /// <summary>
    /// Generates a secure URL specifically for a thumbnail
    /// Default expiration: 1 hour (read-only)
    /// </summary>
    public async Task<string> GetThumbnailUrlAsync(string thumbnailBlobName, TimeSpan? expiresIn = null)
    {
        if (string.IsNullOrEmpty(thumbnailBlobName))
        {
            throw new ArgumentException("Thumbnail blob name cannot be null or empty", nameof(thumbnailBlobName));
        }

        return await GetSecureUrlAsync(thumbnailBlobName, expiresIn, allowWrite: false);
    }

    /// <summary>
    /// Gets a JobPhoto with secure URLs for both original and thumbnail
    /// </summary>
    public async Task<(JobPhoto Photo, string OriginalUrl, string? ThumbnailUrl)> GetJobPhotoWithUrlsAsync(
        int jobPhotoId,
        TimeSpan? urlExpiresIn = null)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var jobPhoto = await context.JobPhotos
                .Include(p => p.WorkOrder)
                .FirstOrDefaultAsync(p => p.JobPhotoId == jobPhotoId);

            if (jobPhoto == null)
            {
                throw new FileNotFoundException($"JobPhoto not found: {jobPhotoId}");
            }

            var originalUrl = await GetSecureUrlAsync(jobPhoto.BlobName, urlExpiresIn);
            string? thumbnailUrl = null;

            if (!string.IsNullOrEmpty(jobPhoto.ThumbnailBlobName))
            {
                thumbnailUrl = await GetThumbnailUrlAsync(jobPhoto.ThumbnailBlobName, urlExpiresIn);
            }

            return (jobPhoto, originalUrl, thumbnailUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting JobPhoto with URLs: {JobPhotoId}", jobPhotoId);
            throw;
        }
    }

    /// <summary>
    /// Gets all JobPhotos for a WorkOrder with secure URLs
    /// </summary>
    public async Task<List<(JobPhoto Photo, string OriginalUrl, string? ThumbnailUrl)>> GetWorkOrderPhotosWithUrlsAsync(
        int workOrderId,
        TimeSpan? urlExpiresIn = null)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var jobPhotos = await context.JobPhotos
                .Where(p => p.WorkOrderId == workOrderId)
                .OrderByDescending(p => p.UploadedDate)
                .ToListAsync();

            var results = new List<(JobPhoto, string, string?)>();

            foreach (var photo in jobPhotos)
            {
                var originalUrl = await GetSecureUrlAsync(photo.BlobName, urlExpiresIn);
                string? thumbnailUrl = null;

                if (!string.IsNullOrEmpty(photo.ThumbnailBlobName))
                {
                    thumbnailUrl = await GetThumbnailUrlAsync(photo.ThumbnailBlobName, urlExpiresIn);
                }

                results.Add((photo, originalUrl, thumbnailUrl));
            }

            _logger.LogInformation("Retrieved {Count} photos for WorkOrder {WorkOrderId}",
                results.Count, workOrderId);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting photos for WorkOrder {WorkOrderId}", workOrderId);
            throw;
        }
    }

    /// <summary>
    /// Deletes a photo and its thumbnail from blob storage (legacy method)
    /// </summary>
    public async Task DeletePhotoAsync(string blobName, string? thumbnailBlobName = null)
    {
        try
        {
            await _retryPolicy.ExecuteAsync(async () =>
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);

                // Delete original
                var blobClient = containerClient.GetBlobClient(blobName);
                await blobClient.DeleteIfExistsAsync();

                // Delete thumbnail if provided
                if (!string.IsNullOrEmpty(thumbnailBlobName))
                {
                    var thumbnailClient = containerClient.GetBlobClient(thumbnailBlobName);
                    await thumbnailClient.DeleteIfExistsAsync();
                }
            });

            _logger.LogInformation("Deleted photo: {BlobName}", blobName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting photo: {BlobName}", blobName);
            throw;
        }
    }

    /// <summary>
    /// Deletes a JobPhoto from both blob storage and database
    /// </summary>
    public async Task DeleteJobPhotoAsync(int jobPhotoId)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var jobPhoto = await context.JobPhotos.FindAsync(jobPhotoId);
            if (jobPhoto == null)
            {
                _logger.LogWarning("JobPhoto {JobPhotoId} not found for deletion", jobPhotoId);
                return;
            }

            // Delete from blob storage first
            await DeletePhotoAsync(jobPhoto.BlobName, jobPhoto.ThumbnailBlobName);

            // Delete from database
            context.JobPhotos.Remove(jobPhoto);
            await context.SaveChangesAsync();

            _logger.LogInformation("Deleted JobPhoto {JobPhotoId} from WorkOrder {WorkOrderId}",
                jobPhotoId, jobPhoto.WorkOrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting JobPhoto {JobPhotoId}", jobPhotoId);
            throw;
        }
    }

    /// <summary>
    /// Deletes all photos for a WorkOrder from both blob storage and database
    /// </summary>
    public async Task DeleteWorkOrderPhotosAsync(int workOrderId)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var jobPhotos = await context.JobPhotos
                .Where(p => p.WorkOrderId == workOrderId)
                .ToListAsync();

            foreach (var photo in jobPhotos)
            {
                await DeletePhotoAsync(photo.BlobName, photo.ThumbnailBlobName);
            }

            context.JobPhotos.RemoveRange(jobPhotos);
            await context.SaveChangesAsync();

            _logger.LogInformation("Deleted {Count} photos for WorkOrder {WorkOrderId}",
                jobPhotos.Count, workOrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting photos for WorkOrder {WorkOrderId}", workOrderId);
            throw;
        }
    }
}

