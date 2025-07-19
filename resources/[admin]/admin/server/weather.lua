lib.addCommand('weather', {
    help = 'Change the weather',
    params = {
        {
            name = 'weather',
            help = 'The weather to change to',
            type = 'string',
        }
    },
    restricted = 'group.admin'
}, function(source, args, rawCommand)
    local weather = args.weather

    if not weather then
        lib.notify(source, {
            title = 'Weather',
            description = 'Usage: /weather <weather>',
            type = 'error'
        })
        return
    end

    local weathers = {
        'CLEAR',
        'EXTRASUNNY',
        'CLOUDS',
        'OVERCAST',
        'RAIN',
        'CLEARING',
        'THUNDER',
        'SMOG',
        'FOGGY',
        'XMAS',
        'SNOW',
        'SNOWLIGHT',
        'BLIZZARD',
        'HALLOWEEN',
        'NEUTRAL',
        'RAIN_HALLOWEEN',
        'SNOW_HALLOWEEN',
    }

    if not lib.table.contains(weathers, weather) then
        lib.notify(source, {
            title = 'Weather',
            description = 'Invalid weather',
            type = 'error'
        })
        return
    end

    TriggerClientEvent('weather:set', -1, weather)
end)