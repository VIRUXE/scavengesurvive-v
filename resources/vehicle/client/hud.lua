local FADE_TIME <const> = 1000 -- ms

local Locked, VehicleName
local DisplayingHUD = false
local Alpha = 0
local FadeStart = 0
local FadeDirection = 'in' -- 'in' or 'out'

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

local function getVehicleName(vehicleId) return GetDisplayNameFromVehicleModel(GetEntityModel(vehicleId)) end

local function displayHud()
    Locked = false

    local RED_COLOR   = {r = 255, g = 0, b = 0, a = 255}
    local GREEN_COLOR = {r = 0, g = 255, b = 0, a = 255}
    local WHITE_COLOR = {r = 255, g = 255, b = 255, a = 255}

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
        while DisplayingHUD or Alpha > 0 do
            local now = GetGameTimer()
            
            -- Handle fading
            if FadeDirection == 'in' and Alpha < 255 then
                Alpha = math.min(255, math.floor((now - FadeStart) / FADE_TIME * 255))
            elseif FadeDirection == 'out' and Alpha > 0 then
                Alpha = math.max(0, math.floor(255 - ((now - FadeStart) / FADE_TIME * 255)))
            end

            -- Main vehicle status (top right)
            local dorColor = {r = RED_COLOR.r, g = RED_COLOR.g, b = RED_COLOR.b, a = Alpha}
            if not Locked then dorColor = {r = GREEN_COLOR.r, g = GREEN_COLOR.g, b = GREEN_COLOR.b, a = Alpha} end
            local whiteAlpha = {r = WHITE_COLOR.r, g = WHITE_COLOR.g, b = WHITE_COLOR.b, a = Alpha}

            drawText('DOR', 0.98, 0.685, dorColor)
            drawText('ENG', 0.98, 0.725, {r = GREEN_COLOR.r, g = GREEN_COLOR.g, b = GREEN_COLOR.b, a = Alpha})
            drawText('DMG', 0.98, 0.765, {r = GREEN_COLOR.r, g = GREEN_COLOR.g, b = GREEN_COLOR.b, a = Alpha})

            -- Fuel and speed grouped together further down
            drawText('100/100L', 0.98, 0.835, whiteAlpha)
            drawText(('%.0f MP/H'):format(tonumber(GetEntitySpeed(cache.vehicle)) * 2.236936), 0.98, 0.875, whiteAlpha)

            -- Vehicle name separated at the bottom
            drawText(VehicleName, 0.98, 0.915, whiteAlpha)

            Wait(0)
        end
    end)
end

lib.onCache('vehicle', function(vehicleId, oldVehicleId)
    if vehicleId then
        -- Player entered a vehicle
        DisplayingHUD = true
        VehicleName = getVehicleName(vehicleId)
        FadeDirection = 'in'
        FadeStart = GetGameTimer()
        Alpha = 0
        displayHud()
    else
        -- Player left a vehicle
        DisplayingHUD = false
        FadeDirection = 'out'
        FadeStart = GetGameTimer()
    end
end)

if cache.vehicle then
    DisplayingHUD = true
    VehicleName = getVehicleName(cache.vehicle)
    FadeDirection = 'in'
    FadeStart = GetGameTimer()
    Alpha = 0
    displayHud()
end