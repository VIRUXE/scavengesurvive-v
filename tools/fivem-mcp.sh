#!/bin/sh
# fivem-mcp's bin is a symlink. The server only starts when argv[1] is the
# real index.mjs, so launching the symlink name exits before the handshake.
bin=$(command -v fivem-mcp) || {
  echo "fivem-mcp is not on PATH. npm install -g fivem-mcp@0.2.0" >&2
  exit 1
}
real=$(readlink -f "$bin")
exec node "$real" "$@"
