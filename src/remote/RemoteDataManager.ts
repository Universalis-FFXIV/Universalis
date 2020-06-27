import bent from "bent";
import csvParser from "csv-parse";
import fs from "fs";
import path from "path";
import util from "util";

import { Logger } from "winston";

import { RemoteDataManagerOptions } from "../models/RemoteDataManagerOptions";

const exists = util.promisify(fs.exists);
const readFile = util.promisify(fs.readFile);
const writeFile = util.promisify(fs.writeFile);

const urlDictionary = {
	"Materia.csv":
		"https://raw.githubusercontent.com/xivapi/ffxiv-datamining/master/csv/Materia.csv",
	"World.csv":
		"https://raw.githubusercontent.com/xivapi/ffxiv-datamining/master/csv/World.csv",
};

const csvMap = new Map<string, string[][]>();
const remoteFileMap = new Map<string, any>();

const download = bent("GET", "buffer");

export class RemoteDataManager {
	private exts: string[];
	private logger: Logger;
	private remoteFileDirectory: string;

	constructor(options: RemoteDataManagerOptions) {
		this.exts = options.exts ? options.exts : ["csv", "json"];
		this.logger = options.logger;
		this.remoteFileDirectory = options.remoteFileDirectory
			? options.remoteFileDirectory
			: "../../public";

		for (const ext of this.exts) {
			const extPath = path.join(__dirname, this.remoteFileDirectory, ext);
			if (!fs.existsSync(extPath)) {
				fs.mkdirSync(extPath, { recursive: true });
			}
		}
	}

	/** Get all marketable item IDs. It is accessible while it is being populated. */
	public async getMarketableItemIDs(): Promise<number[]> {
		const existingFile = path.join(
			__dirname,
			this.remoteFileDirectory,
			"json/item.json",
		);

		let storageFile: { itemID: number[] } = {
			itemID: [],
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
			delimiter: ",",
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
		const filePath = path.join(
			__dirname,
			this.remoteFileDirectory,
			ext,
			fileName,
		);

		if (!(await exists(filePath))) {
			const remoteData = (await download(urlDictionary[fileName])) as Buffer;
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
