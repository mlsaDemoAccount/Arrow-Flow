# Arrow Attack — Unity game source

Original arrow-removal puzzles for Android and iOS. No purchases, subscriptions, or paid hints. Ads include optional rewarded life refills, menu banners, and interstitials between levels (see **Ads (Android and iOS)** below). A refill requires earning the ad reward; restarting a puzzle is always free.

## Open and play

1. Add this `ArrowFlow` folder in Unity Hub. Use **Unity 6000.4.4f1** with an active editor license.
2. Allow the package manifest to resolve and the scripts to compile.
3. Choose **Arrow Attack → Prepare project** from the editor menu.
4. Open `Assets/Scenes/Main.unity` and press Play.

**Reference-screen redesign:** The supplied artwork is implemented as separate editable Unity UI objects in `Assets/Scenes/Main.unity`, including Splash, Entry, Home, Game, Daily, Levels, Weekly, Trophies and Win. The slices live in `Assets/ArrowFlow/Art/Reference/`; labels remain editable text. No full-screen reference screenshot is used as a flattened UI.

**All UI is saved in Main.unity**, including every screen, dialog and persistent Button On Click event. The runtime reuses these objects and updates only game data. Read **SCENE_UI_GUIDE.md** for hierarchy editing, screen preview, Inspector callbacks, and migration of existing scenes.

## Name and app icon

The game is called **Arrow Attack**: that is its name on the phone, its Player Settings Product Name and the editor menu. `Editor/Branding.cs` sets the Product Name, the Company Name (`ArrowAttackStudio`) and the default app icon, `Assets/ArrowFlow/Art/Icon/AppIcon.png` (the Arrow Attack logo, 1024×1024), which Unity scales to every Android, iOS and Windows icon size. Unity applies these settings when it first loads the script, before every build from the **Arrow Attack** menu, and from **Arrow Attack → Apply name and icon**. To change the icon, replace AppIcon.png; `Art/Icon/Source~/build_icon.py` regenerates it from the logo.

The `ArrowFlow` folder, C# namespace and package identifier keep their original names, so scene button events keep working and installed copies update in place with their progress.

## Features implemented in source

- 500 original deterministic campaign puzzles with guaranteed solutions.
- A campaign that gets harder level by level: 500 unique seeded, gap-free boards, picked from 1,000 generated candidates. Level 1 is an 11×12 board with 43 arrows (32 blocked at the start). Level 5 has 51 arrows, level 10 has 59, level 20 has 73, level 50 has 99, level 100 has 127, level 200 has 156, level 300 has 175 and level 500 (26×32) has 221. Boards grow a little almost every level, arrows get longer and turn more, and later levels use harder layouts (more arrows blocked, longer chains). No level has fewer arrows than the one before. In the first 100 levels, no more than three levels in a row share an arrow count; after that the count rises at least every 20 levels. Every level has at least one blocked arrow. The four-arrow example is used only for the home illustration. Updated unfinished layouts reset moves and hearts while preserving unlocked levels, completed awards and spent hints.
- Medium-weight arrows with solid heads (lines 16% of a cell, kept between 6 and 8 UI units; adjustable as Line Thickness, Min Line Thickness and Max Line Thickness on Board View), a touch area of about half a cell, directional obstruction checks, animated departures, and three lives. A correctly tapped arrow turns white as it leaves. A blocked arrow turns red, slides forward until it hits the arrow in its way (which gets knocked a little), slides back to its place and blinks red three times; other arrows can be tapped meanwhile. Losing the last heart lets that animation finish before the out-of-hearts popup opens.
- A level-complete popup with three gold stars that pop in one by one, a soft three-note jingle in time with them, and a **Next level** button (after a daily puzzle it continues the campaign). The out-of-hearts popup shows three gray stars with a gentle "you lose" phrase.
- Three free hints per puzzle, unlimited path guides, red blocked-path feedback, and free puzzle restarts. Hint usage is saved and survives reopening or restarting an unfinished puzzle. Repeated taps on an already highlighted hint do not spend another hint. Completed rewarded ads refill three lives while keeping board progress. Resuming a puzzle preserves its remaining hearts; restarting resets both hearts and the board.
- Pinch zoom, drag/two-finger pan, mouse-wheel zoom, and Fit view.
- Local move-by-move progress, unfinished attempts, backup recovery, and unlocked-level replay.
- Daily calendar, past-day replay, daily stars, monthly trophies, and leap-year handling. Daily puzzles get harder through each month: the 1st plays like campaign level 10 and each later date like the next level, up to level 40 on the 31st (about 60 arrows on a 13×14 board up to about 94 on 16×18). Every date has its own board.
- Weekly solo puzzles with bronze/silver/gold milestones (5, 15 and 30 puzzles a week) and saved medal totals. Every weekly puzzle plays like campaign level 100 (19×23 boards with about 127 arrows); online tournaments use the same boards.
- Optional real online tournaments: generated anonymous names, rankings, validated solutions, and final top-three medals.
- Optional 6 pm daily notification on Android/iOS, with permission requested only when enabled.
- Soft 76 BPM background loop (pad, gentle electric piano, sine bass and a quiet melody, no drums), plus soft correct/wrong move sounds.
- Sound/haptic settings, tutorial, privacy explanation, and confirmed deletion of progress.
- Q Solutions startup splash using the supplied logo on a full white background, shown for three seconds before the entry screen. The logo slowly zooms out from 1.3× to its saved size over two seconds while fading in; the splash length, start size and zoom time are adjustable on the Arrow Attack controller.
- A portrait layout that fills every phone and tablet screen, keeps clear of notches, camera holes, rounded corners and the ad banner, and adapts to split screen, foldables and resized windows (see **Phones and tablets** below).

