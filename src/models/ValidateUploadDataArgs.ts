import { Context } from "koa";

import { BlacklistManager } from "../db/BlacklistManager";
import { GenericUpload } from "./GenericUpload";

export interface ValidateUploadDataArgs {
    blacklistManager: BlacklistManager;
    ctx: Context;
    uploadData: GenericUpload;
}
