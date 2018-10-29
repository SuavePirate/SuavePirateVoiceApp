using AlexDunnVoice.Models;
using ServiceResult;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlexDunnVoice.DataProviders
{
    public interface IBlogProvider
    {
        Task<Result<List<BlogPost>>> GetLatestPosts();
    }
}
