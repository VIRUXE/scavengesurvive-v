RegisterNetEvent('time:set', function(time)
    SetClockTime(math.floor(time / 100), time % 100, 0)

    lib.notify({
        title = 'Time',
        description = 'Time set to ' .. time,
        type = 'success'
    })
end)