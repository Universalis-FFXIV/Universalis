import { Client, Message, MessageEmbed, TextChannel } from 'discord.js';
import { Logger } from 'winston';
import { BlacklistManager } from '../db/BlacklistManager';
import { FlaggedUploadManager } from '../db/FlaggedUploadManager';

export class UniversalisDiscordClient {
    public static async create(token: string, logger: Logger, blacklistManager: BlacklistManager, flaggedUploadManager: FlaggedUploadManager): Promise<UniversalisDiscordClient> {
        const discord = new UniversalisDiscordClient(new Client(), logger, blacklistManager, flaggedUploadManager);

        await discord.client.login(token);
        discord.alertsChannel = await discord.client.channels.fetch("868169460983431178") as TextChannel;

        return discord;
    }

    private client: Client;
    private logger: Logger;
    private blacklistManager: BlacklistManager;
    private flaggedUploadManager: FlaggedUploadManager;

    private alertsChannel: TextChannel;

    private constructor(client: Client, logger: Logger, blacklistManager: BlacklistManager, flaggedUploadManager: FlaggedUploadManager) {
        this.client = client;
        this.logger = logger;
        this.blacklistManager = blacklistManager;
        this.flaggedUploadManager = flaggedUploadManager;

        this.client.once("ready", () => {
            this.logger.info("Discord client logged-in.");
        });

        this.client.on("message", this.onMessage);

        this.logger.info(`${this.flaggedUploadManager == null}`);
    }

    public async sendUploadAlert(data: string) {
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
                return message.reply("no uploader ID was supplied.");
            }

            await this.blacklistManager.add(args[0]);
            return message.reply("Uploader blocked.");
        }

        if (command === "!unblock") {
            if (args.length < 1) {
                return message.reply("no uploader ID was supplied.");
            }

            await this.blacklistManager.remove(args[0]);
            return message.reply("Uploader unblocked.");
        }

        if (command === "!flag") {
            if (args.length < 2) {
                return message.reply("expected world ID, item ID, and optionally listings.");
            }

            await this.flaggedUploadManager.add(parseInt(args.shift()), parseInt(args.shift()), args.length === 0 ? null : JSON.parse(args.join(" ")));
            return message.reply("Pattern flagged.");
        }

        if (command === "!unflag") {
            if (args.length < 2) {
                return message.reply("expected world ID, item ID, and optionally listings.");
            }

            await this.flaggedUploadManager.add(parseInt(args.shift()), parseInt(args.shift()), args.length === 0 ? null : JSON.parse(args.join(" ")));
            return message.reply("Pattern unflagged.");
        }
    }
}