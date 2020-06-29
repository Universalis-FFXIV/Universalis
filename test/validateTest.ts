import * as should from "should";

import * as validate from "../src/validate";

describe("validate", function () {
	describe("isValidName", function () {
		it("should accept Chinese retainer names", function () {
			should.equal(validate.isValidName("音无结弦"), true);
		});

		it("should accept English retainer names", function () {
			should.equal(validate.isValidName("Nichiren"), true);
		});
	});
});
