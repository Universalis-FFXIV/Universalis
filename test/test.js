const assert   = require("assert");
const fs       = require("fs");
const isEqual  = require("lodash.isequal");
const request  = require("request-promise");
const should   = require("should");
const util     = require("util");

const readFile = util.promisify(fs.readFile);

const universalis = "http://localhost:3000";

const now = Date.now();
const updateTimeout = 500; // The time allowed for the server to process data before the test checks the outcome

describe("The upload process:", function() {
    describe("A price listing upload:", function() {
        it("should save the listing properties to a file.", function() {
            return new Promise(async (resolve, reject) => {
                let uploadData = {
                    worldID: 74,
                    itemID: 26465,
                    listings: [{
                        hq: 1,
                        materia: [5666, 5666],
                        pricePerUnit: 9999,
                        quantity: 1,
                        retainerName: "Retainername",
                        retainerCity: "Kugane",
                        creatorName: "Creator Name"
                    }]
                };

                request.post(`${universalis}/upload`, {
                    json: uploadData
                }, (err, res) => {
                    if (err) throw err;

                    setTimeout(async () => {
                        let savedData = JSON.parse(await request(`${universalis}/api/74/26465/`));

                        resolve(isEqual(uploadData.listings, savedData.listings));
                    }, updateTimeout);
                }).catch((err) => { if (err.statusCode !== 404) console.error(err); }); // This throws a 404 but continues correctly?
            }).should.eventually.be.exactly(true);
        });
    });

    describe("A history entry upload:", function() {
        it("should save the entry properties to a file.", function() {
            return new Promise(async (resolve, reject) => {
                let uploadData = {
                    worldID: 74,
                    itemID: 26465,
                    entries: [{
                        hq: 1,
                        pricePerUnit: 9999,
                        quantity: 1,
                        buyerName: "Buyer Name",
                        timestamp: now
                    }]
                };

                request.post(`${universalis}/upload`, {
                    json: uploadData
                }, (err, res) => {
                    if (err) throw err;

                    setTimeout(async () => {
                        let savedData = JSON.parse(await request(`${universalis}/api/74/26465/`));

                        resolve(isEqual(uploadData.entries, savedData.recentHistory))
                    }, updateTimeout);
                }).catch((err) => { if (err.statusCode !== 404) console.error(err); });
            }).should.eventually.be.exactly(true);
        });
        it("should save minimized entries to an extended history file.", function() {
            return new Promise(async (resolve, reject) => {
                let uploadData = {
                    worldID: 74,
                    itemID: 26465,
                    entries: [{
                        hq: 1,
                        pricePerUnit: 9999,
                        quantity: 1,
                        buyerName: "Buyer Name",
                        timestamp: now
                    }]
                };

                request.post(`${universalis}/upload`, {
                    json: uploadData
                }, (err, res) => {
                    if (err) throw err;

                    setTimeout(async () => {
                        let savedData = JSON.parse(await request(`${universalis}/api/history/74/26465/`));

                        uploadData.entries = uploadData.entries.map((entry) => {
                            delete entry.buyerName;
                            delete entry.quantity;
                            return entry;
                        });

                        resolve(isEqual(uploadData.entries, savedData.entries.slice(0, uploadData.entries.length)))
                    }, updateTimeout);
                }).catch((err) => { if (err.statusCode !== 404) console.error(err); });
            }).should.eventually.be.exactly(true);
        });
    });
});
