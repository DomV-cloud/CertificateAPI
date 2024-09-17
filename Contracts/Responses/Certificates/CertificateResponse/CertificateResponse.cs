namespace Contracts.Responses.Certificates.CertificateResponse
{
    public class CertificateResponse
    {
        public int CertificateId { get; set; }
        public string CertificateContent { get; set; }
        public DateTime ValidUntil { get; set; }
        public bool IsExpired { get; set; }
        public string CommonName { get; set; }  
    }
}
