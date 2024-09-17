using Application.Interfaces.Logging;
using Application.Interfaces.Repository.Certificates;
using Domain.Entities.Certificates;
using Infrastructure.Persistance.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Repository.Certificates
{
    public class CertificatesRepository : ICertificateRepository
    {
        private readonly CertisysDbContext _dbContext;
        private readonly ILoggingService _logger;

        public CertificatesRepository(CertisysDbContext dbContext, ILoggingService logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task DeleteCertificateAsync(int certificateIdToDelete)
        {
            try
            {
                await _logger.LogInformationAsync("Attempting to delete certificate with ID: {0}", certificateIdToDelete);

                var certificateToDelete = await _dbContext.Certificates
                    .FirstOrDefaultAsync(c => c.Id == certificateIdToDelete);

                if (certificateToDelete == null)
                {
                    await _logger.LogWarningAsync("Certificate with ID {0} was not found.", certificateIdToDelete);
                    throw new ArgumentNullException($"Certificate with ID {certificateIdToDelete} was not found.");
                }

                _dbContext.Certificates.Remove(certificateToDelete);
                var isDeleted = await _dbContext.SaveChangesAsync();

                if (isDeleted <= 0)
                {
                    await _logger.LogErrorAsync("Failed to delete certificate with ID {0}.", certificateIdToDelete);
                    throw new Exception("Entity was not deleted successfully.");
                }

                await _logger.LogInformationAsync("Certificate with ID {0} was successfully deleted.", certificateIdToDelete);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("An error occurred while deleting certificate with ID {0}.", certificateIdToDelete, ex);
                throw;
            }
        }

        public async Task<List<Certificate>> GetAllCertificatesAsync()
        {
            try
            {
                await _logger.LogInformationAsync("Retrieving all certificates.");
                var certificates = await _dbContext.Certificates.ToListAsync();
                await _logger.LogInformationAsync("Retrieved {0} certificates.", certificates.Count);
                return certificates;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("An error occurred while retrieving all certificates.", ex);
                throw;
            }
        }

        public async Task InsertCertificateAsync(Certificate certificate)
        {
            try
            {
                if (certificate == null)
                {
                    await _logger.LogWarningAsync("Attempted to insert a null certificate.");
                    throw new ArgumentNullException(nameof(certificate), "Certificate cannot be null.");
                }

                if (string.IsNullOrWhiteSpace(certificate.CertificateContent))
                {
                    await _logger.LogWarningAsync("Attempted to insert a certificate with empty content.");
                    throw new ArgumentException("Certificate content cannot be empty.", nameof(certificate.CertificateContent));
                }

                await _logger.LogInformationAsync("Inserting certificate with ID: {0}", certificate.Id);
                await _dbContext.Certificates.AddAsync(certificate);
                var isInserted = await _dbContext.SaveChangesAsync();

                if (isInserted <= 0)
                {
                    await _logger.LogErrorAsync("Failed to insert certificate with ID {0}.", certificate.Id);
                    throw new Exception("Entity was not inserted successfully.");
                }

                await _logger.LogInformationAsync("Certificate with ID {0} was successfully inserted.", certificate.Id);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("An error occurred while inserting certificate with ID {0}.", certificate?.Id, ex);
                throw;
            }
        }
    }
}
