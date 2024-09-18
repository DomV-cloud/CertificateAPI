using Application.Interfaces.Certificates.CertificateAuthorityService;
using Application.Interfaces.Logging;
using Application.Interfaces.Repository.Certificates;
using Application.Interfaces.Services.CSR;
using Contracts.Requests.Certificates.CSRRequest;
using Contracts.Responses.Certificates.CertificateResponse;
using Contracts.Responses.Certificates.CSRResponse;
using Infrastructure.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace CertificateAPI.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v1/[controller]")]
    [ApiKey]
    public class CsrCertificateController : Controller
    {
        private readonly ICsrService _csrService;
        private readonly ICertificateAuthorityService _certificateAuthorityService;
        private readonly ICertificateRepository _certificateRepository;
        private readonly ILoggingService _logger;

        public CsrCertificateController(
            ICsrService csrService,
            ICertificateAuthorityService certificateAuthorityService,
            ICertificateRepository certificateRepository,
            ILoggingService logger)
        {
            _csrService = csrService;
            _certificateAuthorityService = certificateAuthorityService;
            _certificateRepository = certificateRepository;
            _logger = logger;
        }

        [HttpPost("create-csr")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CsrCertificateResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateCsrCertificate([FromBody] CsrCertificateRequest csrCertificateRequest)
        {
            await _logger.LogInformationAsync("Incoming request to create CSR certificate with FriendlyName: {0}", csrCertificateRequest.FriendlyName);

            try
            {
                if (string.IsNullOrEmpty(csrCertificateRequest.Subject) || csrCertificateRequest.Subject.Length < 3)
                {
                    await _logger.LogWarningAsync("Invalid or missing Subject: {0}", csrCertificateRequest.Subject);
                    return BadRequest("Subject is not valid or not found.");
                }

                var response = await _csrService.Create(
                    csrCertificateRequest.FriendlyName,
                    csrCertificateRequest.Subject,
                    csrCertificateRequest.SubjectAlternativeNames,
                    csrCertificateRequest.MachineContext,
                    csrCertificateRequest.KeyLength);

                if (!response.Success || string.IsNullOrEmpty(response.Certificate))
                {
                    await _logger.LogErrorAsync("CSR generation failed for FriendlyName: {0}", csrCertificateRequest.FriendlyName);
                    return StatusCode(StatusCodes.Status500InternalServerError, "CSR generation failed.");
                }

                await _logger.LogInformationAsync("CSR certificate successfully created for FriendlyName: {0}", csrCertificateRequest.FriendlyName);
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                await _logger.LogWarningAsync("ArgumentException while creating CSR certificate.", ex);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Unexpected error while creating CSR certificate.", ex);
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "An unexpected error occurred.", Detail = ex.Message });
            }
        }

        [HttpPost("generate-certificate")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CertificateResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GenerateCertificate([FromBody] CsrCertificateRequest csrCertificateRequest)
        {
            await _logger.LogInformationAsync("Incoming request to generate certificate with FriendlyName: {0}", csrCertificateRequest.FriendlyName);

            try
            {
                if (string.IsNullOrEmpty(csrCertificateRequest.Subject) || csrCertificateRequest.Subject.Length < 3)
                {
                    await _logger.LogErrorAsync("Invalid or missing Subject: {0}", csrCertificateRequest.Subject);
                    return BadRequest("Subject is not valid or not found.");
                }

                var csrResponse = await _csrService.Create(
                    csrCertificateRequest.FriendlyName,
                    csrCertificateRequest.Subject,
                    csrCertificateRequest.SubjectAlternativeNames,
                    csrCertificateRequest.MachineContext,
                    csrCertificateRequest.KeyLength);

                if (!csrResponse.Success || string.IsNullOrEmpty(csrResponse.Certificate))
                {
                    await _logger.LogErrorAsync("CSR generation failed for FriendlyName: {0}", csrCertificateRequest.FriendlyName);
                    return StatusCode(StatusCodes.Status500InternalServerError, "CSR generation failed.");
                }

                await _logger.LogInformationAsync("CSR certificate successfully created for FriendlyName: {0}", csrCertificateRequest.FriendlyName);

                var certificate = await _certificateAuthorityService.IssueCertificateAsync(csrResponse);

                if (certificate == null)
                {
                    await _logger.LogErrorAsync("Failed to issue certificate for FriendlyName: {0}", csrCertificateRequest.FriendlyName);
                    return StatusCode(StatusCodes.Status500InternalServerError, "Failed to issue certificate.");
                }

                await _logger.LogInformationAsync("Certificate successfully issued with ID: {0}", certificate.Id);

                await _certificateRepository.InsertCertificateAsync(certificate);
                await _logger.LogInformationAsync("Certificate with ID {0} successfully saved to the database.", certificate.Id);

                var certificateResponse = new CertificateResponse
                {
                    CertificateId = certificate.Id,
                    CertificateContent = certificate.CertificateContent,
                    ValidUntil = certificate.ValidUntil,
                    CommonName = certificate.CommonName,
                    IsExpired = false
                };

                return Ok(certificateResponse);
            }
            catch (ArgumentException ex)
            {
                await _logger.LogWarningAsync("ArgumentException while generating certificate.", ex);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Unexpected error while generating certificate.", ex);
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "An unexpected error occurred.", Detail = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves all certificates.
        /// </summary>
        /// <returns>List of certificates or NotFound if none are found.</returns>
        [HttpGet("all")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<CertificateResponse>))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllCertificates()
        {
            await _logger.LogInformationAsync("Incoming request to retrieve all certificates.");

            try
            {
                var certificates = await _certificateRepository.GetAllCertificatesAsync();

                if (certificates == null || certificates.Count == 0)
                {
                    await _logger.LogInformationAsync("No certificates found.");
                    return NotFound("No certificates found");
                }

                await _logger.LogInformationAsync("Found {0} certificates.", certificates.Count);
                return Ok(certificates);
            }
            catch (Exception ex)
            {
                await _logger.LogWarningAsync("Error occurred while retrieving certificates.", ex);
                return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error");
            }
        }

        /// <summary>
        /// Deletes a certificate by its ID.
        /// </summary>
        /// <param name="certificateId">ID of the certificate to delete.</param>
        /// <returns>Status of the delete operation.</returns>
        [HttpDelete("delete/{certificateId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteCertificate(int certificateId)
        {
            await _logger.LogInformationAsync("Incoming request to delete certificate with ID: {0}", certificateId);

            if (certificateId <= 0)
            {
                await _logger.LogWarningAsync("Invalid certificate ID: {0}", certificateId);
                return BadRequest("Invalid certificate ID.");
            }

            try
            {
                await _certificateRepository.DeleteCertificateAsync(certificateId);

                await _logger.LogInformationAsync("Certificate with ID {0} successfully deleted.", certificateId);
                return NoContent();
            }
            catch (Exception)
            {
                await _logger.LogErrorAsync("Error occurred while deleting certificate with ID: {0}", certificateId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deleting the certificate.");
            }
        }
    }
}
