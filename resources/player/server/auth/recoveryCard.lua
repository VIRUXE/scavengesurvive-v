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

return showRecoveryCard