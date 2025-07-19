lib.addCommand('time', {
    help = 'Change the time',
    params = {
        {
            name = 'time',
            help = 'The time to change to',
            type = 'number',
        }
    },
    restricted = 'group.admin'
}, function(source, args, rawCommand)
    local time = args.time

    if not time then
        lib.notify(source, {
            title = 'Time',
            description = 'Usage: /time <time>',
            type = 'error'
        })
        return
    end

    TriggerClientEvent('time:set', -1, time)
end)