RegisterNetEvent('admin:giveWeapon', function(targetPlayerId, weaponHash, weaponName, ammo)
    GiveWeaponToPed(GetPlayerPed(targetPlayerId), weaponHash, ammo, false, true)

    lib.notify(source, {
        title = 'Weapon Given',
        description = ('You gave %s to %s.'):format(weaponName, GetPlayerName(targetPlayerId)),
        type = 'success'
    })

    lib.notify(targetPlayerId, {
        title = 'Weapon Received',
        description = ('You have received a %s.'):format(weaponName),
        type = 'info'
    })
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
}, function(source, args, rawCommand)
    TriggerClientEvent('admin:showWeaponsMenu', source, args.player)
end)