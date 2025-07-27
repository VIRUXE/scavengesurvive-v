local function setDefaultPedFlags(ped)
    SetPedConfigFlag(ped, 35, false) -- 35 is the helmet prop index, 1 disables auto helmet
end

lib.onCache('ped', function(ped, oldPed)
    setDefaultPedFlags(ped)
end)

setDefaultPedFlags(cache.ped)