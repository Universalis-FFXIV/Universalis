import { Context } from "koa";

import { BlacklistManager } from "../db/BlacklistManager";
import { RemoteDataManager } from "../remote/RemoteDataManager";
import { GenericUpload } from "./GenericUpload";

export interface ValidateUploadDataArgs {
	blacklistManager: BlacklistManager;
	remoteDataManager: RemoteDataManager;
	ctx: Context;
	uploadData: GenericUpload;
}
