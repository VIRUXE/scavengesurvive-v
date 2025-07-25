-- local SPAWN_COORDS <const> = vector3(0.0, 0.0, 70.0)

local isSpawning = false
local lastSpawnLocation = nil

function Spawn(position, heading, cleanupCompleted)
    if isSpawning then return end

    isSpawning = true

    DoScreenFadeOut(500)
    while not IsScreenFadedOut() do Wait(500) end

    local x, y, z in position

    RequestCollisionAtCoord(x, y, z)

    SetEntityCoords(cache.ped, x, y, z, true, false, false, false)
    if heading then SetEntityHeading(cache.ped, heading) end

    -- Wait for collision to load, if it doesn't load in 5 seconds (dogshit computer stuff), we'll just continue
    local time = GetGameTimer()
    while not HasCollisionLoadedAroundEntity(cache.ped) and (GetGameTimer() - time) < 5000 do Wait(0) end

    NetworkResurrectLocalPlayer(x, y, z, 0.0, 0, false)

    ClearPedTasksImmediately(cache.ped)
    --SetEntityHealth(ped, 300)
    RemoveAllPedWeapons(cache.ped, false)
    ClearPlayerWantedLevel(cache.id)

    cleanupCompleted()

    --[[ * Leftover shit from spawnmanager ]]

    -- why is this even a flag?
    --SetCharWillFlyThroughWindscreen(ped, false)

    -- set primary camera heading
    --SetGameCamHeading(spawn.heading)
    --CamRestoreJumpcut(GetGameCam())

    -- load the scene; streaming expects us to do it
    --ForceLoadingScreen(true)
    --loadScene(spawn.x, spawn.y, spawn.z)
    --ForceLoadingScreen(false)

    ShutdownLoadingScreen()

    if IsScreenFadedOut() then
        DoScreenFadeIn(5000)
        Wait(5000)
    end

    if IsEntityPositionFrozen(cache.ped) then FreezeEntityPosition(cache.ped, false) end

    isSpawning = false

    TriggerServerEvent('player:spawned')
    TriggerEvent('player:spawned')
end

local function generateNewSpawnLocation()
    -- First, randomly select a section
    local sectionKeys = {}
    for sectionName, _ in pairs(PedSpawnLocations) do
        table.insert(sectionKeys, sectionName)
    end
    
    local randomSectionKey = sectionKeys[math.random(1, #sectionKeys)]
    local selectedSection = PedSpawnLocations[randomSectionKey]
    
    -- Then, randomly select a location from that section
    local locationKeys = {}
    for locationKey, locationData in pairs(selectedSection) do
        if locationData.position then
            table.insert(locationKeys, locationKey)
        end
    end
    
    local randomLocationKey = locationKeys[math.random(1, #locationKeys)]
    return selectedSection[randomLocationKey]
end

function SpawnAtRandomLocation()
    local spawnLocation

    repeat spawnLocation = generateNewSpawnLocation() until not lastSpawnLocation or spawnLocation ~= lastSpawnLocation
    lastSpawnLocation = spawnLocation

    -- TODO: Fix z at coordinate level
    Spawn(spawnLocation.position + vec3(0.0, 0.0, 0.5), spawnLocation.heading, function()
        if spawnLocation.giveParachute then
            -- We're doing this so the parachute is already visible and the player doesn't fall all ragdoll
            GiveWeaponToPed(cache.ped, `gadget_parachute`, 1, false, false)
            lib.print.info('Received parachute')
        end
        
        lib.print.info('Spawned at ' .. spawnLocation.description)
    end)
end

RegisterNetEvent('player:respawn', SpawnAtRandomLocation)

lib.callback.register('player:getForwardVector', function() return GetEntityForwardVector(cache.ped) end)

SpawnAtRandomLocation()