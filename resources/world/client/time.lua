RegisterNetEvent('time:set', function(time)
    NetworkOverrideClockTime(time, 0, 0)

    lib.notify({
        title = 'Time',
        description = 'Time set to ' .. time,
        type = 'success'
    })
end)