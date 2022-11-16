# JavaScript client applications with a backend
---

This lab is based on a simple movie review website.
It allows customers to browse movies.

## Overview

In this lab the movie review application has been rewritten as a JavaScript-based application with a backend.
Given that there is a backend to the SPA, the server-side code will be used to initiate and consume OpenID Connect requests and results (using the BFF pattern and the Duende.BFF framework).
It won't have as much functionality as the previous labs, but it will suffice to show how to manage a session and call a web API using the "Backend For Frontend" pattern.

__Lab Note__: For this lab you will need to keep three solutions open: one for IdentityServer, one for the movie review web API, and another for the movie review client application.

## Part 1: Logging a user in and out via the BFF pattern

In this part you will examine the structure of the JavaScript-based application.
You will then modify it (with the help of the Duende.BFF framework) to perform an OpenID Connect login and manage a session.

* Open the "MoviesWebApp" solution from `~/before`.
* Run the application to see that the home page looks essentially the same as it did in the prior labs.
   * Open `~/Startup.cs` and notice how it resembles a normal server-side configuration with the cookie and OIDC authentication handlers. 
   * Also notice, though, how the project has no server-side UI code (neither Razor pages nor MVC controllers).
* Expand the `~/wwwroot` folder and notice the `.html` files. These are the files that now make up the UI for this application.
  * Open `~/wwwroot/index.html` to examine the structure. Notice it includes a JavaScript file `site.js` which contains the application-specific UI code.
  * Open `~/wwwroot/js/site.js` to examine the application code.
    This file contains the logic to dynamically build the UI based on the state of the logged in user and the user's activity. 
    
    Notice at the top of the file the declaration of the `userClaims` variable and the associated comment. 
    The `userClaims` variable is used as a global flag to detect if the user is logged in or not.
    If it is assigned then it contains the user's claims.

The first thing we will add is code to enable the BFF management endpoints.

* In `~/Startup.cs` in `ConfigureServices`:
   * Add the BFF services to DI with `AddBff()`.
   * The BFF management infrastructure assumes the ASP.NET Core authentication system's *Challenge* and *SignOut* schemes to be configured for the OpenID Connect handler. Configure these now in the call to `AddAuthentication`:     
```
public void ConfigureServices(IServiceCollection services)
{
    services.AddBff();

    services.AddAuthentication(options=> {
        options.DefaultScheme = "Cookies";
        options.DefaultChallengeScheme = "oidc";
        options.DefaultSignOutScheme = "oidc";
    })
        .AddCookie("Cookies")
        .AddOpenIdConnect("oidc", ...

        /// rest omitted
}
```

* In `~/Startup.cs` in `Configure`:
   * Add the BFF middleware before the authorization middleware with `UseBff()`.
   * In `UseEndpoints` add the BFF management endpoints with a call to `MapBffManagementEndpoints()`.   

```
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
        endpoints.MapControllers()
            .RequireAuthorization();
    });
}
```

The BFF management endpoints should now be configured to handle triggering login, logout, and querying the user's status.

The next thing we will add is code to the client-side to query the user's session status.

* Open `~/wwwroot/js/site.js` and find the `showLoginStatus` function.
* In the function will will invoke the "user" BFF endpoint.
   * Use `fetch` to make a *GET* request to the '/bff/user' endpoint. Be sure to set the `X-CSRF` request header to the constant `1` to help protect against cross-site request forgery.
   * If the result is a 401 then call `showAnonymousUser()`.
   * If the result is a 200 then call `showLoggedInUser()` and parse the response body JSON and assign the result to the `userClaims` variable.

```
async function showLoginStatus() {

    const req = new Request("/bff/user", {
        headers: new Headers({
            'X-CSRF': '1'
        })
    })

    try {
        const resp = await fetch(req);
        if (resp.ok) {
            userClaims = await resp.json();
            showLoggedInUser();
        }
        else if (resp.status === 401) {
            showAnonymousUser();
        }
    }
    catch (e) {
        console.log("error checking user status");
    }
}
```

