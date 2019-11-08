# CSGO_Event_Parser
A event parser (or extractor) to parse into a json file for my Java "CSGO Event Finder" program

The program itself can be found in my discord server:
https://discord.gg/bfrGfJ8
Under #software_releases channel

This program may not be updated as often as the software itself... This is just to show how I extracted the events from 3 different .res files from the games .vpk files into 1 json file... Enjoy!

## Project
This program is designed to help the Java program (CSGO Event Finder) to show and filter all the events that can be accessed with Hammer.

This program is written in C# (Not the note, haha! Didn't laugh... Did you?)

## Regex
Regex has been used for matching events.
The last build (0.2) didn't use regex to find patterns, which caused problems when using the Java application.

The following regex(s) patterns are:
### Pattern to find Event name and Comment (If any)
```
^(?:\s)?\"(.+)\"(?:\s+|\n)?(?://\s(.+))?
```
### Pattern to find Attribute(s) name, type and comment (If any)
```
^(?:\s+)\"(.*?)\"(?:\s+)\"(.*?)\"(?:\s+//\s(.+))?
```
## Events
The events were extracted from pak01_dir.vpk from the CSGO installation folder
The following events (You can access them from this repository!) are found in Events folder from above!
| Event | Event .res File | Type |
| :-------------:|:-------------:| :-----:|
| Mod Events | modevents.res | Mod Events |
| Server Events | serverevents.res  |   Server-related events |
| Game Events| gamevents.res |  Client-related events|

## Note
The program.cs doesn't have any direct file locations, please replace the "..." of where you plan to store these files! Thank you.
