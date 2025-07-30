const fs = require('fs');
const path = require('path');
const { REST, Routes } = require('discord.js');

const commands = [];
const commandsPath = path.join('resources', 'discord', 'commands');
const commandFiles = fs.readdirSync(commandsPath).filter(file => file.endsWith('.js'));

console.log(commandFiles);

console.log('Current working directory:', process.cwd());


for (const file of commandFiles) {
    const filePath = path.join(commandsPath, file);
    const command = require(filePath);
    
    if ('data' in command && 'execute' in command) {
        commands.push(command.data.toJSON());
    } else {
        console.log(`[WARNING] The command at ${filePath} is missing a required "data" or "execute" property.`);
    }
}

if (!Config.Token) {
    console.error('Discord bot token not found. Please set discord_bot_token convar.');
    process.exit(1);
}

if (!Config.ClientID) {
    console.error('Discord client ID not found. Please set discord_client_id convar.');
    process.exit(1);
}

const rest = new REST({ version: '10' }).setToken(Config.Token);

(async () => {
    try {
        console.log('Started refreshing application (/) commands.');

        // Register commands for a specific guild (faster for development)
        await rest.put(
            Routes.applicationGuildCommands(Config.ClientID, Config.GuildID),
            { body: commands },
        );

        console.log('Successfully reloaded application (/) commands.');
    } catch (error) {
        console.error(error);
    }
})(); 