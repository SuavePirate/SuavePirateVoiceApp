using Alexa.NET.Request;
using Alexa.NET.Request.Type;
using AlexDunnVoice.Controllers.Filters;
using AlexDunnVoice.DataProviders;
using AlexDunnVoice.Handlers;
using AlexDunnVoice.Models;
using AlexDunnVoice.Models.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AlexDunnVoice.Controllers
{
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AssistantController : Controller
    {
        private IResponseHandler _responseHandler;
        private readonly IBlogProvider _blogProvider;

        public AssistantController(IBlogProvider blogProvider)
        {
            _blogProvider = blogProvider;
        }

        [HttpPost("Alexa")]
        //[ServiceFilter(typeof(AlexaValidationFilter))]
        public async Task<ActionResult> Alexa([FromBody]SkillRequest input)
        {
            var isValid = await ValidateRequest(HttpContext.Request, input);
            if (!isValid)
                return BadRequest();
            _responseHandler = new AlexaHandler();
            return await Route(GetAlexaIntentName(input));
        }
        [HttpPost("Alexa/Unsigned")]
        public async Task<JsonResult> AlexaUnsigned([FromBody]SkillRequest input)
        {
            _responseHandler = new AlexaHandler();
            return await Route(GetAlexaIntentName(input));
        }
        private async Task<bool> ValidateRequest(HttpRequest request, SkillRequest skillRequest)
        {
            try
            {
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
                var body = "";
                request.EnableRewind();
                using (var stream = new StreamReader(request.Body))
                {
                    stream.BaseStream.Position = 0;
                    body = stream.ReadToEnd();
                    stream.BaseStream.Position = 0;
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

        private async Task<JsonResult> Route(string intentName)
        {
            switch(intentName)
            {
                case Intents.Welcome:
                    return _responseHandler.Welcome();
                case Intents.Help:
                    return _responseHandler.Help();
                case Intents.Feed:
                    var result = await _blogProvider.GetLatestPosts();
                    if(result.ResultType == ServiceResult.ResultType.Ok)
                    {
                        return _responseHandler.BlogPosts(result.Data);
                    }
                    break;
                case Intents.Cancel:
                case Intents.Stop:
                    return _responseHandler.Exit();
            }

            return _responseHandler.Help();
        }


        // we will have a similar method for dialog flow for google
        private string GetAlexaIntentName(SkillRequest input)
        {
            if(input.GetRequestType() == typeof(LaunchRequest))
            {
                return "WelcomeIntent";
            }
            if (input.GetRequestType() == typeof(IntentRequest))
            {
                return (input.Request as IntentRequest).Intent.Name.Replace("AMAZON.", string.Empty);
            }

            return null;
        }
        
    }
}
