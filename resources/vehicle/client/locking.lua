local function lockVehicle(vehicleId, lock)
    if not vehicleId then return end
    if GetNumberOfVehicleDoors(vehicleId) < 1 then return end -- If the vehicle has no doors, don't lock it

    SetVehicleDoorsLocked(vehicleId, lock and 2 or 1)
end

-- Player exited vehicle
lib.onCache('vehicle', function(vehicleId, oldVehicleId)
    if vehicleId then return end
    if GetNumberOfVehicleDoors(oldVehicleId) < 1 then return end

    local lockStatus = GetVehicleDoorLockStatus(oldVehicleId)
    local locked = lockStatus == 2 or lockStatus == 3

    -- If we exit the locked vehicle then it should remain unlocked
    if locked then lockVehicle(oldVehicleId, false) end -- Unlock the vehicle
end)

RegisterCommand('lock', function()
    local lockStatus = GetVehicleDoorLockStatus(cache.vehicle)
    local locked = lockStatus == 2 or lockStatus == 3

    lockVehicle(cache.vehicle, not locked) -- Toggle the lock
end)