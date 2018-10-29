using Alexa.NET.Request;
using AlexDunnVoice.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace AlexDunnVoice.Controllers.Filters
{

    /// <summary>
    /// Filter used to validate the signature and certificate sent from Alexa
    /// </summary>
    public class AlexaValidationFilter : IActionFilter
    {

        private readonly IOptions<SkillSettings> _settings;

        public AlexaValidationFilter(IOptions<SkillSettings> settings)
        {
            _settings = settings;
        }
        public void OnActionExecuted(ActionExecutedContext context)
        {
            // intentionally empty
        }

        public async void OnActionExecuting(ActionExecutingContext context)
        {
            try
            {
                var body = context.ActionArguments.Values.FirstOrDefault() as SkillRequest;

                // validate app
                if (body?.Session?.Application?.ApplicationId != _settings.Value.AlexaSkillId)
                {
                    context.Result = new BadRequestResult();
                    return;
                }

                var isValid = await IsValidAlexaRequestAsync(context.HttpContext.Request, JsonConvert.SerializeObject(body));

                if (!isValid)
                {
                    context.Result = new BadRequestResult();
                    return;
                }

            }
            catch
            {
                context.Result = new BadRequestResult();
                return;
            }
        }

        /// <summary>
        /// Validates the HTTP request against the Alexa requirements.
        /// </summary>
        /// <returns></returns>
        private async Task<bool> IsValidAlexaRequestAsync(HttpRequest request, string body)
        {
            try
            {
                // Verify SignatureCertChainUrl is present
                var hasHeader = request.Headers.TryGetValue("SignatureCertChainUrl", out var signatureChainUrl);
                if (!hasHeader)
                    return false;

                if (string.IsNullOrWhiteSpace(signatureChainUrl))
                {
                    Trace.TraceError(@"Alexa - empty Signature Cert Chain Url header");
                    return false;
                }

                Uri certUrl;
                try
                {
                    certUrl = new Uri(signatureChainUrl);
                    var validUrl = RequestVerifier.VerifyCertificateUrl(certUrl);
                    if (!validUrl)
                        return false;
                }
                catch
                {
                    Trace.TraceError($@"Alexa - Unable to put sig chain url in to Uri: {signatureChainUrl}");
                    return false;
                }

                // Verify SignatureCertChainUrl is Signature
                var hasSignature = request.Headers.TryGetValue("Signature", out var signature);
                if (!hasSignature)
                    return false;

                if (string.IsNullOrWhiteSpace(signature))
                {
                    Trace.TraceError(@"Alexa - empty Signature header");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(body))
                {
                    Trace.TraceError(@"Alexa - empty body");
                    return false;
                }
                var valid = await RequestVerifier.Verify(signature, certUrl, body);

                if (!valid)
                {
                    Trace.TraceError(@"Alexa - RequestVerification.Verify failed");
                    return false;
                }

                return valid;

            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// This class holds all verification methods needed to authorize requests to an Alexa backend
        /// </summary>
        public static class RequestVerifier
        {
            private const int AllowedTimestampToleranceInSeconds = 150;

            public static bool RequestTimestampWithinTolerance(SkillRequest request)
            {
                return RequestTimestampWithinTolerance(request.Request.Timestamp);
            }

            public static bool RequestTimestampWithinTolerance(DateTime timestamp)
            {
                return Math.Abs(DateTime.Now.Subtract(timestamp).TotalSeconds) <= AllowedTimestampToleranceInSeconds;
            }

            public static async Task<bool> Verify(string encodedSignature, Uri certificatePath, string body)
            {
                if (!RequestTimestampWithinTolerance(JsonConvert.DeserializeObject<SkillRequest>(body)))
                    return false;

                if (!VerifyCertificateUrl(certificatePath))
                {
                    return false;
                }

                var certificate = await GetCertificate(certificatePath);
                if (!ValidSigningCertificate(certificate) || !VerifyChain(certificate))
                {
                    return false;
                }

                if (!AssertHashMatch(certificate, encodedSignature, body))
                {
                    return false;
                }

                return true;
            }

            public static bool AssertHashMatch(X509Certificate2 certificate, string encodedSignature, string body)
            {
                var signature = Convert.FromBase64String(encodedSignature);
                var rsa = certificate.GetRSAPublicKey();

                return rsa.VerifyData(Encoding.UTF8.GetBytes(body), signature, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
            }

            public static async Task<X509Certificate2> GetCertificate(Uri certificatePath)
            {
                var response = await new HttpClient().GetAsync(certificatePath);
                var bytes = await response.Content.ReadAsByteArrayAsync();
                return new X509Certificate2(bytes);
            }

            public static bool VerifyChain(X509Certificate2 certificate)
            {
                //https://stackoverflow.com/questions/24618798/automated-downloading-of-x509-certificatePath-chain-from-remote-host

                X509Chain certificateChain = new X509Chain();
                //If you do not provide revokation information, use the following line.
                certificateChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                return certificateChain.Build(certificate);
            }

            private static bool ValidSigningCertificate(X509Certificate2 certificate)
            {
                return DateTime.Now < certificate.NotAfter && DateTime.Now > certificate.NotBefore &&
                       certificate.GetNameInfo(X509NameType.SimpleName, false) == "echo-api.amazon.com";
            }

            public static bool VerifyCertificateUrl(Uri certificate)
            {
                return certificate.Scheme == "https" &&
                    certificate.Host == "s3.amazonaws.com" &&
                    certificate.LocalPath.StartsWith("/echo.api") &&
                    certificate.IsDefaultPort;
            }
        }
    }
}
