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
            services.AddBff()
                .AddRemoteApis();

            services.AddAuthentication(options=> {
                options.DefaultScheme = "Cookies";
                options.DefaultChallengeScheme = "oidc";
                options.DefaultSignOutScheme = "oidc";
            })
                .AddCookie("Cookies")
                .AddOpenIdConnect("oidc", options =>
                {
                    options.Authority = "https://localhost:5001/";

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
            
            app.UseBff();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                // login, logout, user, backchannel logout...
                endpoints.MapBffManagementEndpoints();

                // local APIs
                //endpoints.MapControllers()
                //    .RequireAuthorization()
                //    .AsBffApiEndpoint();

                // proxy endpoint for APIs
                // all calls to /api/* will be forwarded to the remote API
                // user access token will be attached in API call
                // user access token will be managed automatically using the refresh token
                endpoints.MapRemoteBffApiEndpoint("/api", "https://localhost:5009")
                    .RequireAccessToken(TokenType.User);
            });
        }
    }
}
