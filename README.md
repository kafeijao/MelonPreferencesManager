# MelonPreferencesManager

In-game UI for managing MelonLoader Mod Preferences. Supports IL2CPP and Mono Unity games.

Requires MelonLoader 0.3.1+ (currently you must build this version yourself, otherwise wait for the release).

* [Download (IL2CPP)](https://github.com/sinai-dev/MelonPreferencesManager/releases/latest/download/MelonPrefManager.IL2CPP.zip)
* [Download (Mono)](https://github.com/sinai-dev/MelonPreferencesManager/releases/latest/download/MelonPrefManager.Mono.zip)

[![](img/preview.png)](https://raw.githubusercontent.com/sinai-dev/MelonPreferencesManager/master/img/preview.png)

## Info for developers

The UI supports the following types:

* Toggle: `bool`
* Number input: `int`, `float`, etc (NOTE: `decimal` is **not** supported by MelonLoader currently)
* Input: `string`
* Color editor: `Color`
* Struct editor: `Vector2`, `Vector3`, `Vector4`, `Rect`

To make a slider, use a number value and set the `ValueValidator` when creating the entry.

## Todo

* Support arbitrary TomlObject types by using the provided Mapper and editing through a string
