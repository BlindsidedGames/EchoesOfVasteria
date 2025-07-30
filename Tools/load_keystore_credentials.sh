#!/usr/bin/env bash
# Load keystore credentials into the current shell
CRED_FILE="$(dirname "$0")/../.keystore_credentials"
if [ -f "$CRED_FILE" ]; then
    set -a
    source "$CRED_FILE"
    set +a
else
    echo "Keystore credential file not found: $CRED_FILE" >&2
fi