## Phones and tablets

The game runs on any Android or iOS phone or tablet, in portrait. The screens are designed at 720×1280, and while the game runs, `Runtime/FitDesign.cs` reshapes them to the device instead of leaving empty bands:

- **Taller phones** (19.5:9, 21:9, the Galaxy Z Fold cover screen): headers stay at the top, buttons at the bottom, and content stays in the middle. Sizes do not change.
- **Tablets and foldables** (iPad, Android tablets, an unfolded Fold): the screens grow sideways and the side frames move out to the screen edges. Compared with scaling the phone layout up, buttons and text are up to 13% smaller and the game board's cells up to 12% bigger (the **Tablet Room** setting on the Design object controls this).
- **Split screen, desktop windows and landscape windows**: the same rules apply, so the layout still fits without overlaps.
- Backgrounds and the dimmed backdrop behind dialogs cover the whole screen, including behind notches and rounded corners. Buttons and text stay inside the safe area and above the ad banner.

The layout is never smaller than 720×1280, so nothing overlaps or is cut off on any screen shape. Edit mode shows the fixed 720×1280 layout for editing. To preview other devices, press Play and pick a Game view size, or use **Window → General → Device Simulator**. The rules are in `Runtime/ResponsiveLayout.cs`, and SCENE_UI_GUIDE.md ("Responsive layout") explains them.

The game stays in portrait on tablets. iPad runs it full screen (Requires Full Screen). The Android manifest marks it as a game (`android:appCategory="game"`): from Android 16, large screens ignore the orientation an app asks for, except in games.

## Android build

Install Android Build Support with SDK, NDK, and OpenJDK through Unity Hub.

- **Arrow Attack → Build Android APK** creates `Builds/Android/ArrowAttack.apk`.
- **Arrow Attack → Build Android App Bundle** creates `Builds/Android/ArrowAttack.aab`.
- Configuration: ARMv7 and ARM64 (32- and 64-bit devices), IL2CPP, minimum API 26, newest installed target SDK, portrait orientation, resizable window (split screen, foldables), drawing behind camera cutouts, Activity entry point.
- Before release, change the placeholder package identifier `com.arrowflowstudio.arrowflow` and configure your signing keystore. Build setup currently assigns that identifier each time; change it in `Editor/BuildTools.cs`.

## iOS build

On a Mac, install Unity iOS Build Support and Xcode. Open this project and select **Arrow Attack → Export iOS Xcode project**. Open the generated project in `Builds/iOS`, select your signing team and identifier, and build/run or archive through Xcode.

The Xcode project targets iPhone and iPad. On iPad the game runs full screen in portrait (Requires Full Screen); older Xcode versions reject a portrait-only iPad app without it. Apple deprecated this setting in iPadOS 26 and plans to ignore it in a later version, and newer Xcode versions may warn that all orientations will soon be required. If iPadOS resizes the game's window, the layout adapts to it.

The source includes a native notification bridge and a post-build script to link UserNotifications.framework. iOS native compilation and signing still need verification on a Mac.

## Ads (Android and iOS)

AdMob supports three placements:
- Rewarded ads refill three lives only after the SDK confirms the reward. Skipping, failed ads, and unavailable ads grant no lives; players can restart the puzzle.
- Bottom banners appear on menu screens, with space reserved below the UI. Gameplay and dialogs hide them.
- Interstitials appear when continuing from a completed level, after at least three completions and two minutes since startup or the last full-screen ad. Unavailable ads never delay navigation.

The Google Mobile Ads Unity package is installed through `Packages/manifest.json`. All platform IDs are configured in `Assets/Resources/AdsConfiguration.json`. The Google Mobile Ads settings asset is included, and a build preprocessor synchronizes its App IDs from that JSON before Google's native build processors run.

