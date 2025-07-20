lib.addCommand('giveweapon', {
    help = 'Give a weapon to a player',
    params = {
        {
            name = 'player',
            help = 'The player to give the weapon to',
            type = 'playerId'
        },
        {
            name = 'weapon',
            help = 'The weapon to give',
            type = 'string'
        },
        {
            name = 'ammo',
            help = 'The amount of ammo to give',
            type = 'number'
        }
    }
}, function(source, args, rawCommand)
    local targetPlayerId = args.player
    local weapon         = args.weapon

    GiveWeaponToPed(GetPlayerPed(targetPlayerId), GetHashKey(weapon), args.ammo, true, false)

    lib.notify(source, {
        title = 'Weapon given',
        description = 'You have given ' .. weapon .. ' to ' .. GetPlayerName(targetPlayerId),
        type = 'success'
    })
end)