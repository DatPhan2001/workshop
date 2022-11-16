/// <reference path="../lib/jquery/dist/jquery.js" />

(async function () {
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

        function showLoggedInUser() {
            const name = userClaims.find(claim => claim.type === 'name').value;
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
        if (userClaims) {
            const logoutUrlClaim = userClaims.find(claim => claim.type === 'bff:logout_url');
            window.location = logoutUrlClaim.value;
        }
        else {
            window.location = "/bff/login";
        }
    }
})();
