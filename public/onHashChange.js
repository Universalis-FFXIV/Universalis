/**
 * Creates the per-data center navigation bar.
 *
 * @return {Element} An element of class infobox.
 */
function onHashChange_createWorldNav() {
    var worldNav = createElement("div", {
        "class": "infobox nav",
    });

    var nav = worldNav.appendChild(createElement("table", {
        "id": "navbar",
    }));
    nav = nav.appendChild(createElement("tr"));

    var trimmedWorldList = ["Cross-World"].concat(worldList[dataCenter]);

    for (var world of trimmedWorldList) { // Will exist at execution time
        // Table cell
        var w = nav.appendChild(createElement("td"));
        if (trimmedWorldList.indexOf(world) == 0) {
            w.setAttribute("class", "left-cell");
        }

        // Link table cell
        w = w.appendChild(createElement("a", {
            "href": `#/market/${itemID}?world=${world}`,
        }));
        w = w.appendChild(createElement("div", {
            "class": "table-cell-link",
        }));
        w = w.appendChild(createElement("p", null, world));
    }

    return worldNav;
}

/**
 * Creates the history graph.
 *
 * @param {Element} graphContainer - A container to put the graph in.
 */
async function onHashChange_drawGraph(graphContainer) {
    const worldID = getWorldID(world);

    let historyData = await request(`api/history/${worldID}/${itemID}`);
    try {
        historyData = JSON.parse(historyData);
    } catch (err) {
        let errorElement = createElement("div", {
            "class": "infobox",
        });
        return errorElement;
    }

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
            .map((sale) => {
                sale = sale.pricePerUnit;
                return sale;
            })
        ));
        yAxisDataNQ.push(average(...normalQualitySales
            .filter((sale) => sale.timestamp <= timestamp && sale.timestamp > lastTimestamp)
            .map((sale) => {
                sale = sale.pricePerUnit;
                return sale;
            })
        ));
    }

    var graph = graphContainer.appendChild(createElement("canvas", {
        "id": "echarts",
        "height": "300",
    }));
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
async function onHashChange_genCheapest() {
    const worldID = getWorldID(world);

    let marketData = await request(`api/${worldID}/${itemID}`);
    try {
        marketData = JSON.parse(marketData);
    } catch (err) {
        let errorElement = createElement("div", {
            "class": "infobox",
        });
        return errorElement;
    }

    const highQualityListings = marketData.listings.filter((listing) => listing.hq === 1);
    const normalQualityListings = marketData.listings.filter((listing) => listing.hq === 0);

    var cheapestListings = createElement("div");
    cheapestListings.setAttribute("class", "infobox cheapest-listings");

    if (highQualityListings.length > 0) {
        let highQuality = cheapestListings.appendChild(createElement("div", {
            "class": "col1",
        }));
        let bigTextHQ = highQuality.appendChild(createElement("p", {
            "class": "col1 gen-cheapest",
        }, "Cheapest High-Quality"));
        let priceHQ = bigTextHQ.appendChild(createElement("h1", null,
            formatNumberWithCommas(highQualityListings[0].quantity) + " x " +
            formatNumberWithCommas(highQualityListings[0].pricePerUnit)
        ));

        let serverLabelHQ = highQuality.appendChild(createElement("p", {
            "class": "col2 gen-cheapest",
        }, " Server: <b>" +
            highQualityListings[0].worldName +
            "</b> - Total: <b>" +
            formatNumberWithCommas(highQualityListings[0].total) +
            "</b>"
        ));
    }

    if (normalQualityListings.length > 0) {
        let normalQuality = cheapestListings.appendChild(createElement("div", {
            "class": "col2",
        }));
        let bigTestNQ = normalQuality.appendChild(createElement("p", {
            "class": "col1 gen-cheapest",
        }, "Cheapest Normal Quality"));
        let priceNQ = bigTestNQ.appendChild(createElement("h1", null,
            formatNumberWithCommas(normalQualityListings[0].quantity) + " x " +
            formatNumberWithCommas(normalQualityListings[0].pricePerUnit)
        ));

        let serverLabelNQ = normalQuality.appendChild(createElement("p", {
            "class": "col2 gen-cheapest",
        }, " Server: <b>" +
            normalQualityListings[0].worldName +
            "</b> - Total: <b>" +
            formatNumberWithCommas(normalQualityListings[0].total) +
            "</b>"
        ));
    }

    return cheapestListings;
}


