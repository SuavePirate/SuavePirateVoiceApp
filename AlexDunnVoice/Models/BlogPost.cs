using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlexDunnVoice.Models
{
    public class BlogPost
    {
        public string Title { get; set; }
        public string ImageUrl { get; set; }
        public DateTime PublishedDate { get; set; }
        public string Description { get; set; }
        public List<string> Categories { get; set; }
    }
}
