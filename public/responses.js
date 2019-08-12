//
// Initialization
//

var lang = "en";
var dataCenter = "Gaia";

var asyncInitCount = 2; // The number of asynchronous initialization functions that need to finish before post-init

// Item categories
var itemCategories = [null];
(async function() {
    var dataFile = JSON.parse(await request(`https://www.garlandtools.org/db/doc/core/${lang}/3/data.json`));

    var categories = dataFile.item.categoryIndex;
    for (var category in categories) {
        if (categories[category].id == -2) continue;
        itemCategories.push(categories[category].name);
    }

    initDone();
})();

// Worlds
var worldList;
(async function() {
    try {
        worldList = JSON.parse(await request("json/dc.json"));
    } catch { // Second failsafe, in case the user connects before the file is downloaded and it doesn't already exist (edge case)
        worldList = JSON.parse(await request("https://xivapi.com/servers/dc"));
    }
    initDone();
})();

//
// Search
//

var searchBox = document.getElementById("search-bar");
searchBox.value = "";

var searchResultArea = document.getElementById("search-results");

searchBox.addEventListener("input", fetchSearchResults);
searchBox.addEventListener("propertychange", fetchSearchResults);

fetchSearchResults();

/**
 * Get the search results from the text entry field.
 * The bottom search result and the one above it get clipped because of my garbage CSS, TODO fix
 */
async function fetchSearchResults() {
    // Clear search results.
    searchResultArea.innerHTML = "";

    // Get new search results and add them to the search result area.
    search(searchBox.value, addSearchResult);
}

/**
 * Pull search info from XIVAPI, or GT as a fallback.
 *
 * @param {string} query - Search query
 * @param {function} callback - A function to be executed on each search result
 */
async function search(query, callback) {
    var searchResults;

    try { // Best, filters out irrelevant items
        searchResults = JSON.parse(await request(`https://xivapi.com/search?string=${query}&string_algo=wildcard_plus&indexes=Item&columns=ID,IconID,ItemSearchCategory.Name,LevelItem,Name_${lang}&filters=IsUntradable=0`)).Results; // Will throw an error if ES is down
    } catch { // Functional, doesn't filter out MB-restricted items such as raid drops
        // TODO: Notification that ES is down
        searchResults = JSON.parse(await request(`https://www.garlandtools.org/api/search.php?text=${query}&lang=${lang}&type=item`));
        searchResults.map(function(el) {
            el.ItemSearchCategory = {
                Name: itemCategories[el.obj.t],
            };
            el.IconID = el.obj.c;
            el.ID = el.obj.i;
            el.LevelItem = el.obj.l;
            el[`Name_${lang}`] = el.obj.n;
        });
    }

    for (var result of searchResults) {
        // Readable variable names
        category = result.ItemSearchCategory.Name;
        icon = `https://www.garlandtools.org/files/icons/item/${result.IconID}.png`;
        id = result.ID;
        ilvl = result.LevelItem;
        name = result[`Name_${lang}`];

        if (category == null) continue; // For garbage like tomestones, that can't be filtered out through the query

        callback(category, icon, id, ilvl, name);
    }
}

/**
 * Append a search result to the page.
 *
 * @param {string} category
 * @param {string} icon
 * @param {string} id
 * @param {string} ilvl
 * @param {string} name
 */
function addSearchResult(category, icon, id, ilvl, name) {
    // Template element
    var clickable = document.createElement("a");
    clickable.setAttribute("href", `/#/market/${id}`);
    var searchResultEntry = document.createElement("div");
    searchResultEntry.setAttribute("class", "infobox search-result-infobox");
    clickable.appendChild(searchResultEntry);

    // Element properties
    var inlineField = document.createElement("p"); // Create inline field
    searchResultEntry.appendChild(inlineField);

    var iconField = document.createElement("img"); // Icon first
    iconField.setAttribute("class", "search-result-icon");
    iconField.setAttribute("src", icon);
    inlineField.appendChild(iconField);

    var nameField = document.createElement("span"); // Name second
    nameField.innerHTML = name;
    inlineField.appendChild(nameField);

    var subtextField = document.createElement("p"); // iLvl/category third, new line
    subtextField.setAttribute("class", "subtext");
    subtextField.innerHTML = `iLvl ${ilvl} ${category}`;
    inlineField.appendChild(subtextField);

    // Add element to DOM
    searchResultArea.appendChild(clickable);
}

//
// Market Data
//

var creditBox = document.getElementById("credits");

window.onhashchange = onHashChange;

/**
 * Fetch market board data from the server.
 */
