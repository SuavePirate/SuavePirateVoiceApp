using AlexDunnVoice.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlexDunnVoice.Handlers
{
    public interface IResponseHandler
    {
        JsonResult Welcome();
        JsonResult Help();
        JsonResult BlogPosts(List<BlogPost> posts);
    }
}
