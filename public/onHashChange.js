/**
 * Creates the per-data center navigation bar.
 *
 * @param {number} id - The item ID.
 * @return {Element} An element of class infobox.
 */
function onHashChange_createWorldNav(id) {
    var worldNav = document.createElement("div");
    worldNav.setAttribute("class", "infobox nav");
    worldNav.setAttribute("id", id);

    var nav = document.createElement("table");
    nav.setAttribute("id", "navbar");
    worldNav.appendChild(nav);
    nav = nav.appendChild(document.createElement("tr"));
    for (var world of worldList[dataCenter]) { // Will exist at execution time
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

    return worldNav;
}

/**
 * Creates the history graph.
 *
 * @param {Element} graphContainer - A container to put the graph in.
 */
function onHashChange_drawGraph(graphContainer) {
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
}


/**
 * Generate tables with market board data.
 *
 * @return {Element} An element of class infobox.
 */
function onHashChange_genMarketTables() {
    var marketData = document.createElement("div");
    marketData.setAttribute("class", "infobox market-data");

    // Columns
    var col1 = marketData.appendChild(document.createElement("div"));
    col1.setAttribute("class", "col1");
    var col2 = marketData.appendChild(document.createElement("div"));
    col2.setAttribute("class", "col2");

    // HQ
    col1.appendChild((() => {
        let table = [
            ["#", "Server", "HQ", "Materia", "Price", "Quantity", "Total", "%Diff", "Retainer", "Creator"],
            ["1", "Adamantoise", "$hq", "", "4,000,000", "1", "4,000,000", "0%", "Sample", "Sample Creator"],
            ["2", "Adamantoise", "$hq", "", "4,000,000", "1", "4,000,000", "0%", "Sample", "Sample Creator"]
        ];
        return onHashChange_genMarketTables_helper0(table, "HQ Prices");
    })());
    col2.appendChild((() => {
        let table = [
            ["#", "Server", "HQ", "Price", "Quantity", "Total", "%Diff", "Buyer", "Date"],
            ["1", "Adamantoise", "$hq", "4,000,000", "1", "4,000,000", "0%", "Sample Buyer", "10 Aug 10:00"],
            ["2", "Adamantoise", "$hq", "4,000,000", "1", "4,000,000", "0%", "Sample Buyer", "10 Aug 10:00"]
        ];
        return onHashChange_genMarketTables_helper0(table, "HQ Purchase History");
    })());

    // NQ
    col1.appendChild((() => {
        let table = [
            ["#", "Server", "HQ", "Materia", "Price", "Quantity", "Total", "%Diff", "Retainer", "Creator"],
            ["1", "Adamantoise", "", "", "4,000,000", "1", "4,000,000", "0%", "Sample", "Sample Creator"],
            ["2", "Adamantoise", "", "", "4,000,000", "1", "4,000,000", "0%", "Sample", "Sample Creator"]
        ];
        return onHashChange_genMarketTables_helper0(table, "NQ Prices");
    })());
    col2.appendChild((() => {
        let table = [
            ["#", "Server", "HQ", "Price", "Quantity", "Total", "%Diff", "Buyer", "Date"],
            ["1", "Adamantoise", "", "4,000,000", "1", "4,000,000", "0%", "Sample Buyer", "10 Aug 10:00"],
            ["2", "Adamantoise", "", "4,000,000", "1", "4,000,000", "0%", "Sample Buyer", "10 Aug 10:00"]
        ];
        return onHashChange_genMarketTables_helper0(table, "NQ Purchase History");
    })());

    // Averages
    col1.appendChild((() => {
        return onHashChange_genMarketTables_helper1("Average Price Per Unit", "Average Total Price");
    })());
    col2.appendChild((() => {
        return onHashChange_genMarketTables_helper1("Average Purchased Price Per Unit", "Average Total Purchased Price")
    })());

    return marketData;
}

/**
 * Actually generate the table, and replace $hq with a HQ icon.
 *
 * @return {Element} An element containing a table.
 */
function onHashChange_genMarketTables_helper0(table, header) {
    let container = document.createElement("div");
    let label = container.appendChild(document.createElement("h3"));
    label.innerHTML = header;

    let tableElement = makeTable(table);
    container.appendChild(tableElement);

    for (let i = 0; i < table.length; i++) {
        let cell = tableElement.children[i].children[2];
        if (cell.innerHTML === "$hq") {
            let img = document.createElement("img");
            img.setAttribute("src", "img/hq.png");
            img.setAttribute("style", `
                height: 16px;
                width: 16px
            `);
            cell.innerHTML = "";
            cell.appendChild(img);
        }
    }
    return container;
}

/**
 * Generate the "average purchase" sections.
 *
 * @param {string} header1
 * @param {string} header2
 * @return {Element} An element containing the average prices.
 */
function onHashChange_genMarketTables_helper1(header1, header2) {
    let container = document.createElement("div");

    let col1 = container.appendChild(document.createElement("div"));
    col1.setAttribute("class", "col1");
    let label1 = col1.appendChild(document.createElement("h3"));
    label1.innerHTML = header1;

    let label1_1 = col1.appendChild(document.createElement("h3"));
    label1_1.setAttribute("class", "col1");
    label1_1.innerHTML = "HQ";
    let label1_2 = col1.appendChild(document.createElement("h3"));
    label1_2.setAttribute("class", "col1");
    label1_2.innerHTML = "NQ";


    let col2 = container.appendChild(document.createElement("div"));
    col2.setAttribute("class", "col2");
    let label2 = col2.appendChild(document.createElement("h3"));
    label2.innerHTML = header2;

    let label2_1 = col2.appendChild(document.createElement("h3"));
    label2_1.setAttribute("class", "col1");
    label2_1.innerHTML = "HQ";
    let label2_2 = col2.appendChild(document.createElement("h3"));
    label2_2.setAttribute("class", "col2");
    label2_2.innerHTML = "NQ";

    return container;
}

/**
 * Gets item data from Garlandtools.
 *
 * @param {number} id - The item ID.
 * @return {Element} An element of class infobox.
 */
async function onHashChange_fetchItem(id) {
    var itemInfo = document.createElement("div");
    itemInfo.setAttribute("class", "infobox");
    itemInfo.setAttribute("id", id);

    itemData = JSON.parse(await request(`https://www.garlandtools.org/db/doc/item/${lang}/3/${id}.json`));

    // Rename/parse data
    var category = itemCategories[itemData.item.category]; // itemCategories will exist at execution time.
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

    return itemInfo;
}
