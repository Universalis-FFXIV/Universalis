import { Logger } from "winston";

export interface RemoteDataManagerOptions {
    exts?: string[];
    logger: Logger;
    remoteFileDirectory?: string;
}
