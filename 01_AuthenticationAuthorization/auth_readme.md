# ASP.NET Core Authentication and Authorization
---

This lab is based on a simple movie review website.
It allows customers to browse and search movies and view movie reviews.
It also allows reviewers to create and edit movie reviews.

## Overview

In this lab you will add cookie-based authentication to the movie review website using the cookie authentication handler and claims-based identity.
Once users are authenticated, you will then also implement policy-based and resource-based authorization using the ASP.NET Core authorization framework.

### Application notes

_Data_: All the data for the movie review website it kept in-memory, so any changes to data will be lost when the application restarts.

_Users_: The lab predefines five users whose usernames are **user1** through **user5**.
These users' passwords will be the same as their username.
Once these users login to the applicaiton they will have different roles within the application: 
*user1*, *user2* and *user3* are reviewers, 
*user4* is a customer, 
and *user5* is an administrator.
When you login you can choose one of those usernames in order to trigger different behavior in the application.

## Part 1: Cookie-based authentication

In this part you will add the cookie authentication handler, add the authentication middleware, allow the user to login and logout, and use claims to model the identity of the authenticated user.

* Open the application from the `~/before` folder. 
  * Inspect the code to become familiar with the structure.
  * Run the application to see what it currently does.

To authenticate users, we need to add the authentication services and middleware to the application. We will do this work in the `MoviesWebApp` project.

* Open `~/Startup.cs`.
* In the `ConfigureServices` method add the authentication services to DI.
  * Set `"Cookies"` as the default scheme.
* Also add the cookie handler with `AddCookies`. 
  * Set the `LoginPath` to `"/Account/Login"`.
  * Set the `AccessDeniedPath` to `"/Account/AccessDenied"`.

```
services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });
```

* In the `Configure` method register the  authentication middleware after the routing middleware, but before the endpoints middleware.

```
app.UseRouting();

app.UseAuthentication();

app.UseEndpoints(endpoints =>
{
    endpoints.MapDefaultControllerRoute();
});
```
Next we will use the authentication services to issue the login cookie.

* Open `~/Controllers/AccountController.cs` and find the `Login` action methods.
* Implement the logic to allow users to signin.
  * We don't have a real database of username/passwords, so just check that they are the same.
  * If successful, create a list of `Claim`s and populate it with the `sub` claim with the value of the `username`.
  * Notice there is a `MovieIdentityService` in the `AccountController` -- this allows application specific claims to be loaded based upon the `sub` claim.
   Feel free to look in the implementation to understand the additional claims being loaded for the users.
   Invoke it and merge the claims returned into the claims collection you created.
  * Create `ClaimsIdentity` and `ClaimsPrincipal` from the claims.
  * Use the `SignInAsync` method on the `HttpContext` and issue the cookie from the `ClaimsPrincipal`.
  * Rediriect the user to the `ReturnUrl` (if present), or to the home page. 

```
[HttpPost]
public async Task<IActionResult> Login(LoginViewModel model)
{
    if (model.Username == model.Password)
    {
        var claims = new List<Claim>
        {
            new Claim("sub", model.Username)
        };
        claims.AddRange(_identityService.GetClaimsForUser(model.Username));

        var ci = new ClaimsIdentity(claims, "password", "name", "role");
        var cp = new ClaimsPrincipal(ci);

        await HttpContext.SignInAsync("Cookies", cp);

        if (model.ReturnUrl != null)
        {
            return LocalRedirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    ModelState.AddModelError("", "Invalid username or password");
    return View();
}
```

* Find the `Logout` method.
* Implement the logic to allow a user to signout.

```
public async Task<IActionResult> Logout()
{
    await HttpContext.SignOutAsync("Cookies");
    return RedirectToAction("Index", "Home");
}
```

* Run the application and test signing in and signing out.

## Part 2: Requiring authenticated users

In this part you will start to implement authorization, and the first step will be to prevent anonymous access to much of the application.
This will require adding a route requirement and adding the authorization middleware.

