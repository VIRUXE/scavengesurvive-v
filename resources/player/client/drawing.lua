
--- Draws text on the screen with various formatting options.
-- @param text string: The text to display.
-- @param x number: X position on the screen (0.0 - 1.0).
-- @param y number: Y position on the screen (0.0 - 1.0).
-- @param font number|nil: Font index to use (default 0).
-- @param color table|nil: Table with r, g, b, a values (0-255) for text colour.
-- @param scale number|nil: Scale of the text (default 1.0).
-- @param right boolean|nil: Whether to right-justify the text.
-- @param shadow boolean|nil: Whether to add a shadow to the text.
-- @param outline boolean|nil: Whether to add an outline to the text.
-- @param fontType number|nil: Alternative font index to use (overrides font).
-- @param justify number|nil: Justification type (default 0).
function DrawText(text, x, y, font, color, scale, right, shadow, outline, justify)
    if not text or not x or not y then return end

    SetTextFont(font or 0)
    SetTextScale(scale or 1.0, scale or 1.0)

    if color and type(color) == "table" and color.r and color.g and color.b and color.a then
        SetTextColour(color.r, color.g, color.b, color.a)
    else
        SetTextColour(255, 255, 255, 255)
    end

    if shadow then SetTextDropShadow() end
    if outline then SetTextOutline() end
    if right then SetTextRightJustify(true) end
    SetTextJustification(justify or 0)
    SetTextWrap(0.0, 1.0)
    BeginTextCommandDisplayText("STRING")
    AddTextComponentSubstringPlayerName(text)
    EndTextCommandDisplayText(x, y)
end

exports('DrawText', DrawText)