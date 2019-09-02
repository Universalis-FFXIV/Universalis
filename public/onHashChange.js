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

    var trimmedWorldList = ["Cross-World"].concat(worldList[dataCenter]);

    for (var world of trimmedWorldList) { // Will exist at execution time
        // Table cell
        var w = nav.appendChild(document.createElement("td"));
        if (trimmedWorldList.indexOf(world) == 0) {
            w.setAttribute("class", "left-cell");
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
 * @param {string} world - The world to get data for.
 * @param {number} itemID - The item to get data for.
 */
async function onHashChange_drawGraph(graphContainer, world, itemID) {
    const worldID = worldMap.get(world);

    const historyData = JSON.parse(await request(`api/history/${worldID}/${itemID}`));

    const highQualitySales = historyData.entries.filter((entry) => entry.hq === 1);
    const normalQualitySales = historyData.entries.filter((entry) => entry.hq === 0);

    let xAxisLabels = [];
    let yAxisDataHQ = [];
    let yAxisDataNQ = [];

    const interval = (historyData.entries[0].timestamp - historyData.entries[historyData.entries.length - 1].timestamp)
        / historyData.entries.length;

    for (let i = 0; i < historyData.entries.length; i++) {
        const lastTimestamp = historyData.entries[0].timestamp + (i - 1) * interval;
        const timestamp = historyData.entries[0].timestamp + i * interval;

        xAxisLabels.push(parseDate(timestamp));
        yAxisDataHQ.push(average(...highQualitySales
            .filter((sale) => sale.timestamp <= timestamp && sale.timestamp > lastTimestamp)
            .map((sale) => { sale = sale.pricePerUnit; return sale; })
        ));
        yAxisDataNQ.push(average(...normalQualitySales
            .filter((sale) => sale.timestamp <= timestamp && sale.timestamp > lastTimestamp)
            .map((sale) => { sale = sale.pricePerUnit; return sale; })
        ));
    }

    console.log(xAxisLabels);
    console.log(yAxisDataHQ);
    console.log(yAxisDataNQ);

    var graph = graphContainer.appendChild(document.createElement("canvas"));
    graph.setAttribute("id", "echarts");
    graph.setAttribute("height", "300");
    var style = graphContainer.currentStyle || window.getComputedStyle(graphContainer);
    graph.setAttribute("width", graphContainer.clientWidth - parseInt(style.marginRight));
    graph = echarts.init(graph);

    var options = {
        title: {
            text: `${world} Purchase History (500 Sales)`,
        },
        tooltip: {
            trigger: 'axis',
        },
        legend: {
            data: ['High-Quality Price Per Unit', 'Normal Quality Price Per Unit'],
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
            data: xAxisLabels,
        },
        yAxis: {
            type: 'value',
        },
        series: [
            {
                name: 'High-Quality Price Per Unit',
                type: 'line',
                data: yAxisDataHQ,
            },
            {
                name: 'Normal Quality Price Per Unit',
                type: 'line',
                data: yAxisDataNQ,
            },
        ],
    };

    graph.setOption(options);
}

/**
 * Generate infobox with cheapest listings NQ/HQ.
 *
 * @return {Element} An element of class infobox.
 */
function onHashChange_genCheapest() {
    var cheapestListings = document.createElement("div");
    cheapestListings.setAttribute("class", "infobox cheapest-listings");

    let highQuality = cheapestListings.appendChild(document.createElement("div"));
    highQuality.setAttribute("class", "col1");
    let bigTextHQ = highQuality.appendChild(document.createElement("p"));
    bigTextHQ.setAttribute("class", "col1 gen-cheapest");
    bigTextHQ.innerHTML = "Cheapest High-Quality";
    let priceHQ = bigTextHQ.appendChild(document.createElement("h1"));
    priceHQ.innerHTML = "1 x 4,000,000";

    let serverLabelHQ = highQuality.appendChild(document.createElement("p"));
    serverLabelHQ.setAttribute("class", "col2 gen-cheapest");
    serverLabelHQ.innerHTML = " Server: <b>Adamantoise</b> - Total: <b>4,000,000</b>";

    let normalQuality = cheapestListings.appendChild(document.createElement("div"));
    normalQuality.setAttribute("class", "col2");
    let bigTestNQ = normalQuality.appendChild(document.createElement("p"));
    bigTestNQ.setAttribute("class", "col1 gen-cheapest");
    bigTestNQ.innerHTML = "Cheapest Normal Quality";
    let priceNQ = bigTestNQ.appendChild(document.createElement("h1"));
    priceNQ.innerHTML = "1 x 4,000,000";

    let serverLabelNQ = normalQuality.appendChild(document.createElement("p"));
    serverLabelNQ.setAttribute("class", "col2 gen-cheapest");
    serverLabelNQ.innerHTML = " Server: <b>Adamantoise</b> - Total: <b>4,000,000</b>";

    return cheapestListings;
}


/**
 * Generate tables with market board data.
 *
 * @param {string} world - The world to get data for.
 * @param {number} itemID
 * @return {Element} An element of class infobox.
 */
async function onHashChange_genMarketTables(world, itemID) {
    let worldID = worldMap.get(world);
    let marketBoardData;
    try {
        marketBoardData = await getMarketData(worldID, itemID);
    } catch {
        let noDataInfobox = document.createElement("div");
        noDataInfobox.setAttribute("class", "infobox");
        let ref = noDataInfobox.appendChild(document.createElement("h3"));
        ref.setAttribute("class", "centered-text");
        ref.innerText = "No market data has been uploaded for this item.";
        return noDataInfobox;
    }

    var marketData = document.createElement("div");
    marketData.setAttribute("class", "infobox market-data");

    // Columns
    var col1 = marketData.appendChild(document.createElement("div"));
    col1.setAttribute("class", "col1");
    var col2 = marketData.appendChild(document.createElement("div"));
    col2.setAttribute("class", "col2");

    // Averages
    let averagePricePerUnitHQ;
    let averageTotalPriceHQ;
    let averagePurchasedPricePerUnitHQ;
    let averageTotalPurchasedPriceHQ;
    let averagePricePerUnitNQ;
    let averageTotalPriceNQ;
    let averagePurchasedPricePerUnitNQ;
    let averageTotalPurchasedPriceNQ;

    try {
        averagePricePerUnitHQ = average(
            ...marketBoardData.listings.map((listing) => {
                if (listing.hq === 0) return false;
                return listing.pricePerUnit;
            })
        );
        averageTotalPriceHQ = average(
            ...marketBoardData.listings.map((listing) => {
                if (listing.hq === 0) return false;
                return listing.total;
            })
        );
        averagePricePerUnitNQ = average(
            ...marketBoardData.listings.map((listing) => {
                if (listing.hq === 1) return false;
                return listing.pricePerUnit;
            })
        );
        averageTotalPriceNQ = average(
            ...marketBoardData.listings.map((listing) => {
                if (listing.hq === 1) return false;
                return listing.total;
            })
        );
    } catch {
        averagePricePerUnitHQ = 0;
        averageTotalPriceHQ = 0;
        averagePricePerUnitNQ = 0;
        averageTotalPriceNQ = 0;
    }

    try {
        averagePurchasedPricePerUnitHQ = average(
            ...marketBoardData.recentHistory.map((entry) => {
                if (entry.hq === 0) return false;
                return entry.pricePerUnit;
            })
        );
        averageTotalPurchasedPriceHQ = average(
            ...marketBoardData.recentHistory.map((entry) => {
                if (entry.hq === 0) return false;
                return entry.total;
            })
        );
        averagePurchasedPricePerUnitNQ = average(
            ...marketBoardData.recentHistory.map((entry) => {
                if (entry.hq === 1) return false;
                return entry.pricePerUnit;
            })
        );
        averageTotalPurchasedPriceNQ = average(
            ...marketBoardData.recentHistory.map((entry) => {
                if (entry.hq === 1) return false;
                return entry.total;
            })
        );
    } catch {
        averagePurchasedPricePerUnitHQ = 0;
        averageTotalPurchasedPriceHQ = 0;
        averagePurchasedPricePerUnitNQ = 0;
        averageTotalPurchasedPriceNQ = 0;
    }

    // HQ

    col1.appendChild((() => {
        let table = [
            ["#", "Server", "HQ", "Materia", "Price", "Quantity", "Total", "%Diff", "Retainer", "Creator"],
        ];

        try {
            for (let i = 0; i < marketBoardData.listings.length; i++) {
                let listing = marketBoardData.listings[i];

                if (listing.hq === 0) continue;

                onHashChange_genMarketTables_helper2(table, i, listing, averagePricePerUnitHQ, world);
            }
        } catch {}

        let header = document.createElement("div");
        let img = header.appendChild(document.createElement("img"));
        img.setAttribute("src", "img/hq.png");
        img.setAttribute("class", "prefix-hq-icon");
        let label = header.appendChild(document.createElement("h3"));
        label.innerHTML = "HQ Prices";
        return onHashChange_genMarketTables_helper0(table, header);
    })());
    col2.appendChild((() => {
        let table = [
            ["#", "Server", "HQ", "Price", "Quantity", "Total", "%Diff", "Buyer", "Date"],
        ];

        try {
            for (let i = 0; i < marketBoardData.recentHistory.length; i++) {
                let entry = marketBoardData.recentHistory[i];

                if (entry.hq === 0) continue;

                onHashChange_genMarketTables_helper3(table, i, entry, averagePricePerUnitHQ, world);
            }
        } catch {}

        let header = document.createElement("div");
        let img = header.appendChild(document.createElement("img"));
        img.setAttribute("src", "img/hq.png");
        img.setAttribute("class", "prefix-hq-icon");
        let label = header.appendChild(document.createElement("h3"));
        label.innerHTML = "HQ Purchase History";
        return onHashChange_genMarketTables_helper0(table, header);
    })());

    // NQ

    col1.appendChild((() => {
        let table = [
            ["#", "Server", "HQ", "Materia", "Price", "Quantity", "Total", "%Diff", "Retainer", "Creator"],
        ];

        try {
            for (let i = 0; i < marketBoardData.listings.length; i++) {
                let listing = marketBoardData.listings[i];

                if (listing.hq === 1) continue;

                onHashChange_genMarketTables_helper2(table, i, listing, averagePricePerUnitNQ, world);
            }
        } catch {}

        let header = document.createElement("h3");
        header.innerHTML = "NQ Prices";
        return onHashChange_genMarketTables_helper0(table, header);
    })());
    col2.appendChild((() => {
        let table = [
            ["#", "Server", "HQ", "Price", "Quantity", "Total", "%Diff", "Buyer", "Date"],
        ];

        try {
            for (let i = 0; i < marketBoardData.recentHistory.length; i++) {
                let entry = marketBoardData.recentHistory[i];

                if (entry.hq === 1) continue;

                onHashChange_genMarketTables_helper3(table, i, entry, averagePricePerUnitHQ, world);
            }
        } catch {}

        let header = document.createElement("h3");
        header.innerHTML = "NQ Purchase History";
        return onHashChange_genMarketTables_helper0(table, header);
    })());

    // Averages
    col1.appendChild((() => {
        return onHashChange_genMarketTables_helper1(
            "Average Price Per Unit",
            Math.floor(averagePricePerUnitHQ),
            Math.floor(averagePricePerUnitNQ),
            "Average Total Price",
            Math.floor(averageTotalPriceHQ),
            Math.floor(averagePricePerUnitNQ)
        );
    })());
    col2.appendChild((() => {
        return onHashChange_genMarketTables_helper1(
            "Average Purchased Price Per Unit",
            Math.floor(averagePurchasedPricePerUnitHQ),
            Math.floor(averagePurchasedPricePerUnitNQ),
            "Average Total Purchased Price",
            Math.floor(averageTotalPurchasedPriceHQ),
            Math.floor(averageTotalPurchasedPriceNQ)
        );
    })());

    return marketData;
}

/**
 * Actually generate the table, and replace $hq with a HQ icon.
 *
 * @param {Object[]} table - A 2D array of objects.
 * @param {Element} header - An element to use as the table title.
 * @return {Element} An element containing a table.
 */
function onHashChange_genMarketTables_helper0(table, header) {
    let container = document.createElement("div");
    let label = container.appendChild(header);

    let tableElement = makeTable(table, "market-data-table");
    container.appendChild(tableElement);

    for (let i = 0; i < table.length; i++) {
        let cell = tableElement.children[i].children[2];
        if (cell.innerHTML === "$hq") {
            let img = document.createElement("img");
            img.setAttribute("src", "img/hq.png");
            img.setAttribute("style", `
                height: 16px;
                width: 16px;
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
 * @param {number} ppuHQ
 * @param {number} ppuNQ
 * @param {string} header2
 * @param {number} tpHQ
 * @param {number} tpNQ
 * @return {Element} An element containing the average prices.
 */
function onHashChange_genMarketTables_helper1(header1, ppuHQ, ppuNQ, header2, tpHQ, tpNQ) {
    if (ppuHQ === 0) ppuHQ = "N/A";
    if (ppuNQ === 0) ppuNQ = "N/A";
    if (tpHQ === 0) tpHQ = "N/A";
    if (tpNQ === 0) tpNQ = "N/A";

    let container = document.createElement("div");
    container.setAttribute("style", "text-align: center;");

    let col1 = container.appendChild(document.createElement("div"));
    col1.setAttribute("class", "col1");
    let label1 = col1.appendChild(document.createElement("h4"));
    label1.innerHTML = header1;

    let label1_1 = col1.appendChild(document.createElement("h4"));
    label1_1.setAttribute("class", "col1");
    let img1_1 = label1_1.appendChild(document.createElement("img"));
    img1_1.setAttribute("src", "img/hq.png");
    img1_1.setAttribute("class", "prefix-hq-icon_alt");
    label1_1.innerHTML += formatNumberWithCommas(ppuHQ);
    let label1_2 = col1.appendChild(document.createElement("h4"));
    label1_2.setAttribute("class", "col1");
    label1_2.innerHTML = formatNumberWithCommas(ppuNQ);


    let col2 = container.appendChild(document.createElement("div"));
    col2.setAttribute("class", "col2");
    let label2 = col2.appendChild(document.createElement("h4"));
    label2.innerHTML = header2;

    let label2_1 = col2.appendChild(document.createElement("h4"));
    label2_1.setAttribute("class", "col1");
    let img2_1 = label2_1.appendChild(document.createElement("img"));
    img2_1.setAttribute("src", "img/hq.png");
    img2_1.setAttribute("class", "prefix-hq-icon_alt");
    label2_1.innerHTML += formatNumberWithCommas(tpHQ);
    let label2_2 = col2.appendChild(document.createElement("h4"));
    label2_2.setAttribute("class", "col2");
    label2_2.innerHTML = formatNumberWithCommas(tpNQ);

    return container;
}

/**
 * Format listing table.
 *
 * @param {Array[]} table
 * @param {number} i
 * @param {object} listing
 * @param {number} averagePricePerUnit
 * @param {string} world
 */
function onHashChange_genMarketTables_helper2(table, i, listing, averagePricePerUnit, world) {
    let percentDifference = formatNumberWithCommas(
        Math.round(getPercentDifference(listing.pricePerUnit, averagePricePerUnit) * 100) / 100
    );

    if (percentDifference > 0) {
        percentDifference = "+" + percentDifference;
    }

    table.push([
        i + 1,
        world,
        listing.hq === 1 ? "$hq" : "",
        (() => { // Materia
            let materiaElements = document.createElement("div");
            for (let materia of listing.materia) {
                let materiaName = materiaIDToItemName(materia);
                let materiaGrade = romanNumerals.indexOf(materiaName.split(" ")[materiaName.split(" ").length - 1]);

                let materiaElement = document.createElement("img");
                materiaElement.setAttribute("src", `img/materia${materiaGrade}.png`);
                materiaElement.setAttribute("title", materiaName);

                materiaElements.appendChild(materiaElement);
            }
            return materiaElements.outerHTML;
        })(),
        formatNumberWithCommas(listing.pricePerUnit),
        formatNumberWithCommas(listing.quantity),
        formatNumberWithCommas(listing.total),
        percentDifference + "%",
        listing.retainerName,
        listing.creator ? listing.creator : "",
    ]);
}

/**
 * Format history table.
 *
 * @param {Array[]} table
 * @param {number} i
 * @param {object} entry
 * @param {number} averagePricePerUnit
 * @param {string} world
 */
function onHashChange_genMarketTables_helper3(table, i, entry, averagePricePerUnit, world) {
    let percentDifference = formatNumberWithCommas(
        Math.round(getPercentDifference(entry.pricePerUnit, averagePricePerUnit) * 100) / 100
    );

    if (percentDifference > 0) {
        percentDifference = "+" + percentDifference;
    }

    table.push([
        i + 1,
        world,
        entry.hq === 1 ? "$hq" : "",
        formatNumberWithCommas(entry.pricePerUnit),
        formatNumberWithCommas(entry.quantity),
        formatNumberWithCommas(entry.total),
        percentDifference + "%",
        entry.buyerName,
        parseDate(entry.timestamp),
    ]);
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

/**
 * Get the percent difference between two prices.
 *
 * @param {number} cheapestPrice
 * @param {number} testPrice
 * @return {number} The percent difference between the second number and the first.
 */
function getPercentDifference(cheapestPrice, testPrice) {
    return (cheapestPrice - testPrice) / average(cheapestPrice, testPrice);
}

/**
 * Get the average of some numbers.
 *
 * @param {...number} numbers
 * @return {number} The average.
 */
function average(...numbers) {
    let result = 0;
    for (let i = 0; i < numbers.length; i++) {
        result += numbers[i];
    }
    result /= numbers.length;
    return result;
}

/**
 * Insert commas every three digits in a number.
 *
 * @param {number} x
 * @return {string} The formatted number.
 */
function formatNumberWithCommas(x) {
    let output = "" + x;
    let wholePart = output;
    let decimalPart;

    if (output.indexOf(".") !== -1) {
        wholePart = output.substr(0, output.indexOf("."));
        decimalPart = output.substr(output.indexOf(".") + 1);
    }

    let originalLength = wholePart.length;
    for (let i = originalLength - 1; i > 0; i--) {
        if ((originalLength - i) % 3 === 0) {
            wholePart = wholePart.substr(0, i) + "," + wholePart.substr(i);
        }
    }

    if (decimalPart) {
        output = wholePart + "." + decimalPart;
    } else {
        output = wholePart;
    }

    return output;
}

const romanNumerals = [undefined, "I", "II", "III", "IV", "V", "VI", "VII", "VIII"];
