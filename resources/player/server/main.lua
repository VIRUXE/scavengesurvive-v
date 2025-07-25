Players = {}

local function parseIdentifier(identifier)
    local sep = string.find(identifier, ':', 1, true)
    if not sep then return nil end
    return { type = string.sub(identifier, 1, sep - 1), value = string.sub(identifier, sep + 1) }
end

-- Handle player joining
AddEventHandler('playerConnecting', function(name, setKickReason, deferrals)
    deferrals.defer()

    local playerId = source
    local playerIdentifiers = GetPlayerIdentifiers(playerId)
    local playerTokens = GetPlayerTokens(playerId)

    -- print('Player ' .. name .. ' is connecting with identifiers: ' .. json.encode(playerIdentifiers))
    -- print('Player ' .. name .. ' is connecting with tokens: ' .. json.encode(playerTokens))

    deferrals.update('Checking your account status...')

    local showLoginCard
    local showRegistrationCard
    local showAccountChoiceCard
    local showRecoveryCard

    showAccountChoiceCard = function(account)
        deferrals.presentCard({
            type = 'AdaptiveCard',
            version = '1.3',
            body = {
                {
                    type = 'TextBlock',
                    text = 'Welcome back, ' .. account.username .. '!',
                    weight = 'bolder',
                    size = 'Medium'
                },
                {
                    type = 'TextBlock',
                    text = 'Would you like to log in as this user or use a different account?',
                    wrap = true
                }
            },
            actions = {
                {
                    type = 'Action.Submit',
                    title = 'Continue as ' .. account.username,
                    data = { action = 'login_as_user' }
                },
                {
                    type = 'Action.Submit',
                    title = 'Use another account',
                    data = { action = 'use_another_account' }
                }
            }
        }, function(data, rawData)
            if not data then
                deferrals.done('Connection closed.')
                return
            end

            if data.action == 'login_as_user' then
                showLoginCard(account)
            elseif data.action == 'use_another_account' then
                showRegistrationCard()
            else
                deferrals.done()
            end
        end)
    end

    showLoginCard = function(account, message)
        local cardBody = {
            {
                type = 'TextBlock',
                text = 'Welcome back, ' .. account.username .. '!',
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
                                id = 'password',
                                label = 'Password',
                                placeholder = 'Enter your password',
                                style = 'password'
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
                    title = 'Login',
                    data = { action = 'login' }
                },
                {
                    type = 'Action.Submit',
                    title = 'Back',
                    data = { action = 'back_to_account_choice' }
                }
            }
        }, function(data, rawData)
            if not data then
                deferrals.done('Connection closed.')
                return
            end

            if data.action == 'login' then
                local password = data.password
                if not password or password == '' then
                    showLoginCard(account, 'Password cannot be empty.')
                    return
                end

                local dbPassword = MySQL.scalar.await('SELECT password FROM accounts WHERE id = ?', { account.id })

                if dbPassword and MySQL.scalar.await('SELECT SHA2(?, 256) = ?', { password, dbPassword }) then
                    for _, identifier in ipairs(playerIdentifiers) do
                        local parsedId = parseIdentifier(identifier)
                        if parsedId then
                            MySQL.insert.await('INSERT IGNORE INTO account_identifiers (account_id, type, value) VALUES (?, ?, ?)', {
                                account.id,
                                parsedId.type,
                                parsedId.value
                            })
                        end
                    end

                    for _, token in ipairs(playerTokens) do
                        MySQL.insert.await('INSERT IGNORE INTO account_tokens (account_id, token) VALUES (?, ?)', {
                            account.id,
                            token
                        })
                    end

                    Players[playerId] = account
                    deferrals.done()
                else
                    showLoginCard(account, 'Incorrect password.')
                end
            elseif data.action == 'back_to_account_choice' then
                showAccountChoiceCard(account)
            else
                deferrals.done()
            end
        end)
    end

    showRecoveryCard = function(message)
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
                    data = { action = 'back_to_registration' }
                }
            }
        }, function(recoveryData)
            if not recoveryData then
                deferrals.done('Connection closed.')
                return
            end

            if recoveryData.action == 'back_to_registration' then
                showRegistrationCard()
                return
            end

            local email = recoveryData.email
            if not email or email == '' then
                showRecoveryCard('E-mail address cannot be blank.')
                return
            end

            if not string.match(email, "^[%w_.-]+@[%w_.-]+%.[%w]{2,6}$") then
                showRecoveryCard('Please enter a valid e-mail address.')
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

    showRegistrationCard = function(message, username, password)
        local cardBody = {
            {
                type = 'TextBlock',
                text = 'Welcome to the Server!',
                weight = 'bolder',
                size = 'medium'
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
                        type = 'Column',
                        width = 1,
                        items = {
                            {
                                type = 'Input.Text',
                                id = 'username',
                                label = 'Username',
                                placeholder = 'Enter your username',
                                value = username
                            }
                        }
                    },
                    {
                        type = 'Column',
                        width = 1,
                        items = {
                            {
                                type = 'Input.Text',
                                id = 'password',
                                label = 'Password',
                                placeholder = 'Enter your password',
                                style = 'password',
                                value = password
                            }
                        }
                    },
                    { type = 'Column', width = 2 }
                }
            }
        }
    
        if message then cardBody[2].color = 'attention' end
    
        deferrals.presentCard({
            type = 'AdaptiveCard',
            version = '1.3',
            body = cardBody,
            actions = {
                {
                    type = 'Action.Submit',
                    title = 'Login',
                    data = { action = 'login' }
                },
                {
                    type = 'Action.Submit',
                    title = 'Register',
                    data = { action = 'register' }
                },
                {
                    type = 'Action.Submit',
                    title = 'Forgot Password?',
                    data = { action = 'recover' }
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
                    showRegistrationCard('Username and password cannot be empty.', username, password)
                    return
                end
    
                local account = MySQL.single.await('SELECT * FROM accounts WHERE username = ?', { username })
    
                if account then
                    if MySQL.scalar.await('SELECT SHA2(?, 256) = ?', { password, account.password }) then
                        for _, identifier in ipairs(playerIdentifiers) do
                            local parsedId = parseIdentifier(identifier)
                            if parsedId then
                                MySQL.insert.await('INSERT IGNORE INTO account_identifiers (account_id, type, value) VALUES (?, ?, ?)', { account.id, parsedId.type, parsedId.value })
                            end
                        end
    
                        for _, token in ipairs(playerTokens) do
                            MySQL.insert.await('INSERT IGNORE INTO account_tokens (account_id, token) VALUES (?, ?)', { account.id, token })
                        end

                        Players[playerId] = account
                        deferrals.done()
                    else
                        showRegistrationCard('Incorrect password.', username, password)
                    end
                else
                    showRegistrationCard('Account not found. Please try again or register.', username, password)
                end
            elseif data.action == 'register' then
                local username = data.username
                local password = data.password
    
                if not username or not password or username == '' or password == '' then
                    showRegistrationCard('Username and password cannot be empty.', username, password)
                    return
                end
    
                deferrals.presentCard({
                    type = 'AdaptiveCard',
                    version = '1.3',
                    body = {
                        {
                            type = 'TextBlock',
                            text = 'Registration',
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
                        showRegistrationCard('E-mail cannot be empty.', username, password)
                        return
                    end
    
                    local accountId = MySQL.insert.await('INSERT INTO accounts (username, password, email) VALUES (?, SHA2(?, 256), ?)', { username, password, email })
    
                    if accountId then
                        for _, identifier in ipairs(playerIdentifiers) do
                            local parsedId = parseIdentifier(identifier)
                            if parsedId then
                                MySQL.insert.await('INSERT INTO account_identifiers (account_id, type, value) VALUES (?, ?, ?)', { accountId, parsedId.type, parsedId.value })
                            end
                        end
    
                        for _, token in ipairs(playerTokens) do
                            MySQL.insert.await('INSERT INTO account_tokens (account_id, token) VALUES (?, ?)', { accountId, token })
                        end

                        Players[playerId] = { id = accountId, username = username, email = email }
                        deferrals.done()
                    else
                        showRegistrationCard('An error occurred during registration. The username might be taken.', username, password)
                    end
                end)
            elseif data.action == 'recover' then
                showRecoveryCard()
            else
                deferrals.done()
            end
        end)
    end

    local account
    for _, identifier in ipairs(playerIdentifiers) do
        local parsedId = parseIdentifier(identifier)
        if parsedId then
            account = MySQL.single.await('SELECT a.* FROM accounts a JOIN account_identifiers ai ON a.id = ai.account_id WHERE ai.type = ? AND ai.value = ?', { parsedId.type, parsedId.value })
            if account then
                break
            end
        end
    end

    if account then
        showAccountChoiceCard(account)
    else
        showRegistrationCard()
    end
end)

-- Handle player joining
AddEventHandler('playerJoining', function()
    -- spawnedPlayers[source] = false

    lib.print.debug('Player ' .. GetPlayerName(source) .. ' is joining')
end)

AddEventHandler('playerLoggedIn', function()
    local playerId = source
    spawnedPlayers[playerId] = true

    local account = Players[playerId]

    if account then
        if account.last_position then
            TriggerClientEvent('player:spawn', playerId, json.decode(account.last_position), 0.0)
        else
            TriggerClientEvent('player:spawn', playerId)
        end
    else
        TriggerClientEvent('player:spawn', playerId)
    end

    lib.print.info('Player ' .. GetPlayerName(playerId) .. ' spawned')
end)

AddEventHandler('playerDropped', function()
    local playerId = source
    local account = Players[playerId]

    if account then
        MySQL.update('UPDATE accounts SET last_position = ? WHERE id = ?', {
            json.encode(GetEntityCoords(GetPlayerPed(playerId))),
            account.id
        })
    end

    Players[playerId] = nil
end)

-- Export function to check if player is spawned
--[[ exports('isPlayerSpawned', function(playerId)
    return spawnedPlayers[playerId] or false
end) ]]

-- Export function to get all spawned players
--[[ exports('getSpawnedPlayers', function()
    return spawnedPlayers
end) ]]