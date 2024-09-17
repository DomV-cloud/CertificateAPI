using Contracts.Responses.Certificates.CSRResponse;

namespace Application.Interfaces.Services.CSR
{
    public interface ICsrService
    {
        public Task<CsrCertificateResponse> Create(
            string friendlyName,
            string subject,
            IEnumerable<string> subjectAlternativeNames,
            bool machineContext,
            int keyLength);
    }
}
