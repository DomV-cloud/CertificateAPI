using Contracts.Responses.Certificates.CertificateResponse;
using Contracts.Responses.Certificates.CSRResponse;
using Domain.Entities.Certificates;

namespace Application.Interfaces.Certificates.CertificateAuthorityService
{
    public interface ICertificateAuthorityService
    {
        Task<Certificate> IssueCertificateAsync(CsrCertificateResponse certificate);
    }
}
