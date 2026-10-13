# Edit every screen in Unity

## Open the delivered scene

1. Exit Play mode before copying updated files into your project.
2. Copy the updated `Assets/ArrowFlow` scripts and layout files, and the updated `Assets/Scenes/Main.unity` scene (including its `.meta`). Preserve the included script `.meta` files.
3. Let Unity recompile, then open `Assets/Scenes/Main.unity`.
4. Expand **Canvas → Safe area → Design**. Every screen and dialog is already saved there.

The supplied scene contains **462 UI elements and 130 buttons with persistent On Click bindings**. Camera, EventSystem, audio source, and the game controller are also saved in the scene.

## Update your own existing scene

If you want to keep your existing scene instead of replacing Main:

1. Copy all updated scripts and `Editor/Layout/SceneLayout.json` into the matching folders.
2. Stop Play mode and select **Arrow Attack → Build scene UI**.
3. Save with **Ctrl+S**.

This creates the complete editable UI and assigns every reference. Existing UI/controller/camera/event-system roots are moved into an inactive **Previous UI (disabled backup)** object. The operation can be undone. Do not run Build scene UI after making layout changes unless you intend to generate another default layout; it builds a new layout, not an incremental layout edit.

## Preview screens without playing

Select **Arrow Attack** in the Hierarchy. In the **Scene UI** component Inspector, use **Preview screen** or **Preview dialog**.

Alternatively, enable the desired `Screen/...` root under Design and disable the others. Dialogs are under `Dialog/...`. Select a root and press **F** in Scene view to frame it. Home is the enabled editor preview in the delivered scene. Play mode shows the authored Splash screen, then Entry; its Play button opens Home.

Saved screens:

- Splash and Entry, with separate background, logo and loading/play controls
- Home
- Game, including board preview, lives, counter, progress bar, hints, path guide, fit view and restart
- Daily calendar, including all 42 reusable date slots
- Levels, including all 32 reusable page slots (four columns by eight rows)
- Weekly challenge
- Trophies
- Settings
- Tournament loading
- Tournament leaderboard, including all eight displayed rows

Saved dialogs: Tutorial, Restart, Fail, Win, About, Delete, Notice, NetworkError, SubmissionError, Submitted, Submitting, and Medals.

## Inspect button methods

Select a button in the Hierarchy and expand **Button → On Click ()**. Its target is the scene's **Arrow Attack** controller, with a public method on `GameApp`.

Examples:

| Button | Inspector callback |
|---|---|
| Home/Play | GameApp.PlayCampaign() |
| Home/Daily | GameApp.ShowDaily() |
| Home/Settings | GameApp.ShowSettings() |
| Game/Guide | GameApp.ToggleGuide() |
| Game/Hint | GameApp.Hint() |
| Game/Fit | GameApp.FitView() |
| Game/Restart | GameApp.RequestRestart() |
| Restart/Confirm | GameApp.RestartPuzzle() |
| Fail/Refill | GameApp.RefillLives() |
| Win/Next | GameApp.NextPuzzle() |
| Daily/Day0 | GameApp.PlayDailySlot(0) |
| Levels/Level0 | GameApp.PlayLevelSlot(0) |
| Settings/Sound | GameApp.ToggleSound() |
| Delete/Confirm | GameApp.DeleteProgress() |
| SubmissionError/Retry | GameApp.RetrySubmission() |

There are no runtime `Button.onClick.AddListener` callbacks. You can reassign persistent events directly in the Inspector. Slot arguments are zero-based; the controller resolves the displayed date or level from the selected page.

## What can be edited

Change RectTransforms, colors, fonts, sizes, static text, and Button transitions/events directly in the scene. Runtime navigation enables/disables existing screens; it does not rebuild UI or overwrite the layout. Board colors are on **Game/Board** (Board View): **Ink** for arrows, **Accent** for hints, **Error** for a blocked tap (the arrow bumps into the arrow in its way, slides back and blinks) and **Success** (white) for an arrow leaving after a correct tap.

Dynamic values still update during play: lives, level/date labels, score rows, day/level slot labels and availability, settings On/Off labels, progress-bar scale, result messages, and the puzzle mesh. These are data updates to existing scene components. The three hearts are separate Image objects using the supplied life slice. Individual puzzle arrows are still drawn by the existing BoardView component, because puzzle content changes by level.

