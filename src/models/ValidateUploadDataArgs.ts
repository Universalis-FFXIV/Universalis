import { Context } from "koa";

import { BlacklistManager } from "../db/BlacklistManager";
import { CharacterContentIDUpload } from "./CharacterContentIDUpload";
import { MarketBoardListingsUpload } from "./MarketBoardListingsUpload";
import { MarketBoardSaleHistoryUpload } from "./MarketBoardSaleHistoryUpload";
import { MarketTaxRatesUpload } from "./MarketTaxRatesUpload";

export interface ValidateUploadDataArgs {
    blacklistManager: BlacklistManager;
    ctx: Context;
    uploadData: CharacterContentIDUpload &
                MarketBoardListingsUpload &
                MarketBoardSaleHistoryUpload &
                MarketTaxRatesUpload;
}
