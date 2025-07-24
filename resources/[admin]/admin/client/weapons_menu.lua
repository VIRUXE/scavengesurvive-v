local weapons = exports.game:GetWeapons()
local targetPlayerId = nil
local ammoToGive = 250
local ammoValues = {10, 50, 100, 250, 500, 1000}
local defaultAmmoIndex = 4

RegisterNetEvent('admin:showWeaponsMenu', function(playerId)
    targetPlayerId = playerId
    ammoToGive = ammoValues[defaultAmmoIndex]
    lib.showMenu('giveWeapon')
end)

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

for categoryName, categoryWeapons in pairs(weapons) do
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

lib.registerMenu({
    id = 'giveWeapon',
    title = 'Weapons',
    options = options,
    onSideScroll = function(selected, scrollIndex, args)
        if selected == 1 then -- Setting the ammo amount
            ammoToGive = ammoValues[scrollIndex]
        end
    end,
    onClose = function()
        targetPlayerId = nil
    end
}, function(selected, scrollIndex)
    if not selected or selected == 1 or not scrollIndex then return end

    local categoryName = options[selected].label
    local weapon = weapons[categoryName][scrollIndex]

    if not weapon then return end

    TriggerServerEvent('admin:giveWeapon', targetPlayerId, weapon.hash, weapon.name, ammoToGive)
end)