* Back in `~/Startup.cs`, add the authorization middleware to the pipeline in `Configure`. It should be placed after the authentication middleware and before the endpoint middleware.

```
app.UseAuthentication();

app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapDefaultControllerRoute();
});
```

Next we want a global authorization requirement to the endpoint that handles MVC controllers.
This will also be done in the in `Configure` method.

* Use the `RequireAuthorization` fluent API on the result of `MapDefaultControllerRoute` where the controller endpoint is registered. 

```
app.UseEndpoints(endpoints =>
{
    endpoints.MapDefaultControllerRoute().RequireAuthorization();
});
```

If you were to run the application now, an anonymous user would not be able to access any page including the login page. 
We now need to relax the requirement except for the few places where we want to allow anoymous access.

* Add the `[AllowAnonymous]` attribute to both the `HomeController` and the `AccountController`.

```
[AllowAnonymous]
public class AccountController : Controller
{
    ...
}
```

* Run the application to test that an anonymous user cannot access the movies, but can login.



## Part 3: Claims transformation to add roles for user

The next authorization we want to enforce is only customers (and admins) may use the search feature. 
The logic for allowing a user to use search will be based on roles.
This means we need to have a strategy to map users into roles, and this could be done in several ways.

One approach would be to load the roles at login time and store them in the cookie.
This could be done in the `MovieIdentityService` that is being invoked from the `AccountController`'s `Login` action method.
In fact, if you look into the implementation you can see how this might be done.
This approach has the side effect of possibly bloating the cookie (as more and more claims/roles are added), and if the user's roles change they would need to logout and then login again to have them take effect.

Another approach, which will take in this lab, is to use a claims-transformation approach and load the roles on each request.
The role assignments will be configured via an external configuration file, and this is a typical design where the identity data is maintained separate from authorization data.
These roles will then augment the in-memory `ClaimsPrincipal` created on each request from the authentication cookie.
To perform the role mappings, we will use the open source `PolicyServer.Local` framework (whose NuGet has already been referenced in the project).

`PolicyServer.Local` uses JSON syntax in `~/appsettings.json` to define the mappings. Open that file and have a look at what's been configured in the "Policy" section. It should looks like this:

```
  "Policy": {
    "roles": [
      {
        "name": "Reviewer",
        "subjects": [ "user1", "user2", "user3" ]
      },
      {
        "name": "Customer",
        "subjects": [ "user4" ]
      },
      {
        "name": "Admin",
        "subjects": [ "user5" ]
      }
    ]
  }
```

This indicates which users should be reviewers, customers, and admins.

The next thing we want to do is make the services from `PolicyServer.Local` available to the application by adding them into the DI system in `~/Startup.cs`.
Do this in the `ConfigureServices` method.
* Call `AddPolicyServerClient` and pass the configuration loaded via `Configuration.GetSection("Policy")`.

```
services.AddPolicyServerClient(Configuration.GetSection("Policy"));
```

Next, add the middleware to perform the claims transformation via `UsePolicyServerClaims`. 
Add this in `Configure` after the authentication middleware and before the authorization middleware.

```
app.UseAuthentication();

app.UsePolicyServerClaims();

app.UseAuthorization();
```

If you run the application and login, then on the home page you should see the appropriate role now being added on each request to the user's list of claims.

## Part 4: Policy-based authorization

Now that we have roles for the user, we can implement and enfore our search authorization, such that only customers may use the search feature.
This involves building an ASP.NET Core authorization policy that will grant access to users based on their role.

* Add a call to `AddAuthorization` in `ConfigureServices`.
* In the call accept an delegate that passes the options.

```
services.AddAuthorization(options =>
{
    ...
});
```

 * In the callback, use the options API `AddPolicy` to create a new policy and name it `"SearchPolicy"`.
 * Build the policy to use `RequireAuthenticatedUser` and `RequireAssertion`. 
   * For the assertion callback check for either the `"Admin"` or `"Customer"` role claim and if they are present return `true`. Return `false` otherwise.

