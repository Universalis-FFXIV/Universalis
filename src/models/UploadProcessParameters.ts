import { BlacklistManager } from "../db/BlacklistManager";
import { ContentIDCollection } from "../db/ContentIDCollection";
import { ExtraDataManager } from "../db/ExtraDataManager";
import { TrustedSourceManager } from "../db/TrustedSourceManager";
import { RemoteDataManager } from "../remote/RemoteDataManager";
import { HistoryTracker } from "../trackers/HistoryTracker";
import { PriceTracker } from "../trackers/PriceTracker";

import { ParameterizedContext } from "koa";
import { Logger } from "winston";

export interface UploadProcessParameters {
	ctx: ParameterizedContext;
	logger: Logger;

	worldIDMap: Map<number, string>;

	blacklistManager: BlacklistManager;
	contentIDCollection: ContentIDCollection;
	extraDataManager: ExtraDataManager;
	historyTracker: HistoryTracker;
	priceTracker: PriceTracker;
	trustedSourceManager: TrustedSourceManager;
	remoteDataManager: RemoteDataManager;
}
