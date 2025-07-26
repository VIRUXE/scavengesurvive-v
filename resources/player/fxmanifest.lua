fx_version 'cerulean'
game 'gta5'

lua54 'yes'

server_scripts {
    '@oxmysql/lib/MySQL.lua',
    'server/Player.lua',
    'server/main.lua'
}

shared_scripts {
    '@ox_lib/init.lua',
    'shared/spawn_locations.lua'
}

client_scripts {
    'client/*.lua'
}