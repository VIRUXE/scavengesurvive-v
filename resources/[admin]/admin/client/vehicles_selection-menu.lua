RegisterNetEvent('admin:showVehicleSelectionMenu', function(vehicles)
    local options = {}

    for _, vehicle in ipairs(vehicles) do
        local option = {
            title = vehicle.DisplayName or 'Unknown',
            image = ('https://docs.fivem.net/vehicles/%s.webp'):format(vehicle.ModelName),
            onSelect = function()
                TriggerServerEvent('admin:spawnVehicle', vehicle)
            end
        }

        if vehicle.RealName then option.description = vehicle.RealName end

        table.insert(options, option)
    end

    lib.registerContext({
        id = 'admin_vehicle_spawn',
        title = 'Vehicle Spawner',
        options = options,
    })

    lib.showContext('admin_vehicle_spawn')
end) 