local SPAWN_COORDS <const> = vector3(0.0, 0.0, 70.0)

local isSpawning = false

function SpawnPlayer()
    if isSpawning then return end

    isSpawning = true

    DoScreenFadeOut(500)
    Wait(500)

    local x, y, z in SPAWN_COORDS

    RequestCollisionAtCoord(x, y, z)

    SetEntityCoordsNoOffset(cache.ped, x, y, z, false, false, false)

    NetworkResurrectLocalPlayer(x, y, z, 0.0, 0, false)

    ClearPedTasksImmediately(cache.ped)
    --SetEntityHealth(ped, 300)
    RemoveAllPedWeapons(cache.ped, false)
    ClearPlayerWantedLevel(cache.id)

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

    -- Wait for collision to load, if it doesn't load in 5 seconds (dogshit computer stuff), we'll just continue
    local time = GetGameTimer()
    while (not HasCollisionLoadedAroundEntity(cache.ped) and (GetGameTimer() - time) < 5000) do Wait(0) end

    ShutdownLoadingScreen()

    if IsScreenFadedOut() then
        DoScreenFadeIn(500)
        while not IsScreenFadedIn() do Wait(0) end
    end

    DoScreenFadeIn(500)
    Wait(500)

    if IsEntityPositionFrozen(cache.ped) then FreezeEntityPosition(cache.ped, false) end

    isSpawning = false

    TriggerServerEvent('player:spawned')
    TriggerEvent('player:spawned')
end

SetArtificialLightsState(true)
SetArtificialLightsStateAffectsVehicles(false)
SetClockTime(0, 0, 0)
PauseClock(true) -- We'll have it paused for now

SpawnPlayer()