#!/bin/sh
# Set PATH to include dotnet tools
export PATH="/root/.dotnet/tools:$PATH"

# If no command is provided, start an interactive shell with PATH set
if [ $# -eq 0 ]; then
    exec /bin/sh
else
    # Execute the command passed to the entrypoint
    exec "$@"
fi

