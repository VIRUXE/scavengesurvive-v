on('playerConnecting', (playerName, setKickReason, deferrals) => {
    const playerId = global.source;
    const player   = exports.player.Get(playerId);

    if (player) {
        sendMessage(`${player.name} (${player.Account.username}) has connected to the server.`, Config.Channels.Gameserver.Chat);
    }
});

on('playerDropped', (reason, resourceName, clientDropReason) => {
    const playerId   = global.source;
    const playerName = GetPlayerName(playerId);
    
    sendMessage(`${playerName} has disconnected from the server.`, Config.Channels.Gameserver.Chat);
    console.log(`Player ${playerName} dropped (Reason: ${reason}, Resource: ${resourceName}, Client Drop Reason: ${clientDropReason}).`);
});