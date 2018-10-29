using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using AlexDunnVoice.Models;
using Microsoft.Extensions.Options;
using Microsoft.SyndicationFeed;
using Microsoft.SyndicationFeed.Atom;
using ServiceResult;

namespace AlexDunnVoice.DataProviders
{
    public class RssBlogProvider : IBlogProvider
    {
        private readonly HttpClient _client;
        private readonly IOptions<BlogsSettings> _settings;
        public RssBlogProvider(HttpClient httpClient, IOptions<BlogsSettings> settings)
        {
            _client = httpClient;
            _settings = settings;
        }

        public async Task<Result<List<BlogPost>>> GetLatestPosts()
        {
            try
            {
                var posts = await GetFeed();
                return new SuccessResult<List<BlogPost>>(posts);
            }
            catch
            {
                return new UnexpectedResult<List<BlogPost>>();
            }
        }

        private async Task<List<BlogPost>> GetFeed()
        {
            var xmlReader = XmlReader.Create(_settings.Value.FeedUrl);
            var reader = new AtomFeedReader(xmlReader);

            var posts = new List<BlogPost>();
            //
            // Read the feed
            while (await reader.Read())
            {
                //
                // Check the type of the current element.
                switch (reader.ElementType)
                {
                    //
                    // Read category
                    case SyndicationElementType.Category:
                        ISyndicationCategory category = await reader.ReadCategory();
                        break;

                    //
                    // Read image
                    case SyndicationElementType.Image:
                        ISyndicationImage image = await reader.ReadImage();
                        break;

                    //
                    // Read entry 
                    case SyndicationElementType.Item:
                        IAtomEntry entry = await reader.ReadEntry();
                        // these are the only ones we need for now
                        posts.Add(new BlogPost
                        {
                            Title = entry.Title,
                            PublishedDate = entry.Published.DateTime,
                            Categories = entry.Categories?.Select(c => c.Name)?.ToList()
                        });
                        break;

                    //
                    // Read link
                    case SyndicationElementType.Link:
                        ISyndicationLink link = await reader.ReadLink();
                        break;

                    //
                    // Read person
                    case SyndicationElementType.Person:
                        ISyndicationPerson person = await reader.ReadPerson();
                        break;

                    //
                    // Read content
                    default:
                        ISyndicationContent content = await reader.ReadContent();
                        break;
                }
            }

            return posts;
        }
    }
}
