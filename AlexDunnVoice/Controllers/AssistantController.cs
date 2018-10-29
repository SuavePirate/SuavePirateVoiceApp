using Alexa.NET.Request;
using Alexa.NET.Request.Type;
using AlexDunnVoice.Controllers.Filters;
using AlexDunnVoice.DataProviders;
using AlexDunnVoice.Handlers;
using AlexDunnVoice.Models;
using AlexDunnVoice.Models.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
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
        [ServiceFilter(typeof(AlexaValidationFilter))]
        public async Task<JsonResult> Alexa([FromBody]SkillRequest input)
        {
            _responseHandler = new AlexaHandler();
            return await Route(GetAlexaIntentName(input));
        }
        [HttpPost("Alexa/Unsigned")]
        public async Task<JsonResult> AlexaUnsigned([FromBody]SkillRequest input)
        {
            _responseHandler = new AlexaHandler();
            return await Route(GetAlexaIntentName(input));
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
                return (input.Request as IntentRequest).Intent.Name;
            }

            return null;
        }
        
    }
}
