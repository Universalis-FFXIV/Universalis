import { Collection } from "mongodb";

import { TrustedSourceManager } from "../TrustedSourceManager";

export interface CLIResources {
    extendedHistory: Collection;
    recentData: Collection;
    trustedSources: TrustedSourceManager;
}
