local MAX_JUMPS <const> = 10

local function displayWarning()
    CreateThread(function()
        local displayTime = GetGameTimer() + 3000
        while GetGameTimer() < displayTime do
            DrawText("Bunnyhopping is disabled", 0.5, 0.5, 0, {r=255, g=0, b=0, a=255}, 1.0, true)
            Wait(0)
        end
    end)
end

CreateThread(function()
    local timesJumped = 0
    local ped = cache.ped

    while true do
        if IsPedOnFoot(ped) and not IsPedSwimming(ped) and (IsPedRunning(ped) or IsPedSprinting(ped)) and not IsPedClimbing(ped) and IsPedJumping(ped) and not IsPedRagdoll(ped) then
            timesJumped += 1

            if timesJumped >= MAX_JUMPS then
                SetPedToRagdoll(ped, 5000, 1400, 2, false, false, false)
                timesJumped = 0
                displayWarning()
            end

            Wait(1)
        else
            timesJumped = 0
            Wait(500)
        end
    end
end)
