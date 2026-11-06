RegisterNetEvent('time:set', function(time)
    NetworkOverrideClockTime(time, 0, 0)

    lib.print.info('Clock override: Time set to ' .. time)
end)