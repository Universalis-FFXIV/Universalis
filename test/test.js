const assert  = require("assert");
const clone   = require("lodash.clonedeep");
const fs      = require("fs");
const isEqual = require("lodash.isequal");
const request = require("request-promise");
const should  = require("should");
const util    = require("util");

const readFile = util.promisify(fs.readFile);

const universalis = "http://localhost:3000";
const debugKey = "PyrDFpV2mBCfOUtXFByRELx7SZbzxuEfrY6zTExX";

const now = Date.now();
const updateTimeout = 500; // The time allowed for the server to process data before the test checks the outcome

const listingUpload = {
    worldID: 74,
    itemID: 26465,
    listings: [{
        hq: 1,
        materia: [
            {slotID: 0, itemID: 5666},
            {slotID: 1, itemID: 5666}
        ],
        pricePerUnit: 999999999,
        quantity: 1,
        retainerName: "Retainername",
        retainerCity: "Kugane",
        creatorName: "Creator Name",
        sellerID: 18446744073709551614
    }],
    uploaderID: 456158163
};

const historyUpload = {
    worldID: 74,
    itemID: 26465,
    entries: [{
        hq: 1,
        pricePerUnit: 99999,
        quantity: 1,
        buyerName: "Buyer Name",
        timestamp: now,
        buyerID: 18446744073709551614,
        sellerID: 18446744073709551614
    }],
    uploaderID: 456158163
};

