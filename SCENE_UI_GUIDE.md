# Edit every screen in Unity

## Open the delivered scene

1. Exit Play mode before copying updated files into your project.
2. Copy the updated `Assets/ArrowFlow` scripts and layout files, and the updated `Assets/Scenes/Main.unity` scene (including its `.meta`). Preserve the included script `.meta` files.
3. Let Unity recompile, then open `Assets/Scenes/Main.unity`.
4. Expand **Canvas → Safe area → Design**. Every screen and dialog is already saved there.

The supplied scene contains **396 UI elements and 125 buttons with persistent On Click bindings**. Camera, EventSystem, audio source, and the game controller are also saved in the scene.

## Update your own existing scene

If you want to keep your existing scene instead of replacing Main:

1. Copy all updated scripts and `Editor/Layout/SceneLayout.json` into the matching folders.
2. Stop Play mode and select **Arrow Flow → Build scene UI**.
3. Save with **Ctrl+S**.

This creates the complete editable UI and assigns every reference. Existing UI/controller/camera/event-system roots are moved into an inactive **Previous UI (disabled backup)** object. The operation can be undone. Do not run Build scene UI after making layout changes unless you intend to generate another default layout; it builds a new layout, not an incremental layout edit.

## Preview screens without playing

Select **Arrow Flow** in the Hierarchy. In the **Scene UI** component Inspector, use **Preview screen** or **Preview dialog**.

Alternatively, enable the desired `Screen/...` root under Design and disable the others. Dialogs are under `Dialog/...`. Select a root and press **F** in Scene view to frame it. Home is the only enabled screen in the delivered scene. Play mode starts on Home regardless of which screen you last previewed.

Saved screens:

- Home
- Game, including board preview, lives, counter, progress bar, hints, path guide, fit view and restart
- Daily calendar, including all 42 reusable date slots
- Levels, including all 30 reusable page slots
- Weekly challenge
- Trophies
- Settings
- Tournament loading
- Tournament leaderboard, including all eight displayed rows

Saved dialogs: Tutorial, Restart, Fail, Win, About, Delete, Notice, NetworkError, SubmissionError, Submitted, Submitting, and Medals.

## Inspect button methods

Select a button in the Hierarchy and expand **Button → On Click ()**. Its target is the scene's **Arrow Flow** controller, with a public method on `GameApp`.

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

Change RectTransforms, colors, fonts, sizes, static text, and Button transitions/events directly in the scene. Runtime navigation enables/disables existing screens; it does not rebuild UI or overwrite the layout.

Dynamic values still update during play: lives, level/date labels, score rows, day/level slot labels and availability, settings On/Off labels, progress-bar scale, result messages, and the puzzle mesh. These are data updates to existing scene components. Individual arrows are drawn by the saved BoardView component rather than being separate UI objects.

SceneUI holds serialized object references, so renaming or reparenting objects does not break lookups. Keep its reference IDs unchanged unless you also update the controller's corresponding IDs. Deleting an object requires fixing its SceneUI entry and affected logic.

Run **Arrow Flow → Validate scene UI** to check for missing references and persistent button events. External compilation and scene-reference checks passed; the new scene still needs Unity import and Play-mode confirmation on your machine.