Both apps and all six rewarded/banner/interstitial units were created in publisher account pub-6588216680762223 and their production IDs are configured. AdMob payment setup, store linking/review, and privacy-message setup remain required before live serving. Release mobile builds reject missing, malformed, or Google test IDs; enable **Development Build** in Unity Build Profiles for test builds. The custom release build menu intentionally enforces this check. While testing production units, populate `testDeviceIds` with the hashed test-device identifiers reported by the Google Mobile Ads SDK, not advertising IDs.

UMP refreshes consent information at startup, displays a form when required, and gates SDK initialization and ad requests with `CanRequestAds()`. **Settings → About & privacy → Ad privacy options** is available when UMP requires it. Changing privacy choices discards cached and in-flight ads. Configure and publish the appropriate privacy messages in the AdMob console for both apps; the app cannot create these console settings. This integration does not request Apple's tracking permission or enable IDFA access.

For unpublished-app testing, use **Arrow Attack → Build Android Test Ads APK** (or `./Build.ps1 -Target AndroidTestAds`) and install `Builds/Android/ArrowAttack-TestAds.apk`. Editor and Development builds automatically use Google's sample banner, rewarded, and interstitial units; the build guard also selects Google's sample App IDs. No test-device registration is needed for sample units. Production IDs remain in the JSON and are restored to the native settings on release builds. Consent checks and normal placement timing remain enabled. For iOS, enable Development Build when exporting. Rebuild and reinstall to replace an existing production-ID build.

Ad callbacks run on Unity's main thread. Failed loads retry with bounded backoff, and cached full-screen ads are replaced after 55 minutes.

Run the deterministic lifecycle checks with `dotnet run --project Tests/Ads/AdsTests.csproj`. These simulate SDK callbacks; Android/iOS device testing is still required for real ad delivery, consent forms, suspend/resume, and banner layout.

## Online tournaments

The sibling `TournamentServer` folder contains the server. Keep both folders beside each other because they share `Core/Puzzle.cs` and `Core/CampaignOrder.cs`.

The default endpoint is blank, so the game remains usable offline. To enable real rankings, host the server behind HTTPS and set `Assets/Resources/TournamentConfiguration.json`:

```json
{ "url": "https://your-host.example" }
```

No public server has been deployed. The online button explains unavailability when unconfigured. Weekly solo challenges remain available.

Tournament rules are original to this project: UTC Monday boundaries, one point per validated puzzle, gold/silver/bronze for the top three, stable player-ID ordering for tied scores. The source includes result retry and anonymous identity deletion.

## Source map

| File | Responsibility |
|---|---|
| Core/Puzzle.cs | Geometry, rules, seeded generation, progress and calendar models |
| Core/CampaignOrder.cs | Generated campaign catalogue ordered by measured difficulty |
| Core/Checks.cs | Reproducible logic and campaign progression tests |
| Runtime/GameApp.cs | Public button handlers, navigation and game-data updates |
| Runtime/Ads.cs | Rewarded-video ad loading/showing (Google Mobile Ads Unity Plugin) for the life refill |
| Runtime/SceneUI.cs | Serialized references to every saved UI object |
| Editor/SceneUIBuilder.cs | Optional editor-time UI generation and persistent event wiring |
| Runtime/BoardView.cs | Vector rendering, animation, gestures and safe-area layout |
| Runtime/FitDesign.cs | Fits the 720×1280 screens to each phone, tablet or window while playing |
| Runtime/ResponsiveLayout.cs | Rules for where each element goes on taller or wider screens |
| Runtime/StarDisplay.cs | Animated gold or gray stars on the Win and Fail dialogs |
| Editor/RewardStarsSetup.cs | Adds missing dialog stars and jingles to the open Main scene |
| Runtime/SaveStore.cs | Local JSON persistence and backup recovery |
| Runtime/TournamentClient.cs | Online identity, rankings, submission and deletion |
| Runtime/Reminders.cs and Assets/Plugins | Mobile notifications |
| Editor/BuildTools.cs | Setup, tests and platform builds |
| Editor/Branding.cs | Game name and app icon in Player Settings |

## Validation and limitations

See **TEST_RESULTS.md**. Logic, external C# compilation, Android Java compilation, and local server integration tests passed. The reference UI passed Unity 6 import, rendered visual checks at three portrait sizes, 37 Play-mode assertions and 51 heart/hint lifecycle assertions on September 29, 2026. Screenshots are in `Builds/ReferenceUI/`. This UI task did not build a new APK, AAB or IPA. The phone and tablet layout was checked outside Unity on 15 screen shapes (54,075 layout checks, no overlaps or cut-off elements); it still needs a Play-mode check in Unity and on real devices.

The UI uses the reference artwork supplied for this project, including the Arrow Attack logo, which is also the app icon. Puzzle generation and game logic are this project's own implementation. Cloud-save/device transfer and localization are not implemented. On-device testing is still required for touch accuracy, save recovery, suspend/resume, safe areas and notifications.

The optional server is suitable as a small single-instance starter; review its README before hosting publicly.


