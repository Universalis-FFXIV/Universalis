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

            // Text
            w.innerHTML = world;
        }

        infoArea.insertBefore(worldNav, creditBox);
    }

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
            text: '折线图堆叠'
        },
        tooltip: {
            trigger: 'axis'
        },
        legend: {
            data:['邮件营销','联盟广告','视频广告','直接访问','搜索引擎']
        },
        grid: {
            left: '3%',
            right: '4%',
            bottom: '3%',
            containLabel: true
        },
        toolbox: {
            feature: {
                saveAsImage: {}
            }
        },
        xAxis: {
            type: 'category',
            boundaryGap: false,
            data: ['周一','周二','周三','周四','周五','周六','周日']
        },
        yAxis: {
            type: 'value'
        },
        series: [
            {
                name:'邮件营销',
                type:'line',
                stack: '总量',
                data:[120, 132, 101, 134, 90, 230, 210]
            },
            {
                name:'联盟广告',
                type:'line',
                stack: '总量',
                data:[220, 182, 191, 234, 290, 330, 310]
            },
            {
                name:'视频广告',
                type:'line',
                stack: '总量',
                data:[150, 232, 201, 154, 190, 330, 410]
            },
            {
                name:'直接访问',
                type:'line',
                stack: '总量',
                data:[320, 332, 301, 334, 390, 330, 320]
            },
            {
                name:'搜索引擎',
                type:'line',
                stack: '总量',
                data:[820, 932, 901, 934, 1290, 1330, 1320]
            }
        ],
    };

    graph.setOption(options);

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
