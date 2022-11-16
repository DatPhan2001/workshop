/// <reference path="../lib/jquery/dist/jquery.js" />

(async function () {
    // userClaims is being used to signal to the JS code if the user is logged in or not
    // it will be undefined if the user is not logged in.
    // if the code below in showLoginStatus() detects the user is logged in, then it will be assigned an array of {type:"", value:""} objects that represent the logged in user's claims
    let userClaims;

    const home_button = $("#home_button");
    const movies_button = $("#movies_button");
    const login_button = $("#login_button");
    const login_status = $("#login_status");
    const body_container = $("#body_container");

    home_button.click(showHomePage);
    movies_button.click(showMoviesPage);
    login_button.click(toggleLogin);

    await showLoginStatus();

    async function showLoginStatus() {

        // TODO: make a fetch request to the ~/bff/user management endpoint 
        // to determine if the user is logged in

        // TODO: if the response is 200, then populate the userClaims with the response payload
        // and call showLoggedInUser() to update the login/logout button

        // TODO: if the response is 401, then call showAnonymousUser() to update the login/logout button

        function showLoggedInUser() {
            // TODO: find the "name" claim for the logged in user and set the name variable so it will be displayed
            const name = "";
            login_status.text("Logged in as: " + name);
            login_button.text("Logout");
        }

        function showAnonymousUser() {
            login_status.text("Not logged in");
            login_button.text("Login");
        }
    }

    if (location.hash === "#movies") {
        await showMoviesPage();
    }
    else {
        await showHomePage();
    }

    async function showHomePage() {
        window.location.hash = "";
        body_container.load("_home.html", bindHomePage);

        function bindHomePage() {
            const claims = $("#claims");
            if (userClaims) {
                const dl = claims.find("dl");
                for (var i = 0; i < userClaims.length; i++) {
                    dl.append("<dt>" + userClaims[i].type + "</dt>");
                    dl.append("<dd>" + userClaims[i].value + "</dd>");
                }
                claims.show();
            }
            else {
                claims.hide();
            }
        }
    }

    async function showMoviesPage() {
        window.location.hash = "#movies";

        if (userClaims) {
            body_container.load("_movies.html", bindMoviePage);
        }
        else {
            toggleLogin();
        }

        function bindMoviePage() {
            (async function () {
                $(".pagination").on("click", "a", async function () {
                    await getMovieData($(this).data('page'));
                });

                await getMovieData(1);
            })();
        }

        async function getMovieData(page) {
            // TODO: make a fetch request to /api/movies with a "page" query param

            // TODO: if the response is 200, parse the JSON payload and call bindMovieData passing the data
            // TODO: if the response is 401, show an error


            function bindMovieData(movies) {
                $("#total_count").text(movies.totalCount + " total");
                $("#current_page").text(movies.page);
                $("#total_pages").text(movies.totalPages);

                var count = 7;
                var half = parseInt((count / 2) + 1);
                var start = Math.max(movies.page - (count - half), 1);
                var end = Math.min(Math.max(movies.page - (count - half), 1) + (count - 1), movies.totalPages);
                if (end - start < count) {
                    start = end - count + 1;
                }

                var ul = $(".pagination");
                ul.empty();

                ul.append("<li><a data-page='1'>&laquo;</a></li>");
                for (var i = start; i <= end; i++) {
                    var li = "<li class='" + (i == movies.page ? "active" : "") + "'>";
                    li += ("<a data-page='" + i + "'>" + i + "</a>");
                    li += "</li>";
                    ul.append(li);
                }
                ul.append("<li><a data-page='" + movies.totalPages + "'>&raquo;</a></li>");

                var tbody = $("tbody");
                tbody.html('');

                for (var i = 0; i < movies.count; i++) {
                    var row = "<tr>";
                    row += "<td>" + movies.movies[i].title + "</td>";
                    row += "<td>" + movies.movies[i].year + "</td>";
                    row += "<td>" + movies.movies[i].rating + "</td>";
                    row += "<td>" + movies.movies[i].countryName + "</td>";
                    row += "<td>" + movies.movies[i].directorName + "</td>";
                    row += "<td><img width='50' src=\"/Posters/" + encodeURIComponent(movies.movies[i].posterName) + "\" /></td>";
                    row += "</tr>";

                    tbody.append(row);
                }
            }
        }
    }

    function toggleLogin() {
        // TODO: if the user is not logged in, navigate to the "/bff/login" endpoint
        
        // TODO: if the user is logged in, find the 'bff:logout_url' claim type and navigate to it
    }
})();
