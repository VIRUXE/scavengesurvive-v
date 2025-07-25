local requests = {}

local function spawnVehicleForPlayer(playerId, vehicle)
    local playerPed = GetPlayerPed(playerId)

    -- Calculate the distance from the player's ped by checking the dimensions of the vehicle
    local min, max = lib.callback.await('game:getModelDimensions', playerId, vehicle.Hash)
    local size = max - min
    local radius = math.max(size.x, size.y, size.z) / 2 + 0.5

    -- The Y dimension is the length of the vehicle.
    -- We spawn it half its length in front of the player, plus a bit of extra space.
    local distanceFromPed = size.y / 2 + 1.0
    local playerForwardVector = lib.callback.await('player:getForwardVector', playerId)
    local spawnCoords = GetEntityCoords(playerPed) + playerForwardVector * distanceFromPed
    
    local nearbyVehicles = lib.getNearbyVehicles(spawnCoords, radius)
    local nearbyPlayers = lib.getNearbyPlayers(spawnCoords, radius)

    if #nearbyVehicles > 0 or #nearbyPlayers > 0 then
        return lib.notify(playerId, {
            title = 'Spawn Area Obstructed',
            description = 'The area is obstructed by another vehicle or player.',
            type = 'error'
        })
    end

    local x, y, z in spawnCoords

    CreateVehicleServerSetter(vehicle.Hash, vehicle.Type, x, y, z, GetEntityHeading(playerPed))

    -- TODO: Send staff notice

    lib.logger(playerId, 'admin:spawnVehicle', ('Spawned Vehicle (%s) at %.2f, %.2f, %.2f'):format(vehicle.ModelName, x, y, z))
end

RegisterNetEvent('admin:spawnVehicle', function(vehicle)
    local playerId = source

    if not lib.table.contains(requests, playerId) then
        return lib.logger(playerId, 'admin:spawnVehicle', 'Tried to trigger illegally')
    end

    spawnVehicleForPlayer(playerId, vehicle)

    if lib.table.contains(requests, playerId) then table.remove(requests, playerId) end
end)

lib.addCommand('veh', {
    help = 'Spawn a vehicle',
    params = {
        {
            name = 'vehicle',
            help = 'The vehicle to spawn',
            type = 'string'
        }
    }
}, function(playerId, args, rawCommand)
    local vehicles = exports.game:GetVehiclesByPartialName(args.vehicle)

    if not vehicles then
        lib.notify(playerId, {
            title = 'Vehicle Not Found',
            description = 'The vehicle you are looking for does not exist.',
            type = 'error'
        })
        return
    else
        -- If there's only one match then we can just spawn it straight away
        if #vehicles == 1 then
            spawnVehicleForPlayer(playerId, vehicles[1])
        else
            table.insert(requests, playerId)
            TriggerClientEvent('admin:showVehicleSelectionMenu', playerId, vehicles)
        end
    end
end)

AddEventHandler('playerDropped', function()
    if lib.table.contains(requests, source) then table.remove(requests, source) end
end)
 