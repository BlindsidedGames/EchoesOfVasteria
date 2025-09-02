#!/bin/bash

set -e

FRAMEWORK="${PRODUCT_NAME}.framework"
DSYM="${FRAMEWORK}.dSYM"

SOURCE_FRAMEWORK="${TARGET_BUILD_DIR}/${FRAMEWORK}"
SOURCE_DSYM="${DWARF_DSYM_FOLDER_PATH}/${DSYM}"

DEST="${BUILD_DIR}/Dependencies"

mkdir -p "$DEST"

# Copy framework
if [ -d "$SOURCE_FRAMEWORK" ]; then
    echo "Copying framework: $SOURCE_FRAMEWORK -> $DEST"
    rm -rf "${DEST}/${FRAMEWORK}"
    cp -R "$SOURCE_FRAMEWORK" "$DEST/"
else
    echo "⚠️ Framework not found at: $SOURCE_FRAMEWORK"
fi

# Copy dSYM if exists
if [ -d "$SOURCE_DSYM" ]; then
    echo "Copying dSYM: $SOURCE_DSYM -> $DEST"
    rm -rf "${DEST}/${DSYM}"
    cp -R "$SOURCE_DSYM" "$DEST/"
else
    echo "ℹ️ No dSYM found to copy."
fi