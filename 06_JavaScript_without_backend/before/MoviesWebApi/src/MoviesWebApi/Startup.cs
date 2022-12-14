using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace MoviesWebApi
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
			// TODO: add CORS and configure a policy for the movies web app
            services.AddAuthentication("Bearer")
                .AddJwtBearer("Bearer", options => { 
                    options.Authority = "https://localhost:5001/";
                    options.MapInboundClaims = false;
                    options.TokenValidationParameters.ValidateAudience = false;
                });

            services.AddMoviesLibrary();

            services.AddAuthorization(options =>
            {
                options.AddPolicy("TokenScopePolicy", builder =>
                {
                    builder.RequireAuthenticatedUser()
                        .RequireClaim("scope", "movie_api");
                });

                options.AddPolicy("SearchPolicy", builder =>
                {
                    builder.RequireAuthenticatedUser();
                    builder.RequireAssertion(ctx =>
                    {
                        if (ctx.User.HasClaim("role", "Admin") ||
                            ctx.User.HasClaim("role", "Customer"))
                        {
                            return true;
                        }
                        return false;
                    });
                });
            });

            services.AddTransient<IAuthorizationHandler, Authorization.ReviewAuthorizationHandler>();
            services.AddTransient<IAuthorizationHandler, Authorization.MovieAuthorizationHandler>();

            services.AddControllers();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();

			// TODO: add the CORS middleware and use the policy configured above

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers().RequireAuthorization("TokenScopePolicy");
            });
        }
    }
}
