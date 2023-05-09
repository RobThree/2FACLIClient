# LastPass 2FA CLI authenticator

## Usage

* Put the executable (`2fa.exe` for windows, `2fa` for MacOs / Linux) somewhere in your path.
* Use [Lastpass Authenticator Export](https://github.com/dmaasland/lastpass-authenticator-export) ([fork here](https://github.com/RobThree/lastpass-authenticator-export/tree/main)) to download the secrets file (`export.json`).
* Place the `export.json` file in a known directory.
* Encrypt the `export.json` file with the following command:<br>
    `2fa encrypt -i /path/to/export.json`<br>
  You will be asked for a password and to confirm the entered password. Then, this command will encrypt the `export.json` file **in-place** (i.e. the file is overwritten with it's encrypted equivalent).
* From now on you can lookup 2fa codes:<br>
    `2fa find -i /path/to/export.json -f <searchstring>`<br>
    You will be asked for the password used to encrypt the file in the previous step and will then be shown all accounts matching `<searchstring>` with the current TOTP code.

Whenever new 2FA codes are added to LastPass Authenticator, simply download a new export and encrypt it again.

## Roadmap

* Implement downloading export file in this tool so we no longer need to rely on a completely different environment (python) to be installed as well.

## FAQ

1. **Q: Why can't I pass the password as an argument like `2fa find -i /path/to/export.json -f <searchstring> --password supers3cret`?**<br> 
  A: Because that would store your password in your terminal's history which is a security risk. Also because it prevents using this tool non-interactively (which could be dangerous when some malicious code tries to run this tool unseen in the background).

## License

Licensed under MIT license. See [LICENSE](LICENSE) for details.

---

Icon made by [Freepik](http://www.flaticon.com/authors/freepik) from [www.flaticon.com](http://www.flaticon.com) is licensed by [CC 3.0](http://creativecommons.org/licenses/by/3.0/).