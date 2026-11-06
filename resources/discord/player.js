on('playerConnecting', (playerName, setKickReason, deferrals) => {
    sendMessage(`${playerName} has connected to the server.`, Config.Channels.Gameserver.Chat);
});

on('playerAuthenticated', (playerId) => {
    const name = GetPlayerName(playerId);
    const username = Player(playerId).state.username;
    const who = username ? `${name} (${username})` : name;

    sendMessage(`${who} has joined the server.`, Config.Channels.Gameserver.Chat);
});

on('playerDropped', (reason, resourceName, clientDropReason) => {
    const playerId   = global.source;
    const playerName = GetPlayerName(playerId);

    sendMessage(`${playerName} has disconnected from the server.`, Config.Channels.Gameserver.Chat);
    console.log(`Player ${playerName} dropped (Reason: ${reason}, Resource: ${resourceName}, Client Drop Reason: ${clientDropReason}).`);
});