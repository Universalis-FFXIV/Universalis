//
// Initialization
//

var lang = "en";
var dataCenter = "Aether";

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
    var id = path.substr(path.lastIndexOf("/") + 1, path.length).replace(/[^0-9]/g, "");

    // This has to be done backwards, because removing a child alters the array
    // of child nodes, which means some nodes would be skipped going forwards.
    var existing = infoArea.children;
    for (var i = existing.length - 1; i >= 0; i--) {
        var elementToBeDeleted = existing[i];
        if (!elementToBeDeleted.id || parseInt(elementToBeDeleted.id) && parseInt(elementToBeDeleted.id) != id) {
            infoArea.removeChild(elementToBeDeleted);
        }
    }

    var itemInfo = document.createElement("div");
    itemInfo.setAttribute("class", "infobox");
    itemInfo.setAttribute("id", id);

    if (!itemData || itemData.item.id != id) { // We don't want to re-render this if we don't need to
        itemData = JSON.parse(await request(`https://www.garlandtools.org/db/doc/item/${lang}/3/${id}.json`));

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
        worldNav.setAttribute("id", id);

        var nav = document.createElement("table");
        nav.setAttribute("id", "navbar");
        worldNav.appendChild(nav);
        nav = nav.appendChild(document.createElement("tr"));
        for (var world of worldList[dataCenter]) {
            // Table cell
            var w = nav.appendChild(document.createElement("td"));
            if (worldList[dataCenter].indexOf(world) == 0) {
                w.setAttribute("id", "left-cell");
            }

            // Link table cell
            w = w.appendChild(document.createElement("a"));
            w.setAttribute("href", `#/market/${id}?world=${world}`);
            w = w.appendChild(document.createElement("div"));
            w.setAttribute("class", "table-cell-link");
            w = w.appendChild(document.createElement("p"));

            // Text
            w.innerHTML = world;
        }

        infoArea.insertBefore(worldNav, creditBox);
    }

    // TODO: Cheapest

    // Graph
    var graphContainer = document.createElement("div");
    graphContainer.setAttribute("class", "infobox graph");
    infoArea.insertBefore(graphContainer, creditBox); // Needs to be inserted earlier for width

    var graph = graphContainer.appendChild(document.createElement("canvas"));
    graph.setAttribute("id", "echarts");
    graph.setAttribute("height", "300");
    var style = graphContainer.currentStyle || window.getComputedStyle(graphContainer);
    graph.setAttribute("width", graphContainer.clientWidth - parseInt(style.marginRight));
    graph = echarts.init(graph);

    var options = {
        title: {
            text: 'Cross-World Purchase History (500 Sales)',
        },
        tooltip: {
            trigger: 'axis',
        },
        legend: {
            data:['Series 1', 'Series 2'],
        },
        grid: {
            left: '3%',
            right: '4%',
            bottom: '3%',
            containLabel: true,
        },
        toolbox: {
            feature: {
                saveAsImage: {},
            },
        },
        xAxis: {
            type: 'category',
            boundaryGap: false,
            data: ['Day 1','Day 2','Day 3','Day 4','Day 5','Day 6','Day 7'],
        },
        yAxis: {
            type: 'value',
        },
        series: [
            {
                name:'Series 1',
                type:'line',
                data:[120, 132, 101, 134, 90, 230, 210],
            },
            {
                name:'Series 2',
                type:'line',
                data:[220, 182, 191, 234, 290, 330, 310],
            },
        ],
    };

    graph.setOption(options);

    // Market info from servers
    var marketData = document.createElement("div");
    marketData.setAttribute("class", "infobox market-data");
    infoArea.insertBefore(marketData, creditBox);

    // Columns
    var col1 = marketData.appendChild(document.createElement("div"));
    col1.setAttribute("class", "col1");
    var col2 = marketData.appendChild(document.createElement("div"));
    col2.setAttribute("class", "col2");

    // HQ
    col1.appendChild((() => {
        let container = document.createElement("div");
        let label = container.appendChild(document.createElement("h3"));
        label.innerHTML = "HQ Prices";
        container.appendChild(makeTable([
            ["#", "Server", "HQ", "Materia", "Price", "Quantity", "Total", "%Diff", "Retainer", "Creator"],
            ["1", "Adamantoise", "$hq", "", "4,000,000", "1", "4,000,000", "0%", "Sample", "Sample Creator"],
            ["2", "Adamantoise", "$hq", "", "4,000,000", "1", "4,000,000", "0%", "Sample", "Sample Creator"]
        ]));
        return container;
    })());
    col2.appendChild((() => {
        let container = document.createElement("div");
        let label = container.appendChild(document.createElement("h3"));
        label.innerHTML = "HQ Purchase History";
        container.appendChild(makeTable([
            ["#", "Server", "HQ", "Price", "Quantity", "Total", "%Diff", "Buyer", "Date"],
            ["1", "Adamantoise", "$hq", "4,000,000", "1", "4,000,000", "0%", "Sample Buyer", "10 Aug 10:00"],
            ["2", "Adamantoise", "$hq", "4,000,000", "1", "4,000,000", "0%", "Sample Buyer", "10 Aug 10:00"]
        ]));
        return container;
    })());

    // NQ
    col1.appendChild((() => {
        let container = document.createElement("div");
        let label = container.appendChild(document.createElement("h3"));
        label.innerHTML = "NQ Prices";
        container.appendChild(makeTable([
            ["#", "Server", "HQ", "Materia", "Price", "Quantity", "Total", "%Diff", "Retainer", "Creator"],
            ["1", "Adamantoise", "", "", "4,000,000", "1", "4,000,000", "0%", "Sample", "Sample Creator"],
            ["2", "Adamantoise", "", "", "4,000,000", "1", "4,000,000", "0%", "Sample", "Sample Creator"]
        ]));
        return container;
    })());
    col2.appendChild((() => {
        let container = document.createElement("div");
        let label = container.appendChild(document.createElement("h3"));
        label.innerHTML = "NQ Purchase History";
        container.appendChild(makeTable([
            ["#", "Server", "HQ", "Price", "Quantity", "Total", "%Diff", "Buyer", "Date"],
            ["1", "Adamantoise", "", "4,000,000", "1", "4,000,000", "0%", "Sample Buyer", "10 Aug 10:00"],
            ["2", "Adamantoise", "", "4,000,000", "1", "4,000,000", "0%", "Sample Buyer", "10 Aug 10:00"]
        ]));
        return container;
    })());

    // Averages
    col1.appendChild((() => {
        let container = document.createElement("div");

        let col1 = container.appendChild(document.createElement("div"));
        col1.setAttribute("class", "col1");
        let label1 = col1.appendChild(document.createElement("h3"));
        label1.innerHTML = "Average Price Per Unit";

        let subCol1 = col1.appendChild(document.createElement("div"));
        subCol1.setAttribute("class", "col1");
        let label1_1 = subCol1.appendChild(document.createElement("h3"));
        label1_1.setAttribute("class", "col1");
        label1_1.innerHTML = "HQ";
        let label1_2 = subCol1.appendChild(document.createElement("h3"));
        label1_2.setAttribute("class", "col1");
        label1_2.innerHTML = "NQ";


        let col2 = container.appendChild(document.createElement("div"));
        col2.setAttribute("class", "col2");
        let label2 = col2.appendChild(document.createElement("h3"));
        label2.innerHTML = "Average Total Price";

        let subCol2 = col2.appendChild(document.createElement("div"));
        subCol2.setAttribute("class", "col2");
        let label2_1 = subCol2.appendChild(document.createElement("h3"));
        label2_1.setAttribute("class", "col1");
        label2_1.innerHTML = "HQ";
        let label2_2 = subCol2.appendChild(document.createElement("h3"));
        label2_2.setAttribute("class", "col2");
        label2_2.innerHTML = "NQ";

        return container;
    })());
    col2.appendChild((() => {
        let container = document.createElement("div");

        let col1 = container.appendChild(document.createElement("div"));
        col1.setAttribute("class", "col1");
        let label1 = col1.appendChild(document.createElement("h3"));
        label1.innerHTML = "Average Purchased Price Per Unit";

        let subCol1 = col1.appendChild(document.createElement("div"));
        subCol1.setAttribute("class", "col1");
        let label1_1 = subCol1.appendChild(document.createElement("h3"));
        label1_1.setAttribute("class", "col1");
        label1_1.innerHTML = "HQ";
        let label1_2 = subCol1.appendChild(document.createElement("h3"));
        label1_2.setAttribute("class", "col1");
        label1_2.innerHTML = "NQ";


        let col2 = container.appendChild(document.createElement("div"));
        col2.setAttribute("class", "col2");
        let label2 = col2.appendChild(document.createElement("h3"));
        label2.innerHTML = "Average Total Purchased Price";

        let subCol2 = col2.appendChild(document.createElement("div"));
        subCol2.setAttribute("class", "col2");
        let label2_1 = subCol2.appendChild(document.createElement("h3"));
        label2_1.setAttribute("class", "col1");
        label2_1.innerHTML = "HQ";
        let label2_2 = subCol2.appendChild(document.createElement("h3"));
        label2_2.setAttribute("class", "col2");
        label2_2.innerHTML = "NQ";

        return container;
    })());

    /*var marketListings = document.createElement("table");
    marketListings.setAttribute("class", "market-listings");
    marketData.appendChild(marketListings);

    var cheapestListings = marketListings.appendChild(document.createElement("tr"));
    var ref = cheapestListings.appendChild(document.createElement("td"))
    ref.setAttribute("colspan", "2");
    ref.innerHTML = "Cheapest High-Quality";
    ref = cheapestListings.appendChild(document.createElement("td"));
    ref.setAttribute("colspan", "2");
    ref.innerHTML = "Cheapest Normal Quality";

    var allListings = marketListings.appendChild(document.createElement("tr"));
    ref = allListings.appendChild(document.createElement("td"));
    ref.setAttribute("colspan", "2");
    ref.innerHTML = "HQ Prices";
    ref.appendChild(makeTable([
        ["#", "Server", "Materia", "Price", "Quantity", "Total", "%Diff", "Retainer", "Creator"],
        ["1", "Adamantoise", "", "4,000,000", "1", "4,000,000", "0%", "Sample", "Sample Creator"],
        ["2", "Adamantoise", "", "4,000,000", "1", "4,000,000", "0%", "Sample", "Sample Creator"]
    ]));
    ref = allListings.appendChild(document.createElement("td"));
    ref.setAttribute("colspan", "2");
    ref.innerHTML = "HQ Purchase History";

    allListings = marketListings.appendChild(document.createElement("tr"));
    ref = allListings.appendChild(document.createElement("td"));
    ref.setAttribute("colspan", "2");
    ref.innerHTML = "NQ Prices";
    ref = allListings.appendChild(document.createElement("td"));
    ref.setAttribute("colspan", "2");
    ref.innerHTML = "NQ Purchase History";

    allListings = marketListings.appendChild(document.createElement("tr"));
    ref = allListings.appendChild(document.createElement("td"));
    ref.innerHTML = "Average Listed Price Per Unit";
    ref = allListings.appendChild(document.createElement("td"));
    ref.innerHTML = "Average Total Price";
    ref = allListings.appendChild(document.createElement("td"));
    ref.innerHTML = "Average Listed Price Per Unit";
    ref = allListings.appendChild(document.createElement("td"));
    ref.innerHTML = "Average Total Price";

    allListings = marketListings.appendChild(document.createElement("tr"));
    ref = allListings.appendChild(document.createElement("td"));
    ref.innerHTML = "HQ";
    ref = allListings.appendChild(document.createElement("td"));
    ref.innerHTML = "NQ";
    ref = allListings.appendChild(document.createElement("td"));
    ref.innerHTML = "HQ";
    ref = allListings.appendChild(document.createElement("td"));
    ref.innerHTML = "NQ";*/
}

//
// Utility
//

/**
 * Generate a table.
 *
 * @param {Object[][]} dataArray - A 2D data array with headers in the first row
 * @return {Element} A table.
 */
function makeTable(dataArray) {
    let table = document.createElement("table");

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
