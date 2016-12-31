#nmssavetool
A command line tool to decrypt, encrypt, and modify No Man's Sky game save files
===============================================

Created by [Matthew Humphrey](https://github.com/matthew-humphrey)

This is a simple tool to allow decoding, encoding, and convenient editing operations
on saves for the No Man's Sky game.

Download [precompiled binary](https://www.mediafire.com/?qfgxf7dun3zr6b7)

##Usage

Run "nmssavetool -h" for help.

```
nmssavetool [ -h|--help ] [ --version ] [ -g|--game-mode G ] [ -f|--decrypted-file F ] [ --v1-format  ] COMMAND {Decrypt|Encrypt|Refill}

Positional Arguments:
 Command               Command to perform (Decrypt|Encrypt|Refill).

Optional Arguments:
 --v1-format           When encrypting a save file, use the old NMS V1 format
 -f, --decrypted-file  Specifies the destination file for decrypt or the source file for encrypt.
 -g, --game-mode       Use saves for which game mode (Normal|Surival|Creative)
 -h, --help            Display this help document.
 --version             Displays the version of the current executable.
```

##Supported commands

* Decrypt - Decrypt the latest save game for the specified game mode (normal/survival/creative) and write it to a file whose location you specify.
* Encrypt - Encrypt the file you specify and write it to the latest save game slot for the specified game mode.
* Refill - Edit the latest game save slot for the specified game mode to maximize inventory and technology levels for suit, ship, and freighter.

##Changelog

###2016-12-30 1.0.0.0

* Initial release