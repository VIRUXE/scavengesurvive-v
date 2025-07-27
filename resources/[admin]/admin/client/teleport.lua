RegisterNetEvent('admin:teleportToMarker', function()
    local playerPed      = cache.ped
    local waypoint       = GetFirstBlipInfoId(8)
    local waypointCoords = GetBlipCoords(waypoint)

    if waypoint == 0 then
        lib.notify({
            title = 'Waypoint not found',
            description = 'No waypoint found, please set a waypoint first.',
            type = 'error'
        })
        return
    end

    local x, y, z in waypointCoords

    -- Teleport there so we can be within render distance, in order to calculate the ground z
    SetEntityCoords(playerPed, x, y, z, true, false, false, false)
    FreezeEntityPosition(playerPed, true)

    -- * Not sure if this is needed, but just in case
    while not HasCollisionLoadedAroundEntity(playerPed) do Wait(100) end

    FreezeEntityPosition(playerPed, false)

    -- Now we can hopefully get the ground z
    local success, z = GetGroundZFor_3dCoord(x, y, z, true)

    if not success then
        lib.notify({
            title = 'Failed to get ground z',
            description = 'Failed to get ground z for the waypoint.',
            type = 'error'
        })
        return
    else
        -- Just teleport anyway and let the player fall back to the ground
        SetEntityCoords(playerPed, x, y, z, true, false, false, false)
    end
end)