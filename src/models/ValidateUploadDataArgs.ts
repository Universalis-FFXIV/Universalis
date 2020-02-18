import { Context } from "koa";

import { BlacklistManager } from "../db/BlacklistManager";
import { GenericUpload } from "./GenericUpload";
import { RemoteDataManager } from "../remote/RemoteDataManager";

export interface ValidateUploadDataArgs {
    blacklistManager: BlacklistManager;
    remoteDataManager: RemoteDataManager;
    ctx: Context;
    uploadData: GenericUpload;
}
