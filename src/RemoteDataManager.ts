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
    "World.csv": "https://raw.githubusercontent.com/xivapi/ffxiv-datamining/master/csv/World.csv"
};

const csvMap = new Map<string, string[][]>();
const remoteFileMap = new Map<string, Buffer>();

export class RemoteDataManager {
    private exts: string[];
    private logger: Logger;
    private remoteFileDirectory: string;

    constructor(options: RemoteDataManagerOptions) {
        this.exts = options.exts ? options.exts : ["csv", "json"];
        this.logger = options.logger;
        this.remoteFileDirectory = options.remoteFileDirectory ? options.remoteFileDirectory : "../public";

        for (const ext of this.exts) {
            const extPath = path.join(__dirname, this.remoteFileDirectory, ext);
            if (!fs.existsSync(extPath)) {
                fs.mkdirSync(extPath, { recursive: true });
            }
        }
    }

    /** Get all marketable item IDs from XIVAPI. It is accessible while it is being populated. */
    public async getMarketableItemIDs(): Promise<number[]> {
        const url = "https://xivapi.com/search?indexes=item&filters=ItemSearchCategory.ID%3E8&columns=ID";

        let storageFile = JSON.parse((await readFile(path.join(this.remoteFileDirectory, "item.json"))).toString());
        if (storageFile && storageFile.itemID) {
            return storageFile.itemID;
        }

        storageFile = [];

        (async () => { // This floats around and runs after the array is returned.
            const firstPage = JSON.parse(await request(url));
            const pageCount = firstPage.Pagination.PageTotal;
            storageFile.push(firstPage.Results.map((item: { ID: number }) => item.ID));
            for (let i = 2; i < pageCount; i++) {
                await new Promise((resolve) => { setTimeout(resolve, 100) });
                const nextPage = JSON.parse(await request(url + `&page=${i}`));
                storageFile.push(nextPage.Results.map((item: { ID: number }) => item.ID));
            }

            await writeFile(path.join(this.remoteFileDirectory, "item.json"), JSON.stringify({ storageFile }));
        })();

        return storageFile;
    }

    /** Parse a CSV, retrieving it if it does not already exist. */
    public async parseCSV(fileName: string): Promise<string[][]> {
        if (csvMap.get(fileName)) {
            return csvMap.get(fileName);
        }

        const table = await this.fetchFile(fileName);

        const parser = csvParser({
            bom: true,
            delimiter: ","
        });

        const data: Promise<string[][]> = new Promise((resolve) => {
            const output = [];

            parser.write(table);

            parser.on("readable", () => {
                let record: string[] = parser.read();
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
        const ext = fileName.substr(fileName.lastIndexOf(".") + 1);
        const filePath = path.join(__dirname, this.remoteFileDirectory, ext, fileName);

        if (!await exists(filePath)) {
            const remoteData = await request(urlDictionary[fileName]);
            await writeFile(filePath, remoteData);

            remoteFileMap.set(fileName, remoteData);

            return remoteData;
        } else {
            if (!remoteFileMap.get(fileName)) {
                remoteFileMap.set(fileName, await readFile(filePath));
            }

            return remoteFileMap.get(fileName);
        }
    }
}
