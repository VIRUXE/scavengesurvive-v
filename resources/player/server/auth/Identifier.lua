---@class Identifier
---@field Type 'steam'|'discord'|'xbl'|'live'|'license'|'license2'|'fivem'|'ip'
---@field Value string
Identifier = {}
Identifier.__index = Identifier

---Creates a new Identifier object
---@param rawIdentifier string Identifier string from GetPlayerIdentifiers
---@return Identifier|nil
function Identifier:new(rawIdentifier)
    local instance = setmetatable({}, Identifier)

    local sep = string.find(rawIdentifier, ':', 1, true)
    if not sep then return nil end

    instance.Type = string.sub(rawIdentifier, 1, sep - 1)
    instance.Value = string.sub(rawIdentifier, sep + 1)
    return instance
end

return Identifier