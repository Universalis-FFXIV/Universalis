import chalk from "chalk";
import csvParser from "csv-parse";
import flatten from "lodash.flatten"
import fs from "fs";
import path from "path";
import request from "request-promise";
import util from "util";

import { Logger } from "winston";

import { RemoteDataManagerOptions } from "../models/RemoteDataManagerOptions";

const exists = util.promisify(fs.exists);
const readFile = util.promisify(fs.readFile);
const writeFile = util.promisify(fs.writeFile);

const urlDictionary = {
    "Materia.csv": "https://raw.githubusercontent.com/xivapi/ffxiv-datamining/master/csv/Materia.csv",
    "World.csv": "https://raw.githubusercontent.com/xivapi/ffxiv-datamining/master/csv/World.csv"
};

const XIVAPI_RATELIMIT = 12;

const csvMap = new Map<string, string[][]>();
const remoteFileMap = new Map<string, any>();

export class RemoteDataManager {
    private exts: string[];
    private logger: Logger;
    private remoteFileDirectory: string;

    constructor(options: RemoteDataManagerOptions) {
        this.exts = options.exts ? options.exts : ["csv", "json"];
        this.logger = options.logger;
        this.remoteFileDirectory = options.remoteFileDirectory ? options.remoteFileDirectory : "../../public";

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
        const existingFile = path.join(__dirname, this.remoteFileDirectory, "json/item.json");

        let storageFile: { itemID: number[] } = {
            itemID: []
        };

        if (remoteFileMap.get("item.json")) {
            return remoteFileMap.get("item.json").itemID;
        } else if (await exists(existingFile)) {
            storageFile = JSON.parse((await readFile(existingFile)).toString());
            if (storageFile && storageFile.itemID) {
                remoteFileMap.set("item.json", storageFile);
                return storageFile.itemID;
            }
        }

        // This fills the array after it's returned, since this is a somewhat lengthy job.
        (async () => {
            let pageCount = Number.MAX_VALUE;
            const startTime = Date.now();

            for (let i = 1; i <= pageCount; i += XIVAPI_RATELIMIT) {
                const startTime = Date.now();

                // Batch as many requests as possible.
                const requestPromises: Promise<number[]>[] = [];
                for (let j = i; j < i + XIVAPI_RATELIMIT && j <= pageCount; j++) {
                    requestPromises.push(new Promise(async (resolve) => {
                        await new Promise((resolve) => setTimeout(resolve, (j - i) * 15));
                        const nextPage = JSON.parse(await request(url + `&page=${j}`));
                        if (nextPage.Pagination.PageTotal < pageCount) pageCount = nextPage.Pagination.PageTotal;
                        const nextPageItems = nextPage.Results.map((item: { ID: number }) => item.ID);
                        resolve(nextPageItems);
                    }));
                }

                const nextLine = flatten(await Promise.all(requestPromises));
                storageFile.itemID.push.apply(storageFile.itemID, nextLine);

                const timeDiff = Date.now() - startTime;
                this.logger.info("(Marketable Item ID Catalog) Pushed " +
                    `${chalk.greenBright(requestPromises.length.toString())} pages in ${timeDiff}ms.`);

                // If the batched requests took less than a second, wait the remainder of the second
                // to avoid hitting the rate limit.
                await new Promise((resolve) => setTimeout(resolve, 1000 - timeDiff));
            }

            const timeDiff = Date.now() - startTime;
            remoteFileMap.set("item.json", storageFile);
            await writeFile(existingFile, JSON.stringify(storageFile));
            this.logger.info(`(Marketable Item ID Catalog) Wrote ${pageCount} pages of ${storageFile.itemID.length} ` +
                `items out to ${chalk.greenBright(existingFile)} (operation time: ${timeDiff}ms).`);
        })();

        return storageFile.itemID;
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
        const promises: Array<Promise<any>> = [];
        for (const fileName in urlDictionary) {
            if (urlDictionary.hasOwnProperty(fileName)) {
                promises.push(this.fetchFile(fileName));
            }
        }
        promises.push(this.getMarketableItemIDs());
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
