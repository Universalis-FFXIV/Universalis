import { CronJob } from "cron";

import { Collection } from "mongodb";

export abstract class Tracker {
	protected collection: Collection;
	private scoringJob: CronJob;

	constructor(collection: Collection) {
		this.collection = collection;
		// this.scoringJob = new CronJob("* * * * */5", this.scoreAndUpdate, null, true);
	}

	public abstract set(...params);
}
