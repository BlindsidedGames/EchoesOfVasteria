#!/bin/bash

set -e

echo "Passed params $0 $1 ..."

FRAMEWORK_NAME="$1"

if [ -z "$FRAMEWORK_NAME" ]; then
    echo "No framework name provided. Usage: ./script.sh <FrameworkName.framework>"
    exit 1
fi

echo "Copying dependency: $FRAMEWORK_NAME"

DEPENDENCIES_DIR="${BUILD_DIR}/Dependencies"
DESTINATION="${TARGET_BUILD_DIR}"

# Exit if dependencies folder doesn't exist
if [ ! -d "$DEPENDENCIES_DIR" ]; then
    echo "Dependencies folder not found: $DEPENDENCIES_DIR. Skipping copy."
    exit 0
fi

FRAMEWORK_PATH="${DEPENDENCIES_DIR}/${FRAMEWORK_NAME}"
DEST_FRAMEWORK="${DESTINATION}/${FRAMEWORK_NAME}"

# Check if the requested framework exists
if [ ! -d "$FRAMEWORK_PATH" ]; then
    echo "Framework not found at: $FRAMEWORK_PATH. Skipping copy."
    exit 0
fi

# Copy the framework
echo "Copying framework: $FRAMEWORK_NAME"
rm -rf "$DEST_FRAMEWORK"
cp -R "$FRAMEWORK_PATH" "$DEST_FRAMEWORK"

# Copy dSYM if available
DSYM_PATH="${FRAMEWORK_PATH}.dSYM"
if [ -d "$DSYM_PATH" ]; then
    DSYM_NAME="$(basename "$DSYM_PATH")"
    DEST_DSYM="${DESTINATION}/${DSYM_NAME}"
    echo "Copying dSYM: $DSYM_NAME"
    rm -rf "$DEST_DSYM"
    cp -R "$DSYM_PATH" "$DEST_DSYM"
fi

echo "Copied $FRAMEWORK_NAME and dSYM (if exists) successfully."