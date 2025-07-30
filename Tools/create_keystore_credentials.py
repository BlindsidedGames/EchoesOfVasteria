#!/usr/bin/env python3
import os
import getpass


def main():
    path = input("Keystore path: ").strip()
    alias = input("Key alias name: ").strip()
    keystore_pass = getpass.getpass("Keystore password: ")
    key_pass = getpass.getpass("Key alias password: ")

    with open('.keystore_credentials', 'w') as f:
        f.write(f"UNITY_KEYSTORE_PATH={path}\n")
        f.write(f"UNITY_KEY_ALIAS_NAME={alias}\n")
        f.write(f"UNITY_KEYSTORE_PASS={keystore_pass}\n")
        f.write(f"UNITY_KEY_PASS={key_pass}\n")
    os.chmod('.keystore_credentials', 0o600)
    print('Credentials saved to .keystore_credentials (gitignored).')


if __name__ == '__main__':
    main()
