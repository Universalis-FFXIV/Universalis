import { Client, Message, MessageEmbed, TextChannel } from 'discord.js';
import { Logger } from 'winston';
import { BlacklistManager } from '../db/BlacklistManager';
import { FlaggedUploadManager } from '../db/FlaggedUploadManager';
import { sleep } from '../util';

export class UniversalisDiscordClient {
    private client = new Client();
    private logger: Logger;
    private blacklistManager: BlacklistManager;
    private flaggedUploadManager: FlaggedUploadManager;

    private alertsChannel: TextChannel;
    private initialized = false;

    constructor(token: string, logger: Logger, blacklistManager: BlacklistManager, flaggedUploadManager: FlaggedUploadManager) {
        this.logger = logger;
        this.blacklistManager = blacklistManager;
        this.flaggedUploadManager = flaggedUploadManager;

        this.client.once("ready", () => {
            this.logger.info("Discord client logged-in.");
        })

        this.client.on("message", this.onMessage);

        this.client.login(token)
        .then(async () => {
            this.alertsChannel = await this.client.channels.fetch("868169460983431178") as TextChannel;
            this.initialized = true;
        });
    }

    public async sendUploadAlert(data: string) {
        await this.waitUntilInitialized();

        this.logger.info("Sending flagged upload to alerts channel.");
        
        const embed = new MessageEmbed()
            .setTitle("Upload flagged!")
            .setColor(0xFF0000)
            .setDescription("```\n" + data + "\n```");

        await this.alertsChannel.send(embed);
    }

    public destroy() {
        this.client.destroy();
    }

    private async onMessage(message: Message) {
        if (message.guild == null) return;

        const member = message.guild.member(message.author.id);
        if (!member.roles.cache.has("617151910251855873")) return;

        const args = message.content.split(" ");
        const command = args.shift().toLowerCase();

        if (command === "!block") {
            if (args.length < 1) {
                return message.reply("No uploader ID supplied.");
            }

            await this.blacklistManager.add(args[0]);
            return message.reply("Uploader blocked.");
        }

        if (command === "!unblock") {
            if (args.length < 1) {
                return message.reply("No uploader ID supplied.");
            }

            await this.blacklistManager.remove(args[0]);
            return message.reply("Uploader unblocked.");
        }

        if (command === "!flag") {
            if (args.length < 2) {
                return message.reply("Expected world ID, item ID, and optionally listings.");
            }

            await this.flaggedUploadManager.add(parseInt(args.shift()), parseInt(args.shift()), args.length === 0 ? null : JSON.parse(args.join(" ")));
            return message.reply("Pattern flagged.");
        }

        if (command === "!unflag") {
            await this.flaggedUploadManager.add(parseInt(args.shift()), parseInt(args.shift()), args.length === 0 ? null : JSON.parse(args.join(" ")));
            return message.reply("Pattern unflagged.");
        }
    }

    private async waitUntilInitialized() {
        while (!this.initialized) {
            await sleep(1000);
        }
    }
}