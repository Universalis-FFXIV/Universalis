import { Context } from "koa";

import { BlacklistManager } from "../db/BlacklistManager";
import { FlaggedUploadManager } from "../db/FlaggedUploadManager";
import { UniversalisDiscordClient } from "../discord";
import { RemoteDataManager } from "../remote/RemoteDataManager";
import { GenericUpload } from "./GenericUpload";

export interface ValidateUploadDataArgs {
	blacklistManager: BlacklistManager;
	flaggedUploadManager: FlaggedUploadManager;
	remoteDataManager: RemoteDataManager;
	ctx: Context;
	uploadData: GenericUpload;
	discord: UniversalisDiscordClient;
}
