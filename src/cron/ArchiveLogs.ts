import archiver from "archiver";
import fs from "fs";

export namespace ArchiveLogs {
    export const cronString = "0 0 0 0 0";

    export async function execute() {
        // Fetch old logs
        // Compress logs
        // Write out archive
        // Delete old logs
        return;
    }
}

export default ArchiveLogs;
