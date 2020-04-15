import * as should from "should";

import * as util from "../src/util";

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
		it("should calculate averages correctly.");
	});

	describe("calcTrimmedAverage", function () {
		it(
			"should correctly calculate trimmed averages using the range of values between three standard deviations from the mean.",
		);
	});

	describe("calcSaleVelocity", function () {
		it("should correctly calculate the sale volume per day.");
	});

	describe("calcStandardDeviation", function () {
		it(
			"should correctly calculate the standard deviation for any given data set.",
		);
	});

	describe("makeDistrTable", function () {
		it(
			"should build an array from a set of values, in which each value in the input corresponds to an index in the output. The value of output[index] should be the number of incidences of index in the input.",
		);
	});

	describe("createLogger", function () {
		it("should create the logger without throwing any errors.");
	});

	describe("capitalise", function () {
		it(
			"should turn any string into one in which the first letter is capitalised, if applicable. The rest of the string should become lowercase.",
		);
	});

	describe("getDCWorlds", function () {
		it(
			"should get a string[] of world names that correspond to a data center.",
		);
	});

	describe("getWorldDC", function () {
		it("should get the DC that corresponds to any given world.");
	});

	describe("getWorldName", function () {
		it("should get a world's name given its ID.");
	});

	describe("levenshtein", function () {
		it("should generate the correct score for any two strings.");
	});

	describe("sleep", function () {
		it("should sleep for 1 second.", async function () {
			await util.sleep(1000);
		});
	});
});
