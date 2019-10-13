import { Context } from "koa";

import { BlacklistManager } from "../db/BlacklistManager";
import { CharacterContentIDUpload } from "./CharacterContentIDUpload";
import { MarketBoardListingsUpload } from "./MarketBoardListingsUpload";
import { MarketBoardSaleHistoryUpload } from "./MarketBoardSaleHistoryUpload";

export interface ValidateUploadDataArgs {
    blacklistManager: BlacklistManager;
    ctx: Context;
    uploadData: CharacterContentIDUpload & MarketBoardListingsUpload & MarketBoardSaleHistoryUpload;
}
