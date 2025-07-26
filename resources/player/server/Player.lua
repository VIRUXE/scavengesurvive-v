local Identifier = require 'server.auth.Identifier'

---@class Player
---@field Id number Player id
---@field Name string Player name
---@field Identifiers table<string, string> Player identifiers
---@field Tokens string[] Player tokens
---@field Account table|nil Player account
---@field Spawned boolean Player spawned
Player = {}
Player.__index = Player

---Creates a new Player object
---@param id number Player id
---@return Player
function Player:new(id)
    local p = setmetatable({}, Player)

    p.Id = id
    p.Name = GetPlayerName(id)

    -- local tokens = GetPlayerTokens(id)

    -- Create a table of Identifier objects from the identifiers array
    p.Identifiers = {}
    for _, identifierStr in ipairs(GetPlayerIdentifiers(id)) do
        local identifier = Identifier:new(identifierStr)
        if identifier then p.Identifiers[identifier.Type] = identifier.Value end
    end
    
    p.Tokens = GetPlayerTokens(id)
    p.Account = nil
    p.LoggedIn = false

    return p
end

function Player:GetIp() return self.Identifiers.ip end

function Player:GetDiscordId() return self.Identifiers.discord end

function Player:GetSteamId() return self.Identifiers.steam end

function Player:GetCoords() return self.Spawned and GetEntityCoords(GetPlayerPed(self.Id)) end
function Player:GetHeading() return self.Spawned and GetEntityHeading(GetPlayerPed(self.Id)) end