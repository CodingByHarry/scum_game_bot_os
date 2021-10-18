# SCUM Game Bot

We created this bot for our own personal SCUM server and never intended to open source it. It was created in a way to just work for our setup with little regard for good principles and efficiency. Please keep this in mind when using it and make your own adjustments as needed.

There are 3 parts to this project
 - [Discord Bot (this page)](https://github.com/CodingByHarry/scum_discord_bot_os)
 - [Log Parser](https://github.com/CodingByHarry/scum_log_parser_os)
 - [SCUM Game Bot](https://github.com/CodingByHarry/scum_game_bot_os)

This part of the bot needs to be ran on a PC or server 24/7 and handles running the admin commands to deliver players items as well as getting squad info & automatic announcements. We found a server which was able to run the game client 24/7 for about $99usd per month.

Unfortunately, we never got around to writing the automatic reconnect part so on each restart you need to manually reconnect the bot. I'd strongly suggest this as the first feature to add if you're planning on doing any.

## Getting started

 - Install Microsoft Visual Studio
 - Update `DBConnect.cs` and `Drone.cs` with the correct MySQL connection string
 - Update `Drone.cs` line 285 with the correct FTP connection string
 - Update `Drone.cs' line 18 with the STEAMID of the client running the Game Bot
 - Compile the updated program

First time you run the bot it will create a file `identity.drone` which you need to grab the contents of and create a new row in the drones database table with the state of "active". This was a bit of a legacy thing as we sometimes found multiple bots running at once which would result in weird things.

One issue we ran in to early on when running the game bot on dedicated hardware was when you disconnect from the server, it would also cause the game to stop running. We fixed this by only disconnecting from the server with a disconnect.bat file with the below contents.

```bat
for /f "skip=1 tokens=3" %%s in ('query user %USERNAME%') do (tscon.exe %%s /dest:console)
```

## Contribute / Licensing / Credits
This bot is not to be used in a commercial scenario or for a profit driven server. This was released for other SCUM server owners to setup and use for free without being preasured in to paying for it by other SCUM bots. We would appreciate you crediting us (the authors) although not required.

Feel free to open a pull request if you think there are changes that should be made. I'll review them eventually.

Credits to [myself](https://github.com/CodingByHarry/) and [Daniel](https://github.com/danieldraper) as well as the SCUMTOPIA community for their support and testing. For the SCUM Game Bot, we did take inspiration for the start from somewhere, however I'm not able to find the exact repo. If you recognise some of your code in here, send me a message and I'll add you here.
