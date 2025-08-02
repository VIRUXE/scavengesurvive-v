-- https://raw.githubusercontent.com/root-cause/v-cargens/refs/heads/master/CarGens_ZoneVehicles.json
local locations = json.decode(LoadResourceFile(GetCurrentResourceName(), 'CarGens_ZoneVehicles.json')) -- By root-cause
local locationsNearby = {}

--[[ 
    {
      "x": -1660.31348,
      "y": -3107.79468,
      "z": 12.854578,
      "heading": -216.266144,
      "models": [
        "youga"
      ]
    },
]]

-- Get neavby locations every second
CreateThread(function()
    while true do
        table.wipe(locationsNearby)
        
        local playerCoords = GetEntityCoords(PlayerPedId())
        for locationIndex, location in ipairs(locations) do
            if #(playerCoords - vec3(location.x, location.y, location.z)) < 100.0 then
                location.id = locationIndex
                table.insert(locationsNearby, location)
            end
        end

        Wait(500)
    end
end)

-- Display markers for each nearby location
CreateThread(function()
    while true do
        for _, location in ipairs(locationsNearby) do
            DrawMarker(
                1, -- Marker type: Cylinder
                location.x, location.y, location.z - 2.0, -- Position (slightly lowered for ground)
                0.0, 0.0, 0.0, -- Direction
                0.0, 0.0, 0.0, -- Rotation
                2.0, 2.0, 10.0, -- Scale
                0, 255, 0, 120, -- Colour (green, semi-transparent)
                false, true, 2, false, nil, nil, false
            )

            exports.player:DrawText3D({
                text = ('ID: %d Models: %d'):format(location.id, #location.models),
                coords = vec3(location.x, location.y, location.z - 2.0),
                scale = 0.5,
                center = true,
            })
        end
        Wait(0)
    end
end)