const assert  = require("assert");
const clone   = require("lodash.clonedeep");
const fs      = require("fs");
const isEqual = require("lodash.isequal");
const request = require("request-promise");
const should  = require("should");
const util    = require("util");

const readFile = util.promisify(fs.readFile);

const universalis = "http://localhost:4000";
const debugKey = "PyrDFpV2mBCfOUtXFByRELx7SZbzxuEfrY6zTExX";

const now = Date.now();
const updateTimeout = 1000; // The time allowed for the server to process data before the test checks the outcome

const flushedUpload = {
    worldID: 83,
    itemID: 1605,
    listings: [],
    recentHistory: []
};
const listingUpload = {
    worldID: 74,
    itemID: 26465,
    listings: [{
        hq: 1,
        lastReviewTime: 453,
        materia: [
            {slotID: 2, materiaID: 25},
            {slotID: 2, materiaID: 25}
        ],
        listingID: 6544789198,
        pricePerUnit: 999999999,
        quantity: 1,
        retainerID: 56418681861,
        retainerName: "Retainername",
        retainerCity: "Kugane",
        creatorName: "Creator Name",
        sellerID: 184467440551614
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
        buyerID: 184467440737,
        sellerID: 184467440738
    }],
    uploaderID: 456158163
};
const miuUpload = {
  worldID: 83,
  itemID: 1605,
  uploaderID: "0",
  listings: [
    {
      listingID: "0",
      hq: false,
      materia: [],
      pricePerUnit: 300,
      quantity: 1,
      total: 300,
      retainerID: "0",
      retainerName: "Mefus",
      retainerCity: "Ul'dah",
      creatorName: "",
      creatorID: "0",
      sellerID: "0",
      lastReviewTime: 64015,
      stainID: 0
    },
    {
      listingID: "0",
      hq: true,
      materia: [],
      pricePerUnit: 8000,
      quantity: 1,
      total: 8000,
      retainerID: "0",
      retainerName: "Keikoayano",
      retainerCity: "Ul'dah",
      creatorName: "",
      creatorID: "0",
      sellerID: "0",
      lastReviewTime: 51168,
      stainID: 0
    }
  ]
};

describe("The upload process:", function() {
    describe("A price listing upload:", function() {
        it("should leave history entries intact.");
        it("should save the listing properties to the database.", function() {
            return new Promise(async (resolve, reject) => {
                await request.post(`${universalis}/upload/${debugKey}`, { json: flushedUpload });

                let uploadData = clone(miuUpload);

                request.post(`${universalis}/upload/${debugKey}`, {
                    json: uploadData
                }, (err, res) => {
                    if (err) throw err;

                    setTimeout(async () => {
                        let savedData = JSON.parse(await request(`${universalis}/api/${uploadData.worldID}/${uploadData.itemID}/`));

                        savedData.listings = savedData.listings.map((listing) => {
                            return {
                                pricePerUnit: listing.pricePerUnit,
                                quantity: listing.quantity,
                                retainerName: listing.retainerName,
                                creatorName: listing.creatorName,
                                lastReviewTime: listing.lastReviewTime,
                                stainID: listing.stainID
                            }
                        });

                        uploadData.listings = uploadData.listings.map((listing) => {
                            return {
                                pricePerUnit: listing.pricePerUnit,
                                quantity: listing.quantity,
                                retainerName: listing.retainerName,
                                creatorName: listing.creatorName,
                                lastReviewTime: listing.lastReviewTime,
                                stainID: listing.stainID
                            }
                        });

                        let isEquivalent = isEqual(uploadData.listings, savedData.listings);

                        if (!isEquivalent) {
                            console.log(uploadData.listings);
                            console.log(savedData.listings);
                        }

                        resolve(isEquivalent);
                    }, updateTimeout);
                })
            }).should.eventually.be.exactly(true);
        });
        it("should correctly merge sources to create crossworld market listings per data center.");
    });

    describe("A history entry upload:", function() {
        it("should leave market listings intact.");
        it("should save the entry properties to the database.");
        it("should save minimized entries to the extended history database.");
        it("should correctly merge sources to create crossworld market history per data center.");
        it("should merge crossworld minimized entries into the extended history database.");
    });

    it("should not throw errors.", function() {
        return new Promise(async (resolve, reject) => {
            let uploadData = clone(listingUpload);

            let existingListings = JSON.parse(await request(`${universalis}/api/${uploadData.worldID}/${uploadData.itemID}/`)).listings;

            request.post(`${universalis}/upload/${debugKey}`, {
                json: uploadData
            }, (err, res) => {
                setTimeout(async () => {
                    resolve(!err);
                }, updateTimeout);
            });
        }).should.eventually.be.exactly(true);
    });
});
