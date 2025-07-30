const { SlashCommandBuilder, PermissionFlagsBits } = require('discord.js');

module.exports = {
    data: new SlashCommandBuilder()
        .setName('server')
        .setDescription('Shows server information')
        .setDefaultMemberPermissions(PermissionFlagsBits.Administrator),
    
    async execute(interaction) {
        const serverInfo = `**Server Information**\n` +
            `Players: ${GetNumPlayerIndices()}/${GetConvarInt('sv_maxclients', 32)}\n` +
            `Uptime: ${GetGameTimer() / 1000 / 60} minutes`;
        await interaction.reply({ content: serverInfo, ephemeral: true });
    }
}; 