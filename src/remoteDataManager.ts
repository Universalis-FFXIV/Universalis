import csvParser from "csv-parse";
import fs from "fs";
import path from "path";
import request from "request-promise";
import util from "util";

import { Logger } from "winston";

const exists = util.promisify(fs.exists);
const readFile = util.promisify(fs.readFile);
const writeFile = util.promisify(fs.writeFile);

const csvMap = new Map();
const remoteFileMap = new Map();

const exts = ["csv", "json"];
const urlDictionary = {
    "Materia.csv": "https://raw.githubusercontent.com/xivapi/ffxiv-datamining/master/csv/Materia.csv",
    "World.csv": "https://raw.githubusercontent.com/xivapi/ffxiv-datamining/master/csv/World.csv",
    "data.json": "https://www.garlandtools.org/db/doc/core/en/3/data.json",
    "dc.json": "https://xivapi.com/servers/dc",
    "search_de.json": "https://xivapi.com/search?string=" +
                      "&string_algo=wildcard_plus&indexes=item&filters=ItemSearchCategory.ID%3E8" +
                      "&columns=ID,IconID,ItemSearchCategory.Name_de,LevelItem,Name_de",
    "search_en.json": "https://xivapi.com/search?string=" +
                      "&string_algo=wildcard_plus&indexes=item&filters=ItemSearchCategory.ID%3E8" +
                      "&columns=ID,IconID,ItemSearchCategory.Name_en,LevelItem,Name_en",
    "search_fr.json": "https://xivapi.com/search?string=" +
                      "&string_algo=wildcard_plus&indexes=item&filters=ItemSearchCategory.ID%3E8" +
                      "&columns=ID,IconID,ItemSearchCategory.Name_fr,LevelItem,Name_fr",
    "search_jp.json": "https://xivapi.com/search?string=" +
                      "&string_algo=wildcard_plus&indexes=item&filters=ItemSearchCategory.ID%3E8" +
                      "&columns=ID,IconID,ItemSearchCategory.Name_jp,LevelItem,Name_jp",
};

for (const ext of exts) {
    if (!fs.existsSync(path.join(__dirname, `../public/${ext}`))) {
        fs.mkdirSync(path.join(__dirname, `../public/${ext}`));
    }
}

const remoteFileDirectory = "../public";

module.exports.fetchAll = () => {
    for (const fileName in urlDictionary) {
        if (urlDictionary.hasOwnProperty(fileName)) {
            this.fetchFile(fileName);
        }
    }
};

// Get local copies of certain remote files, should they not exist locally
module.exports.fetchFile = async (fileName: string) => {
    const ext = fileName.substr(fileName.indexOf(".") + 1);
    const file = await exists(path.join(__dirname, remoteFileDirectory, ext, fileName));

    if (!file) {
        let remoteData: any;

        remoteData = await request(urlDictionary[fileName]);
        await writeFile(path.join(__dirname, remoteFileDirectory, ext, fileName), remoteData);

        remoteFileMap.set(fileName, remoteData);

        return remoteData;
    } else {
        if (!remoteFileMap.get(fileName)) {
            remoteFileMap.set(fileName, await readFile(path.join(__dirname, remoteFileDirectory, ext, fileName)));
        }

        return remoteFileMap.get(fileName);
    }
};

module.exports.parseCSV = async (fileName: string, logger: Logger) => {
    if (csvMap.get(fileName)) {
        return csvMap.get(fileName);
    }

    const table = await this.fetchFile(fileName);

    const parser = csvParser({
        delimiter: ","
    });

    const data = await new Promise((resolve, reject) => {
        const output = [];

        parser.write(table);

        parser.on("readable", () => {
            let record: any = parser.read();
            while (record) {
                output.push(record);
                record = parser.read();
            }
        });

        parser.on("error", (err) => {
            logger.error(err.message);
        });

        parser.once("end", () => {
            resolve(output);
            parser.removeAllListeners();
        });

        parser.end();
    });

    csvMap.set(fileName, data);

    return data;
};

export default module.exports;
