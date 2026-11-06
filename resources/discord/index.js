const { Client, GatewayIntentBits, Partials, ActivityType } = require('discord.js');


const client = new Client({
    intents: [
        GatewayIntentBits.Guilds,
        GatewayIntentBits.GuildMessages
    ],
    partials: [Partials.Channel]
});

client.once('ready', () => {
    console.log(`Logged in as ${client.user.tag}`);
    client.user.setActivity('Scavenge and Survive V', { type: ActivityType.Playing });

    sendMessage('I\'m back!', Config.Channels.Staff.Staff);
});

client.on('messageCreate', message => {
    if (message.author.bot) return;
    // if (message.content === '!ping') message.reply('Pong!');
});

// Handle slash command interactions
client.on('interactionCreate', async interaction => {
    if (!interaction.isChatInputCommand()) return;

    const { commandName } = interaction;

    try {
        // Load and execute the command
        await require(`./commands/${commandName}.js`).execute(interaction);
    } catch (error) {
        console.error(`Error executing command ${commandName}:`, error);
        await interaction.reply({ 
            content: 'There was an error while executing this command!', 
            ephemeral: true 
        });
    }
});

client.login(Config.Token).catch((error) => {
    console.error('Discord login failed: ' + (error.message || error.code || 'request failed'));
});

function sendMessage(message, channelId) {
    const channel = client.channels.cache.get(channelId);
    if (!channel) {
        console.error('Discord channel ' + channelId + ' is not available.');
        return;
    }
    channel.send(message).catch((error) => {
        console.error('Discord send failed: ' + (error.message || error.code || 'request failed'));
    });
}

global.sendMessage = sendMessage;
exports('sendMessage', sendMessage);
