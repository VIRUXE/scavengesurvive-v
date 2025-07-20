lib.addCommand('respawn', {
        help = 'Respawn a player',
        params = {
            {
                name = 'player',
                help = 'The player to respawn',
                type = 'playerId'
            }
        }
    },
    function(source, args, rawCommand)
        TriggerClientEvent('player:respawn', args.player)
    end
)