* Open and run the IdentityServer project. 
* Run the movie review web app.
You should now see that the application indicates that the user is not logged in by showing the text "Not logged in" in the top right menu.

   You can confirm this is working properly by also using the developer tools in your browser to monitor the network traffic and see the "user" endpoint being called and returning a 401 status code.

Next, configure the application to trigger login using the BFF "login" endpoint.

* Find the `toggleLogin` function.
* In this method navigate the browser to the "/bff/login" endpoint.

```
function toggleLogin() {
   window.location = "/bff/login";
}
```

* You should now be able to run the app and try to login. 
  You should now see some claims for the user on the home page.

You might notice that that the menu does say "Logged in as" but is missing the user's name. We need to update the code to locate the name claim and have its value displayed correctly.

* Locate the `showLoggedInUser` function.

    The `userClaims` collection contains an array of objects that represent the user's claims. These objects have `type` and `value` properties.
* Update the code to find the object in the `userClaims` collection whose `type` is "name".
* Once located, read its `value` property and assign to the local `name` variable to have it displayed into the menu.

```
function showLoggedInUser() {
    const name = userClaims.find(claim => claim.type === 'name').value;
    login_status.text("Logged in as: " + name);
    login_button.text("Logout");
}
```

Login again (or reload the page) to confirm it's working.

Next we want to support logout. 
* Locate the `toggleLogin` function.

This function is called for both login and logout. 
We modified it earlier to just handle login, so we'll modify this to now perform the correction action based on the user's status.

* Use the `userClaims` to detect of the user is already logged in or not. 
* If the user is not logged in, then keep the code that redirects to "/bff/login".
* If the user is logged in, we need to redirect to the BFF "logout" endpoint. Given that this is a well-known endpoint, we don't want to allow XSRF attacks against it, so the "logout" endpoint expects an anti-XSRF token. The logout URL with this token is provided in the user's claims.

    Find the claim of `type` "bff:logout_url" and use its `value` to redirect the browser so the user can logout.

```
function toggleLogin() {
    if (userClaims) {
        const logoutUrlClaim = userClaims.find(claim => claim.type === 'bff:logout_url');
        window.location = logoutUrlClaim.value;
    }
    else {
        window.location = "/bff/login";
    }
}
```

Run the app and test logging in and out.

In the next two parts you will call the web API with the user's access token via the BFF pattern. In the first part we will invoke a local API, and in the second part we will invoke a remote API using the BFF proxy feature.

## Part 2: Invoke a local API

* In the movie web app, open `~/MoviesController.cs` and inspect the code.

This code represents a local API that the client-side JavaScript can invoke.
It could access a database or perform other operations, but it just happens to invoke a remote API to get the movie data.

When invoking the remote API, we need to send the user's access token to secure the call.
This access token is part of the user's session, and is available when the front-end makes a call to the local API.
You will modify the code now to obtain the access token and pass it to the remote API.

* In the `GetMovies` method, obtain the user's access token via a call to `GetUserAccessTokenAsync()` extension method on the `HttpContext`.
* Set this access token as the `Authorization` on the `HttpClient`. Use the "Bearer" scheme.

```
[HttpGet("/api/movies")]
public async Task<IActionResult> GetMovies(int page)
{
    var token = await HttpContext.GetUserAccessTokenAsync();

    var request = new HttpRequestMessage(HttpMethod.Get, 
        "https://localhost:5009/movies?page=" + page);
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    var response = await _client.SendAsync(request);

    var data = await response.Content.ReadAsStringAsync();

    return new ContentResult()
    {
        Content = data,
        ContentType = "application/json"
    };
}
```

Our local API now has a way to call out to remote APIs using the user's access token. We will now update our client-side JavaScript to invoke the local API.

* Open `~/wwwroot/js/site.js`.
* Find the `getMovieData` function.
This is invoked when the user clicks the "Movies" link at the top of the page.
* Use `fetch` to make a GET request to the '/api/movies' endpoint.
  * Pass a "?page=" query param based on the `page` parameter passed into this function. 
  * Is the response is successful, then parse the JSON response body and call `bindMovieData(data)` passing the results as a parameter.
  * If the response is not successful, then log an error.

