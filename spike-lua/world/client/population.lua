--[[ SetPedPopulationBudget(0)
SetVehiclePopulationBudget(0)
SetGarbageTrucks(false)
SetRandomBoats(false)
SetCreateRandomCops(false)
SetCreateRandomCopsNotOnScenarios(false)
SetCreateRandomCopsOnScenarios(false)

CreateThread(function()
    while true do
        SetPedDensityMultiplierThisFrame(0.0)
        SetScenarioPedDensityMultiplierThisFrame(0.0, 0.0)
        SetParkedVehicleDensityMultiplierThisFrame(0.0)
        SetVehicleDensityMultiplierThisFrame(0.0)
        SetRandomVehicleDensityMultiplierThisFrame(0.0)

        Wait(0)
    end
end) ]]