SceneUI holds serialized object references, so renaming or reparenting objects does not break lookups. Keep its reference IDs unchanged unless you also update the controller's corresponding IDs. Deleting an object requires fixing its SceneUI entry and affected logic.

Run **Arrow Attack → Validate scene UI** to check for missing references and persistent button events. Unity 6000.4.4f1 imported and rendered the scene successfully. Automated Play-mode navigation and gameplay UI checks also passed; physical-device touch testing remains separate.

## Responsive layout (phones, tablets and windows)

Every screen is authored at 720×1280 under **Design**. In edit mode Design shows exactly that layout, scaled to fit the Game view, so what you edit is what a 9:16 phone shows. While playing, the **Fit Design** component on Design reshapes the screens to the device at startup, and again whenever the screen size, safe area or ad banner changes (rotating a foldable, split screen, resizing a window):

- The Design area takes the shape of the safe area but is never smaller than 720×1280. On phones it keeps the 720 width and gets taller. On wider screens (tablets, foldables, windows) it keeps at least the 1280 height and gets wider.
- Each direct child of a screen gets a role from where it sits in the 720×1280 layout:

| Role | Elements | On a taller or wider screen |
|---|---|---|
| Top | wholly above 310 units from the top (200 on Levels) | keeps its distance from the top edge |
| Bottom | wholly below 1016 units from the top (740 on Home) | keeps its distance from the bottom edge |
| Middle | everything between, and every child of a dialog | stays centred; rows and grids move together |
| Stretch | Game/Viewport, and the board that fills it | grows into the free space |
| Frame | FrameTop, FrameBottom, FrameLeft, FrameRight (full-width or full-height strips) | hug the screen edges and grow with them; the mitred corners keep their size |
| Cover | full-screen backgrounds, and each dialog's dimmed backdrop | cover the whole screen, including behind notches, rounded corners and the ad banner, keeping the artwork's shape |
| Centre | art already larger than any screen (the white Splash background) | stays centred |

- Children deeper in the hierarchy move with their parent and keep their size.
- **Responsive** (on by default) switches this off to get the old behaviour: the fixed layout scaled to fit, with empty bands. **Tablet Room** (1.15) lets the UI shrink by up to that factor on screens wider than 9:16, so the game board gets bigger cells on tablets; 1 keeps phone-sized UI.
- Nothing is saved to the scene: the changes exist only while playing.

When you add or move elements, keep headers above the top line and bottom buttons below the bottom line to give them those roles; anything in between moves with the middle group. To make another element grow with the free space, add its SceneUI id to `StretchIds` in `Runtime/ResponsiveLayout.cs`.

To preview other devices, press Play and choose a size in the Game view's resolution menu (add sizes such as 1179×2556, 1640×2360 or 2176×1812 with **+**), or open **Window → General → Device Simulator** and pick a phone or tablet. Changing the size while playing updates the layout immediately.

## Reference artwork design (September 2026)

The reference layouts are saved in `Assets/Scenes/Main.unity`; no setup command or runtime UI builder is needed to see or edit them. The original scene is retained in an inactive **Previous UI (disabled backup)** hierarchy. Expand **Canvas → Safe area → Design**, enable one `Screen/...` root, and edit its individual children. Show `Dialog/Win` over Game to preview the clear screen.

- `Assets/ArrowFlow/Art/Reference/` contains the supplied slices from `E:/RefrenceScreen/Slices/`. Complete reference screenshots are not used as screen images.
- Backgrounds, bevel sides, logos, button surfaces, labels, calendar/level cells, popup panels, and heart icons are separate objects. Labels use editable Unity Text components with a Text Stroke component for the black outline.
- The missing glossy yellow/green button surfaces and frame sides use editable native `ReferenceDecoration` graphics. Their style and color are exposed in the Inspector.
- `Assets/ArrowFlow/Art/Fonts/` includes Lilita One, Montserrat and Quicksand, with their open-font licenses. The supplied Arrow Attack logo is the game's logo and, as `Art/Icon/AppIcon.png`, its app icon.
- `Arrow Attack → Apply reference screen design` imports the slices and rebuilds the default reference layout from `Editor/Layout/SceneLayout.json`. This is an editor-only authoring action; avoid rebuilding after hand-editing the scene unless you want another default layout. The existing setup is preserved in a disabled backup.
- Game values (level labels, dates, lives, hints, progress and scores) remain live. The calendar supports six-row months and leap years; the level menu pages through 500 levels in groups of 32. Fit view and How to play remain available below the three reference gameplay actions.

