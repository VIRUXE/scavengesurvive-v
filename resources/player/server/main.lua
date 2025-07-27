local showWelcomeCard = require 'server.auth.welcomeCard'

Players = {}

AddEventHandler('playerConnecting', function(name, setKickReason, deferrals)
    deferrals.defer()

    local playerId = source
    local player = User:new(playerId)

    Players[playerId] = player

    deferrals.update('Checking your account status...')
    
    showWelcomeCard(deferrals, player)

    player:log('playerConnecting', 'is connecting.')
end)

-- A server-side event that is triggered when a player has a finally-assigned NetID.
-- https://docs.fivem.net/docs/scripting-reference/events/server-events/#playerjoining
AddEventHandler('playerJoining', function(oldPlayerId)
    oldPlayerId = tonumber(oldPlayerId) -- ? why tf would oldPlayerId be a string?
    local playerId = source
    local player = Players[oldPlayerId]

    player.Id = playerId

    Players[playerId] = player
---@diagnostic disable-next-line: need-check-nil
    Players[oldPlayerId] = nil -- Remove the temporary player object

    TriggerEvent('player:loggedIn', playerId) -- Now we're good

    player:log('playerJoining', 'is joining.')
end)

AddEventHandler('player:loggedIn', function(playerId)
    local player = Players[playerId]

    -- Ok let's tell the client to spawn at their last known position, if they have one
    -- TODO: I forgot to add the heading to the last position
    print(player.Account.last_position)
    TriggerClientEvent('player:spawn', playerId, json.decode(player.Account.last_position), 0.0)

    player.Spawned = true

    Player(playerId).state.username = player.Account.username

    player:log('playerLoggedIn', 'has logged in.')
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

    player:log('playerDropped', 'dropped')

    Players[playerId] = nil
end)

exports('Get', function(playerId)
    local player = Players[playerId]

    if not player then lib.print.error('Unknown player id: ' .. playerId) end
        
    return player
end)

-- * This is mainly for when this resource is restarted, so we can get the player's account from the database and re-add them to the Players table
for playerId in ipairs(GetPlayers()) do
    local playerUsername = User(playerId).state.username

    local account = MySQL.query.await('SELECT * FROM accounts WHERE username = ?', { playerUsername })

    if account then Players[playerId] = account end
end