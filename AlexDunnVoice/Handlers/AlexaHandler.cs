using Alexa.NET;
using Alexa.NET.Response;
using AlexDunnVoice.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace AlexDunnVoice.Handlers
{
    public class AlexaHandler : IResponseHandler
    {
        public JsonResult BlogPosts(List<BlogPost> posts)
        {
            var output = HttpUtility.HtmlDecode($"Here are the latest posts: {string.Join(", ", posts.Take(4).Select(p => p.Title).ToList())} - be sure to check back regularly for new content from Alex!");
            return new JsonResult(ResponseBuilder.Tell(output, null));
        }

        public JsonResult Help()
        {
            return new JsonResult(ResponseBuilder.Ask(Messages.Help, null));
        }

        public JsonResult Exit()
        {
            return new JsonResult(ResponseBuilder.Tell("Be sure to come back for updates from Suave Pirate!", null));
        }
        public JsonResult Welcome()
        {
            return new JsonResult(ResponseBuilder.Ask(Messages.Welcome, null));
        }
    }
}
