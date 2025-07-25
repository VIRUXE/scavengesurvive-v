local ammoToGive = 250
local ammoValues = {10, 50, 100, 250, 500, 1000}
local defaultAmmoIndex = 4

RegisterNetEvent('admin:showWeaponsMenu', function()
    ammoToGive = ammoValues[defaultAmmoIndex]
    lib.showMenu('giveWeapon')
end)

-- Create a local copy to sort, to avoid modifying the global table
local sortedWeapons = lib.table.deepclone(exports.game:GetWeapons())

-- Define the order of weapon categories
local sortedWeaponGroups = {
    "Melee", "Pistol", "SMG", "Shotgun", "Rifle", "MachineGun", "Sniper",
    "Heavy", "Thrown", "PetrolCan", "StunGun", "Unarmed", "FireExtinguisher",
    "HackingDevice", "MetalDetector"
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
    local categoryWeapons = sortedWeapons[categoryName]

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
                label = categoryName,
                icon = 'fa-solid fa-gun',
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
    local weapon = sortedWeapons[categoryName][scrollIndex]

    if not weapon then return end

    TriggerServerEvent('admin:giveWeapon', weapon.hash, ammoToGive)
end)
