using Alexa.NET.Request;
using AlexDunnVoice.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
                var request = context.HttpContext.Request;
                var body = context.ActionArguments.Values.FirstOrDefault() as SkillRequest;

                // validate app
                if (body?.Session?.Application?.ApplicationId != _settings.Value.AlexaSkillId)
                {
                    context.Result = new BadRequestResult();
                    return;
                }

                
                var isValid = await ValidateRequest(request, body);

                if (!isValid)
                {
                    context.Result = new BadRequestResult();
                    return;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                context.Result = new BadRequestResult();
                return;
            }
        }

        private async Task<bool> ValidateRequest(HttpRequest request, SkillRequest skillRequest)
        {
            try
            {
                var body = "";
                request.EnableRewind();
                using (var stream = new StreamReader(request.Body))
                {
                    stream.BaseStream.Position = 0;
                    body = stream.ReadToEnd();
                    stream.BaseStream.Position = 0;
                }
                request.Headers.TryGetValue("SignatureCertChainUrl", out var signatureChainUrl);
                if (string.IsNullOrWhiteSpace(signatureChainUrl))
                {
                    return false;
                }

                Uri certUrl;
                try
                {
                    certUrl = new Uri(signatureChainUrl);
                }
                catch
                {
                    return false;
                }

                request.Headers.TryGetValue("Signature", out var signature);
                if (string.IsNullOrWhiteSpace(signature))
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(body))
                {
                    return false;
                }

                bool isTimestampValid = RequestVerification.RequestTimestampWithinTolerance(skillRequest);
                bool valid = await RequestVerification.Verify(signature, certUrl, body);

                if (!valid || !isTimestampValid) { return false; } else { return true; }

            }
            catch
            {
                return false;
            }
        }

    }
}
