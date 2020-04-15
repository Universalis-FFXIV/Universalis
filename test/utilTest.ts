import * as should from "should";

import * as util from "../src/util";

describe("util", function () {
	it("should calculate raw averages", function () {
		const numbers = [2, 5, 8, 9];
		should.equal(util.calcAverage(...numbers), 6);
	});

	it("should calculate trimmed averages", function () {
		const numbers = [2, 5, 8, 9, 2, 3, 5, 1];
		should.equal(util.calcAverage(...numbers), 8);
	});

	it("should correctly make a distribution table of numbers", function () {
		const numbers = [
			0,
			0,
			0,
			0,
			1,
			1,
			1,
			1,
			2,
			2,
			2,
			2,
			2,
			2,
			2,
			3,
			3,
			4,
			4,
			4,
			4,
			4,
			5,
			5,
			6,
		];
		const expected = [4, 4, 7, 2, 5, 2, 1];
		const result = util.makeDistrTable(...numbers);
		for (let i = 0; i <= 6; i++) {
			should.equal(result[i], expected[i]);
		}
	});
});
