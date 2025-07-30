const { SlashCommandBuilder, PermissionFlagsBits } = require('discord.js');

module.exports = {
    data: new SlashCommandBuilder()
        .setName('online')
        .setDescription('Shows the number of online players')
        .setDefaultMemberPermissions(PermissionFlagsBits.Administrator),
    
    async execute(interaction) {
        const onlineCount = GetNumPlayerIndices();
        const maxPlayers  = GetConvarInt('sv_maxclients', 32);

        await interaction.reply({ 
            content: `**Online Players:** ${onlineCount}/${maxPlayers}`, 
            ephemeral: true 
        });
    }
}; 