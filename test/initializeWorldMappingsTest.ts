import * as should from "should";

import { initializeWorldMappings } from "../src/initializeWorldMappings";

describe("initializeWorldMappings", function () {
	let worldMap: Map<string, number>;
	let worldIdMap: Map<number, string>;

	before(async function () {
		worldMap = new Map();
		worldIdMap = new Map();
		await initializeWorldMappings(worldMap, worldIdMap);
	});

	const makeTest = (id: number, name: string, expectedSuccess: boolean) => {
		it(`should ${
			expectedSuccess ? "" : "not "
		}register ${id} (${name}).`, function () {
			should.equal(worldMap.get(name) != null, expectedSuccess);
			should.equal(worldIdMap.get(id) != null, expectedSuccess);
		});
	};

	makeTest(74, "Coeurl", true);
	makeTest(25, "Chaos", false);
	makeTest(1, "reserved1", false);
	makeTest(6, "c-funereus", false);
	makeTest(1169, "YanXia", true);
	makeTest(117, "e-contents", false);
	makeTest(0, "crossworld", false);
	makeTest(100, "dev-test", false);
});
