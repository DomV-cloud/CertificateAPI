using Application.Interfaces.Certificates.CertificateAuthorityService;
using Application.Interfaces.Logging;
using Contracts.Responses.Certificates.CSRResponse;
using Domain.Entities.Certificates;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Infrastructure.Services.Certificates
{
    public class CertificateAuthorityService : ICertificateAuthorityService
    {
        private readonly ILoggingService _logger;

        public CertificateAuthorityService(ILoggingService logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Issues a certificate by sending the CSR to the Certificate Authority.
        /// </summary>
        /// <param name="certificate">CSR response containing the certificate request.</param>
        /// <returns>Response with issued certificate data.</returns>
        /// <exception cref="CertificateIssuanceException">Thrown when certificate issuance fails.</exception>
        public async Task<Certificate> IssueCertificateAsync(CsrCertificateResponse certificate)
        {
            await _logger.LogInformationAsync("Starting certificate issuance for CommonName: {0}", certificate.CommonName);

            try
            {
                await _logger.LogInformationAsync("Sending CSR to Certificate Authority.");
                var issuedCertificateContent = await SendCsrToCertificateAuthorityAsync(certificate.Certificate);

                if (string.IsNullOrEmpty(issuedCertificateContent))
                {
                    await _logger.LogErrorAsync("Certificate was not successfully issued.");
                    throw new Exception("Failed to issue certificate from CA.");
                }
                var extractedCn = await ExtractCommonNameFromCsr(certificate.CommonName);

                var issuedCertificate = new Certificate
                {
                    CertificateContent = issuedCertificateContent,
                    ValidUntil = DateTime.UtcNow.AddYears(1),
                    IsExpired = false,
                    CommonName = extractedCn
                };

                await _logger.LogInformationAsync("Certificate successfully issued for CommonName: {0}", issuedCertificate.CommonName);
                return issuedCertificate;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error occurred while issuing certificate for CommonName: {0}", certificate.CommonName, ex);
                throw;
            }
        }

        /// <summary>
        /// Simulates sending the CSR to the Certificate Authority and receiving a certificate.
        /// </summary>
        /// <param name="csr">CSR request string.</param>
        /// <returns>Content of the issued certificate.</returns>
        private async Task<string> SendCsrToCertificateAuthorityAsync(string csr)
        {
            await _logger.LogInformationAsync("Simulating sending CSR to Certificate Authority.");

            var simulatedCertificate = Convert.ToBase64String(Encoding.UTF8.GetBytes("Simulated Certificate Content"));

            await _logger.LogInformationAsync("Simulated certificate content generated.");

            return simulatedCertificate;
            //return Task.Run(() =>
            //{
            //    // Simulate a delay as if a real request is being sent
            //    Task.Delay(1000).Wait();

            //    var simulatedCertificate = Convert.ToBase64String(Encoding.UTF8.GetBytes("Simulated Certificate Content"));
            //    await _logger.LogInformationAsync("Simulated certificate content generated.");
            //    return simulatedCertificate;
            //});
        }

        /// <summary>
        /// Extracts the Common Name (CN) from the CSR.
        /// </summary>
        /// <param name="commonName">Common Name extracted from the CSR.</param>
        /// <returns>Extracted Common Name.</returns>
        private async Task<string> ExtractCommonNameFromCsr(string commonName)
        {
            const string cnPrefix = "CN=";
            var cnStartIndex = commonName.IndexOf(cnPrefix, StringComparison.OrdinalIgnoreCase);
            if (cnStartIndex == -1)
            {
                await _logger.LogWarningAsync("Common Name was not found in the CSR.");
                return "CN=Unknown";
            }

            var cnEndIndex = commonName.IndexOf(',', cnStartIndex);
            if (cnEndIndex == -1)
            {
                cnEndIndex = commonName.Length;
            }

            var extractedCommonName = commonName.Substring(cnStartIndex, cnEndIndex - cnStartIndex);
            await _logger.LogInformationAsync("Extracted Common Name from CSR: {0}", extractedCommonName);
            return extractedCommonName;
        }

        // Uncomment and implement this method if you plan to integrate with a real Certificate Authority.
        /*
        /// <summary>
        /// Asynchronously sends the CSR to the Certificate Authority and retrieves the certificate.
        /// </summary>
        /// <param name="csr">CSR request string.</param>
        /// <returns>Content of the issued certificate.</returns>
        private async Task<string> SendCsrToCertificateAuthorityCaAsync(string csr)
        {
            // Example CA endpoint URL
            var caEndpoint = "https://api.certificateauthority.com/issue";

            var requestContent = new StringContent(JsonSerializer.Serialize(new { Csr = csr }), Encoding.UTF8, "application/json");

            _logger.LogInformation("Sending HTTP POST request to CA endpoint: {CaEndpoint}", caEndpoint);
            var response = await _httpClient.PostAsync(caEndpoint, requestContent);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("HTTP request to CA failed with status code: {StatusCode}", response.StatusCode);
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync();

            // Assuming CA returns certificate in JSON format
            var caResponse = JsonSerializer.Deserialize<CaResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _logger.LogInformation("Certificate successfully retrieved from CA.");
            return caResponse?.CertificateContent;
        }
        */
    }
}
