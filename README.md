# RDLMINT
A (dis)assembler for Kirby's Return to Dream Land's MINT bytecode 

## Current Features
* Can unpack and disassemble MINT Archives
* Can recompile and repack MINT XBIN scripts

## Usage
```RDLMINT.exe <action> [options]```
### Actions
```-x <file>```

* Extracts and decompiles a MINT Archive.bin file or individual script

```-r <folder|file>```

* Packs and recompiles a MINT Archive.bin file or individual script

```-rdb <folder|file>```

* Packs and recompiles a MINT Archive.bin file or individual script with debug comment support

```-h```

* Prints help message to console

### Debug Comments
By using `/#`, you can declare all following text as a debug comment. These act like regular comments, but when building using the `-rdb` action, become:

```
loadString rXX, "comment"
setArg [00] rXX
call Mint.Debug.puts(string)
```

This prints the comment to the console (In Dolphin, use View > Show Log and turn on OSReport in the Log Type list).

Register count in the `declare` command are automatically increased by 1, and the new register is used specifically for debug comment printing, so nothing is accidentally broken.

If not using the `-rdb` build action, they will be treated as normal comments and will be ignored when building.

## Credits
* FruitMage - Programming
* BenHall - Programming
* Fireyfly - Opcode research and documentation, created MINT Explorer (was used as reference)
* Reserved - XBIN/MINT research
* DarkKirb - Official Opcode names, functions, and research
