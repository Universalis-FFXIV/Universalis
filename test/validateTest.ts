import * as should from "should";

import * as validate from "../src/validate";

describe("validate", function () {
	describe("isValidName", function () {
		it("should accept 音无结弦", function () {
			should.equal(validate.isValidName("音无结弦"), true);
		});

		it("should accept 达斯利亚", function () {
			should.equal(validate.isValidName("达斯利亚"), true);
		});

		it("should accept 诺兰里亚特", function () {
			should.equal(validate.isValidName("诺兰里亚特"), true);
		});

		it("should accept 米特斯拉", function () {
			should.equal(validate.isValidName("米特斯拉"), true);
		});

		it("should accept Nichiren", function () {
			should.equal(validate.isValidName("Nichiren"), true);
		});
	});

	describe("isValidWorld", function () {
		it("should accept 1060", function () {
			should.equal(validate.isValidWorld(1060), true);
		});
	});
});
