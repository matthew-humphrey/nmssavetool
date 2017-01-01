#nmssavetool
A command line tool to decrypt, encrypt, and modify No Man's Sky game save files
===============================================

Created by [Matthew Humphrey](https://github.com/matthew-humphrey)

This is a simple tool to allow decoding, encoding, and convenient editing operations
on saves for the No Man's Sky game.

Download [precompiled binary](http://www.mediafire.com/file/qfgxf7dun3zr6b7/nmssavetool-1.1.zip)

##Usage

Run "nmssavetool help" for help.

```
> nmssavetool help

nmssavetool 1.1.0.0
Copyright c  2016

  decrypt    Decrypt the latest game save slot and write it to a formatted JSON file.

  encrypt    Encrypt a JSON file and write it to the latest game save slot.

  modify     Modify one or more attributes of a game save.

  help       Display more information on a specific command.

  version    Display version information.
```

##Supported commands

* decrypt - Decrypt the latest save game for the specified game mode (normal/survival/creative) and write it to a file whose location you specify.
* encrypt - Encrypt the file you specify and write it to the latest save game slot for the specified game mode.
* modify - Edit the latest game save slot for the specified game mode to repair damage, maximize energy levels, and/or maximize inventory levels.
* help - Provide help on command-line syntax for any command.
* version - Displays the program's version information.

##Changelog

###2016-12-30 1.0.0.0

* Initial release

###2016-12-30 1.1.0.0

* Added damage repair to Refill Command
* Switched to a new command-line parsing library
* Moved to a verb-style command line interface, and provide more fine-grained control over modifications.
* Minor refactoring to reduce duplicated code