/**
 * Generate tables with market board data.
 *
 * @return {Element} An element of class infobox.
 */
async function onHashChange_genMarketTables() {
    const worldID = getWorldID(world);
    let marketBoardData;
    try {
        marketBoardData = await getMarketData(worldID, itemID);
    } catch (err) {
        return createElement("div", {
            "class": "infobox",
        }, createElement("h3", {
            "class": "centered-text",
        }, "No market data has been uploaded for this item.").outerHTML);
    }

    var marketData = createElement("div", {
        "class": "infobox market-data",
    });

    // Columns
    var col1 = marketData.appendChild(createElement("div", {
        "class": "col1",
    }));
    var col2 = marketData.appendChild(createElement("div", {
        "class": "col2",
    }));

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
    } catch (err) {
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
    } catch (err) {
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
            let j = 0;
            for (let i = 0; i < marketBoardData.listings.length; i++) {
                let listing = marketBoardData.listings[i];

                if (listing.hq === 0) {
                    continue;
                }

                onHashChange_genMarketTables_helper2(table, j, listing, averagePricePerUnitHQ);
                j++;
            }
        } catch (err) {}

        let header = createElement("div");
        header.appendChild(createElement("img", {
            "src": "img/hq.png",
            "class": "prefix-hq-icon",
        }));
        header.appendChild(createElement("h3", null, "HQ Prices"));
        return onHashChange_genMarketTables_helper0(table, header);
    })());
    col2.appendChild((() => {
        let table = [
            ["#", "Server", "HQ", "Price", "Quantity", "Total", "%Diff", "Buyer", "Date"],
        ];

        try {
            let j = 0;
            for (let i = 0; i < marketBoardData.recentHistory.length; i++) {
                let entry = marketBoardData.recentHistory[i];

                if (entry.hq === 0) {
                    continue;
                }

                onHashChange_genMarketTables_helper3(table, j, entry, averagePricePerUnitHQ);
                j++;
            }
        } catch (err) {}

        let header = createElement("div");
        header.appendChild(createElement("img", {
            "src": "img/hq.png",
            "class": "prefix-hq-icon",
        }));
        header.appendChild(createElement("h3", null, "HQ Purchase History"));
        return onHashChange_genMarketTables_helper0(table, header);
    })());

    // NQ

    col1.appendChild((() => {
        let table = [
            ["#", "Server", "HQ", "Materia", "Price", "Quantity", "Total", "%Diff", "Retainer", "Creator"],
        ];

        try {
            let j = 0;
            for (let i = 0; i < marketBoardData.listings.length; i++) {
                let listing = marketBoardData.listings[i];

                if (listing.hq === 1) {
                    continue;
                }

                onHashChange_genMarketTables_helper2(table, j, listing, averagePricePerUnitNQ);
                j++;
            }
        } catch (err) {}

        let header = createElement("h3", null, "NQ Prices");
        return onHashChange_genMarketTables_helper0(table, header);
    })());
    col2.appendChild((() => {
        let table = [
            ["#", "Server", "HQ", "Price", "Quantity", "Total", "%Diff", "Buyer", "Date"],
        ];

        try {
            let j = 0;
            for (let i = 0; i < marketBoardData.recentHistory.length; i++) {
                let entry = marketBoardData.recentHistory[i];

                if (entry.hq === 1) {
                    continue;
                }

                onHashChange_genMarketTables_helper3(table, j, entry, averagePricePerUnitHQ);
                j++;
            }
        } catch (err) {}

        let header = createElement("h3", null, "NQ Purchase History");
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
    let container = createElement("div");
    let label = container.appendChild(header);

    let tableElement = makeTable(table, "market-data-table");
    container.appendChild(tableElement);

    for (let i = 0; i < table.length; i++) {
        let cell = tableElement.children[i].children[2];
        if (cell.innerHTML === "$hq") {
            let img = createElement("img", {
                "src": "img/hq.png",
                "style": "height: 16px; width: 16px;",
            });
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

    let container = createElement("div", {
        "style": "text-align: center;",
    });

    let col1 = container.appendChild(createElement("div", {
        "class": "col1",
    }, createElement("h4", null, header1).outerHTML));

    let label1_1 = col1.appendChild(createElement("h4", {
        "class": "col1",
    }, createElement("img", {
        "src": "img/hq.png",
        "class": "prefix-hq-icon_alt",
    }).outerHTML));
    label1_1.innerHTML += formatNumberWithCommas(ppuHQ);

    let label1_2 = col1.appendChild(createElement("h4", {
        "class": "col1",
    }, formatNumberWithCommas(ppuNQ)));

    let col2 = container.appendChild(createElement("div", {
        "class": "col2",
    }, createElement("h4", null, header2).outerHTML));

    let label2_1 = col2.appendChild(createElement("h4", {
        "class": "col1",
    }, createElement("img", {
        "src": "img/hq.png",
        "class": "prefix-hq-icon_alt",
    }).outerHTML));
    label2_1.innerHTML += formatNumberWithCommas(tpHQ);

    let label2_2 = col2.appendChild(createElement("h4", {
        "class": "col2",
    }, formatNumberWithCommas(tpNQ)));

    return container;
}

/**
 * Format listing table.
 *
 * @param {Array[]} table
 * @param {number} i
 * @param {object} listing
 * @param {number} averagePricePerUnit
 */
function onHashChange_genMarketTables_helper2(table, i, listing, averagePricePerUnit) {
    let percentDifference = formatNumberWithCommas(
        Math.round(getPercentDifference(listing.pricePerUnit, averagePricePerUnit) * 100) / 100
    );

    if (percentDifference > 0) {
        percentDifference = "+" + percentDifference;
    }

    table.push([
        i + 1,
        listing.worldName ? listing.worldName : world,
        listing.hq === 1 ? "$hq" : "",
        (() => { // Materia
            let materiaElements = createElement("div");
            for (let materia of listing.materia) {
                materia = materia.itemID;
                let materiaName = materiaIDToItemName(materia);
                let materiaGrade = romanNumerals.indexOf(materiaName.split(" ")[materiaName.split(" ").length - 1]);

                let materiaElement = createElement("img", {
                    "src": `img/materia${materiaGrade}.png`,
                    "title": materiaName,
                });

                materiaElements.appendChild(materiaElement);
            }
            return materiaElements.outerHTML;
        })(),
        formatNumberWithCommas(listing.pricePerUnit),
        formatNumberWithCommas(listing.quantity),
        formatNumberWithCommas(listing.total),
        percentDifference + "%",
        listing.retainerName,
        listing.creatorName ? listing.creatorName : "",
    ]);
}

/**
 * Format history table.
 *
 * @param {Array[]} table
 * @param {number} i
 * @param {object} entry
 * @param {number} averagePricePerUnit
 */
function onHashChange_genMarketTables_helper3(table, i, entry, averagePricePerUnit) {
    let percentDifference = formatNumberWithCommas(
        Math.round(getPercentDifference(entry.pricePerUnit, averagePricePerUnit) * 100) / 100
    );

    if (percentDifference > 0) {
        percentDifference = "+" + percentDifference;
    }

    table.push([
        i + 1,
        entry.worldName ? entry.worldName : entry,
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
 * @return {Element} An element of class infobox.
 */
async function onHashChange_fetchItem() {
    var itemInfo = createElement("div", {
        "class": "infobox",
        "id": itemID,
    });

    itemData = JSON.parse(await request(`https://www.garlandtools.org/db/doc/item/${lang}/3/${itemID}.json`));

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
    var container = createElement("div", {
        "class": "info-header",
    });
    itemInfo.appendChild(container);

    var itemThumbnail = createElement("img", {
        "class": "info-thumbnail",
        "src": icon,
    }); // Image
    container.appendChild(itemThumbnail);

    var basicInfo = createElement("h1", null, name);
    container.appendChild(basicInfo);

    var subInfo = createElement("h3", null,
        `iLvl ${ilvl} ${category} - Stack: ${stackSize}${equipLevel ? ` - Level ${equipLevel} ${jobs}` : ""}`
    );
    container.appendChild(subInfo);

    itemInfo.appendChild(createElement("br"));

    if (description) { // Description
        container.appendChild(createElement("span", null, description));
    }

    return itemInfo;
}
