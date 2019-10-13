import archiver from "archiver";
import fs from "fs";

export class ArchiveLogs {
    public cronString = "0 0 0 0 0";

    public async execute() {
        // Fetch old logs
        // Compress logs
        // Write out archive
        // Delete old logs
        return;
    }
}

export default ArchiveLogs;
