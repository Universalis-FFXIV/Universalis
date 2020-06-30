import * as should from "should";

import * as validate from "../src/validate";

describe("validate", function () {
	describe("isValidName", function () {
		const makeTest = (name: string, expectedSuccess: boolean) => {
			it(`should ${
				expectedSuccess ? "accept" : "reject"
			} ${name}.`, function () {
				should.equal(validate.isValidName(name), expectedSuccess);
			});
		};

		makeTest("音无结弦", true);
		makeTest("达斯利亚", true);
		makeTest("诺兰里亚特", true);
		makeTest("米特斯拉", true);
		makeTest("Tom Hanks", true);
		makeTest("Nichiren", true);
		makeTest("392cmu328g5huh48terf94jgoi34j52kl535", false);
		makeTest("보라", false);
	});

	describe("isValidWorld", function () {
		const makeTest = (
			id: number,
			worldName: string,
			expectedSuccess: boolean,
		) => {
			it(`should ${
				expectedSuccess ? "accept" : "reject"
			} ${id} (${worldName}).`, function () {
				should.equal(validate.isValidWorld(id), expectedSuccess);
			});
		};

		makeTest(1060, "MengYaChi", true);
		makeTest(25, "Chaos", false);
		makeTest(74, "Coeurl", true);
	});
});
