using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Identity;
using System;

namespace MoviesWebApp
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
            services.AddMoviesLibrary();
            

            // TODO: add authentication service to DI
            // set the default scheme to "Cookies"
            services.AddAuthentication(defaultScheme: "cookies")
                .AddCookie("cookies", options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.AccessDeniedPath= "/Account/AccessDenied";
                    options.Cookie.Name = "tp";
                  /*  options.Cookie.Expiration = TimeSpan.FromMinutes(5);
                    options.SlidingExpiration = false;*/
                });

            // TODO: add the cookie handler
            // set login and access denied paths ("/Account/Login" and "/Account/AccessDenied")


            // TODO: add authorization services to DI
            services.AddAuthorization();
            // TODO: configure a "SearchPolicy" to only allow authenticated customers and admins

            // TODO: add the PolicyServer.Local services to DI

            // TODO: register the authorization handlers in DI


            // Add framework services.
            services.AddControllersWithViews();
            services.AddMvc(options =>
            {
                // TODO: add a AuthorizeFilter that uses a policy that prevents anonymous access
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseRouting();

            // TODO: add the authentication middleware
            app.UseAuthentication();
            // TODO: add the claims transformation middleware from PolicyServer.Local

            // TODO: add the authorization middleware

            app.UseEndpoints(endpoints =>
            {
                // TODO: add the authorization requirement to the route
                endpoints.MapDefaultControllerRoute();
            });
        }
    }
}
