//
// Initialization
//

var lang = "en";

var asyncInitCount = 2; // The number of asynchronous initialization functions that need to finish before post-init

// Item categories
var itemCategories = [ null ];
request(`https://www.garlandtools.org/db/doc/core/${lang}/3/data.json`, function(dataFile) {
    dataFile = JSON.parse(dataFile);

    var categories = dataFile.item.categoryIndex;
    for (var category in categories) {
        if (categories[category].id == -2) continue;
        itemCategories.push(categories[category].name);
    }

    initDone();
});

// Worlds
var worldList = {};
request(`json/dc.json`, function(servers) {
    try {
        worldList = JSON.parse(servers);

        initDone();
    } catch { // Second failsafe, in case the user connects before the file is downloaded and it doesn't already exist (edge case)
        request("https://xivapi.com/servers/dc", function(servers) {
            worldList = JSON.parse(servers);

            initDone();
        });
    }
});

//
// Search
//

var searchBox = document.getElementById("search-bar");
searchBox.value = "";

var searchResultArea = document.getElementById("search-results");

searchBox.addEventListener("input", fetchSearchResults);
searchBox.addEventListener("propertychange", fetchSearchResults);

fetchSearchResults();

// The bottom search result and the one above it get clipped because of my garbage CSS, TODO fix
async function fetchSearchResults() {
    // Clear search results.
    searchResultArea.innerHTML = "";

    // Get new search results.
    request(`https://www.garlandtools.org/api/search.php?text=${searchBox.value}&lang=${lang}&type=item&tradeable=false`, async function(searchResults) {
        searchResults = JSON.parse(searchResults);

        for (var result of searchResults) {
            // Readable variable names
            var category = itemCategories[result.obj.t];
            var icon = `https://www.garlandtools.org/files/icons/item/${result.obj.c}.png`;
            var id = result.obj.i;
            var ilvl = result.obj.l;
            var name = result.obj.n;

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
            iconField.setAttribute("height", "14%");
            iconField.setAttribute("width", "14%");
            inlineField.appendChild(iconField);

            var nameField = document.createElement("span"); // Name second
            nameField.innerHTML = name;
            inlineField.appendChild(nameField);

            var subtextField = document.createElement("p");  // iLvl/category third, new line
            subtextField.setAttribute("class", "subtext");
            subtextField.innerHTML = `iLvl ${ilvl} ${category}`;
            inlineField.appendChild(subtextField);

            // Add element to DOM
            searchResultArea.appendChild(clickable);
        }
    });
}

//
// Market Data
//

var creditBox = document.getElementById("credits");

window.onhashchange = onHashChange;

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

    request(`https://www.garlandtools.org/db/doc/item/${lang}/3/${id}.json`, async function(itemData) {
        itemData = JSON.parse(itemData);

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
        var itemThumbnail = document.createElement("img"); // Image
        itemThumbnail.innerHTML = itemData.item.description;
        itemThumbnail.setAttribute("src", icon);
        itemThumbnail.setAttribute("height", "10%");
        itemThumbnail.setAttribute("width", "10%");
        itemInfo.appendChild(itemThumbnail);

        itemInfo.appendChild(document.createElement("br"));

        if (description) {
            var itemDescription = document.createElement("span");
            itemDescription.innerHTML = description;
            itemInfo.appendChild(itemDescription);
        }
    });

    infoArea.insertBefore(itemInfo, creditBox);

    var worldNav = document.createElement("div");
    worldNav.setAttribute("class", "infobox navbar");

    // World navbar
    worldNav.innerHTML = worldList.Crystal.join();

    infoArea.insertBefore(worldNav, creditBox);

    // Market info from server
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

// https://www.kirupa.com/html5/making_http_requests_js.htm
// More or less copied from here.
async function request(url, callback) {
    var xhr = new XMLHttpRequest();
    xhr.open("GET", url, true);
    xhr.send();

    async function processResponse(e) {
        if (xhr.readyState == 4) {
            callback(xhr.responseText);
        }
    }

    xhr.addEventListener("readystatechange", processResponse, false);
}

//
// Post-Initialization
//

// This makes the webpage respond correctly if it is initialized with a hash name.
function initDone() {
    asyncInitCount--;
    if (asyncInitCount == 0 && window.location.href.indexOf("#") != -1) onHashChange();
}
