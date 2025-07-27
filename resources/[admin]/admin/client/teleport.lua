-- TODO: Move these functions somewhere else as they might be required for other resources

--[[ 
    Thank you to Tabarra for these teleport functions, from txAdmin.
 ]]

--- Calculate a safe Z coordinate based off the (X, Y)
---@param x number
---@param y number
---@return number|nil
function FindZForCoords(x, y)
    local found   = true
    local START_Z = 1500
    local z       = START_Z

    while found and z > 0 do
        local _found, _z = GetGroundZAndNormalFor_3dCoord(x + 0.0, y + 0.0, z - 1.0)

        if _found then z = _z + 0.0 end

        found = _found
        Wait(0)
    end

    if z == START_Z then return nil end

    return z + 0.0
end

local function handleTpNormally(x, y, z)
    local ped = PlayerPedId()
    local veh = GetVehiclePedIsIn(ped, false)

    SetPedCoordsKeepVehicle(ped, x, y, 100.0)
    FreezeEntityPosition(veh > 0 and veh or ped, true)

    while IsEntityWaitingForWorldCollision(ped) do
        lib.print.debug("waiting for collision...")
        Wait(100)
    end

    -- Automatically calculate ground Z
    if z == 0 then
        local _finalZ
        local DELAY = 500

        for i = 1, 5 do
            if _finalZ ~= nil then break end

            lib.print.debug("Z calc attempt #" .. i .. " (" .. (i * DELAY) .. "ms)")
            _finalZ = FindZForCoords(x, y)

            if _finalZ == nil then -- ? this used to be called "_z" but was unused
                lib.print.debug("Didn't resolve! Trying again in " .. DELAY)
                Wait(DELAY)
            end
        end

        if _finalZ ~= nil then z = _finalZ end
    end

    -- Teleport to targert
    ped = PlayerPedId() --update ped id
    SetPedCoordsKeepVehicle(ped, x, y, z)

    -- handle vehicle teleport
    if veh > 0 then
        veh = GetVehiclePedIsIn(ped, false) --update veh id
        SetEntityAlpha(veh, 125)
        SetEntityCoords(veh, x, y, z + 0.5, false, false, false, false)
        SetPedIntoVehicle(ped, veh, -1)
        SetVehicleOnGroundProperly(veh)
        SetEntityCollision(veh, true, true)
        FreezeEntityPosition(veh, false)
        CreateThread(function()
            Wait(2000)
            ResetEntityAlpha(veh)
            SetVehicleCanBreak(veh, true)
            SetVehicleWheelsCanBreak(veh, true)
        end)
    else
        FreezeEntityPosition(ped, false)
    end

    -- point camera to the ped direction
    SetGameplayCamRelativeHeading(0)
end

--[[ local function handleTpForFreecam(x, y, z)
    lib.print.debug("Handling TP for freecam")
    local ped = PlayerPedId()
    -- As we allow the freecam to have a vehicle attached. We need to make
    -- sure to teleport this as well
    local veh = GetVehiclePedIsIn(ped, false)
    lib.print.debug('Freecam has vehicle attached: ' .. tostring(veh))
    if veh and veh > 0 then
        SetEntityCoords(veh, x, y, z)
    end
    SetFreecamPosition(x, y, z)
end ]]

local function teleportToCoords(coords)
    if type(coords) ~= 'vector3' then return lib.print.error("^1Invalid coords") end

    DoScreenFadeOut(500)
    while not IsScreenFadedOut() do Wait(0) end

    handleTpNormally(coords.x, coords.y, coords.z)

    DoScreenFadeIn(500)
end

RegisterNetEvent('admin:teleportToMarker', function()
    if not IsWaypointActive() then
        lib.notify({
            title = 'Waypoint not found',
            description = 'No waypoint found, please set a waypoint first.',
            type = 'error'
        })
        return
    end
    
    local waypoint = GetFirstBlipInfoId(GetWaypointBlipEnumId())
    local waypointCoords = GetBlipInfoIdCoord(waypoint)
    
    teleportToCoords(vec3(waypointCoords.x, waypointCoords.y, 0))
end)