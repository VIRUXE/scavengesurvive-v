---@class DrawTextParams
---@field text string
---@field scale? integer default: `0.35`
---@field font? integer default: `4`
---@field color? vector4 rgba, white by default
---@field enableDropShadow? boolean
---@field enableOutline? boolean

---@class DrawText2DParams : DrawTextParams
---@field coords vector2
---@field width? number default: `1.0`
---@field height? number default: `1.0`

---Draws text onto the screen in 2D space for a single frame.
---@param params DrawText2DParams
function DrawText2D(params)
    local text             = params.text
    local coords           = params.coords
    local scale            = params.scale or 0.35
    local font             = params.font or 4
    local color            = params.color or vec4(255, 255, 255, 255)
    local width            = params.width or 1.0
    local height           = params.height or 1.0
    local enableDropShadow = params.enableDropShadow or false
    local enableOutline    = params.enableOutline or false

    SetTextScale(scale, scale)
    SetTextFont(font)
    SetTextColour(math.floor(color.r), math.floor(color.g), math.floor(color.b), math.floor(color.a))
    if enableDropShadow then SetTextDropShadow() end
    if enableOutline then SetTextOutline() end

    SetTextCentre(true)
    BeginTextCommandDisplayText('STRING')
    AddTextComponentSubstringPlayerName(text)
    EndTextCommandDisplayText(coords.x - width / 2, coords.y - height / 2 + 0.005)
end

---@class DrawText3DParams : DrawTextParams
---@field coords vector3
---@field disableDrawRect? boolean
---@field scale? integer | vector2 default: `vec2(0.35,0.35)`

---Draws text onto the screen in 3D space for a single frame.
---@param params DrawText3DParams
function DrawText3D(params) -- luacheck: ignore
    local isScaleparamANumber = type(params.scale) == "number"
    local text                = params.text
    local coords              = params.coords
    local scale               = (isScaleparamANumber and vec2(params.scale, params.scale)) or params.scale or vec2(0.35, 0.35)
    local color               = params.color or vec4(255, 255, 255, 255)
    local enableDropShadow    = params.enableDropShadow or false
    local enableOutline       = params.enableOutline or false

    SetTextScale(scale.x, scale.y)
    SetTextFont(params.font or 4)
    SetTextColour(math.floor(color.r), math.floor(color.g), math.floor(color.b), math.floor(color.a))
    if enableDropShadow then SetTextDropShadow() end
    if enableOutline then SetTextOutline() end
    SetTextCentre(true)
    BeginTextCommandDisplayText('STRING')
    AddTextComponentSubstringPlayerName(text)
    SetDrawOrigin(coords.x, coords.y, coords.z, 0)
    EndTextCommandDisplayText(0.0, 0.0)

    if not params.disableDrawRect then DrawRect(0.0, 0.0125, 0.017 + (#text / 370), 0.03, 0, 0, 0, 75) end
    ClearDrawOrigin()
end

exports('DrawText', DrawText2D)
exports('DrawText2D', DrawText2D)
exports('DrawText3D', DrawText3D)