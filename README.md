[![Crowdin](https://badges.crowdin.net/playnite-extensions/localized.svg)](https://crowdin.com/project/playnite-extensions)
[![GitHub release (latest by date)](https://img.shields.io/github/v/release/TheMaster007/playnite-successstory-plugin?cacheSeconds=5000&logo=github)](https://github.com/TheMaster007/playnite-successstory-plugin/releases/latest)
[![GitHub Release Date](https://img.shields.io/github/release-date/TheMaster007/playnite-successstory-plugin?cacheSeconds=5000)](https://github.com/TheMaster007/playnite-successstory-plugin/releases/latest)
[![Github Lastest Releases](https://img.shields.io/github/downloads/TheMaster007/playnite-successstory-plugin/latest/total.svg)]()
[![GitHub commit activity](https://img.shields.io/github/commit-activity/m/TheMaster007/playnite-successstory-plugin)](https://github.com/TheMaster007/playnite-successstory-plugin/graphs/commit-activity)
[![GitHub contributors](https://img.shields.io/github/contributors/TheMaster007/playnite-successstory-plugin?cacheSeconds=5000)](https://github.com/TheMaster007/playnite-successstory-plugin/graphs/contributors)
[![GitHub](https://img.shields.io/github/license/TheMaster007/playnite-successstory-plugin?cacheSeconds=50000)](https://github.com/Lacro59/playnite-successstory-plugin/blob/master/LICENSE)

# playnite-successstory-plugin
Extension for [Playnite](https://playnite.link).

Fork by TheMaster007: Improving Steam Emulator Achievements

This fork of the SuccessStory plugin specifically attempts to fix and enhance Steam Emulator Achievement support. Please visit the Wiki Page on how to get started.
Key Enhancements:

    Achievement Watcher Next Integration
        Optionally prefer achievement data from Achievement Watcher Next through its local WebSocket bridge, while keeping achievements in SuccessStory's database and UI.
        Configure the bridge port and optional user:password in SuccessStory settings. AW Next is checked before the manual provider picker and on manual refresh; if it is unavailable or has no matching game, the existing provider remains the fallback.

## Information
The plugin gets localised achievements (if they exist) or manual achievements.

<a href="https://github.com/Lacro59/playnite-successstory-plugin/blob/master/forum/main_01.jpg?raw=true">
  <picture>
    <img alt="main_01" src="https://github.com/Lacro59/playnite-successstory-plugin/blob/master/forum/main_01.jpg?raw=true" height="150px">
  </picture>
</a>
<a href="https://github.com/Lacro59/playnite-successstory-plugin/blob/master/forum/main_02.jpg?raw=true">
  <picture>
    <img alt="main_01" src="https://github.com/Lacro59/playnite-successstory-plugin/blob/master/forum/main_02.jpg?raw=true" height="150px">
  </picture>
</a>
<a href="https://github.com/Lacro59/playnite-successstory-plugin/blob/master/forum/control_01.jpg?raw=true">
  <picture>
    <img alt="steamgriddb_01" src="https://github.com/Lacro59/playnite-successstory-plugin/blob/master/forum/control_01.jpg?raw=true" height="150px">
  </picture>
</a>
<a href="https://github.com/Lacro59/playnite-successstory-plugin/blob/master/forum/settings_01.jpg?raw=true">
  <picture>
    <img alt="settings_01" src="https://github.com/Lacro59/playnite-successstory-plugin/blob/master/forum/settings_01.jpg?raw=true" height="150px">
  </picture>
</a>

With this plugin you can
* add a store's achievements or manual achievements
* works for GOG, Steam, Origin, Xbox, Epic Games, Game Jolt, Guild Wars 2, World of Warcraft, RPCS3 and RetroAchievements
* manual achievements with import from trackers for Genshin Impact, Wuthering Waves, Honkai Star Rail and Zenless Zone Zero
* optionally prefer achievements from [Achievement Watcher Next](https://github.com/Shirowwww/Achievement-Watcher-Next) via its local WebSocket bridge
* add tag with time to unlock all achievements
* many user stats
  
If the theme supports such functions, you can:
* add plugin data in game list in "Details View" & "Grid View"
* add plugin data in game details

## Translations
You can help to translate the extension to different languages via the project [Crowdin](https://crowdin.com/project/playnite-extensions) page.

## Custom theme integration
The extension contains elements that can fit into a custom theme.  
[More information](https://github.com/Lacro59/playnite-successstory-plugin/wiki/Addition-in-a-custom-theme)

## More
Remember to support [Playnite](https://www.patreon.com/playnite) and [Freepik](https://www.flaticon.com/authors/freepik) and [SteamDB](https://steamdb.info/donate/) and [Steam Hunters](https://steamhunters.com/supporters).
