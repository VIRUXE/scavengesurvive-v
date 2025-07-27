---@diagnostic disable: param-type-mismatch, need-check-nil

local showWelcomeCard = require 'server.auth.welcomeCard'

Players = {}

-- Handle player joining
AddEventHandler('playerConnecting', function(name, setKickReason, deferrals)
    deferrals.defer()

    local playerId = source
    local player = Player:new(playerId)

    Players[playerId] = player

    deferrals.update('Checking your account status...')
    
    showWelcomeCard(deferrals, player)

    lib.print.debug(('Player %s (%d) is connecting'):format(name, playerId))
end)

-- A server-side event that is triggered when a player has a finally-assigned NetID.
-- https://docs.fivem.net/docs/scripting-reference/events/server-events/#playerjoining
AddEventHandler('playerJoining', function(oldPlayerId)
    local playerId = source
    local player = Players[tonumber(oldPlayerId)] -- why tf would oldPlayerId be a string?

    player.Id = playerId

    Players[playerId] = player
    Players[oldPlayerId] = nil -- Remove the temporary player object

    TriggerEvent('player:loggedIn', playerId) -- Now we're good

    lib.print.debug(('Player %s (%d) is joining'):format(player.Name, playerId))
end)

AddEventHandler('player:loggedIn', function(playerId)
    local player = Players[playerId]

    -- Ok let's tell the client to spawn at their last known position, if they have one
    -- TODO: I forgot to add the heading to the last position
    print(player.Account.last_position)
    TriggerClientEvent('player:spawn', playerId, json.decode(player.Account.last_position), 0.0)

    player.Spawned = true

    lib.print.debug(('Player %s (%d) logged in.'):format(player.Name, playerId))
end)

AddEventHandler('playerDropped', function()
    local playerId = source
    local player = Players[playerId]

    if player?.Account then -- A player might not have logged in yet when they exited
        local playerCoords = player:GetCoords()
        local playerHeading = player:GetHeading()

        MySQL.update('UPDATE accounts SET last_position = ? WHERE id = ?', {
            json.encode(playerCoords),
            player.Account.id
        })
    end

    Players[playerId] = nil
end)