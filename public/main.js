//
// Initialization
//

// Item categories
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
(async function() {
    try {
        worldList = JSON.parse(await request("json/dc.json"));
    } catch {
        worldList = JSON.parse(await request("https://xivapi.com/servers/dc"));
    }
    initDone();
})();

// World IDs
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
// Settings bar
//

var settingsBar = document.getElementById("settings-bar");

/**
 * Fill the settings bar. This is contingent on dc.json, so it's called after initialization.
 */
function populateSettings() {
    let dataCenterDropdown = settingsBar.appendChild(createElement("select"));
    let dcArray = [];
    dataCenterDropdown.id = "data-center-dropdown";
    
    for (let dc in worldList) {
        if (worldList.hasOwnProperty(dc)) {
            dcArray.push(dc);
            dataCenterDropdown.appendChild((() => {
                return createElement("option", {
                    "value": dc,
                }, dc);
            })());
        }
    }

    if (dataCenter) dataCenterDropdown.selectedIndex = dcArray.indexOf(dataCenter);
    settingsBar.addEventListener("change", assignDataCenter);
}

/**
 * Assign the selected dropdown value to dataCenter.
 */
function assignDataCenter() {
    const newDataCenter = document.getElementById("data-center-dropdown").value;

    dataCenter = newDataCenter;
    document.cookie = `dataCenter=${dataCenter}`;
    if (location.hash) location.hash = location.hash.substr(0, location.hash.indexOf("=") + 1) + newDataCenter;
}

/**
 * Move other elements below the settings bar so they don't clip.
 *
 * @param {string} className The name of the class used to ID elements to shift down.
 */
function moveBelowSettings(className) {
    const elementsToMove = document.getElementsByClassName(className);
    const distanceToMove = settingsBar.clientHeight;
    for (let element of elementsToMove) {
        element.style.paddingTop = distanceToMove + "px";
    }
}

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
        searchResults = JSON.parse(await request(`https://xivapi.com/search?string=${query}&string_algo=wildcard_plus&indexes=item&filters=ItemSearchCategory.ID%3E8&columns=ID,IconID,ItemSearchCategory.Name_${lang},LevelItem,Name_${lang}`)).Results; // Will throw an error if ES is down
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
    let clickable = createElement("a", {
        "href": `/#/market/${id}?world=Cross-World`,
    });

    let inlineField = clickable.appendChild(createElement("div", {
        "class": "infobox search-result",
    }, createElement("p").outerHTML));

    inlineField.appendChild(createElement("img", {
        "class": "search-result-icon",
        "src": icon,
    }));

    inlineField.appendChild(createElement("span", null, name));

    inlineField.appendChild(createElement("p", {
        "class": "subtext",
    }, `iLvl ${ilvl} ${category}`));

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
    itemID = path.substr(path.lastIndexOf("/") + 1).replace(/[^0-9]/g, "");
    world = path.substr(path.lastIndexOf("=") + 1);
    if (world === "Cross-World") {
        world = dataCenter;
    }

    // This has to be done backwards, because removing a child alters the array
    // of child nodes, which means some nodes would be skipped going forwards.
    var existing = infoArea.children;
    for (var i = existing.length - 1; i >= 0; i--) {
        var elementToBeDeleted = existing[i];
        if (!elementToBeDeleted.id || parseInt(elementToBeDeleted.id) && parseInt(elementToBeDeleted.id) != itemID) {
            infoArea.removeChild(elementToBeDeleted);
        }
    }

    if (!itemData || itemData.item.id != itemID) { // We don't want to re-render this if we don't need to
        infoArea.insertBefore(await onHashChange_fetchItem(), creditBox);
    }

    infoArea.insertBefore(onHashChange_createWorldNav(), creditBox);

    // Cheapest listing if cross-world
    if (!world || world === path || world === "Cross-World" || worldList[world]) {
        world = "Cross-World";
        infoArea.insertBefore(await onHashChange_genCheapest(), creditBox);
    }

    // Graph
    var graphContainer = createElement("div");
    graphContainer.setAttribute("class", "infobox graph");
    infoArea.insertBefore(graphContainer, creditBox); // Needs to be inserted earlier for width
    onHashChange_drawGraph(graphContainer);

    // Market info from servers
    infoArea.insertBefore(await onHashChange_genMarketTables(), creditBox);
}

//
// Post-Initialization
//

/**
 * Actual main method.
 */
function initDone() {
    asyncInitCount--;

    if (asyncInitCount == 0) {
        dataCenter = (() => {
            let path = window.location.href;
            world = path.substr(path.lastIndexOf("=") + 1);
            if (!worldList[world]) return dataCenter;
            return world;
        })();

        if (window.location.href.indexOf("#") != -1) {
            // Calling onHashChange manually here makes the webpage respond correctly
            // if it is initialized with a hash name, such as /#/market/2234
            onHashChange();
        }

        populateSettings();
        moveBelowSettings("under-settings");
    }
}
