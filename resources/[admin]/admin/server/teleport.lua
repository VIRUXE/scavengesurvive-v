lib.addCommand('tpp', {
    help = 'Teleport to a player',
    params = {
        {
            name = 'player',
            help = 'The player to teleport to',
            type = 'playerId'
        }
    },
    restricted = 'group.admin'
}, function(source, args, rawCommand)
    local targetPlayerId = tonumber(args.player)

    if not targetPlayerId then
        lib.notify({
            title = 'Error',
            description = 'Invalid player ID',
            type = 'error'
        })
    end

    local playerPed = GetPlayerPed(source)
    local targetPlayerPed = GetPlayerPed(targetPlayerId)
    local x, y, z in GetEntityCoords(targetPlayerPed)

    SetEntityCoords(playerPed, x, y, z, true, false, false, false)

    --[[ lib.notify({
        title = 'Teleported',
        description = 'Teleported to player ' .. targetPlayerId,
        type = 'success'
    }) ]]
end)

lib.addCommand('tp', {
    help = 'Teleport to coordinates (x,y,z[,heading]) or JSON {"x":0,"y":0,"z":0[,"w":0]} or {"Position":{"X":0,"Y":0,"Z":0}}',
    restricted = 'group.admin'
}, function(source, args, rawCommand)
    local playerPed = GetPlayerPed(source)
    local x, y, z, heading

    -- Extract coordinates after command name
    local input = rawCommand:match("^%s*[^%s]+%s+(.-)%s*$")
    if not input then
        print("Invalid coordinates provided.")
        return
    end

    -- Try JSON parsing first (most flexible)
    local cleanInput = input
    
    -- If the input starts with Position but doesn't have proper JSON structure, try to fix it
    if cleanInput:match('^%s*"Position"%s*:') then
        -- Check if it's missing the opening brace
        if not cleanInput:match('^%s*{') then
            cleanInput = "{" .. cleanInput .. "}"
        end
    end
    
    local success, parsed = pcall(json.decode, cleanInput)
    
    -- If JSON decode failed, try removing outer quotes
    if not success or not parsed then
        local quotedInput = input:match('^%s*"(.-)"%s*$')
        if quotedInput then
            success, parsed = pcall(json.decode, quotedInput)
        end
    end
    
    if success and parsed then
        if type(parsed) == "table" then
            -- JSON object format with Position key
            if parsed.Position and parsed.Position.X and parsed.Position.Y and parsed.Position.Z then
                x, y, z = parsed.Position.X, parsed.Position.Y, parsed.Position.Z
                heading = parsed.Position.W or parsed.Position.Heading
            -- JSON object format with direct x,y,z keys
            elseif parsed.x and parsed.y and parsed.z then
                x, y, z = parsed.x, parsed.y, parsed.z
                heading = parsed.w or parsed.heading
            -- JSON array format
            elseif #parsed >= 3 and #parsed <= 4 and type(parsed[1]) == "number" and type(parsed[2]) == "number" and type(parsed[3]) == "number" then
                x, y, z = parsed[1], parsed[2], parsed[3]
                if #parsed == 4 and type(parsed[4]) == "number" then
                    heading = parsed[4]
                end
            end
        end
    else
        
        -- Fallback to comma-separated coordinates
        local parts = {}
        for part in input:gmatch("[^,]+") do
            local trimmed = part:match("^%s*(.-)%s*$")
            if trimmed ~= "" then
                table.insert(parts, trimmed)
            end
        end

        if #parts == 3 or #parts == 4 then
            local nums = {}
            for i, part in ipairs(parts) do
                local num = tonumber(part)
                if num then
                    nums[i] = num
                else
                    break
                end
            end
            if #nums == #parts then
                x, y, z = nums[1], nums[2], nums[3]
                if #nums == 4 then
                    heading = nums[4]
                end
            end
        end
    end

    -- Teleport if coordinates are valid
    if x and y and z then
        SetEntityCoords(playerPed, x, y, z, true, false, false, false)
        if heading then SetEntityHeading(playerPed, heading) end

        print("Teleported to: " .. x .. ", " .. y .. ", " .. z .. (heading and ", heading=" .. heading or ""))
    else
        
        print("Invalid coordinates. Use: /tp x,y,z[,heading] or /tp {\"x\":0,\"y\":0,\"z\":0[,\"w\":0]} or /tp {\"Position\":{\"X\":0,\"Y\":0,\"Z\":0}}")
    end
end)

lib.addCommand({'tpm', 'tpw'}, {
    help = 'Teleport to a Marker/Waypoint',
    restricted = 'group.admin'
}, function(playerId, args, rawCommand)
    local player = exports.player:Get(playerId)
    local event = 'admin:teleportToMarker'

    TriggerClientEvent(event, playerId)

    player:log(event, 'Teleported to marker.')
end)