Rendered previews and the verification report are written to `Builds/ReferenceUI/` by `ArrowFlow.Editor.ReferenceUIBuild.Verify`.

## Audio editing

Select the active **Arrow Attack** controller's **Game Audio** component to replace the Correct Move, Wrong Move, Level Complete and Level Failed clips. Its **Background Music** child has the looping track and music volume (0.28); the controller's separate Audio Source controls effects volume (0.8). All clips are saved WAV assets under `Assets/ArrowFlow/Audio/`. The background loop is a soft, roughly 25-second, 76 BPM track regenerated with `python Assets/ArrowFlow/Audio/Source~/compose_background.py`; the move and level-complete sounds come from `compose_effects.py` (Unity ignores the `Source~` folder). Sound mutes both sources; Haptics independently controls vibration for a blocked move. Audio pauses when the app loses focus or a full-screen ad plays.

If Unity was open during this update, reload `Assets/Scenes/Main.unity` from disk before playing to load the saved audio sources and references. No UI rebuild is needed.

## Win and Fail dialogs

`Dialog/Win` shows three reward stars between the title and the message. **Win/Stars** is a single **Star Display** graphic, drawn in code like the hearts, that pops the stars in one after another each time the dialog opens, 0.22 s apart and in time with the Level Complete jingle. **Pop Delay**, **Pop Seconds** and the **Fill**, **Shine**, **Outline** and **Empty** colors are in its Inspector. GameApp shows all three stars on every win (`ui.Stars(3)` in `GameApp.Win`).

The green button reads **Next level** after campaign and daily wins (after a daily puzzle it opens the current campaign level), **Next puzzle** after weekly wins, and **Back home** only once the campaign is finished. The red **Home** button is unchanged.

`Dialog/Fail` (out of hearts) shows three gray stars, **Fail/Stars**, the same Star Display with no stars earned, between its title and message. When the last heart is lost they pop in with the Level Failed phrase. Reopening a level that was already out of hearts shows the stars without the sound. Unearned stars use the **Empty**, **Empty Shine** and **Empty Outline** colors.

Both star objects are in SceneLayout.json (kind `stars`), so Build scene UI recreates them. `Editor/RewardStarsSetup.cs` adds any that are missing to the open Main scene, assigns the two jingles and saves the scene. It runs by itself after scripts reload, when Main.unity opens and when Play mode ends. You can also run it from **Arrow Attack → Add reward stars and jingles**. This matters when Main.unity was open in Unity while its file changed on disk: the scene in memory keeps the old version, and the stars would otherwise be missing. If a dialog opens without stars, the Console shows a warning naming the missing object.

## Q Solutions startup splash

The saved Splash screen uses the supplied QSolutionsLogo.png as a separate Image with Preserve Aspect, over a white RoundedPanel extending beyond the fitted design to cover tall screens and safe-area margins. The old loading-bar artwork is inactive. GameApp holds this screen for three real-time seconds before showing Entry. Its layout is also stored in SceneLayout.json so rebuilding the scene retains the splash. The Home and Entry game logos retain their existing artwork.

On launch the logo slowly zooms out from 1.3× to its saved size over two seconds (gentle ease-out) while fading in, keeping its center fixed, then rests for the final second. GameApp applies this at runtime only and restores the saved scale, position and color before Entry, so the scene and SceneLayout.json keep the authored layout. Adjust **Splash Seconds**, **Splash Logo Start Scale** and **Splash Logo Zoom Seconds** on the **Arrow Attack** controller's Game App component; the splash never ends before the zoom finishes. In device builds the animation starts once Unity's own splash has finished fading out.

## Game name

The game controller object is named **Arrow Attack**, and the About dialog and the hidden Home title text use the same name. The Product Name and app icon are Player Settings rather than scene objects; see **Name and app icon** in README.md. If Unity was open when Main.unity changed on disk, choose **Reload** so the renamed objects load before you save the scene.
