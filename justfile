export NUCLEAR_OPTION_GAME := env_var_or_default("NUCLEAR_OPTION_GAME", "/var/nuclearoption")

# Compile against the assemblies in the selected game installation.
build:
    #!/usr/bin/env bash
    set -euo pipefail

    DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-/tmp/fs45-meridian-dotnet}" dotnet build Fs45Meridian.csproj -v:minimal

# Build and copy the plugin into the selected game's BepInEx folder.
install: build
    #!/usr/bin/env bash
    set -euo pipefail

    plugin_dir="$NUCLEAR_OPTION_GAME/BepInEx/plugins"
    if [[ ! -d "$plugin_dir" ]]; then
      printf 'Missing BepInEx plugins directory: %s\n' "$plugin_dir" >&2
      exit 1
    fi

    install -m 644 bin/Fs45Meridian.dll "$plugin_dir/Fs45Meridian.dll"
    printf '%s\n' "$plugin_dir/Fs45Meridian.dll"
