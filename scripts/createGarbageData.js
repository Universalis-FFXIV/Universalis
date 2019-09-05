const fs = require("fs");
const path = require("path");
const request = require("request-promise");
const util = require("util");

const remoteDataManager = require("../build/remoteDataManager.js");

const mkdir = util.promisify(fs.mkdir);
const writeFile = util.promisify(fs.writeFile);

module.exports = async () => {
    // Setup
    const extendedHistoryFolder = path.join(__dirname, "../history");
    const listingFolder = path.join(__dirname, "../data");

    let itemTable = [];
    const dataFile = await request("https://raw.githubusercontent.com/xivapi/ffxiv-datamining/master/csv/Item.csv");

    let lines = dataFile.match(/[^\r\n]+/g).slice(3);
    for (let line of lines) {
        line = line.split(",");
        itemTable.push(line);
    }

    const dcList = JSON.parse(await remoteDataManager.fetchFile("dc.json"));

    let worldMap = new Map();
    const worldDataCSV = remoteDataManager.fetchFile("World.csv").toString();
    let worldEntries = worldDataCSV.match(/[^\r\n]+/g).slice(3);
    for (let worldEntry of worldEntries) {
        worldEntry = worldEntry.split(",");
        worldMap.set(worldEntry[1].replace(/[^a-zA-Z]+/g, ""), worldEntry[0]);
    }

    // Define function to create folders
    async function createWorldDCFolders(folderPath) {
        worldMap.forEach(async (value, key, map) => {
            console.log(".");
        });
    }

    createWorldDCFolders(extendedHistoryFolder);
    createWorldDCFolders(listingFolder);
};
