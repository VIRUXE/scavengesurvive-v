local Locked, VehicleName

local function drawText(text, x, y, color)
    if not text or not x or not y then return end

    SetTextFont(4)
    SetTextScale(1, 0.9)

    if color and type(color) == "table" and color.r and color.g and color.b and color.a then
        SetTextColour(color.r, color.g, color.b, color.a)
    else
        SetTextColour(255, 255, 255, 255)
    end

    SetTextOutline()
    SetTextJustification(2) -- 2 is right justification
    SetTextWrap(0.0, 1.0)
    BeginTextCommandDisplayText("STRING")
    AddTextComponentSubstringPlayerName(text)
    EndTextCommandDisplayText(x, y)
end

local function getVehicleName(vehicleId)
    return GetDisplayNameFromVehicleModel(GetEntityModel(vehicleId))
end

local function displayHud()
    Locked = false

    local RED_COLOR   = {r = 255, g = 0, b = 0, a = 255}
    local GREEN_COLOR = {r = 0, g = 255, b = 0, a = 255}

    -- Get vehicle data that doesn't need to be updated every frame
    CreateThread(function()
        while cache.vehicle do
            local lockStatus = GetVehicleDoorLockStatus(cache.vehicle)

            Locked = lockStatus == 2 or lockStatus == 3

            Wait(100)
        end
    end)

    -- Render text on the screen
    CreateThread(function()
        while cache.vehicle do
            -- Central locking status
            local speed = tonumber(GetEntitySpeed(cache.vehicle))

            -- Main vehicle status (top right)
            drawText('DOR', 0.98, 0.70, (Locked and RED_COLOR or GREEN_COLOR))
            drawText('ENG', 0.98, 0.74, GREEN_COLOR)
            drawText('DMG', 0.98, 0.78, GREEN_COLOR)

            -- Fuel and speed grouped together further down
            drawText('100/100L', 0.98, 0.85, {r = 255, g = 255, b = 255, a = 255})
            drawText(('%.0f MP/H'):format(speed * 2.236936), 0.98, 0.89, {r = 255, g = 255, b = 255, a = 255})

            -- Vehicle name separated at the bottom
            drawText(VehicleName, 0.98, 0.95, {r = 255, g = 255, b = 255, a = 255})

            Wait(0)
        end
    end)
end

lib.onCache('vehicle', function(vehicleId, oldVehicleId)
    if oldVehicleId then return end -- Player exited a vehicle

    VehicleName = getVehicleName(vehicleId)

    displayHud()
end)

VehicleName = getVehicleName(cache.vehicle)

displayHud()