# LastPass 2FA CLI authenticator

This commandline tool can be used on Windows, MacOS and Linux systems to get a 2FA TOTP code from the console or terminal. The secrets are downloaded from [lastpass.com](https://lastpass.com) and stored safely in an encrypted file (default `vault.dat`).

## Quickstart

1. Put the executable (`2fa.exe` for windows, `2fa` for MacOs / Linux) and dependencies somewhere in your path.

   Either edit the `appsettings.json` file to point to a location where you want to store your MFA vault, or use the `-i` (or `--file`) option when invoking the command to specify where the vault is (to be) stored.

2. Download the vault

   `2fa download -u username@domain.com -o 123456`
   
   The `-o` or `--otp` option specifies the current OTP code (if any). Use `-i` or `--file` to specify a vault location when you want to override use the setting from `appsettings.json`.
   
   You will be asked for your LastPass master password (to be able to download the vault) and a password to use when encrypting your offline copy of the MFA vault so your data is secure. Confirm the vault password and wait for the vault to be downloaded.

3. From now on you can lookup 2fa codes:

    `2fa list`

    or, when you're looking for a specific entry:

    `2fa list -f <searchstring>`

    or, when you want to use specify the vault to use:

    `2fa list -i /path/to/vault.dat -f <searchstring>`

    You will be asked for the local vault password that was used to encrypt the vault in step 2 and will then be shown all accounts (`-f` or `--find` filters the results to `<searchstring>`) with the current TOTP code.

Whenever new 2FA codes are added to LastPass Authenticator, simply refresh your vault:

`2fa refresh -u username@domain.com -o 123456`

Note that the commands `download`, `refresh` and `update` are synonyms for the same action.

## Credits

MFA vault download based on Donny Maasland's [Lastpass Authenticator Export](https://github.com/dmaasland/lastpass-authenticator-export).

## FAQ

**Q**: _Why can't I pass the password as an argument like `2fa list -f <searchstring> --password sup3rs3cr3t`?**_

**A**: Because that would store your password in your terminal's history which is a security risk. Also because it prevents using this tool non-interactively (which could be dangerous when some malicious code tries to run this tool unseen in the background).

## License

Licensed under MIT license. See [LICENSE](LICENSE) for details.

---

Icon made by [Freepik](http://www.flaticon.com/authors/freepik) from [www.flaticon.com](http://www.flaticon.com) is licensed by [CC 3.0](http://creativecommons.org/licenses/by/3.0/).