describe("The upload process:", function() {
    describe("A price listing upload:", function() {
        it("should leave history entries intact.", function() {
            return new Promise(async (resolve, reject) => {
                let existingHistoryEntries = JSON.parse(await request(`${universalis}/api/74/26465/`)).recentHistory;

                let uploadData = clone(listingUpload);

                request.post(`${universalis}/upload/${debugKey}`, {
                    json: uploadData
                }, (err, res) => {
                    if (err) throw err;

                    setTimeout(async () => {
                        let savedData = JSON.parse(await request(`${universalis}/api/74/26465/`));

                        resolve(isEqual(existingHistoryEntries, savedData.recentHistory));
                    }, updateTimeout);
                })
            }).should.eventually.be.exactly(true);
        });
        it("should save the listing properties to a file.", function() {
            return new Promise(async (resolve, reject) => {
                let uploadData = clone(listingUpload);

                request.post(`${universalis}/upload/${debugKey}`, {
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
                })
            }).should.eventually.be.exactly(true);
        });
        it("should correctly merge sources to create crossworld market listings per data center.", function() {
            return new Promise(async (resolve, reject) => {
                let existingData = JSON.parse(await request(`${universalis}/api/Crystal/26465/`));
                if (existingData && existingData.listings)
                    existingData.listings = existingData.listings.filter((entry) => entry.worldName !== "Coeurl");
                else existingData = {
                    listings: []
                };

                let uploadData1 = clone(listingUpload);

                request.post(`${universalis}/upload/${debugKey}`, {
                    json: uploadData1
                }, (err, res) => {
                    if (err) throw err;

                    setTimeout(async () => {
                        let savedData = JSON.parse(await request(`${universalis}/api/Crystal/26465/`));

                        savedData.listings = savedData.listings.map((listing) => {
                            delete listing.worldName;
                            delete listing.total;
                            delete listing.uploaderID;
                            listing = JSON.stringify(listing);
                            return listing;
                        });

                        let testArray = uploadData1.listings.concat(existingData.listings)
                            .map((el) => { el = JSON.stringify(el); return el; });

                        resolve(testArray.every((el) => {
                            return savedData.listings.includes(el)
                        }));
                    }, updateTimeout);
                })
            }).should.eventually.be.exactly(true);
        });
    });

    describe("A history entry upload:", function() {
        it("should leave market listings intact.", function() {
            return new Promise(async (resolve, reject) => {
                let existingMarketListings = JSON.parse(await request(`${universalis}/api/74/26465/`)).listings;

                let uploadData = clone(historyUpload);

                request.post(`${universalis}/upload/${debugKey}`, {
                    json: uploadData
                }, (err, res) => {
                    if (err) throw err;

                    setTimeout(async () => {
                        let savedData = JSON.parse(await request(`${universalis}/api/74/26465/`));

                        resolve(isEqual(existingMarketListings, savedData.listings));
                    }, updateTimeout);
                })
            }).should.eventually.be.exactly(true);
        });
        it("should save the entry properties to a file.", function() {
            return new Promise(async (resolve, reject) => {
                let uploadData = clone(historyUpload);

                request.post(`${universalis}/upload/${debugKey}`, {
                    json: uploadData
                }, (err, res) => {
                    if (err) throw err;

                    setTimeout(async () => {
                        let savedData = JSON.parse(await request(`${universalis}/api/74/26465/`));
                        savedData.recentHistory = savedData.recentHistory.map((entry) => {
                            delete entry.total;
                            return entry;
                        });
                        resolve(isEqual(uploadData.entries, savedData.recentHistory));
                    }, updateTimeout);
                })
            }).should.eventually.be.exactly(true);
        });
        it("should save minimized entries to an extended history file.", function() {
            return new Promise(async (resolve, reject) => {
                let uploadData = clone(historyUpload);

                request.post(`${universalis}/upload/${debugKey}`, {
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

                        resolve(isEqual(uploadData.entries, savedData.entries.slice(0, uploadData.entries.length)));
                    }, updateTimeout);
                })
            }).should.eventually.be.exactly(true);
        });
        it("should correctly merge sources to create crossworld market history per data center.", function() {
            return new Promise(async (resolve, reject) => {
                let existingData = JSON.parse(await request(`${universalis}/api/Crystal/26465/`));
                if (existingData && existingData.recentHistory)
                    existingData.recentHistory = existingData.recentHistory.filter((entry) => entry.worldName !== "Coeurl");
                else existingData = {
                    recentHistory: []
                };

                let uploadData1 = clone(historyUpload);

                request.post(`${universalis}/upload/${debugKey}`, {
                    json: uploadData1
                }, (err, res) => {
                    if (err) throw err;

                    setTimeout(async () => {
                        let savedData = JSON.parse(await request(`${universalis}/api/Crystal/26465/`));

                        savedData.recentHistory = savedData.recentHistory.map((entry) => {
                            delete entry.worldName;
                            delete entry.total;
                            delete entry.uploaderID;
                            entry = JSON.stringify(entry);
                            return entry;
                        });

                        let testArray = uploadData1.entries.concat(existingData.recentHistory)
                            .map((el) => { el = JSON.stringify(el); return el; });

                        resolve(testArray.every((el) => {
                            return savedData.recentHistory.includes(el)
                        }));
                    }, updateTimeout);
                })
            }).should.eventually.be.exactly(true);
        });
        it("should merge crossworld minimized entries into an extended history file.", function() {
            return new Promise(async (resolve, reject) => {
                let uploadData = clone(historyUpload);

                request.post(`${universalis}/upload/${debugKey}`, {
                    json: uploadData
                }, (err, res) => {
                    if (err) throw err;

                    setTimeout(async () => {
                        let savedData = JSON.parse(await request(`${universalis}/api/history/Crystal/26465/`));

                        savedData.entries = savedData.entries.map((entry) => {
                            return {
                                hq: entry.hq,
                                pricePerUnit: entry.pricePerUnit,
                                timestamp: entry.timestamp
                            };
                        });

                        uploadData.entries = uploadData.entries.map((entry) => {
                            return {
                                hq: entry.hq,
                                pricePerUnit: entry.pricePerUnit,
                                timestamp: entry.timestamp
                            };
                        });

                        resolve(isEqual(uploadData.entries, savedData.entries.slice(0, uploadData.entries.length)));
                    }, updateTimeout);
                })
            }).should.eventually.be.exactly(true);
        });
    });
});
