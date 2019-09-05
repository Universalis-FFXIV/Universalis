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
 * Get a world ID from a world name, or else return the Data Center name.
 *
 * @param {number} world - The world to look up.
 * @return {Object} The world ID or DC name.
 */
function getWorldID(world) {
    let worldID;
    if (worldMap.get(world)) worldID = worldMap.get(world);
    else if (world === "Cross-World") worldID = dataCenter;
    else worldID = world;
    return worldID;
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
