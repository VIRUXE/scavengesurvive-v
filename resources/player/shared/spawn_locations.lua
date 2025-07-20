-- GTA V Ped Spawn Positions and Headings
-- Extracted from Base Jumping data with descriptions
-- Using vec3 for positions and vec4 for position + heading

PedSpawnLocations = {
    -- Base Jumping Locations
    baseJumping = {
        -- Harbor Base Jump - Los Santos Harbor area
        harbor = {
            position = vec3(-829.3729, -1289.8170, 4.0005),
            heading = 41.0737,
            description = "Harbor base jumping location - helicopter launch point near Los Santos docks"
        },
        -- Race Track Base Jump - Vinewood Race Track
        raceTrack = {
            position = vec3(1208.2003, 174.3914, 80.1245),
            heading = 10.500,
            description = "Race track base jumping location - helicopter launch from Vinewood Race Track"
        },
        -- Windmills Base Jump - Wind Farm area
        windmills = {
            position = vec3(2463.7935, 1509.9562, 35.0349),
            heading = 289.2623,
            description = "Windmills base jumping location - helicopter launch from wind farm area"
        },
        -- North Cliff Base Jump - Northern mountains
        northCliff = {
            position = vec3(-274.6549, 6633.8984, 7.1166),
            heading = 60.1427,
            description = "North cliff base jumping location - foot launch from northern mountain cliffs"
        },
        -- Maze Bank Base Jump - Downtown Los Santos
        mazeBank = {
            position = vec3(-92.3500, -854.3000, 39.5710),
            heading = 1.8891,
            description = "Maze Bank base jumping location - motorcycle launch from downtown skyscraper"
        },
        -- Crane Base Jump - Construction site
        crane = {
            position = vec3(-120.9200, -976.0500, 295.4900),
            heading = 358.9586,
            description = "Crane base jumping location - foot launch from construction crane",
            giveParachute = true
        },
        -- River Cliff Base Jump - River area
        riverCliff = {
            position = vec3(-1237.2000, 4540.7500, 184.7500),
            heading = 164.6178,
            description = "River cliff base jumping location - foot launch from river cliffs"
        },
        -- Runaway Train Base Jump - Train tracks
        runawayTrain = {
            position = vec3(-742.5269, 4493.3149, 75.1444),
            heading = 112.6,
            description = "Runaway train base jumping location - helicopter launch near train tracks"
        },
        -- Golf Course Base Jump - Golf course area
        golfCourse = {
            position = vec3(-801.3582, 298.8532, 84.9490),
            heading = 104.2070,
            description = "Golf course base jumping location - foot launch from golf course"
        },
        -- The 1K Base Jump - Mountain area
        the1K = {
            position = vec3(-1367.5952, 4381.9434, 41.1320),
            heading = 329.4791,
            description = "The 1K base jumping location - helicopter launch from mountain area"
        },
        -- The 1.5K Base Jump - High altitude area
        the1_5K = {
            position = vec3(2517.9312, 4971.7524, 44.7082),
            heading = 0.0,
            description = "The 1.5K base jumping location - helicopter launch from high altitude area"
        },
        -- Canal Base Jump - Canal area
        canal = {
            position = vec3(1054.5343, -179.6562, 70.3066),
            heading = 24.9200,
            description = "Canal base jumping location - helicopter launch from canal area"
        },
        -- Rock Cliff Base Jump - Rock formations
        rockCliff = {
            position = vec3(-767.415, 4331.792, 147.6820),
            heading = 359.2885,
            description = "Rock cliff base jumping location - foot launch from rock formations"
        }
    },
    -- Course Player Positions (where players appear during the jump)
    coursePlayerPositions = {
        harbor = {
            position = vec3(-1152.0527, -1857.8835, 204.0663),
            description = "Player position during Harbor base jump course",
            giveParachute = true
        },
        raceTrack = {
            position = vec3(885.1140, -437.3520, 529.8670),
            description = "Player position during Race Track base jump course",
            giveParachute = true
        },
        windmills = {
            position = vec3(2034.9117, 1971.0510, 582.7461),
            description = "Player position during Windmills base jump course",
            giveParachute = true
        },
        northCliff = {
            position = vec3(409.7498, 5703.5254, 695.1700),
            description = "Player position during North Cliff base jump course"
        },
        mazeBank = {
            position = vec3(-74.9632, -827.4467, 324.9521),
            description = "Player position during Maze Bank base jump course",
            giveParachute = true
        },
        crane = {
            position = vec3(-117.6998, -975.5710, 295.0000),
            description = "Player position during Crane base jump course"
        },
        riverCliff = {
            position = vec3(-1243.7838, 4534.1631, 184.8471),
            description = "Player position during River Cliff base jump course"
        },
        runawayTrain = {
            position = vec3(-359.1000, 4119.5000, 304.1000),
            description = "Player position during Runaway Train base jump course",
            giveParachute = true
        },
        golfCourse = {
            position = vec3(-807.0730, 330.8846, 232.6766),
            description = "Player position during Golf Course base jump course",
            giveParachute = true
        },
        the1K = {
            position = vec3(-1286.9900, 3668.9216, 1072.4663),
            description = "Player position during The 1K base jump course",
            giveParachute = true
        },
        the1_5K = {
            position = vec3(1018.4410, 3956.7056, 1354.0000),
            description = "Player position during The 1.5K base jump course",
            giveParachute = true
        },
        canal = {
            position = vec3(1627.1957, -421.7584, 1321.4835),
            description = "Player position during Canal base jump course",
            giveParachute = true
        },
        rockCliff = {
            position = vec3(-766.5999, 4334.8052, 147.1205),
            description = "Player position during Rock Cliff base jump course"
        }
    }
}