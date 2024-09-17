
using CERTENROLLLib;

namespace Contracts.Responses.Certificates.CSRResponse
{
    public class CsrCertificateResponse
    {
        public IX509PrivateKey PrivateKey { get; set; } = null!;

        public string Certificate { get; set; } = null!;

        public string CommonName { get; set; } = null!;

        public string? ErrorMessage { get; set; }

        public bool Success { get; set; }
    }
}
