# MelonPreferencesManager

In-game UI for managing MelonLoader Mod Preferences. Supports IL2CPP and Mono Unity games.

Requires MelonLoader v0.5+

✨ Powered by [UniverseLib](https://github.com/sinai-dev/UniverseLib)

## Releases [![](https://img.shields.io/github/release/sinai-dev/MelonPreferencesManager.svg?label=release%20notes)](../../releases/latest)

* [Download (IL2CPP)](https://github.com/sinai-dev/MelonPreferencesManager/releases/latest/download/MelonPrefManager.IL2CPP.zip)
* [Download (Mono)](https://github.com/sinai-dev/MelonPreferencesManager/releases/latest/download/MelonPrefManager.Mono.zip)

## How to use

* Put the DLLs in your `Mods` folder.
* Start the game and press `F5` to open the Menu.
* You can change the keybinding under the `MelonPreferencesManager` category in the Menu, or by editing the file `UserData\MelonPreferences.cfg`.

[![](img/preview.png)](https://raw.githubusercontent.com/sinai-dev/MelonPreferencesManager/master/img/preview.png)

## Common issues and solutions

Although this tool should work out of the box for most Unity games, in some cases you may need to tweak the settings for it to work properly.

To adjust the settings, open the config file: `UserData\MelonPreferences.cfg`

Try adjusting the following settings and see if it fixes your issues:
* `Startup_Delay_Time` - increase to 5-10 seconds (or more as needed), can fix issues with the UI being destroyed or corrupted during startup.
* `Disable_EventSystem_Override` - if input is not working properly, try setting this to `true`.

If these fixes do not work, please create an issue in this repo and I'll do my best to look into it.

## Info for developers

The UI supports the following types by default:

* Toggle: `bool`
* Number input: `int`, `float` etc (any primitive number type)
* String input: `string`
* Key binder: `UnityEngine.KeyCode` or `UnityEngine.InputSystem.Key`
* Dropdown: `enum`
* Multi-toggle: `enum` with `[Flags]` attribute
* Color picker: `UnityEngine.Color` or `UnityEngine.Color32`
* Struct editor: `UnityEngine.Vector3`, `UnityEngine.Quaternion`, etc
* Toml input: Anything else as long as Tomlet can serialize it.

To make a slider, use a number type and provide a `ValueRange` for the Validator when creating the entry. For example:
* `myCategory.CreateEntry("SomeFloat", 0f, validator: new ValueRange<float>(-1f, 1f));`
* `myCategory.CreateEntry("SomeByte", 32, validator: new ValueRange<byte>(0, 255));`

You can override the Toml input for a Type by registering your own InteractiveValue for it. Refer to [existing classes](https://github.com/sinai-dev/MelonPreferencesManager/tree/main/src/UI/InteractiveValues) for more concrete examples.
```csharp
// Define an InteractiveValue class to handle 'Something'
public class InteractiveSomething : InteractiveValue
{
    // declaring this ctor is required
    public InteractiveSomething(object value, Type fallbackType) : base(value, fallbackType) { }

    // you could also check "if type == typeof(Something)" to be more strict
    public override bool SupportsType(Type type) => typeof(Something).IsAssignableFrom(type);

    // override other methods as necessary
}

// Register your class in your MelonMod.OnApplicationStart method
public class MyMod : MelonLoader.MelonMod
{
    public override void OnApplicationStart()
    {
        InteractiveValue.RegisterIValueType<InteractiveSomething>();
    }
}
```
