#!/bin/bash

# ################ Mod build and install script (Unix version) ################
#
# Cross-platform deployment script for RimWorld mods
# Call this script from your build process or run manually:
# ./install.sh [Debug|Release] [project_dir] [project_name] [folders] [files]
#
# Auto-detects Steam installation on macOS and Linux
# Falls back to user-provided paths if auto-detection fails

set -e  # Exit on any error

# Configuration
CONFIG=${1:-Release}
SOLUTION_DIR=${2:-$(pwd)/}
PROJECT_NAME=${3:-Progress-Renderer}
FOLDERS=${4:-"About Common v1.4 v1.5 v1.6"}
FILES=${5:-"LoadFolders.xml"}

# Ensure SOLUTION_DIR ends with /
if [[ ! "$SOLUTION_DIR" =~ /$ ]]; then
    SOLUTION_DIR="${SOLUTION_DIR}/"
fi

echo "Configuration: $CONFIG"
echo "Solution Directory: $SOLUTION_DIR"
echo "Project Name: $PROJECT_NAME"

# Function to detect Steam installation
detect_steam_path() {
    local steam_path=""
    
    if [[ "$OSTYPE" == "darwin"* ]]; then
        # macOS
        local possible_paths=(
            "$HOME/Library/Application Support/Steam/steamapps/common"
            "/Applications/Steam.app/Contents/MacOS/steamapps/common"
            "/Users/Shared/Steam/steamapps/common"
        )
    else
        # Linux
        local possible_paths=(
            "$HOME/.steam/steam/steamapps/common"
            "$HOME/.local/share/Steam/steamapps/common" 
            "/usr/games/steamapps/common"
            "/opt/steam/steamapps/common"
        )
    fi
    
    for path in "${possible_paths[@]}"; do
        if [[ -d "$path/RimWorld" ]]; then
            steam_path="$path"
            break
        fi
    done
    
    echo "$steam_path"
}

# Detect or set RimWorld directory
STEAM_DIR=$(detect_steam_path)

if [[ -n "$STEAM_DIR" && -d "$STEAM_DIR/RimWorld" ]]; then
    RIMWORLD_DIR_STEAM="$STEAM_DIR/RimWorld"
    echo "Auto-detected RimWorld Steam installation: $RIMWORLD_DIR_STEAM"
    
    # Check if RimWorldMac.app exists (macOS packaged version)
    if [[ -d "$RIMWORLD_DIR_STEAM/RimWorldMac.app" ]]; then
        TARGET_DIR="$RIMWORLD_DIR_STEAM/RimWorldMac.app/Mods/$PROJECT_NAME"
        echo "Using macOS packaged mods directory: $TARGET_DIR"
    else
        TARGET_DIR="$RIMWORLD_DIR_STEAM/Mods/$PROJECT_NAME"
        echo "Using standard mods directory: $TARGET_DIR"
    fi
else
    echo "Could not auto-detect RimWorld installation."
    echo "Please set RIMWORLD_DIR_STEAM environment variable to your RimWorld installation path."
    echo "Example paths:"
    if [[ "$OSTYPE" == "darwin"* ]]; then
        echo "  export RIMWORLD_DIR_STEAM=\"$HOME/Library/Application Support/Steam/steamapps/common/RimWorld\""
    else
        echo "  export RIMWORLD_DIR_STEAM=\"$HOME/.steam/steam/steamapps/common/RimWorld\""
    fi
    
    if [[ -z "$RIMWORLD_DIR_STEAM" ]]; then
        echo "Exiting: No RimWorld installation found and RIMWORLD_DIR_STEAM not set."
        exit 1
    fi
    
    # Check if manually set path includes RimWorldMac.app
    if [[ -d "$RIMWORLD_DIR_STEAM/RimWorldMac.app" ]]; then
        TARGET_DIR="$RIMWORLD_DIR_STEAM/RimWorldMac.app/Mods/$PROJECT_NAME"
    else
        TARGET_DIR="$RIMWORLD_DIR_STEAM/Mods/$PROJECT_NAME"
    fi
fi

# Remove the duplicate TARGET_DIR assignment since it's now set above

echo "Target directory: $TARGET_DIR"

# Create target directory if it doesn't exist
mkdir -p "$TARGET_DIR"

# Copy folders
echo "Copying mod folders..."
for folder in $FOLDERS; do
    if [[ -d "$SOLUTION_DIR$folder" ]]; then
        echo "  Copying $folder..."
        cp -r "$SOLUTION_DIR$folder" "$TARGET_DIR/"
    else
        echo "  Warning: Folder $folder not found, skipping..."
    fi
done

# Copy files
echo "Copying mod files..."
for file in $FILES; do
    if [[ -f "$SOLUTION_DIR$file" ]]; then
        echo "  Copying $file..."
        cp "$SOLUTION_DIR$file" "$TARGET_DIR/"
    else
        echo "  Warning: File $file not found, skipping..."
    fi
done

echo "Mod installation completed successfully!"
echo "Mod installed to: $TARGET_DIR"

# Optional: Create archive (if zip is available)
if command -v zip >/dev/null 2>&1; then
    echo "Creating archive..."
    cd "$(dirname "$TARGET_DIR")"
    zip -r "${PROJECT_NAME}.zip" "$PROJECT_NAME" >/dev/null 2>&1
    echo "Archive created: $(dirname "$TARGET_DIR")/${PROJECT_NAME}.zip"
fi