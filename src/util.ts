import fs from "fs";
import path from "path";
import util from "util";
import winston, { Logger } from "winston";

import { ParameterizedContext } from "koa";

import { RemoteDataManager } from "./remote/RemoteDataManager";

require("winston-mongodb"); // Applies itself to the winston.transports namespace

const readFile = util.promisify(fs.readFile);

const logger = winston.createLogger();

const remoteDataManager = new RemoteDataManager({ logger });

export function appendWorldDC(
	obj: any,
	worldMap: Map<string, number>,
	ctx: ParameterizedContext,
): void {
	// Convert worldDC strings (numbers or names) to world IDs or DC names
	if (ctx.params && ctx.params.world) {
		const worldName =
			ctx.params.world.charAt(0).toUpperCase() + ctx.params.world.substr(1);
		if (!parseInt(ctx.params.world) && !worldMap.get(worldName)) {
			ctx.params.dcName = ctx.params.world;
		} else {
			if (parseInt(ctx.params.world)) {
				ctx.params.worldID = parseInt(ctx.params.world);
			} else {
				ctx.params.worldID = worldMap.get(worldName);
			}
		}
	}

	if (ctx.params.worldID) {
		obj["worldID"] = ctx.params.worldID;
	} else {
		if (ctx.params.dcName === "LuXingNiao") ctx.params.dcName = "陆行鸟"; // Little messy, but eh?
		if (ctx.params.dcName === "MoGuLi") ctx.params.dcName = "莫古力";
		if (ctx.params.dcName === "MaoXiaoPang") ctx.params.dcName = "猫小胖";
		obj["dcName"] = ctx.params.dcName;
	}
}

/** Calculate the average of some numbers. */
export function calcAverage(...numbers: number[]): number {
	if (numbers.length === 0) return 0;
	let out = 0;
	numbers.forEach((num) => {
		out += num;
	});
	return out / numbers.length;
}

export function calcTrimmedAverage(
	standardDeviation: number,
	...numbers: number[]
): number {
	if (numbers.length === 0) return 0;
	let out = 0;

	const mean = calcAverage(...numbers);

	// I would benchmark this, but benchmarking in JS is a pain
	// Also lol Prettier
	numbers.forEach(
		(num) =>
			(out +=
				num *
				Number(
					num <= mean + 3 * standardDeviation &&
						num >= mean - 3 * standardDeviation,
				)),
	);

	return out / numbers.length;
}

/** Calculate the rate at which items have been selling per day over the past week. */
export function calcSaleVelocity(...timestamps: number[]): number {
	const thisWeek = timestamps.filter(
		(timestamp) => timestamp * 1000 >= Date.now().valueOf() - 604800000,
	);
	return thisWeek.length / 7;
}

/** Calculate the standard deviation of some numbers. */
export function calcStandardDeviation(...numbers: number[]): number {
	if (numbers.length === 1) return 0;

	const average = calcAverage(...numbers);

	let sumSqr = 0;
	numbers.forEach((num) => {
		sumSqr += Math.pow(num - average, 2);
	});

	return Math.sqrt(sumSqr / (numbers.length - 1));
}

/** Create a distribution table of some numbers. */
export function makeDistrTable(
	...numbers: number[]
): { [key: number]: number } {
	const table: { [key: number]: number } = {};
	for (const num of numbers) {
		if (!table[num]) {
			table[num] = 1;
		} else {
			++table[num];
		}
	}
	return table;
}

export function createLogger(db: string): Logger {
	return winston.createLogger({
		transports: [
			new winston.transports["MongoDB"]({
				capped: true,
				cappedMax: 10000,
				db,
				options: { useNewUrlParser: true, useUnifiedTopology: true },
			}),
			new winston.transports.File({
				filename: "logs/error.log",
				level: "error",
			}),
			new winston.transports.Console({
				format: winston.format.simple(),
			}),
		],
	});
}

export function capitalise(input: string): string {
	return input.charAt(0).toUpperCase() + input.substr(1).toLowerCase();
}

export async function getDCWorlds(dc: string): Promise<string[]> {
	const fpath = path.join(__dirname, "..", "public", "json", "dc.json");
	return JSON.parse((await readFile(fpath)).toString())[dc];
}

export async function getWorldDC(worldInput: string | number): Promise<string> {
	const world = !parseInt(worldInput as string)
		? worldInput
		: await getWorldName(worldInput as number);

	const fpath = path.join(__dirname, "..", "public", "json", "dc.json");
	const dataCenterWorlds = JSON.parse((await readFile(fpath)).toString());
	for (const dc in dataCenterWorlds) {
		if (dataCenterWorlds.hasOwnProperty(dc)) {
			const dcWorlds: string[] = dataCenterWorlds[dc];
			const foundWorld = dcWorlds.find((el: string) => el === world);
			if (foundWorld) return dc;
		}
	}
	return undefined;
}

export async function getWorldName(worldID: number): Promise<string> {
	const worldCSV = await getWorldTable();
	const world = worldCSV.find((line) => line[0] === worldID);
	return world == null ? null : world[1];
}

export async function getWorldTable(): Promise<
	Array<[number, string, number, number, boolean]>
> {
	let csv: any[][] = await remoteDataManager.parseCSV("World.csv");
	csv = csv.slice(3);
	for (const row of csv) {
		row[0] = parseInt(row[0]);
		row[2] = parseInt(row[2]);
		row[3] = parseInt(row[3]);
		row[4] = row[4] === "True";
	}
	return csv as Array<[number, string, number, number, boolean]>;
}

export function sleep(duration: number) {
	return new Promise((resolve) => setTimeout(resolve, duration));
}
