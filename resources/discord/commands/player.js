const { SlashCommandBuilder, PermissionFlagsBits } = require('discord.js');

module.exports = {
    data: new SlashCommandBuilder()
        .setName('player')
        .setDescription('Shows information about a specific player')
        .addStringOption(option =>
            option.setName('name')
                .setDescription('The name of the player to look up')
                .setRequired(true))
        .setDefaultMemberPermissions(PermissionFlagsBits.Administrator),
    
    async execute(interaction) {
        const playerName = interaction.options.getString('name');
        const players = GetPlayers();
        const player = players.find(p => GetPlayerName(p).toLowerCase().includes(playerName.toLowerCase()));
        
        if (player) {
            const playerInfo = `**Player Information**\n` +
                `Name: ${GetPlayerName(player)}\n` +
                `ID: ${player}\n` +
                `Ping: ${GetPlayerPing(player)}ms`;
            await interaction.reply({ content: playerInfo, ephemeral: true });
        } else {
            await interaction.reply({ content: `Player "${playerName}" not found.`, ephemeral: true });
        }
    }
}; 