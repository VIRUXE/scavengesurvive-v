local ammoToGive = 250
local ammoValues = {10, 50, 100, 250, 500, 1000}
local defaultAmmoIndex = 4

RegisterNetEvent('admin:showWeaponsMenu', function()
    ammoToGive = ammoValues[defaultAmmoIndex]
    lib.showMenu('giveWeapon')
end)

-- Create a local copy to sort, to avoid modifying the global table
local weapons = lib.table.deepclone(exports.game:GetWeapons())

-- Define the order of weapon categories
local sortedWeaponGroups = {
    "Melee", "Pistol", "SMG", "Shotgun", "Rifle", "MachineGun", "Sniper",
    "Heavy", "Thrown", "PetrolCan", "StunGun", "Unarmed", "FireExtinguisher",
    "HackingDevice", "MetalDetector"
}

local weaponGroupIcons = {
    Melee            = 'fa-solid fa-hand-fist',
    Pistol           = 'fa-solid fa-gun',
    SMG              = 'fa-solid fa-gun',
    Shotgun          = 'fa-solid fa-gun',
    Rifle            = 'fa-solid fa-bullseye',
    MachineGun       = 'fa-solid fa-gun',
    Sniper           = 'fa-solid fa-crosshairs',
    Heavy            = 'fa-solid fa-rocket',
    Thrown           = 'fa-solid fa-bomb',
    PetrolCan        = 'fa-solid fa-gas-pump',
    StunGun          = 'fa-solid fa-bolt',
    Unarmed          = 'fa-solid fa-hand',
    FireExtinguisher = 'fa-solid fa-fire-extinguisher',
    HackingDevice    = 'fa-solid fa-laptop-code',
    MetalDetector    = 'fa-solid fa-magnet'
}

-- Register the menu
local options = {
    {
        label = 'Ammo',
        icon = 'fa-solid fa-box-archive',
        values = ammoValues,
        defaultIndex = defaultAmmoIndex,
        close = false
    }
}

for _, categoryName in ipairs(sortedWeaponGroups) do
    local categoryWeapons = weapons[categoryName]

    if categoryWeapons and #categoryWeapons > 0 then
        table.sort(categoryWeapons, function(a, b)
            return a.name < b.name
        end)

        local weaponValues = {}
        for _, weapon in ipairs(categoryWeapons) do
            table.insert(weaponValues, {
                label       = weapon.name,
                description = weapon.description or 'No description available.'
            })
        end
        
        if #weaponValues > 0 then
            table.insert(options, {
                label = categoryName:gsub('(%l)(%u)', '%1 %2'),
                icon = weaponGroupIcons[categoryName] or 'fa-solid fa-gun',
                values = weaponValues,
            })
        end
    end
end

lib.registerMenu({
    id = 'giveWeapon',
    title = 'Weapons',
    options = options,
    onSideScroll = function(selected, scrollIndex, args)
        if selected == 1 then -- Setting the ammo amount
            ammoToGive = ammoValues[scrollIndex]
        end
    end
}, function(selected, scrollIndex)
    if not selected or selected == 1 or not scrollIndex then return end

    local categoryName = options[selected].label
    local weapon = weapons[categoryName][scrollIndex]

    if not weapon then return end

    TriggerServerEvent('admin:giveWeapon', weapon.hash, ammoToGive)
end)
