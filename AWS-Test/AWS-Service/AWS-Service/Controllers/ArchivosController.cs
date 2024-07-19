using Amazon.S3;
using Amazon.S3.Model;
using AWS_Service.Features.S3.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;

namespace AWS_Service.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ArchivosController : ControllerBase
    {
        private readonly IAmazonS3 _amazonS3;
        private readonly IHubContext<ImageHub> _hubContext;
        public ArchivosController(IAmazonS3 amazonS3, IHubContext<ImageHub> hubContext)
        {
            _amazonS3 = amazonS3;
            _hubContext = hubContext;
        }
        [HttpPost("CargarArchivo")]
        public async Task<IActionResult> UploadFileAsync([FromForm]CrearImagen imgenDto)
        {
            var bucketExists = await _amazonS3.DoesS3BucketExistAsync(imgenDto.bucketName);
            var filePath = $"{System.Guid.NewGuid()}_{imgenDto.file.FileName}";
            if (!bucketExists) return NotFound($"Bucket {imgenDto.bucketName} does not exist.");
            var request = new PutObjectRequest()
            {
                BucketName = imgenDto.bucketName,
                Key = string.IsNullOrEmpty(imgenDto.prefix) ? imgenDto.file.FileName : $"{imgenDto.prefix?.TrimEnd('/')}/{imgenDto.file.FileName}",
                InputStream = imgenDto.file.OpenReadStream()
            };
            request.Metadata.Add("Content-Type", imgenDto.file.ContentType);
            var response = await _amazonS3.PutObjectAsync(request);
            var fileUrl = $"https://{imgenDto.bucketName}.s3.amazonaws.com/{filePath}";

            if (imgenDto.signalR)
            {
                await _hubContext.Clients.Group(imgenDto.prefijoVariosArchivos).SendAsync("RecibirImagen", imgenDto.prefijoVariosArchivos);
            }
            return Ok(new { FileUrl = fileUrl });
        }
        [HttpGet("ListarArchivos")]
        public async Task<IActionResult> GetAllFilesAsync(string bucketName, string? prefix)
        {
            var bucketExists = await _amazonS3.DoesS3BucketExistAsync(bucketName);
            if (!bucketExists) return NotFound($"Bucket {bucketName} does not exist.");
            var request = new ListObjectsV2Request()
            {
                BucketName = bucketName,
                Prefix = prefix
            };
            var result = await _amazonS3.ListObjectsV2Async(request);
            var s3Objects = result.S3Objects.Select(s =>
            {
                var urlRequest = new GetPreSignedUrlRequest()
                {
                    BucketName = bucketName,
                    Key = s.Key,
                    Expires = DateTime.UtcNow.AddMinutes(1)
                };
                return new S3Dto()
                {
                    Name = s.Key.ToString(),
                    PresignedUrl = _amazonS3.GetPreSignedURL(urlRequest),
                };
            });
            return Ok(s3Objects);
        }
        [HttpDelete("EliminarArchivo")]
        public async Task<IActionResult> EliminarArchivo(string bucketName, string key)
        {
            var bucketExists = await _amazonS3.DoesS3BucketExistAsync(bucketName);
            if (!bucketExists) return NotFound($"Bucket {bucketName} does not exist.");
            var request = new DeleteObjectRequest()
            {
                BucketName = bucketName,
                Key = key
            };

            var response = await _amazonS3.DeleteObjectAsync(request);

            return Ok(response);
        }
    }
}
