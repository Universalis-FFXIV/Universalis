const assert  = require("assert");
const clone   = require("lodash.clonedeep");
const fs      = require("fs");
const isEqual = require("lodash.isequal");
const request = require("request-promise");
const should  = require("should");
const util    = require("util");

const readFile = util.promisify(fs.readFile);

const universalis = "http://localhost:3000";

const now = Date.now();
const updateTimeout = 200; // The time allowed for the server to process data before the test checks the outcome

const listingUpload = {
    worldID: 74,
    itemID: 26465,
    listings: [{
        hq: 1,
        materia: [5666, 5666],
        pricePerUnit: 99999999,
        quantity: 1,
        retainerName: "Retainername",
        retainerCity: "Kugane",
        creatorName: "Creator Name"
    }]
};

const historyUpload = {
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

describe("The upload process:", function() {
    describe("A price listing upload:", function() {
        it("should save the listing properties to a file.", function() {
            return new Promise(async (resolve, reject) => {
                let uploadData = clone(listingUpload);

                request.post(`${universalis}/upload`, {
                    json: uploadData
                }, (err, res) => {
                    if (err) throw err;

                    setTimeout(async () => {
                        let savedData = JSON.parse(await request(`${universalis}/api/74/26465/`));
                        savedData.listings = savedData.listings.map((listing) => {
                            delete listing.total;
                            return listing;
                        });
                        resolve(isEqual(uploadData.listings, savedData.listings));
                    }, updateTimeout);
                }).catch((err) => { if (err.statusCode !== 404) console.error(err); }); // This throws a 404 but continues correctly?
            }).should.eventually.be.exactly(true);
        });
        it("should leave history entries intact.", function() {
            return new Promise(async (resolve, reject) => {
                let existingHistoryEntries = JSON.parse(await request(`${universalis}/api/74/26465/`)).recentHistory;

                let uploadData = clone(listingUpload);

                request.post(`${universalis}/upload`, {
                    json: uploadData
                }, (err, res) => {
                    if (err) throw err;

                    setTimeout(async () => {
                        let savedData = JSON.parse(await request(`${universalis}/api/74/26465/`));

                        resolve(isEqual(existingHistoryEntries, savedData.recentHistory));
                    }, updateTimeout);
                }).catch((err) => { if (err.statusCode !== 404) console.error(err); }); // This throws a 404 but continues correctly?
            }).should.eventually.be.exactly(true);
        });
    });

    describe("A history entry upload:", function() {
        it("should save the entry properties to a file.", function() {
            return new Promise(async (resolve, reject) => {
                let uploadData = clone(historyUpload);

                request.post(`${universalis}/upload`, {
                    json: uploadData
                }, (err, res) => {
                    if (err) throw err;

                    setTimeout(async () => {
                        let savedData = JSON.parse(await request(`${universalis}/api/74/26465/`));
                        savedData.recentHistory = savedData.recentHistory.map((entry) => {
                            delete entry.total;
                            return entry;
                        });
                        resolve(isEqual(uploadData.entries, savedData.recentHistory))
                    }, updateTimeout);
                }).catch((err) => { if (err.statusCode !== 404) console.error(err); });
            }).should.eventually.be.exactly(true);
        });
        it("should leave market listings intact.", function() {
            return new Promise(async (resolve, reject) => {
                let existingMarketListings = JSON.parse(await request(`${universalis}/api/74/26465/`)).listings;

                let uploadData = clone(historyUpload);

                request.post(`${universalis}/upload`, {
                    json: uploadData
                }, (err, res) => {
                    if (err) throw err;

                    setTimeout(async () => {
                        let savedData = JSON.parse(await request(`${universalis}/api/74/26465/`));

                        resolve(isEqual(existingMarketListings, savedData.listings));
                    }, updateTimeout);
                }).catch((err) => { if (err.statusCode !== 404) console.error(err); }); // This throws a 404 but continues correctly?
            }).should.eventually.be.exactly(true);
        });
        it("should save minimized entries to an extended history file.", function() {
            return new Promise(async (resolve, reject) => {
                let uploadData = clone(historyUpload);

                request.post(`${universalis}/upload`, {
                    json: uploadData
                }, (err, res) => {
                    if (err) throw err;

                    setTimeout(async () => {
                        let savedData = JSON.parse(await request(`${universalis}/api/history/74/26465/`));

                        uploadData.entries = uploadData.entries.map((entry) => {
                            return {
                                hq: entry.hq,
                                pricePerUnit: entry.pricePerUnit,
                                timestamp: entry.timestamp
                            };
                        });

                        resolve(isEqual(uploadData.entries, savedData.entries.slice(0, uploadData.entries.length)))
                    }, updateTimeout);
                }).catch((err) => { if (err.statusCode !== 404) console.error(err); });
            }).should.eventually.be.exactly(true);
        });
    });

    describe("All uploads:", function() {
        it("should correctly merge sources to create crossworld market data per data center.");
    });
});
