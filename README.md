# ClimbTrials

I have been watching too many Trackmania videos on youtube the past year. So then I thought why are there no in-game leaderboards in Celeste. Then I made this mod.

## What is ClimbTrials?

- Speedrun and optimize sections of gameplay in Celeste
- In some trials you might have to collect things on the way
- In some trials you might have to deal with modifiers, like dashless, grabless, etc.
- Aim for goal times in each trial to earn a bronze/silver/gold/... heart for that trial.
- PB replays can be rewatched, raced against and submitted to the public in-game leaderboards.
- Watch, race or compare other players PBs in-game

## Important please read

### Data shared with the server

When you install this mod and open it for the first time, you will be automatically registered to the leaderboard server.
Finishing a trial and getting a PB sends your time, inputs and run data and currently loaded mods + versions to the leaderboard server for validation.
If you don't want that, you can turn on the offline mode in mod settings.
You can still play the trials and beat your PB, but any features that needs a connection to the server will no longer work!
Offline PBs can still be submitted after the fact if you turn offline mode back off.

### Playing with other mods loaded

One thing that was important to me was making sure that the replay system is as robust as I can make it, so that replays don't randomly desync for others.
This also means I am very defensive/restrictive by default with what mods are allowed to be loaded when playing and submitting to the public leaderboards.

**Currently no other mods are allowed to be loaded to submit runs** (with the exception of SpeedrunTool for practice). This includes skins, maps, etc..
I hope to improve on that in the future, and feedback from players will help me make priorities.

### Playing on multiple devices

If you want to play on multiple installations/devices, follow these steps:
- (If you havent already) start the game with the mod installed on the first device, open the ClimbTrials menu to register on the server, optionally set a player name for the leaderboards.
- Close the game and open `Saves/modsettings-ClimbTrials.celeste` in your Celeste installation folder
- Copy (at least) the first 3 lines of this file, which should consist of your chosen `PlayerName`, the flag `PlayerNameChosen` and your `ClientId`.
  - Important!!! Consider your `ClientId` to be like a password! **If you share it anywhere, anyone could play and submit under your name/identity!**
- On your second installation/device, either create `Saves/modsettings-ClimbTrials.celeste` if it doesn't exist and paste the copied lines, or replace the lines in an already existing file.

For example, the three lines to copy look like this:

```
PlayerName: CelestePlayer
PlayerNameChosen: true
ClientId: ct_012a345b678c901d234e567f890
```

## Other things I have planned for this

- I would love to give everyone the ability to create and share trials of their own
- Having support for custom maps would be really cool, it's difficult to do though
- PB history, in-game stats, etc.
- More trials and trial features in general
- If there is enough interest, perhaps some sort of regular event trials
- Happy to hear ideas from others as well! A bunch of features already came from great feedback from people playtesting this mod

Speaking of which

## Special Thanks

Thanks to everyone who helped playtest this mod over the last couple of weeks:

- [amberlovesarchie](https://www.speedrun.com/users/AMBERLOVESARCHIE)
- [Tiyo](https://www.speedrun.com/users/Tiyo)
- [zkad](https://www.speedrun.com/users/Zkad)
- [Isaactayy](https://www.speedrun.com/users/Isaactayy)
- isaac
- [curs3dspirit](https://www.speedrun.com/users/CursedSpirit)
- BinaKleinerAls3
- [CactusSB](https://www.speedrun.com/users/CactusSB)
- [Norah](https://gamebanana.com/members/1713780)
- [Windrunner315](https://www.speedrun.com/users/Windrunner315)

The feedback I got from you made working on this even more fun, and even the small things helped making this mod better and more polished, so I appreciate it!

And also thanks to Bina for creating the custom icons for the menus and just being amazing and supportive in general! 💛

## More shoutouts

Buy [Celeste](https://www.celestegame.com/)! It is a very good game. Also support the developers here: https://exok.com/

Also, the modding ecosystem in Celeste is very cool. Take a look at these projects as well:

- [Everest](https://github.com/EverestAPI/Everest): Celeste Mod Loader / Base API that this mod requires to work, so it wouldn't exist without!
- [Celeste.SpeedrunTool](https://github.com/DemoJameson/Celeste.SpeedrunTool): Widely used tool for speedrun practice (and one of my go to references to learn from)
- [CelesteTAS-EverestInterop](https://github.com/EverestAPI/CelesteTAS-EverestInterop): Awesome TAS tools for this game (and my other go to reference to learn from)
