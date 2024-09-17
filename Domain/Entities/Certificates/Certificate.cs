using System.ComponentModel.DataAnnotations;

namespace Domain.Entities.Certificates
{
    public class Certificate : BaseEntity
    {
        [Required]
        public string CertificateContent { get; set; } = null!;

        [Required]
        public DateTime ValidUntil { get; set; }

        [Required]
        public bool IsExpired { get; set; }

        [Required]
        public string CommonName { get; set; } = null!;
    }
}