```
services.AddAuthorization(options =>
{
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
``` 

* Now apply the `"SearchPolicy"` to the `Search` action method on the `MovieController`.

```
[Authorize("SearchPolicy")]
public IActionResult Search(string searchTerm = null)
{
    ...
}
```

* Run the application to test that only customers or admins (i.e. **user4** or **user5**) are allowed to use the search feature.
If not allowed, you should be redirected to the "access denied" page.

As your authorization requirements grow, you might need more and more policies. 
To help with this problem, there is another library called `PolicyServer.Local` in the solution.
It has a permission feature to dynamically create ASP.NET Core policies that are granted by roles.
We will change our application to use this for the `"SearchPolicy"` just to show how it works.

First, comment out the `"SearchPolicy"` that we just added.

```
services.AddAuthorization(options =>
{
    //options.AddPolicy("SearchPolicy", builder =>
    //{
    //    builder.RequireAuthenticatedUser();
    //    builder.RequireAssertion(ctx =>
    //    {
    //        if (ctx.User.HasClaim("role", "Admin") ||
    //            ctx.User.HasClaim("role", "Customer"))
    //        {
    //            return true;
    //        }
    //        return false;
    //    });
    //});
});
```

Next, enable the dynamic policy feature in `PolicyServer.Local` in the DI system by adding a call to `AddAuthorizationPermissionPolicies`.

```
services.AddPolicyServerClient(Configuration.GetSection("Policy"))
    .AddAuthorizationPermissionPolicies();
```

Next, add the permission in `~/appsettings.json`.

```
"Policy": {
    "roles": [
      {
        "name": "Reviewer",
        "subjects": [ "user1", "user2", "user3" ]
      },
      {
        "name": "Customer",
        "subjects": [ "user4" ]
      },
      {
        "name": "Admin",
        "subjects": [ "user5" ]
      }
    ],
    "permissions": [
      {
        "name": "SearchPolicy",
        "roles": [ "Customer", "Admin" ]
      }
    ]
  }
```

This declares a permission called `"SearchPolicy"` which creates the dynamic ASP.NET policy with the same name.

Finally, test that the permission now works for the search feature. Note that we are still using the `[Authorize("SearchPolicy")]` to trigger the policy and enforce the requirement, but it's sourced dynamically from the permission mapping rather than hand-coding the logic.

## [Challenge] Resource-based authorization

For a challenge, you can try to use the resource-based authorization feature in ASP.NET Core's authorization system.
The authorization logic we require is to only allow reviewers to create and edit reviews.
We will do this by building authorization handlers. 

* Expand the `~/Authorization` folder. 
This contains the beginings of the authorization handlers.
Open the files and inspect the starter code.
* For the `MovieAuthorizationHandler` implement the logic that only reviewers are allowed to review movies.

```
protected override Task HandleRequirementAsync(
    AuthorizationHandlerContext context, 
    OperationAuthorizationRequirement requirement, 
    MovieDetails movie)
{
    if (requirement == MovieOperations.Review)
    {
        if (context.User.HasClaim("role", "Reviewer"))
        {
            context.Succeed(requirement);
        }
    }
    return Task.FromResult(0);
}
```

* For the `ReviewAuthorizationHandler` implement the logic that only the reviewer that created the review can edit it.
  * Use the `sub` claim on the user and compare it to the `UserId` property on the `MovieReview`.
  * Also, allow admins to perform any operation.

```
protected override Task HandleRequirementAsync(
    AuthorizationHandlerContext context, 
    OperationAuthorizationRequirement requirement, 
    MovieReview review)
{
    if (context.User.HasClaim("role", "Admin"))
    {
        context.Succeed(requirement);
    }

    if (requirement == ReviewOperations.Edit)
    {
        var sub = context.User.FindFirst("sub")?.Value;
        if (sub != null && review.UserId == sub)
        {
            context.Succeed(requirement);
        }
    }
    return Task.FromResult(0);
}
```

