import archiver from "archiver";
import { CronJob } from "cron";
import fs from "fs";
import path from "path";
import util from "util";

import localUtil from "../util";

const readdir = util.promisify(fs.readdir);
const readFile = util.promisify(fs.readFile);
const unlink = util.promisify(fs.unlink);
const writeFile = util.promisify(fs.writeFile);

export abstract class Tracker {
    protected data: Map<number, { worldID: number, data: any[]}>;
    protected ext: string;
    protected storageLocation: string;
    private scoringJob: CronJob;

    constructor(storageLocation: string, ext: string) {
        this.data = new Map();
        this.ext = ext;
        this.storageLocation = storageLocation;
        // this.scoringJob = new CronJob("* * * * */5", this.scoreAndUpdate, null, true);

        if (!fs.existsSync(path.join(__dirname, storageLocation))) {
            fs.mkdirSync(path.join(__dirname, storageLocation));
        }
    }

    public get(id: number) {
        return this.data.get(id);
    }

    public abstract set(...params);

    private async scoreAndUpdate() {
        let directoryTree = await this.deepSearch(path.join(__dirname, this.storageLocation));
        for (let folder in directoryTree) {
            if (directoryTree.hasOwnProperty(folder)) {
                let liveFileContents: string = (await readFile(directoryTree[folder][0])).toString();
                let files: string[] = directoryTree[folder].slice(1);

                if (files.length <= 0) return;

                let scores: number[] = [];

                // Scoring
                for (let file of files) {
                    let fileContents = (await readFile(file)).toString();
                    // The most similar file to the live one will be considered most likely to be true
                    // (inaccurate, TODO)
                    scores.push(localUtil.levenshtein(liveFileContents, fileContents));
                }

                // Zip failed branches
                let bestFile: string = files[scores.indexOf(Math.min(...scores))];
                let bestFileData: Buffer = await readFile(bestFile);
                directoryTree[folder].splice(directoryTree[folder].indexOf(bestFile));
                let output = fs.createWriteStream(path.join(__dirname, `${new Date()}.zip`));
                let archive = archiver("zip", {
                    zlib: { level: 9 },
                });
                archive.glob(path.join(directoryTree[folder], `*${this.ext}`));
                await archive.finalize();
                archive.pipe(output);
                output.end();

                // Delete all branches
                for (let file of directoryTree[folder]) {
                    await unlink(file);
                }

                // Write new live branch
                await writeFile(bestFile, bestFileData);
            }
        }
    }

    private async deepSearch(folderPath: string, content: object = {}) {
        let folderContents = await readdir(folderPath);
        for (let item of folderContents) {
            if (item.indexOf(this.ext) !== -1) {
                if (!content[folderPath]) content[folderPath] = [];
                content[folderPath].push(item);
            } else {
                await this.deepSearch(path.join(folderPath, item), content);
            }
        }
        return content;
    }
}
