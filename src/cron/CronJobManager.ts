import { CronJob } from "cron";
import { Logger } from "winston";

import { CronJobManagerOptions } from "../models/CronJobManagerOptions";

import ArchiveLogs from "./jobs/ArchiveLogs";

const cronJobObjects = {
	ArchiveLogs,
};

export class CronJobManager {
	private cronJobs: Map<string, CronJob>;
	private logger: Logger;

	constructor(options: CronJobManagerOptions) {
		this.cronJobs = new Map();
		this.logger = options.logger;
		for (const namespace in cronJobObjects) {
			if (cronJobObjects.hasOwnProperty(namespace)) {
				const objectBody = cronJobObjects[namespace];
				this.cronJobs.set(
					namespace,
					new CronJob(objectBody.cronString, objectBody.execute),
				);
			}
		}
	}

	/** Start all cron jobs. */
	public startAll(): void {
		this.cronJobs.forEach((cronJob) => {
			cronJob.start();
		});
		this.logger.info("Started all cron jobs.");
	}

	/** Start a specified cron job. */
	public start(namespace: string): void {
		const cronJob = this.cronJobs.get(namespace);
		if (!cronJob) {
			this.logger.error("No cron job exists with name " + namespace + ".");
			return;
		}
		cronJob.start();
		this.logger.info("Started cron job " + namespace + ".");
	}

	/** Stop a specified cron job. */
	public stop(namespace: string): void {
		const cronJob = this.cronJobs.get(namespace);
		if (!cronJob) {
			this.logger.error("No cron job exists with name " + namespace + ".");
			return;
		}
		cronJob.stop();
		this.logger.info("Stopped cron job " + namespace + ".");
	}
}
