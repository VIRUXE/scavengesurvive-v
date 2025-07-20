local spawnedPlayers = {}

-- Handle player joining
AddEventHandler('playerConnecting', function(name, setKickReason, deferrals)
    deferrals.defer()
    deferrals.update('Connecting to server...')
    deferrals.done()
end)

-- Handle player joining
AddEventHandler('playerJoining', function()
    spawnedPlayers[source] = false
end)

RegisterNetEvent('player:spawned', function()
    local playerId = source
    spawnedPlayers[playerId] = true
    
    print('^2[SPAWN]^7 Player ' .. GetPlayerName(playerId) .. ' spawned at (0, 0, 75)')
end)

AddEventHandler('playerDropped', function()
    spawnedPlayers[source] = nil
end)

-- Command to force respawn a player
--[[ RegisterCommand('respawn', function(source, args, rawCommand)
    local targetId = tonumber(args[1]) or source
    
    if GetPlayerName(targetId) then
        TriggerClientEvent('player:respawn', targetId)
        print('^3[SPAWN]^7 Forced respawn for ' .. GetPlayerName(targetId))
    else
        TriggerClientEvent('chat:addMessage', source, {
            color = {255, 0, 0},
            multiline = true,
            args = {'System', 'Player not found'}
        })
    end
end, false) ]]

-- Command to check spawn status
RegisterCommand('spawnstatus', function(source, args, rawCommand)
    local playerId = source
    local status = spawnedPlayers[playerId] and 'Spawned' or 'Not spawned'
    
    TriggerClientEvent('chat:addMessage', playerId, {
        color = {0, 255, 0},
        multiline = true,
        args = {'Spawn Status', status}
    })
end, false)

-- Export function to check if player is spawned
--[[ exports('isPlayerSpawned', function(playerId)
    return spawnedPlayers[playerId] or false
end) ]]

-- Export function to get all spawned players
--[[ exports('getSpawnedPlayers', function()
    return spawnedPlayers
end) ]]