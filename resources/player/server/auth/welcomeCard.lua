local showRecoveryCard = require 'server.auth.recoveryCard'

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

return showWelcomeCard