* To use these authorization handlers, they need to be registered in DI in `ConfigureServices`. Do that now.

```
services.AddTransient<IAuthorizationHandler, ReviewAuthorizationHandler>();
services.AddTransient<IAuthorizationHandler, MovieAuthorizationHandler>();
```

Next we want to invoke the authorization logic in the MVC code to protect access.

* In the `ReviewController` controller change the consructor and inject an `IAuthorizationService` and store it in a member variable.

```
private IAuthorizationService _authorization;
public ReviewController(ReviewService reviews, 
    MovieService movies, IAuthorizationService authorization)
{
    _reviews = reviews;
    _movies = movies;
    _authorization = authorization;
}
```

* In `New` enforce the authorization for creating a review for the movie. If the user is not allowed, then return the result from `Forbid`.

```
var authz = await _authorization.AuthorizeAsync(User, 
    movie, Authorization.MovieOperations.Review);
if (!authz.Succeeded)
{
    return Forbid();
}
```

* In `Edit` and `Delete` enforce the authorization for editing the review.

```
var authz = await _authorization.AuthorizeAsync(User, 
    review, Authorization.ReviewOperations.Edit);
if (!authz.Succeeded)
{
    return Forbid();
}
```

* Run the application and test that only reviewers can create reviews, and that reviewers can only edit their own reviews.  

Next we want to hide the buttons in the UI if the user is not allowed to create or edit reviews.

* In `~/Views/Movie/Details.cshtml` notice the `IAuthorizationService` is already being injected.
* Locate the "create review" button and hide it if the user is not authorized.

```
@{ 
    var authz = await authorization.AuthorizeAsync(User,
        Model, MoviesWebApp.Authorization.MovieOperations.Review);
    if (authz.Succeeded)
    {
        <div class="row search-form">
            <a asp-action="New" asp-controller="Review" 
                asp-route-movieId="@Model.Id" 
                class="btn btn-primary">Write a review</a>
        </div>
    }
}
```

* Locate the "edit review" button and hide it is the user is not authorized.

```
<td>
@{ 
    var editAuthz = await authorization.AuthorizeAsync(User,
        review, MoviesWebApp.Authorization.ReviewOperations.Edit);
    if (editAuthz.Succeeded)
    {
        <a asp-action="Edit" asp-controller="Review" 
            asp-route-id="@review.Id" 
            class="btn btn-primary">edit</a>
    }
}
</td>
```

* Run and test that the buttons are now hidden when appropriate.

Finally, we need to make a change in our authorization logic. 
Reviewers are not allowed to create reviews for all movies. Certain reviewers are only allowed to review movies from certain countries.
This logic requires a lookup in a permission database and this is implemented in a class called `ReviewPermissionService`. 
You will now incorporate this additional logic in the `MovieAuthorizationHandler`.

*  Change the constructor to accept the `ReviewPermissionService` and store it in a member variable.

```
private ReviewPermissionService _reviewPermissions;
public MovieAuthorizationHandler(ReviewPermissionService reviewPermissions)
{
    _reviewPermissions = reviewPermissions;
}
```

* In `Handle` after the role check, invoke `GetAllowedCountries` on the `ReviewPermissionService` and compare the movie's `CountryName` to the returned list. Only if the movie is from an allowed country, then call `Succeed`.

```
protected override Task HandleRequirementAsync(
    AuthorizationHandlerContext context, 
    OperationAuthorizationRequirement requirement, 
    MovieDetails movie)
{
    if (requirement == MovieOperations.Review)
    {
        if (context.User.HasClaim("role", "Reviewer"))
        {
            var allowed = _reviewPermissions.GetAllowedCountries(context.User);
            if (allowed.Contains(movie.CountryName))
            {
                context.Succeed(requirement);
            }
        }
    }

    return Task.FromResult(0);
}
```

* Run and test the country-specific authorization. *user1* should be able to create reviews from any country, but *user2* cannot create a review for France.
