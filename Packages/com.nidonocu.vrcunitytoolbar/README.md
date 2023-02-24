# 🧰 VRC Unity Toolbar

This package adds additional buttons to the Unity Editor's Toolbar that removes the need to perform various frequent actions done when building avatars and worlds in [VR Chat](https://vrchat.com/).

Currently this is three functions, **Selection Navigation**, **Focus Scene View** and **Auto-Select Avatar**.

More functions may be added over time.

## Compatibility
This package has been tested with `Unity 2019.4.31f` and the latest *VRChat SDK* as of `2023/02/24`.

It should function in projects both using and not using the *VRChat Creator Companion*.

Work is on-going to make this tool compatible with the *[Chillout VR](http://chilloutvr.de/) CCK*.

## Installing
At the moment VRChat has not yet launched their community or curated packages program, so you will need to install (and update!) the package manually.

### VRChat Creator Companion
If you are using the [VRChat Creator Companion](https://vcc.docs.vrchat.com/), you can load this package as a **User Package** so you can then install it in any of your projects.

1. Download the **ZIP** version of the package using the buttons below if you're viewing this on the website or from the [releases page](https://github.com/Nidonocu/vrc-unity-toolbar/releases).
2. Extract this ZIP file to a long term storage location where you keep other downloaded tools. This should be named `vrc-unity-toolbar`.
3. In the VRChat Creator Companion, go to the **Settings** page.
4. Under **User Packages**, click **Add**.
5. Navigate into the folder containing the package files (`vrc-unity-toolbar`) and then click **Select Folder**.

Once added, you can then install this package on to any project:

1. Access the page for your project in the VRChat Creator Companion.
2. Make sure in the sources drop-down in the top right of the window, **Local User Packages** is enabled.
3. Scroll down the packages and find **VRC Unity Toolbar**, click **Add**.
4. Open your project and the package will be installed.

### Standard Unity Package

If you are not using the VRChat Creator Companion in a classic project, you can load this package from a file.

1. Download the **Unitypackage** version of the package using the buttons below if you're viewing this on the website or from the [releases page](https://github.com/Nidonocu/vrc-unity-toolbar/releases).
2. Open your project in Unity.
3. From the **Assets** menu, choose **Import Package** and then **Custom Package**.
4. Find the Unitypackage file you downloaded (`com.nidonocu.vrcunitytoolbar-1.0.0.unitypackage` for example), select it and the click **Open**.
5. Ensure all items are selected and then click the **Import** button to complete the Install.

## How to Use
The package once installed, will add additional buttons to the Unity Toolbar found at the top of the editor.

Located to the right of the **toolbox**, you can find the following buttons.

![The Navigation Buttons](https://nidonocu.github.io/vrc-unity-toolbar/LeftUI.png)

### Selection Navigation

These buttons act much like the back and forward buttons on your web browser and let you move to a previously selected item without undoing changes like the built in Undo function

This includes both Objects within the current Scene and Assets within your Asset Library.

The navigation buttons will remember up to 100 navigation changes. However, the **memory will be cleared** whenever you enter or leave **Play** mode, or recompile any scripts within the project, this is a limitation of Unity.

To move back to a previous selection state, click the **Back** button. Once you have navigated backward, you can navigate forward again in your selection history by clicking the **Forward** button.

Selecting something new will always clear the Forward history, similar to how making a change after Undoing clears the ability to 'Redo'.

If you navigate back to an Object or Asset which no longer exists because it has been deleted or you have loaded a different scene, the current selection will just be set to nothing.

---

Located to the right of the **Play** and **Pause** buttons you can find the following toggle buttons.

![The Toolbar Buttons](https://nidonocu.github.io/vrc-unity-toolbar/UI.png)

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

