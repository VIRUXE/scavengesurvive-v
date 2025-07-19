RegisterNetEvent('weather:set', function(weather)
    SetWeatherTypeNowPersist(weather) -- Let's persist for now

    lib.notify({
        title = 'Weather',
        description = 'Weather set to ' .. weather,
        type = 'success'
    })
end)