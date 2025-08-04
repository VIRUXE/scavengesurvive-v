Players = {}

local function recordIdentifiers(player)
    for type, value in pairs(player.Identifiers) do
        MySQL.insert('INSERT INTO account_identifiers (account_id, type, value) VALUES (?, ?, ?) ON DUPLICATE KEY UPDATE last_used = NOW(), times_used = times_used + 1', { player.Account.id, type, value })
    end
end

local function recordTokens(player)
    for _, token in ipairs(player.Tokens) do
        MySQL.insert('INSERT INTO account_tokens (account_id, token) VALUES (?, ?) ON DUPLICATE KEY UPDATE last_used = NOW(), times_used = times_used + 1', { player.Account.id, token })
    end
end

local function showRecoveryCard(deferrals, player, message)
    local cardBody = {
        {
            type = 'TextBlock',
            text = 'Password Recovery',
            weight = 'bolder',
            size = 'medium'
        },
        {
            type = 'ColumnSet',
            columns = {
                {
                    type = 'Column',
                    width = 1,
                    items = {
                        {
                            type = 'Input.Text',
                            id = 'email',
                            label = 'E-mail',
                            placeholder = 'Enter your e-mail address'
                        }
                    }
                },
                { type = 'Column', width = 1 }
            }
        }
    }

    if message then
        table.insert(cardBody, 2, {
            type = 'TextBlock',
            text = message,
            wrap = true,
            color = 'attention'
        })
    end

    deferrals.presentCard({
        type = 'AdaptiveCard',
        version = '1.3',
        body = cardBody,
        actions = {
            {
                type = 'Action.Submit',
                title = 'Submit',
                data = { action = 'submit_recovery' }
            },
            {
                type = 'Action.Submit',
                title = 'Back',
                data = { action = 'back_to_welcome' }
            }
        }
    }, function(recoveryData)
        if not recoveryData then
            deferrals.done('Connection closed.')
            return
        end

        if recoveryData.action == 'back_to_welcome' then
---@diagnostic disable-next-line: undefined-global
            showWelcomeCard(deferrals, player)
            return
        end

        local email = recoveryData.email
        if not email or email == '' then
            showRecoveryCard(deferrals, player, 'E-mail address cannot be blank.')
            return
        end

        if not string.match(email, "^[%w_.-]+@[%w_.-]+%.[%w]{2,6}$") then
            showRecoveryCard(deferrals, player, 'Please enter a valid e-mail address.')
            return
        end

        -- We don't want to reveal if an email is registered or not
        -- So we just pretend to send an email in any case.
        -- local account = MySQL.single.await('SELECT id FROM accounts WHERE email = ?', { email })

        deferrals.presentCard({
            type = 'AdaptiveCard',
            version = '1.3',
            body = {
                {
                    type = 'TextBlock',
                    text = 'Password Recovery',
                    weight = 'bolder',
                    size = 'medium'
                },
                {
                    type = 'TextBlock',
                    text = 'If an account with that email exists, a password recovery link has been sent.',
                    wrap = true
                }
            },
            actions = {
                {
                    type = 'Action.Submit',
                    title = 'Close'
                }
            }
        }, function()
            deferrals.done('Connection closed.')
        end)
    end)
end

