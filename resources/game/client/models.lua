function PreloadModel(model)
    lib.requestModel(model)
    SetModelAsNoLongerNeeded(model)
end

lib.callback.register('game:preloadModel', PreloadModel)

lib.callback.register('game:getModelDimensions', function(model)
    PreloadModel(model)
    local min, max = GetModelDimensions(model)
    return min, max
end)