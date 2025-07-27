local Identifier = require 'server.auth.Identifier'

---@class User
---@field Id number Player id
---@field Name string Player name
---@field Identifiers table<string, string> Player identifiers
---@field Tokens string[] Player tokens
---@field Account table|nil Player account
---@field Spawned boolean Player spawned
---@field LoggedIn boolean Player logged in
User = {}
User.__index = User

---Creates a new Player object
---@param id number Player id
---@return User
function User:new(id)
    local p = setmetatable({}, User)

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
    p.Spawned = false
    p.LoggedIn = false

    return p
end

---Logs a message to the console and the logger
---@param eventName string Event name
---@param message string Message
---@param ... any Additional arguments to pass to the logger
function User:log(eventName, message, ...)
    lib.print.info(('(%d) %s [%s] %s'):format(self.Id, self.Name, self.Account?.username or 'Not Logged In', message))
    lib.logger(self.Id, eventName, message, ...)
end

function User:GetIp() return self.Identifiers.ip end

function User:GetDiscordId() return self.Identifiers.discord end

function User:GetSteamId() return self.Identifiers.steam end

function User:GetCoords() return self.Spawned and GetEntityCoords(GetPlayerPed(self.Id)) end
function User:GetHeading() return self.Spawned and GetEntityHeading(GetPlayerPed(self.Id)) end