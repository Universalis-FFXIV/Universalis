import csvParser from "csv-parse";
import fs from "fs";
import path from "path";
import request from "request-promise";
import util from "util";

import { Logger } from "winston";

import { RemoteDataManagerOptions } from "./models/RemoteDataManagerOptions";

const exists = util.promisify(fs.exists);
const readFile = util.promisify(fs.readFile);
const writeFile = util.promisify(fs.writeFile);

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

const csvMap = new Map();
const remoteFileMap = new Map();

const remoteFileDirectory = "../public";

export class RemoteDataManager {
    private csvMap: Map<string, any>;
    private remoteFileMap: Map<string, Buffer>;

    private exts: string[];
    private logger: Logger;
    private remoteFileDirectory: string;

    constructor(options: RemoteDataManagerOptions) {
        this.exts = options.exts ? options.exts : ["csv", "json"];
        this.logger = options.logger;
        this.remoteFileDirectory = options.remoteFileDirectory ? options.remoteFileDirectory : "../public";

        this.csvMap = new Map();
        this.remoteFileMap = new Map();

        for (const ext of this.exts) {
            if (!fs.existsSync(path.join(__dirname, `${this.remoteFileDirectory}/${ext}`))) {
                fs.mkdirSync(path.join(__dirname, `${this.remoteFileDirectory}/${ext}`));
            }
        }
    }

    /** Parse a CSV, retrieving it if it does not already exist. */
    public async parseCSV(fileName: string): Promise<any> {
        if (this.csvMap.get(fileName)) {
            return this.csvMap.get(fileName);
        }

        const table = await this.fetchFile(fileName);

        const parser = csvParser({
            delimiter: ","
        });

        const data: Promise<any[]> = new Promise((resolve) => {
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
                this.logger.error(err.message);
            });

            parser.once("end", () => {
                resolve(output);
                parser.removeAllListeners();
            });

            parser.end();
        });

        this.csvMap.set(fileName, await data);

        return data;
    }

    /** Get all files. */
    public async fetchAll(): Promise<void> {
        const promises: Array<Promise<Buffer>> = [];
        for (const fileName in urlDictionary) {
            if (urlDictionary.hasOwnProperty(fileName)) {
                promises.push(this.fetchFile(fileName));
            }
        }
        await Promise.all(promises);
    }

    /** Get local copies of certain remote files, should they not exist locally. */
    public async fetchFile(fileName: string): Promise<Buffer> {
        const ext = fileName.substr(fileName.indexOf(".") + 1);
        const file = await exists(path.join(__dirname, this.remoteFileDirectory, ext, fileName));

        if (!file) {
            let remoteData: any;

            remoteData = await request(urlDictionary[fileName]);
            await writeFile(path.join(__dirname, this.remoteFileDirectory, ext, fileName), remoteData);

            this.remoteFileMap.set(fileName, remoteData);

            return remoteData;
        } else {
            if (!this.remoteFileMap.get(fileName)) {
                this.remoteFileMap.set(
                    fileName,
                    await readFile(path.join(__dirname, this.remoteFileDirectory, ext, fileName))
                );
            }

            return this.remoteFileMap.get(fileName);
        }
    }
}

module.exports.parseCSV = async (fileName: string): Promise<any> => {
    if (csvMap.get(fileName)) {
        return csvMap.get(fileName);
    }

    const table = await this.fetchFile(fileName);

    const parser = csvParser({
        delimiter: ","
    });

    const data: Promise<any> = new Promise((resolve) => {
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
            this.logger.error(err.message);
        });

        parser.once("end", () => {
            resolve(output);
            parser.removeAllListeners();
        });

        parser.end();
    });

    csvMap.set(fileName, await data);

    return data;
};

module.exports.fetchFile = async (fileName: string): Promise<Buffer> => {
    const ext = fileName.substr(fileName.indexOf(".") + 1);
    const file = await exists(path.join(__dirname, remoteFileDirectory, ext, fileName));

    if (!file) {
        let remoteData: any;

        remoteData = await request(urlDictionary[fileName]);
        await writeFile(path.join(__dirname, remoteFileDirectory, ext, fileName), remoteData);

        this.remoteFileMap.set(fileName, remoteData);

        return remoteData;
    } else {
        if (!remoteFileMap.get(fileName)) {
            remoteFileMap.set(fileName, await readFile(path.join(__dirname, remoteFileDirectory, ext, fileName)));
        }

        return remoteFileMap.get(fileName);
    }
};

export default module.exports;
