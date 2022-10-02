# 🧰 VRC Unity Toolbar

This package adds additional buttons to the Unity Editor's Toolbar that removes the need to perform various frequent actions done when building avatars and worlds in [VR Chat](https://vrchat.com/).

Currently this is two functions, **Focus Scene View** and **Auto-Select Avatar**.

More functions may be added over time.

## Compatibility
This package has been tested with `Unity 2019.4.31f` and the latest *VRChat SDK* as of `2022/10/01`.

It should function in projects both using and not using the *VRChat Creator Companion*.

This toolset should also work with the *[Chillout VR](http://chilloutvr.de/) CCK*, but this has not been as fully tested.

## Installing
*Instructions to follow...*

## How to Use

The package once installed, will add additional buttons to the Unity Toolbar found at the top of the editor.

Located to the right of the **Play** and **Pause** buttons you can find the following toggle buttons.

*Image to follow...*

### Focus Scene View

When activated, this will switch the active window back to the **Scene View** after hitting the Play button rather than the **Game View**.

This is useful, for example, if needing to test an Avatar and you need access to things such as the move gizmo for shaking the avatar to test its Physbones.

This toggle will be automatically ignored if running a Build.

### Auto-Select Avatar

When activated, this will switch the currently selected item to the object containing the **Avatar Descriptor** component after hitting the Play button.

This is useful when using something such as [Lyuma Avatar 3.0 Emulator](https://github.com/lyuma/Av3Emulator) and needing to select the avatar to access the menu testing buttons and other emulator functions.

After exiting Play mode, the selection will be *automatically restored* to the *previous item* you had selected when you pressed Play. If you turn off this feature while in Play mode, the current selection will be retained.

This toggle will be automatically ignored if running a Build.

This function will only be available in VRChat Avatar projects and Chillout VR projects.

