namespace AWS_Service.Features.S3.Dto
{
    public class CrearImagen
    {
        public IFormFile? file { get; set; }
        public string? bucketName { get; set; }
        public string? prefijoVariosArchivos { get; set; }
        public string? prefix { get; set; }
        public bool signalR { get; set; }
    }
}