```
async function getMovieData(page) {
    const req = new Request('/api/movies?page=' + page);

    try {
        const resp = await fetch(req);
        if (resp.ok) {
            const data = await resp.json();
            bindMovieData(data);
        }
        else if (resp.status === 401) {
            console.log("Error Calling API");
        }
    }
    catch (e) {
        console.log("error checking user status");
    }
}
```

Now when the movies page is accessed, the local API is invoked. Test this by:

* Running the movie review web API project.
* Running the movie web app and try to access movies again. 

You should see the movies listed in the UI.

Since the local API is just a custom endpoint in the application, it is vulnerbale to a XSRF attack. 
To protect it against such an attack, we need to add an anti-XSRF token to the call and perform validation server-side for the token.
The Duende.BFF library provides support for this.

* Open `~/Startup.cs` and locate the `Configure` method.
* Inside of `UseEndpoints` notice where the local API is registered with `MapControllers()`.
* Add a call to `.AsBffApiEndpoint()` to the end of the fluent API call chain.

```
// local APIs
endpoints.MapControllers()
    .RequireAuthorization()
    .AsBffApiEndpoint();
```

This now enforces that an anti-XSRF token is used for these endpoints.
We now need to pass the anti-XSRF token when calling the local API from the client-side JavaScript.

* Open `~/wwwroot/js/site.js`.
* Find the `getMovieData` function.
* Pass a `X-CSRF` request header to the constant 1 as the anti-XSRF token.

```
async function getMovieData(page) {
    const req = new Request('/api/movies?page=' + page, {
        headers: new Headers({
            'X-CSRF': '1'
        })
    });

    try {
        const resp = await fetch(req);
        if (resp.ok) {
            const data = await resp.json();
            bindMovieData(data);
        }
        else if (resp.status === 401) {
            console.log("Error Calling API");
        }
    }
    catch (e) {
        console.log("error checking user status");
    }
}
```

You can now test to ensure the application still works.
You can also inspect the network traffic to view the header.
Also, you can test the local API by not passing the token to see that the value is required.

## Part 3: Use the BFF proxy to invoke a remote API

We now have a way to secure calls to a local API, and that local API has a way to use the user's access token to invoke remote APIs.
This pattern works if the local API is adding logic or other useful processing to the remote API.
But in some scenarios that is not needed and thus we just need the BFF layer to be a pass-thru to the remote API.
Coding a local API endpoint for every remote API endpoint is tedious, so we will now remove the manually coded local pass-thru API and instead use the BFF proxy feature to automatically allow the front-end JavaScript to be able to invoke the remote APIs.
 
First we will remove the code for the local API.

* Open `~/Startup.cs` and locate the `Configure` method.
* Inside of `UseEndpoints` notice where the local API is registered with `MapControllers()`.
* Remove (or comment out) the entire call to `MapControllers()`.

```
// local APIs
//endpoints.MapControllers()
//    .RequireAuthorization()
//    .AsBffApiEndpoint();
```

Next we will enable the BFF proxy to the remote API:

* Inside of `UseEndpoints` add a call to `MapRemoteBffApiEndpoint()`.
   * Use "/api" and the path prefix.
   * Use "https://localhost:5009" as the remote API address.
* At the end of the call to `MapRemoteBffApiEndpoint()`, add a call to `.RequireAccessToken(TokenType.User)` to indicate that the user's access token should automatically be passed to the remote API (much like we did manually in the prior section).

```
endpoints.MapRemoteBffApiEndpoint("/api", "https://localhost:5009")
   .RequireAccessToken(TokenType.User);
```

Finally, we need to add the services to DI that use YARP to enable remote API calls. 
In `Configure` in `Startup.cs`, use the fluent API `AddRemoteApis` after the call to `AddBff`.

```
public void ConfigureServices(IServiceCollection services)
{
    services.AddBff()
        .AddRemoteApis();

    // ...
}
```

That's all that's needed. 
The client-side JavaScript will not need to change, and we no longer need a manually coded local API code.

Run and test to validate the movie data API is still working.
