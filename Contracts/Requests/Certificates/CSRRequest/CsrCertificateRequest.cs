using System.ComponentModel.DataAnnotations;

namespace Contracts.Requests.Certificates.CSRRequest
{
    public class CsrCertificateRequest
    {
        [Required]
        public string FriendlyName { get; set; } = null!;

        [Required]
        public string Subject { get; set; } = null!;

        public IEnumerable<string> SubjectAlternativeNames { get; set; } = null!;

        public bool MachineContext { get; set; }

        [Required]
        [Range(1024, 4096)]
        public int KeyLength { get; set; }
    }
}
