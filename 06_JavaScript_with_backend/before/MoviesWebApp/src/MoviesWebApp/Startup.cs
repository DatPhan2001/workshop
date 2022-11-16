using Duende.Bff;
using Duende.Bff.Yarp;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace MoviesWebApp
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // TODO: add the BFF services

            services.AddAuthentication(options=> {
                options.DefaultScheme = "Cookies";
                // TODO: set the challenge and signout schemes to be OIDC
            })
                .AddCookie("Cookies")
                .AddOpenIdConnect("oidc", options =>
                {
                    options.Authority = "https://localhost:5001/";
                    options.RequireHttpsMetadata = false;

                    options.ClientId = "movie_client";
                    options.ClientSecret = "secret";
                    
                    options.ResponseType = "code";
                    options.ResponseMode = "query";

                    options.MapInboundClaims = false;
                    options.SaveTokens = true;
                    options.GetClaimsFromUserInfoEndpoint = true;

                    options.Scope.Add("openid");
                    options.Scope.Add("profile");
                    options.Scope.Add("email");
                    options.Scope.Add("movie_api");

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        NameClaimType = "name",
                        RoleClaimType = "role"
                    };
                });


            services.AddControllers();
            services.AddAuthorization();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseAuthentication();
            
            // TODO: Add BFF middleware

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                // TODO: add BFF management endpoints

                // TODO: add BFF support to local endpoints
                // local APIs
                endpoints.MapControllers()
                    .RequireAuthorization();

                // TODO: replace local endpoints with remote endpoint using BFF proxy
            });
        }
    }
}
