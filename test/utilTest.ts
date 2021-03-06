import * as should from "should";

import * as util from "../src/util";

const worldDCs = require("../public/json/dc.json");

describe("util", function () {
	describe("appendWorldDC", function () {
		it("should correctly append the world to an object, given a world ID.");

		it("should correctly append the world to an object, given a world name.");

		it("should correctly append the DC to an object, given a DC name.");

		it(
			"should append a null worldName field to the object, given an invalid input.",
		);
	});

	describe("calcAverage", function () {
		const makeTest = (list: number[], expected: number) => {
			it(`should return ${expected} for [${list.toString()}].`, function () {
				should.equal(util.calcAverage(...list), expected);
			});
		};

		makeTest([], 0);
		makeTest([1], 1);
		makeTest([2, 2], 2);
		makeTest([2, 3], 2.5);
		makeTest([8, 0, 5], (5 + 8) / 3);
		makeTest([0, 5, 8], (5 + 8) / 3);
		makeTest([8, 5, 0], (5 + 8) / 3);
		makeTest([35437, 35438, 35439, 36100, 36100, 36100, 37535], 252149 / 7);
		makeTest([-1, -1, -1, -1], -1);
		makeTest([-1, 1], 0);
	});

	describe("calcTrimmedStats", function () {
		const makeTest = (list: number[], expected: number) => {
			it(`should return the mean ${expected} for [${list.toString()}].`, function () {
				const standardDeviation = util.calcStandardDeviation(...list);
				should.equal(
					util.calcTrimmedStats(standardDeviation, ...list).mean,
					expected,
				);
			});
		};

		makeTest([], 0);
		makeTest([1], 1);
		makeTest([2, 2], 2);
		makeTest([2, 3], 2.5);
		makeTest([8, 0, 5], 4.333333333333333);
		makeTest([0, 5, 8], 4.333333333333333);
		makeTest([8, 5, 0], 4.333333333333333);
		makeTest(
			[35437, 35438, 35439, 36100, 36100, 36100, 37535],
			36021.28571428572,
		);
		makeTest([-1, -1, -1, -1], -1);
		makeTest([-1, 1], 0);
	});

	describe("calcSaleVelocity", function () {
		it(
			"should correctly calculate the sale volume per day, over the past week of data.",
		);
	});

	describe("calcStandardDeviation", function () {
		const makeTest = (list: number[], expected: number) => {
			it(`should return ${expected} for the input [${list.toString()}].`, function () {
				should.equal(
					Math.round(
						(util.calcStandardDeviation(...list) + Number.EPSILON) * 100000,
					) / 100000,
					expected,
				);
			});
		};

		makeTest([13, 23, 12, 44, 55], 19.24318);
		makeTest([324698, 312495, 321458, 134812], 92513.24994);
		makeTest([1, 1, 1, 1, 1], 0);
		makeTest([123, 654, 134, 14, 743, 3865], 1473.01886);
		makeTest([-1, 4, -6, 0, 1, 7], 4.44597);
		makeTest([-1, -2, -3, -4, -5, -6, -7], 2.16025);
		makeTest([0, 0], 0);
		makeTest([9999999999, 1], 7071067810.45126);
		makeTest([1e100, -1e100], 1.414213562373095e100);
		makeTest([1], 0);
		makeTest([0], 0);
	});

	describe("makeDistrTable", function () {
		const makeTest = (list: number[], expected: { [key: number]: number }) => {
			it(`should build the object ${JSON.stringify(
				expected,
			)} from the values [${list.toString()}].`, function () {
				should.deepEqual(util.makeDistrTable(...list), expected);
			});
		};

		makeTest([], {});
		makeTest([0, 0, 0, 1, 1, 1, 2, 2, 3, 3], { 0: 3, 1: 3, 2: 2, 3: 2 });
		makeTest([1, 1, 1, 3, 6, 2, 7, 2, 9, 0], {
			0: 1,
			1: 3,
			2: 2,
			3: 1,
			6: 1,
			7: 1,
			9: 1,
		});
		const arr = [];
		arr.length = 100;
		arr.fill(0);
		makeTest(arr, { 0: 100 });
		makeTest([1, 31, 1, 3, 1, 3, 1, 3], { 1: 4, 3: 3, 31: 1 });
		makeTest([-1, -2, -1, -1], { "-1": 3, "-2": 1 });
		makeTest([-0], { 0: 1 });
	});

	describe("createLogger", function () {
		it("should create the logger without throwing any errors.", function () {
			util.createLogger("mongodb://localhost:27017/");
		});
	});

	describe("capitalise", function () {
		const makeTest = (input: string, expected: string) => {
			it("should turn any string into one in which the first letter is capitalised, if applicable. The rest of the string should become lowercase.", function () {
				should.equal(util.capitalise(input), expected);
			});
		};

		const testInputs = [
			"letters",
			"8, this starts with a number.",
			"Already capitalised.",
			"some more words!",
			"",
		];

		const expectedOutputs = [
			"Letters",
			"8, this starts with a number.",
			"Already capitalised.",
			"Some more words!",
			"",
		];

		for (let i = 0; i < testInputs.length; i++) {
			makeTest(testInputs[i], expectedOutputs[i]);
		}
	});

	describe("getDCWorlds", function () {
		for (const dc in worldDCs) {
			if (worldDCs.hasOwnProperty(dc)) {
				it(`should get a string[] of ${dc}'s worlds.`, async function () {
					const expected = worldDCs[dc];
					should.deepEqual(await util.getDCWorlds(dc), expected);
				});
			}
		}
	});

	describe("getWorldDC", async function () {
		const exclude = ["LuXingNiao", "MoGuLi", "MaoXiaoPang"];
		for (const dc in worldDCs) {
			if (worldDCs.hasOwnProperty(dc)) {
				if (exclude.includes(dc)) continue;

				for (const worldName of worldDCs[dc]) {
					it(`should return ${dc} for the input ${worldName}.`, async function () {
						should.equal(await util.getWorldDC(worldName), dc);
					});
				}
			}
		}
	});

	describe("getWorldName", function () {
		const makeTest = (ID: number, expected: string) => {
			it("should get a world's name given its ID.", async function () {
				should.equal(await util.getWorldName(ID), expected);
			});
		};

		makeTest(-1, undefined);
		makeTest(66, "Odin");
		makeTest(69, "Bahamut");
		makeTest(74, "Coeurl");
		makeTest(91, "Balmung");
	});

	describe("sleep", function () {
		it("should sleep for 1 second.", async function () {
			await util.sleep(1000);
		});
	});
});
