# LastPass 2FA CLI authenticator

## Usage

* Put the executable (`2fa.exe` for windows, `2fa` for MacOs / Linux) somewhere in your path.
* Use [Lastpas Authenticator Export](https://github.com/dmaasland/lastpass-authenticator-export) ([fork here](https://github.com/RobThree/lastpass-authenticator-export/tree/main)) to download the secrets file (`export.json`).
* Place the `export.json` file in some directory
* Encrypt the `export.json` file with the following command:<br>

    `2fa encrypt -i /path/to/export.json`
  
  You will be asked for a password and to confirm the entered password. Then, this command will encrypt the `export.json` file **in-place** (i.e. the file is overwritten with it's encrypted equivalent).
* From now on you can lookup 2fa codes:<br>

    `2fa find -i /path/to/export.json -f <searchstring>`

Whenever new 2FA codes are added to LastPass Authenticator, simply download a new export and encrypt it again.

## Roadmap

* Implement downloading export file in this tool so we no longer need to rely on a completely different environment (python) to be installed as well.

## License

Licensed under MIT license. See [LICENSE](LICENSE) for details.

---

Icon made by [Freepik](http://www.flaticon.com/authors/freepik) from [www.flaticon.com](http://www.flaticon.com) is licensed by [CC 3.0](http://creativecommons.org/licenses/by/3.0/).