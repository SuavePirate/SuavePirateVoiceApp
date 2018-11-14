using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using AlexDunnVoice.Controllers.Filters;
using AlexDunnVoice.DataProviders;
using AlexDunnVoice.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Swagger;

namespace AlexDunnVoice
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            services.AddOptions();
            services.Configure<BlogsSettings>(Configuration.GetSection(nameof(BlogsSettings)));
            services.Configure<SkillSettings>(Configuration.GetSection(nameof(SkillSettings)));
            services.AddScoped<IBlogProvider, RssBlogProvider>();
            services.AddScoped<HttpClient>();
            services.AddScoped<AlexaValidationFilter>();

            services.AddSwaggerGen(config =>
            {
                // gets xml comments directory to show in swagger
                var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var commentsFileName = Assembly.GetExecutingAssembly().GetName().Name + ".XML";
                var commentsFile = Path.Combine(baseDirectory, commentsFileName);

                config.SwaggerDoc("v1", new Info { Title = "alexdunn.org voice api", Version = "V1" });

                if (File.Exists(commentsFile))
                {
                    config.IncludeXmlComments(commentsFile);
                }
            });

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            //if (env.IsDevelopment())
            //{
            //    app.UseDeveloperExceptionPage();
            //}
            //else
            //{
            //    app.UseHsts();
            //}

            app.UseHttpsRedirection();
            app.UseMvc();

            app.UseSwagger();
            app.UseSwaggerUI(config =>
            {
                config.SwaggerEndpoint("/swagger/v1/swagger.json", "alexdunn.org voice v1");
            });
        }
    }
}