local function showWelcomeCard(deferrals, player, message, username, password)
    local cardBody = {
        {
            type   = 'TextBlock',
            text   = 'Scavenge and Survive',
            weight = 'bolder',
            size   = 'large'
        },
        {
            type = 'TextBlock',
            text = 'Welcome to a world where survival is the only thing that matters. Scavenge for resources, build your shelter, and fight for your life in a post-apocalyptic world.',
            wrap = true
        },
        {
            type = 'TextBlock',
            text = message or 'Please login or register to continue.',
            wrap = true
        },
        {
            type = 'ColumnSet',
            columns = {
                {
                    type  = 'Column',
                    width = 1,
                    items = {
                        {
                            type        = 'Input.Text',
                            id          = 'username',
                            label       = 'Username',
                            placeholder = 'Enter your username',
                            value       = username
                        }
                    }
                },
                {
                    type = 'Column',
                    width = 1,
                    items = {
                        {
                            type        = 'Input.Text',
                            id          = 'password',
                            label       = 'Password',
                            placeholder = 'Enter your password',
                            style       = 'password',
                            value       = password
                        }
                    }
                },
                { type = 'Column', width = 2 }
            }
        }
    }

    if message then cardBody[2].color = 'attention' end

    deferrals.presentCard({
        type    = 'AdaptiveCard',
        version = '1.3',
        body    = cardBody,
        actions = {
            {
                type  = 'Action.Submit',
                title = 'Login',
                data  = { action = 'login' }
            },
            {
                type  = 'Action.Submit',
                title = 'Register',
                data  = { action = 'register' }
            },
            {
                type  = 'Action.Submit',
                title = 'Forgot Password?',
                data  = { action = 'recover' }
            }
        }
    }, function(data, rawData)
        if not data then
            deferrals.done('Connection closed.')
            return
        end

        if data.action == 'login' then
            local username = data.username
            local password = data.password

            if not username or not password or username == '' or password == '' then
                showWelcomeCard(deferrals, player, 'Username and password cannot be empty.', username, password)
                return
            end

            local account = MySQL.single.await('SELECT * FROM accounts WHERE username = ?', { username })

            if account then
                if MySQL.scalar.await('SELECT SHA2(?, 256) = ?', { password, account.password }) then
                    player.Account = account

                    recordIdentifiers(player)
                    recordTokens(player)

                    deferrals.done() -- Player is logged in so we'll let them load client resources until ready to spawn
                else
                    showWelcomeCard(deferrals, player, 'Incorrect password.', username, password)
                end
            else
                showWelcomeCard(deferrals, player, 'Account not found. Please try again or register.', username, password)
            end
        elseif data.action == 'register' then
            local username = data.username
            local password = data.password

            if not username or not password or username == '' or password == '' then
                showWelcomeCard(deferrals, player, 'Username and password cannot be empty.', username, password)
                return
            end

            if MySQL.single.await('SELECT id FROM accounts WHERE username = ?', { username }) then
                showWelcomeCard(deferrals, player, 'This username is already registered. Please login or register with a different username.', username, password)
                return
            end

            deferrals.presentCard({
                type    = 'AdaptiveCard',
                version = '1.3',
                body    = {
                    {
                        type   = 'TextBlock',
                        text   = 'Registration',
                        weight = 'bolder',
                        size   = 'medium'
                    },
                    {
                        type = 'ColumnSet',
                        columns = {
                            {
                                type  = 'Column',
                                width = 1,
                                items = {
                                    {
                                        type        = 'Input.Text',
                                        id          = 'email',
                                        label       = 'E-mail',
                                        placeholder = 'Enter your e-mail'
                                    }
                                }
                            },
                            { type = 'Column', width = 1 }
                        }
                    }
                },
                actions = {
                    {
                        type = 'Action.Submit',
                        title = 'Complete Registration'
                    }
                }
            }, function(registerData, rawRegisterData)
                if not registerData then
                    deferrals.done('Connection closed.')
                    return
                end

                local email = registerData.email
                if not email or email == '' then
                    showWelcomeCard(deferrals, player, 'E-mail cannot be empty.', username, password)
                    return
                end

                local accountId = MySQL.insert.await('INSERT INTO accounts (username, password, email) VALUES (?, SHA2(?, 256), ?)', { username, password, email })

                if accountId then
                    player.Account = { id = accountId, username = username, email = email }

                    recordIdentifiers(player)
                    recordTokens(player)

                    deferrals.done()

                    TriggerEvent('player:registered', player) -- I'll leave this here just in case
                else
                    showWelcomeCard(deferrals, player, 'An error occurred during registration. The username might be taken.', username, password)
                end
            end)
        elseif data.action == 'recover' then
            showRecoveryCard(deferrals, player)
        else
            deferrals.done()
        end
    end)
end

AddEventHandler('playerConnecting', function(name, setKickReason, deferrals)
    deferrals.defer()

    local playerId = source
    local player = User:new(playerId)

    Players[playerId] = player

    deferrals.update('Checking your account status...')
    
    showWelcomeCard(deferrals, player)

    player:log('playerConnecting', 'is connecting.')
end)

-- A server-side event that is triggered when a player has a finally-assigned NetID.
-- https://docs.fivem.net/docs/scripting-reference/events/server-events/#playerjoining
AddEventHandler('playerJoining', function(oldPlayerId)
    oldPlayerId = tonumber(oldPlayerId) -- ? why tf would oldPlayerId be a string?
    local playerId = source
    local player = Players[oldPlayerId]

    player.Id = playerId

    Players[playerId] = player
---@diagnostic disable-next-line: need-check-nil
    Players[oldPlayerId] = nil -- Remove the temporary player object

    TriggerEvent('player:loggedIn', playerId) -- Now we're good

    player:log('playerJoining', 'is joining.')
end)

AddEventHandler('player:loggedIn', function(playerId)
    local player = Players[playerId]

    -- Ok let's tell the client to spawn at their last known position, if they have one
    -- TODO: I forgot to add the heading to the last position
    print(player.Account.last_position)
    TriggerClientEvent('player:spawn', playerId, json.decode(player.Account.last_position), 0.0)

    player.Spawned = true

    Player(playerId).state.username = player.Account.username

    player:log('playerLoggedIn', 'has logged in.')
end)

AddEventHandler('playerDropped', function()
    local playerId = source
    local player = Players[playerId]

    if player?.Account then -- A player might not have logged in yet when they exited
        local playerCoords = player:GetCoords()
        local playerHeading = player:GetHeading()

        MySQL.update('UPDATE accounts SET last_position = ? WHERE id = ?', {
            json.encode(playerCoords),
            player.Account.id
        })
    end

    player:log('playerDropped', 'dropped')

    Players[playerId] = nil
end)

exports('Get', function(playerId)
    local player = Players[playerId]

    if not player then lib.print.error('Unknown player id: ' .. playerId) end
        
    return player
end)

-- * This is mainly for when this resource is restarted, so we can get the player's account from the database and re-add them to the Players table
for playerId in ipairs(GetPlayers()) do
    local playerUsername = Player(playerId).state.username

    local account = MySQL.query.await('SELECT * FROM accounts WHERE username = ?', { playerUsername })

    if account then Players[playerId] = account end
end