//
// Initialization
//

var lang = "en";
var dataCenter = "Crystal";

var asyncInitCount = 3; // The number of asynchronous initialization functions that need to finish before post-init

var months = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

// Item categories
var itemCategories = [null];
(async function() {
    let dataFile;
    try {
        dataFile = JSON.parse(await request("json/data.json"));
    } catch { // Second failsafe, in case the user connects before the file is downloaded and it doesn't already exist (edge case)
        dataFile = JSON.parse(await request(`https://www.garlandtools.org/db/doc/core/en/3/data.json`)); // All language options are English for this file (for some reason), so it doesn't matter
    }

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
    } catch {
        worldList = JSON.parse(await request("https://xivapi.com/servers/dc"));
    }
    initDone();
})();

// World IDs
var worldMap = new Map();
(async function() {
    let dataFile;
    try {
        dataFile = await request("csv/World.csv");
    } catch {
        dataFile = await request("https://raw.githubusercontent.com/xivapi/ffxiv-datamining/master/csv/World.csv");
    }

    let lines = dataFile.match(/[^\r\n]+/g).slice(3);
    for (let line of lines) {
        line = line.split(",");
        worldMap.set(line[1].replace(/[^a-zA-Z]+/g, ""), line[0]);
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
        searchResults = JSON.parse(await request(`https://xivapi.com/search?string=${query}&string_algo=wildcard_plus&indexes=Item&columns=ID,IconID,ItemSearchCategory.Name_${lang},LevelItem,Name_${lang}&filters=IsUntradable=0`)).Results; // Will throw an error if ES is down
    } catch { // Functional, doesn't filter out MB-restricted items such as raid drops
        // TODO: Notification that ES is down
        searchResults = JSON.parse(await request(`https://www.garlandtools.org/api/search.php?text=${query}&lang=${lang}&type=item`));
        searchResults.map((el) => {
            el.ItemSearchCategory = {};
            el.ItemSearchCategory[`Name_${lang}`] = itemCategories[el.obj.t],
            el.IconID = el.obj.c;
            el.ID = el.obj.i;
            el.LevelItem = el.obj.l;
            el[`Name_${lang}`] = el.obj.n;
        });
    }

    for (var result of searchResults) {
        if (searchBox.value !== query) return;

        // Readable variable names
        category = result.ItemSearchCategory[`Name_${lang}`];
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
    searchResultEntry.setAttribute("class", "infobox search-result");
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

var itemData;
var creditBox = document.getElementById("credits");

window.onhashchange = onHashChange;

/**
 * Fetch market board data from the server.
 */
async function onHashChange() {
    var infoArea = document.getElementById("info");

    var path = window.location.href;
    var id = path.substr(path.lastIndexOf("/") + 1).replace(/[^0-9]/g, "");
    var world = path.substr(path.lastIndexOf("=") + 1);

    // This has to be done backwards, because removing a child alters the array
    // of child nodes, which means some nodes would be skipped going forwards.
    var existing = infoArea.children;
    for (var i = existing.length - 1; i >= 0; i--) {
        var elementToBeDeleted = existing[i];
        if (!elementToBeDeleted.id || parseInt(elementToBeDeleted.id) && parseInt(elementToBeDeleted.id) != id) {
            infoArea.removeChild(elementToBeDeleted);
        }
    }

    if (!itemData || itemData.item.id != id) { // We don't want to re-render this if we don't need to
        infoArea.insertBefore(await onHashChange_fetchItem(id), creditBox);
        infoArea.insertBefore(onHashChange_createWorldNav(id), creditBox);
    }

    // Cheapest listing if cross-world
    if (!world || world === path || world === "Cross-World") {
        world = "Cross-World";
        infoArea.insertBefore(onHashChange_genCheapest(), creditBox);
    }

    // Graph
    var graphContainer = document.createElement("div");
    graphContainer.setAttribute("class", "infobox graph");
    infoArea.insertBefore(graphContainer, creditBox); // Needs to be inserted earlier for width
    onHashChange_drawGraph(graphContainer, world, id);

    // Market info from servers
    infoArea.insertBefore(await onHashChange_genMarketTables(world, id), creditBox);
}

//
// Utility
//

/**
 * Generate a table.
 *
 * @param {Object[]} dataArray - A 2D data array with headers in the first row
 * @param {string} _class - A class to style the table
 * @return {Element} A table.
 */
function makeTable(dataArray, _class) {
    let table = document.createElement("table");
    if (_class) table.setAttribute("class", _class);

    for (let i = 0; i < dataArray.length; i++) {
        let row = table.appendChild(document.createElement("tr"));
        for (let j = 0; j < dataArray[0].length; j++) {
            let cell;
            if (i === 0) {
                cell = row.appendChild(document.createElement("th"));
                cell.innerHTML = dataArray[0][j];
            } else {
                cell = row.appendChild(document.createElement("td"));
                cell.innerHTML = dataArray[i][j];
            }
        }
    }

    return table;
}

/**
 * https://www.kirupa.com/html5/making_http_requests_js.htm
 * Make an HTTP request.
 *
 * @param {string} url - The URL to get.
 * @return {Promise} The contents of the response body.
 */
async function request(url) {
    return new Promise((resolve, reject) => {
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

/**
 * Get market data from the Universalis API.
 *
 * @param {number} worldID - The world to query for.
 * @param {number} itemID - The item to query for.
 * @return {Object} The market data.
 */
async function getMarketData(worldID, itemID) {
    let data = await request(`api/${worldID}/${itemID}`);
    return JSON.parse(data);
}

/**
 * Format a timestamp into a date string.
 *
 * @param {number} timestamp
 * @return {string}
 */
function parseDate(timestamp) {
    const dateObject = new Date(timestamp);
    const result = dateObject.getDate() + " " +
        months[dateObject.getMonth()] + " " +
        dateObject.getHours() + ":" +
        parseMinutes(dateObject.getMinutes());
    return result;
}

/**
 * Ensure minutes have two digits
 *
 * @param {number} minutes
 * @return {string} parsedMinutes
 */
function parseMinutes(minutes) {
    if (minutes < 10) {
        minutes = `0${minutes}`;
    }
    return "" + minutes;
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
