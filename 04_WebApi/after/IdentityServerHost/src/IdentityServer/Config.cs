using Duende.IdentityServer;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Test;
using System.Collections.Generic;
using System.Security.Claims;

namespace IdentityServerHost
{
    public class Config
    {
        public static IEnumerable<IdentityResource> IdentityResources = new List<IdentityResource>
        {
            new IdentityResources.OpenId(),
            new IdentityResources.Profile(),
            new IdentityResources.Email()
        };

        public static IEnumerable<ApiScope> ApiScopes = new List<ApiScope>
        {
            new ApiScope("movie_api", "Movie Review Service")
            {
                UserClaims = { "role" }
            }
        };

        public static IEnumerable<Client> Clients = new List<Client>
        {
            new Client
            {
                ClientId = "movie_client",
                ClientName = "Movie Review App",

                AllowedGrantTypes = GrantTypes.Code,
				
                ClientSecrets =
                {
                    new Secret("secret".Sha256())
                },

                RedirectUris =
                {
                    "https://localhost:5005/signin-oidc"
                },

                PostLogoutRedirectUris =
                {
                    "https://localhost:5005/signout-callback-oidc"
                },

                AllowedScopes =
                {
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile,
                    IdentityServerConstants.StandardScopes.Email,
                    "movie_api"
                }
            }
        };

        public static List<TestUser> Users = new List<TestUser>()
        {
            new TestUser {SubjectId="user1", Username="user1", Password = "user1",
                Claims =
                {
                    new Claim("name", "User One"),
                    new Claim("email", "User1@gmail.com"),
                    new Claim("role", "Reviewer"),
                }
            },
            new TestUser {SubjectId="user2", Username="user2", Password = "user2",
                Claims =
                {
                    new Claim("name", "User Two"),
                    new Claim("email", "User2@yahoo.com"),
                    new Claim("role", "Reviewer"),
                }
            },
            new TestUser {SubjectId="user3", Username="user3", Password = "user3",
                Claims =
                {
                    new Claim("name", "User Three"),
                    new Claim("email", "User3@outlook.com"),
                    new Claim("role", "Reviewer"),
                }
            },
            new TestUser {SubjectId="user4", Username="user4", Password = "user4",
                Claims =
                {
                    new Claim("name", "User Four"),
                    new Claim("email", "User4@gmail.com"),
                    new Claim("role", "Customer"),
                }
            },
            new TestUser {SubjectId="user5", Username="user5", Password = "user5",
                Claims =
                {
                    new Claim("name", "User Five"),
                    new Claim("email", "User5@admin.com"),
                    new Claim("role", "Admin"),
                }
            },
        };
    }
}
