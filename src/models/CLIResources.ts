import { Collection } from "mongodb";

import { TrustedSourceManager } from "../db/TrustedSourceManager";

export interface CLIResources {
    extendedHistory: Collection;
    recentData: Collection;
    trustedSources: TrustedSourceManager;
}