async function onHashChange() {
    var infoArea = document.getElementById("info");

    // This has to be done backwards, because removing a child alters the array
    // of child nodes, which means some nodes would be skipped going forwards.
    var existing = infoArea.children;
    for (var i = existing.length - 1; i >= 0; i--) {
        var elementToBeDeleted = existing[i];
        if (!elementToBeDeleted.id) {
            infoArea.removeChild(elementToBeDeleted);
        }
    }

    var path = window.location.href;
    var id = path.substr(path.lastIndexOf("/") + 1, path.length);

    var itemInfo = document.createElement("div");
    itemInfo.setAttribute("class", "infobox");

    var itemData = JSON.parse(await request(`https://www.garlandtools.org/db/doc/item/${lang}/3/${id}.json`));

    // Rename/parse data
    var category = itemCategories[itemData.item.category];
    var description = itemData.item.description;
    var equipLevel = itemData.item.elvl;
    var icon = `https://www.garlandtools.org/files/icons/item/${itemData.item.icon}.png`;
    var ilvl = itemData.item.ilvl;
    var jobs = itemData.item.jobCategories;
    var name = itemData.item.name;
    var stackSize = itemData.item.stackSize;

    // Create child elements
    var container = document.createElement("div");
    container.setAttribute("class", "info-header");
    itemInfo.appendChild(container);

    var itemThumbnail = document.createElement("img"); // Image
    itemThumbnail.setAttribute("class", "info-thumbnail");
    itemThumbnail.setAttribute("src", icon);
    container.appendChild(itemThumbnail);

    var basicInfo = document.createElement("h1"); // Name
    basicInfo.innerHTML = name;
    container.appendChild(basicInfo);

    var subInfo = document.createElement("h3"); // Extra data
    subInfo.innerHTML = `iLvl ${ilvl} ${category} - Stack: ${stackSize}${equipLevel ? ` - Level ${equipLevel} ${jobs}` : ""}`;
    container.appendChild(subInfo);

    itemInfo.appendChild(document.createElement("br"));

    if (description) { // Description
        var itemDescription = document.createElement("span");
        itemDescription.innerHTML = description;
        container.appendChild(itemDescription);
    }

    infoArea.insertBefore(itemInfo, creditBox);

    // World navbar
    var worldNav = document.createElement("div");
    worldNav.setAttribute("class", "infobox nav");

    var nav = document.createElement("table");
    nav.setAttribute("id", "navbar");
    worldNav.appendChild(nav);
    nav = nav.appendChild(document.createElement("tr"));
    for (var world of worldList[dataCenter]) {
        var w = nav.appendChild(document.createElement("td"));
        w.innerHTML = world;
    }

    infoArea.insertBefore(worldNav, creditBox);

    // Graph
    var graph = document.createElement("canvas");
    graph.setAttribute("class", "infobox graph");
    var graphCTX = graph.getContext("2d");

    new Chart(graphCTX, {
        type: 'bar',
        data: {
            labels: ['Red', 'Blue', 'Yellow', 'Green', 'Purple', 'Orange'],
            datasets: [{
                label: '# of Votes',
                data: [12, 19, 3, 5, 2, 3],
                backgroundColor: [
                    'rgba(255, 99, 132, 0.2)',
                    'rgba(54, 162, 235, 0.2)',
                    'rgba(255, 206, 86, 0.2)',
                    'rgba(75, 192, 192, 0.2)',
                    'rgba(153, 102, 255, 0.2)',
                    'rgba(255, 159, 64, 0.2)',
                ],
                borderColor: [
                    'rgba(255, 99, 132, 1)',
                    'rgba(54, 162, 235, 1)',
                    'rgba(255, 206, 86, 1)',
                    'rgba(75, 192, 192, 1)',
                    'rgba(153, 102, 255, 1)',
                    'rgba(255, 159, 64, 1)',
                ],
                borderWidth: 1,
            }],
        },
        options: {
            scales: {
                yAxes: [{
                    ticks: {
                        beginAtZero: true,
                    },
                }],
            },
        },
    });

    infoArea.insertBefore(graph, creditBox);

    // Market info from servers
    var marketData = document.createElement("div");
    marketData.setAttribute("class", "infobox market-data");

    var cheapestListings = document.createElement("div");
    cheapestListings.innerHTML = "Cheapest High-Quality, Cheapest Normal-Quality";
    marketData.appendChild(cheapestListings);

    var graph = document.createElement("div");
    graph.innerHTML = "Cross-world purchase history";
    marketData.appendChild(graph);

    var allListings = document.createElement("table");
    allListings.innerHTML = "HQ Prices/HQ Purchase History, NQ Prices/NQ Purchase History, Avg Listed Price Per Unit HQ/NQ -> Avg Listed Total Price HQ/NQ <-> Avg Sale Price Per Unit HQ/NQ -> Avg Sale Total Price HQ/NQ";
    marketData.appendChild(allListings);

    infoArea.insertBefore(marketData, creditBox);
}

//
// Utility
//

/**
 * https://www.kirupa.com/html5/making_http_requests_js.htm
 * Make an HTTP request.
 *
 * @param {string} url - The URL to get.
 * @return {Promise} The contents of the response body.
 */
async function request(url) {
    return new Promise(function(resolve, reject) {
        var xhr = new XMLHttpRequest();
        xhr.open("GET", url, true);
        xhr.send();

        /**
         * Event handler for GET completion
         */
        function processResponse() {
            if (xhr.readyState == 4) {
                resolve(xhr.responseText);
            }
        }

        xhr.addEventListener("readystatechange", processResponse, false);
    });
}

//
// Post-Initialization
//

/**
 * This makes the webpage respond correctly if it is initialized with a hash name such as /#/market/2234
 */
function initDone() {
    asyncInitCount--;
    if (asyncInitCount == 0 && window.location.href.indexOf("#") != -1) onHashChange();
}
