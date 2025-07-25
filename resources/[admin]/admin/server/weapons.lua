local requests = {}

RegisterNetEvent('admin:giveWeapon', function(weaponHash, ammo)
    local playerId = source
    
    if not requests[playerId] then
        lib.logger(source, 'admin:giveWeapon', 'Tried to trigger illegally')
        return
    end
    
    local targetPlayerId   = requests[playerId]
    local targetPlayerName = GetPlayerName(targetPlayerId)
    local weaponName       = exports.game:GetWeaponName(weaponHash)

    GiveWeaponToPed(GetPlayerPed(targetPlayerId), weaponHash, ammo, false, true)

    if targetPlayerId ~= playerId then
        lib.notify(playerId, {
            title = 'Weapon Given',
            description = ('You gave %s to %s.'):format(weaponName, targetPlayerName),
            type = 'success'
        })
    end

    lib.notify(targetPlayerId, {
        title = 'Weapon Received',
        description = ('You have received a %s.'):format(weaponName),
        type = 'info'
    })

    requests[playerId] = nil

    lib.logger(playerId, 'admin:giveWeapon', ('Gave %s to %s.'):format(weaponName, targetPlayerName))
end)

lib.addCommand('giveweapon', {
    help = 'Give a weapon to a player',
    params = {
        {
            name = 'player',
            type = 'playerId',
            help = 'The player to give the weapon to'
        }
    },
    restricted = 'group.admin'
}, function(playerId, args, rawCommand)
    requests[playerId] = args.player

    TriggerClientEvent('admin:showWeaponsMenu', playerId)
end)

-- Cleanup requests when a player leaves
AddEventHandler('playerDropped', function(playerId) requests[playerId] = nil end)