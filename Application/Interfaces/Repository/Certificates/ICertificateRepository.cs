using Contracts.Responses.Certificates.CertificateResponse;
using Domain.Entities.Certificates;

namespace Application.Interfaces.Repository.Certificates
{
    public interface ICertificateRepository
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        public Task InsertCertificateAsync(Certificate certificate);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Task<List<Certificate>> GetAllCertificatesAsync();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        public Task DeleteCertificateAsync(int certificateIdToDelete);
    }
}
