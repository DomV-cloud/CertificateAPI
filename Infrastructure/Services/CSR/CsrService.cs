using Application.Interfaces.Certificates;
using Application.Interfaces.Logging;
using Application.Interfaces.Services.CSR;
using CERTENROLLLib;
using Contracts.Responses.Certificates.CSRResponse;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Infrastructure.Services.Certificates
{
    public class CsrService : ICsrService
    {
        private const string ServerAuthenticationOid = "1.3.6.1.5.5.7.3.1";
        private readonly ILoggingService _logger;

        public CsrService(ILoggingService logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Helper: Creates a new public/private key pair using CNG. The key pair
        /// can be either created in the machine context ("Local Machine") or
        /// user context ("Current User").
        /// </summary>
        private static IX509PrivateKey CreatePrivateKey(bool machineContext, int keyLength)
        {
            var key = new CX509PrivateKey
            {
                ProviderName = "Microsoft Software Key Storage Provider",   // Use CNG, not CryptoAPI
                MachineContext = machineContext,
                Length = keyLength,
                KeySpec = X509KeySpec.XCN_AT_SIGNATURE,
                KeyUsage = X509PrivateKeyUsageFlags.XCN_NCRYPT_ALLOW_SIGNING_FLAG
            };

            key.Create();
            return key;
        }

        /// <summary>
        /// Creates an OID identifying a given hash algorithm.
        /// </summary>
        private static CObjectId CreateHashAlgorithm(string hash)
        {
            var algorithm = new CObjectId();
            algorithm.InitializeFromAlgorithmName(
                ObjectIdGroupId.XCN_CRYPT_HASH_ALG_OID_GROUP_ID,
                ObjectIdPublicKeyFlags.XCN_CRYPT_OID_INFO_PUBKEY_ANY,
                AlgorithmFlags.AlgorithmFlagsNone,
                hash);
            return algorithm;
        }

        /// <summary>
        /// Creates an extension attribute that contains subject alternative names.
        /// </summary>
        private static IX509ExtensionAlternativeNames CreateAlternativeNamesExtension(IEnumerable<string> subjectAlternativeNames)
        {
            var alternativeNames = new CAlternativeNames();
            foreach (var name in subjectAlternativeNames)
            {
                var alternativeName = new CAlternativeName();
                alternativeName.InitializeFromString(
                    AlternativeNameType.XCN_CERT_ALT_NAME_DNS_NAME, name);
                alternativeNames.Add(alternativeName);
            }

            var alternativeNamesExtension = new CX509ExtensionAlternativeNames();
            alternativeNamesExtension.InitializeEncode(alternativeNames);
            return alternativeNamesExtension;
        }

        /// <summary>
        /// Creates an extension that defines what the certificate can be used for.
        /// </summary>
        private static IX509ExtensionKeyUsage CreateKeyUsageExtension()
        {
            var extension = new CX509ExtensionKeyUsage();
            extension.InitializeEncode(
                X509KeyUsageFlags.XCN_CERT_DIGITAL_SIGNATURE_KEY_USAGE |
                X509KeyUsageFlags.XCN_CERT_DATA_ENCIPHERMENT_KEY_USAGE |
                X509KeyUsageFlags.XCN_CERT_KEY_ENCIPHERMENT_KEY_USAGE);
            extension.Critical = true;
            return extension;
        }

        /// <summary>
        /// Creates an extension that defines what the certificate can be used for.
        /// </summary>
        private static IX509ExtensionEnhancedKeyUsage CreateEnhancedKeyUsageExtension(IEnumerable<string> oids)
        {
            var objectIds = new CObjectIds();
            foreach (var oid in oids)
            {
                var objectId = new CObjectId();
                objectId.InitializeFromValue(oid);
                objectIds.Add(objectId);
            }

            var extension = new CX509ExtensionEnhancedKeyUsage();
            extension.InitializeEncode(objectIds);
            return extension;
        }

        /// <summary>
        /// Creates a new key pair and certificate signing request.
        /// </summary>
        public async Task<CsrCertificateResponse> Create(
                string friendlyName,
                string subject,
                IEnumerable<string> subjectAlternativeNames,
                bool machineContext,
                int keyLength)
        {
            await _logger.LogInformationAsync("Starting CSR certificate creation for FriendlyName: {0}", friendlyName);

            try
            {
                // (1) Create private key
                await _logger.LogInformationAsync("Creating private key with Key Length: {0} and Machine Context: {1}", keyLength, machineContext);
                var privateKey = CreatePrivateKey(machineContext, keyLength);
                if (privateKey == null)
                {
                    await _logger.LogErrorAsync("Failed to create private key for FriendlyName: {0}", friendlyName);
                    throw new Exception("Failed to create private key.");
                }
                await _logger.LogInformationAsync("Private key successfully created.");

                // (2) Create certificate signing request (CSR)
                await _logger.LogInformationAsync("Initializing CSR from private key.");
                var csr = new CX509CertificateRequestPkcs10();
                csr.InitializeFromPrivateKey(
                    machineContext
                        ? X509CertificateEnrollmentContext.ContextMachine
                        : X509CertificateEnrollmentContext.ContextUser,
                    privateKey,
                    string.Empty);

                // (3) Set subject
                await _logger.LogInformationAsync("Encoding subject: {0}", subject);
                var subjectDn = new CX500DistinguishedName();
                subjectDn.Encode(subject, X500NameFlags.XCN_CERT_NAME_STR_NONE);
                csr.Subject = subjectDn;

                // (4) Set hash algorithm
                await _logger.LogInformationAsync("Setting hash algorithm to SHA256.");
                csr.HashAlgorithm = CreateHashAlgorithm("SHA256");

                // (5) Set key usage and enhanced key usage
                await _logger.LogInformationAsync("Adding key usage extensions.");
                csr.X509Extensions.Add((CX509Extension)CreateKeyUsageExtension());
                csr.X509Extensions.Add((CX509Extension)CreateEnhancedKeyUsageExtension(
                    new[] { ServerAuthenticationOid }));

                // (6) Add alternative names if provided
                if (subjectAlternativeNames != null && subjectAlternativeNames.Any())
                {
                    await _logger.LogInformationAsync("Adding subject alternative names.");
                    csr.X509Extensions.Add((CX509Extension)CreateAlternativeNamesExtension(subjectAlternativeNames));
                }

                // (7) Generate CSR
                await _logger.LogInformationAsync("Initializing enrollment from CSR request.");
                var enrollment = new CX509Enrollment
                {
                    CertificateFriendlyName = friendlyName
                };
                enrollment.InitializeFromRequest(csr);

                // (8) Create CSR request (Base64)
                await _logger.LogInformationAsync("Creating CSR request in Base64 format.");
                var certificateRequest = enrollment.CreateRequest(EncodingType.XCN_CRYPT_STRING_BASE64REQUESTHEADER);
                if (string.IsNullOrEmpty(certificateRequest))
                {
                    await _logger.LogErrorAsync("Failed to generate certificate request for FriendlyName: {0}", friendlyName);
                    throw new Exception("Failed to generate certificate request.");
                }
                await _logger.LogInformationAsync("CSR request successfully generated.");

                // (9) Return response if everything succeeded
                await _logger.LogInformationAsync("CSR certificate successfully created for FriendlyName: {0}", friendlyName);
                return new CsrCertificateResponse
                {
                    PrivateKey = privateKey,
                    Certificate = certificateRequest,
                    CommonName = subject,
                    Success = true,
                    ErrorMessage = null
                };
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("An error occurred while creating CSR certificate for FriendlyName: {0}", friendlyName, ex);
                // (10) Error handling - returning response with error message
                return new CsrCertificateResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    }
}
