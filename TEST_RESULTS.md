# Validation report

## Passed

- **765,089 logic assertions** across 500 campaign, 366 daily, and 260 weekly boards. Checks include geometry, overlaps, reverse-order solvability, independent solving, hints, unlock/replay rules, attempt separation, leap-year trophies, and week boundaries.
- **External C# compilation against Unity 6 engine/reference assemblies**, including Android conditional code and editor helpers. The available uGUI reference was the installed Unity 2022 uGUI assembly. This checks types and syntax, not editor import or runtime rendering.
- **Android notification Java compilation** against API 36; compiler notes deprecated compatibility APIs.
- **.NET 10 tournament server build**, no errors; three nullable-annotation warnings in the Unity-compatible shared core.
- **51 live local integration checks** covering registration, authentication, invalid proofs, valid cross-runtime proofs, duplicate protection, leaderboard updates, name presentation, expiry, and deletion.
- **Historical medal fixture** covering gold/silver/bronze awards, weekly reset, idempotence, and persisted medals.

## Not verified

- Physical-device gesture behavior, notched-device safe areas, and frame rate. Editor import, rendering and UI navigation passed on September 29 (see the reference UI report below).
- APK/AAB compilation, installation, and Android notification delivery.
- iOS native compilation, Xcode export, signing, installation, and notification delivery.
- Public server hosting or load testing.
- Google Mobile Ads Unity Plugin package resolution, the in-editor App ID Settings step, and actual rewarded-ad load/show behavior (see below).

Earlier runs rejected the installed Unity license. The September 29 reference UI checks succeeded using the normal Unity 6 license connection.

## Re-run

- Unity menu: **Arrow Attack → Run logic checks**.
- Server: run locally, then execute `TournamentServer/Tests/Integration.ps1`.
- Scene UI: run **Arrow Attack → Validate scene UI**, then enter Play mode and click each screen and dialog button. The earlier procedural UI smoke runner was retired in the scene-based conversion.

Before release, test small/tall/notched Android devices, iPhone and iPad: taps, pinch/pan, safe areas, suspend/resume, save recovery, permission denial, tournament reconnect/retry, and deletion.

## Rewarded-ad life refill — September 18

Added `Runtime/Ads.cs`, a wrapper around the Google Mobile Ads Unity Plugin (`com.google.ads.mobile`), and pointed `Packages/manifest.json` at Google's official OpenUPM scoped registry for it. `GameApp.RefillLives()` now shows a rewarded video through `Ads.ShowRewarded` instead of refilling instantly; the refill is granted whether or not an ad actually played, so players are never blocked on ad inventory. The Fail dialog's Refill label switches between "Watch ad — refill" and "Refill" based on `Ads.RewardedReady`, using the same runtime `SceneUI.Text` mechanism as every other dynamic label — no scene changes were needed. Ad unit IDs and (informational) App IDs live in `Assets/Resources/AdsConfiguration.json`, defaulting to Google's public test IDs.

This code was written against Google's currently published Unity API (`RewardedAd.Load`, `.Show`, `.CanShowAd`, `MobileAds.Initialize`, `RequestConfiguration`) confirmed from developers.google.com on September 18, 2026, but could not be compiled here: the Google Mobile Ads package isn't resolvable without opening Unity (Package Manager + the account's own network access), so this addition carries the same "not verified" status as Unity Editor import elsewhere in this report. Everything else in the project still compiles externally against Unity 6 reference assemblies; `Ads.cs` is new and outside that check. Before relying on it: open Unity, let the package resolve, complete Assets → Google Mobile Ads → Settings with real App IDs, replace the test IDs in `AdsConfiguration.json`, and confirm in Play mode that losing all three lives shows the Fail dialog with a working Refill button.

## Ads follow-up — package resolution and threading fix — September 19

Resolution initially failed in the user's Editor with "Project has invalid dependencies" and CS0246 on `GoogleMobileAds`/`RewardedAd`. Cause: `com.google.ads.mobile` depends on `com.google.external-dependency-manager`, which needs to be listed explicitly in `Packages/manifest.json` rather than relying on transitive resolution through the scoped registry. Added it (`1.2.187`, confirmed present on the same OpenUPM registry). The user confirmed the package then appeared correctly in Package Manager.

With the package actually present, its real source became readable from `Library/PackageCache` for the first time (it ships as compiled DLLs, but the bundled `Samples~/HelloWorld` sample scripts are full source for the installed version, 11.5.0). Checked `Ads.cs` line-by-line against `RewardedAdController.cs`, `RewardedAdSnippets.cs`, and `RequestConfigurationSnippets.cs` from that sample. The `RewardedAd.Load` / `.Show` / `.CanShowAd` / `RequestConfiguration` calls matched. One real bug was found: Google's docs and sample explicitly state ad callbacks (`OnAdFullScreenContentClosed`, `OnAdFullScreenContentFailed`) fire off the Unity main thread, and any Unity UI/GameObject work inside them must be dispatched through `GoogleMobileAds.Common.MobileAdsEventExecutor.ExecuteInUpdate`. `Ads.ShowRewarded` was calling its `done` callback directly from those events, and `GameApp.RefillLives()` uses that callback to touch `ui.Text`, `ui.Interactable`, and dialogs - Unity API calls that require the main thread. Fixed by wrapping both `done` invocations in `ExecuteInUpdate`. The ad-reload calls inside `LoadRewarded` were left unwrapped, matching the SDK sample's own `ReloadAd` snippet, which calls `RewardedAd.Load` directly from the same off-thread callback with no wrapping.

Still not verified here: actual Play-mode behavior with a live rewarded ad (requires the Unity Editor and a real or test ad fill), and the Assets → Google Mobile Ads → Settings App ID entry, which is an in-editor step outside this report's reach.

## Rendering fix — September 15

The user's Unity screenshot exposed a MissingComponentException for CanvasRenderer on custom panels. BoardView and RoundedPanel now explicitly require CanvasRenderer, so Unity adds the renderer before initializing either custom graphic. This fixes the same missing dependency for the hero artwork and playable board. External Unity-reference compilation passed after the change. Visual confirmation of the fix is still pending.

Exit Play mode before updating the script, allow Unity to recompile, then enter Play mode again so runtime objects are rebuilt.

## Board centering fix — September 15

The simulator screenshot confirmed renderer recovery but exposed a coordinate-space error: the board mesh assumed a centered pivot while the UI uses a top pivot. BoardView now adds the actual rectangle center to all arrow positions. The same mapping is used for drawing, tap detection, hints, guides, and departure animations, including the home illustration. External Unity-reference compilation passed; simulator confirmation remains pending.

## Scene Canvas — September 15

Main.unity now serializes Canvas, CanvasScaler, GraphicRaycaster, Safe area, Design, and the GameApp controller with assigned references. Removed automatic controller bootstrap and runtime Canvas construction. Added an undoable editor migration command that reuses existing roots. SafeArea/FitDesign now live in separate script files so their scene components can be serialized correctly. External C# compilation passed (serialized-field assignment warnings only); Unity scene import and Play-mode verification remain pending.


## Fully scene-based UI — September 15

- Converted all screens, dialogs and fixed dynamic slots into 396 saved UI elements.
- Serialized 125 persistent Button On Click calls directly to named GameApp methods, including integer arguments for 42 calendar slots and 30 level slots.
- Runtime GameApp has no GameObject creation, UI component creation, UI destruction or runtime button listener registration. Navigation changes active screens and values only.
- Split RoundedPanel into its own file for valid scene serialization. Added SceneUI reference registry, editor-only layout builder, scene preview Inspector, and missing-reference/event validator.
- External Unity-reference C# compilation passed; one expected warning concerns the inspector-assigned SceneUI field.
- Structural scene checks passed: 1,715 objects/components, all local object and script GUID references, 396 unique UI reference IDs, all 125 callback targets/public methods/argument signatures, all runtime screen/dialog IDs, and all calendar/level/leaderboard slots.
- Unity import, visual rendering and actual click behavior for this conversion still need confirmation. No new mobile build was produced.

## Ads corrections and additional formats — September 21, 2026

- Added the Google Mobile Ads settings asset with the supplied Android and iOS App IDs. The build guard synchronizes App IDs from the JSON and blocks release mobile builds using missing, malformed, or Google test IDs.
- Added UMP startup consent, request gating, retry recovery, and a serialized About dialog privacy-options button. Privacy changes invalidate cached and pending ads.
- Marshaled SDK callbacks onto Unity's main thread. Rewarded ads grant lives only on the reward event; skipped/failed/unavailable ads give no refill. Game callbacks target the original attempt and guard against duplicate actions.
- Added bottom menu banners with a reserved UI inset; hidden during gameplay and dialogs. Added interstitials on Continue after at least three level completions and two minutes since startup or the previous full-screen ad.
- Added bounded load retries and 55-minute full-screen cache expiry. Updated scene/layout copy to disclose ads and remove free-refill claims.

Validation: runtime and editor sources compiled externally against the installed Unity 6 and Google Mobile Ads 11.5.0 assemblies. Deterministic tests of production Ads.cs with SDK doubles passed 22 lifecycle assertions for both Android/default and iOS conditional paths. Scene validation found 1,728 unique serialized objects, valid local references, and 398 matching layout/registry entries.

Still required: six production ad unit IDs (rewarded, banner, interstitial for each platform), AdMob-console privacy-message setup, and actual Android/iOS device testing. No native APK/AAB/IPA build, live ad delivery, consent-dialog rendering, or visual device verification was performed for this change. Test units remain intentionally enabled; use Unity's Development Build option to build them.

## AdMob account setup completed

Created Arrow Flow Android and iOS apps in publisher pub-6588216680762223, plus rewarded (3 Lives), banner, and interstitial units for each. Verified creation in the AdMob UI and saved all eight IDs in AdsConfiguration.json; synchronized the Google Mobile Ads settings asset. ID format, publisher, and settings consistency checks passed. Account payment setup remains incomplete; apps need store linking/review and privacy-message setup. Live ad delivery has not been tested.

## Hearts verification — September 23, 2026

The old HUD used LegacyRuntime.ttf characters for filled and empty hearts. A Unity-rendered reference confirmed that the empty glyph was larger than the filled glyphs. Replaced them with a serialized HeartDisplay graphic: three fixed slots, one shared contour, filled remaining lives and outlined lost lives. Updated Main.unity and the scene builder/layout together.

Resuming now preserves saved lives and removed arrows. Resuming at zero lives opens the failure dialog; a free restart restores three lives and resets the board. Editor verification uses a separate save directory under Builds, with no player-save changes.

Validation completed in an isolated copy using Unity 6000.4.4f1:
- 21 heart lifecycle assertions passed using the actual GameApp handlers and saved scene, including safe/blocked moves, save/load/resume at 2/1/0 lives, the zero-life dialog, unavailable refills, and restart.
- Rendered and visually inspected 3/2/1/0 hearts, the failure dialog, and 360x640, 720x1280, and 1080x2400 layouts. Icons stay aligned without clipping. The old font rendering is retained for comparison.
- Scene validation passed: 398 elements and 126 persistent button events.
- 765,089 core logic assertions and 23 ad lifecycle assertions passed. Unity batch verification exited successfully.

Artifacts: Builds/HeartVerification/*.png and result.txt. Re-run with Unity batch mode and executeMethod ArrowFlow.Editor.HeartVerification.Run; omit -nographics to render images. These are Unity scene-rendering and handler checks, not physical-device, touch-input, or live-ad verification. No updated APK was built in this task.

## Three-hint limit — September 23, 2026

Added three free hints per puzzle attempt, tracked as hintsUsed for compatibility with older saves. The Hint button displays the remaining allowance and disables at zero. Usage survives leaving, loading, switching puzzles, and restarting an unfinished puzzle. Repeated taps on the same highlighted arrow do not spend extra hints; dialogs and arrow animations block hint spending. New puzzles receive a separate allowance. Updated the saved scene, layout source, tutorial, settings copy, and README.

Unity verification passed 51 combined heart/hint lifecycle assertions (30 additional hint checks), including old-save compatibility and malformed usage clamping. All 765,089 core assertions and scene-reference validation passed. Visually inspected the exhausted Hint (0) button at 360x640 and the updated tutorial at 720x1280; both fit without clipping. Artifacts are under Builds/HintVerification. No APK rebuild or physical-device test was performed.

## Mobile banner/interstitial audit — September 23, 2026

Expanded the Ads.cs SDK-double tests to cover banner/interstitial platform ID selection, menu visibility, banner failures and retry timing, stale callbacks, privacy invalidation, interstitial no-fill navigation, retry recovery, failed presentation, and expiration. All 37 assertions passed for Android release, Android development, iOS release, and iOS development (148 assertions across four runs). No production ad behavior was changed.

Inspected both existing Android APK manifests: the TestAds APK is debuggable and embeds Google's sample Android App ID, network permissions, and AdActivity; the regular APK embeds the configured production App ID. Sample banner/interstitial unit IDs match Google's official Unity test-ads table. Both APKs were built September 22 and have not been rebuilt for the subsequent heart/hint changes.

Current placement rules: banners are requested on menus and hidden during gameplay/dialogs. Interstitials are attempted on the win-dialog Next action only after three completed levels and 120 seconds since startup or the previous full-screen ad. The counter and cooldown restart with an app launch. Consent must allow requests, and an unexpired loaded ad must be available. No attached Android device was returned by adb devices, so real delivery, phone layout, and the installed build could not be verified. No assertion is made about current production AdMob account readiness or ad inventory.

## Reference screen UI — September 29, 2026

Implemented the supplied reference screens using separate scene objects and the provided slices. The saved scene contains 462 UI entries and 130 persistent button bindings. Missing glossy controls, play icon and frame pieces are native editable Unity graphics; headings and button labels are editable outlined text.

Passed in Unity 6000.4.4f1:

- Script compilation, art/font import, saved scene reference validation and persistent button validation.
- Actual renders of all authored screens and dialogs; representative small (360×640), design (720×1280), and tall (1080×2400) previews. Visually inspected the main reference screens and six-row calendar.
- Four-column/eight-row campaign paging, first/last page boundaries through level 500, locked level availability, leap February, and six-row March.
- **37 Play-mode assertions**: splash-to-entry transition, authored button callbacks, Home/Daily/Levels/Weekly/Trophies/Settings navigation, tutorial dialog, guide, hint, restart, blocked-arrow life loss, and separate heart icon updates. The UI object count stays unchanged through navigation.
- **51 heart/hint lifecycle assertions**, including save/resume and restart behavior.
- **765,089 deterministic puzzle assertions** across campaign, daily and weekly content.

Test saves were isolated under Builds; player progress was not used. Play-mode editor settings were restored after verification. The original scene is retained as a disabled hierarchy backup.

Evidence: `Builds/ReferenceUI/result.txt`, `Builds/ReferenceUI/play-mode-result.txt`, `Builds/HeartVerification/result.txt`, and the PNG previews in `Builds/ReferenceUI/`.

To rerun the render/scene checks, invoke `ArrowFlow.Editor.ReferenceUIBuild.Verify` in a Unity batch session. For Play mode and gameplay UI checks, invoke `ArrowFlow.Editor.ReferenceUIPlayCheck.Run` **without** `-quit`; it exits after the check. This task did not build or install a mobile binary or verify real ad delivery.

## Background music and move feedback — September 29, 2026

Added an original 20-second seamless ambient loop, a short success chime, and a distinct two-note wrong-move sound as WAV assets in `Assets/ArrowFlow/Audio/`. Audio sources and clip references are saved in Main.unity, with separate background and effects volumes. Sound controls music and effects; Haptics controls the existing mobile vibration on blocked moves. Music pauses on focus loss, app pause and full-screen ads, then resumes when allowed.

Validation: 48 Unity Play-mode assertions passed in an isolated project, including sound-off startup, music playback/looping, wrong-move playback, mute suppression and pause/resume. Existing heart/hint lifecycle checks also passed. Test saves were isolated. The validated scene was copied back only after confirming the main scene had not changed during testing. Physical phone vibration and speaker balance still require device testing. No new mobile binary was built.

Report: `Builds/AudioVerification/result.txt`.

## Harder puzzle progression — October 5, 2026

Campaign levels after the tutorial now grow from 11x19 to 26x34. Daily and weekly boards are 23x31 and 24x32. Generation selects among four deterministic candidates using occupancy, turns, blocking relationships and dependency depth. Longer winding arrows are prioritized. Existing unfinished layouts reset moves and hearts on the generation-version change; unlocked levels, awards and spent hints are retained.

Validation: `dotnet run --configuration Release --project Tests/Puzzles/PuzzleTests.csproj` passed 2,001,574 assertions across 500 campaign, 366 daily and 260 weekly boards. Checks cover non-overlap, connected paths, reverse-order and independent greedy solutions, determinism, sampled difficulty and attempt migration. Sample levels 2/20/100/500 contain 29/27/77/71 arrows, with 13/15/44/43 initially blocked. No new mobile build or Unity visual/device verification was performed for this change. Tournament servers sharing Puzzle.cs must be updated alongside clients to keep generated boards consistent.

## Hard first level revision — October 5, 2026

Supersedes the earlier difficulty progression above. Level 1 is now generated as a full 28x42 campaign puzzle (110 arrows, 67 initially blocked, 82.7% occupied), instead of the four-arrow tutorial. Campaign dimensions grow to 34x48. Eight candidate layouts are scored; safe tail extensions fill unused pockets while preserving reverse-order solvability. Generation version 3 also migrates old level-1 attempts. The small tutorial board remains the home illustration.

Validation: 3,440,589 assertions passed over all 500 campaign, 366 daily and 260 weekly boards, including independent greedy solutions, reverse solutions, geometry, determinism and migration. Inspected the actual level-1 geometry rendered to Builds/PuzzlePreview/level-1.png. This is a board preview, not a Unity screen capture. No new APK or device verification was performed.

## Q Solutions splash and full campaign progression — October 5, 2026

The saved startup screen and layout now use the supplied Q Solutions logo as a proportional sprite on a full white native background, with the old loading graphic hidden. The startup iterator holds it for two real-time seconds and then advances to Entry.

Generation version 4 introduces a checked-in catalogue of 500 unique candidates sorted by measured challenge (occupied cells, turns, blocking relationships, available moves and dependency depth). Candidate sizes span 28x42 through 38x58 across the entire pool. Difficulty ratings range from 19,593 to 35,815 and never decrease between consecutive campaign levels. Level 1 has 100 arrows, 62 initially blocked; level 500 has 175 arrows, 111 initially blocked. Path-guide rendering is bounded to preserve Unity's UI mesh vertex limit on dense boards.

Validation: 3,486,172 assertions passed across 500 campaign, 366 daily and 260 weekly boards, including solvability, geometry, uniqueness, determinism, save migration and campaign ordering. Unity 6000.4.4f1 compiled an isolated copy and passed 18 focused checks covering the actual startup iterator, supplied logo, transition to Entry, levels 1/250/500 and guide meshes. Reviewed native Unity renders of the splash at 720x1280 and 1080x2400 and gameplay at levels 1 and 500. Additional 360x640 splash render was generated. Results and screenshots are in Builds/SplashDifficulty. No new APK/AAB/IPA was built; device touch testing remains unverified.

Regenerate the campaign catalogue with `dotnet run --configuration Release --project Tests/Puzzles/PuzzleTests.csproj -- --catalogue` after intentional generation changes, then rerun the complete puzzle checks. Consumers sharing the core code must include both Puzzle.cs and CampaignOrder.cs.

## Q Solutions logo zoom-out — October 6, 2026

The splash logo now appears at 1.3× its saved size and fully transparent, then slowly zooms out to its saved size over 2 s (sine ease-out) while fading in over the first 0.8 s, scaled around its center. The splash now lasts 3 s (previously 2 s), so the logo rests for the final second; the saved scale, position and color are restored before Entry. Splash length, start scale and zoom time are serialized on GameApp (Inspector: Splash Seconds, Splash Logo Start Scale, Splash Logo Zoom Seconds), and the splash never ends before the zoom completes. In player builds the zoom and splash timer begin once Unity's built-in splash reports finished (waiting at most 1.5 s), so its final fade cannot hide the animation. No scene or layout files changed.

Validation: structural check of GameApp.cs (bracket and preprocessor balance), review of each Unity API used, and a numeric simulation of the curve showing the logo center stays fixed and the end state equals the authored layout. A preview GIF was rendered outside Unity. The existing Play-mode checks wait for Entry, and the startup check's two-second minimum still holds with the longer splash. Not yet compiled or played in Unity; StartupPlayVerification was not rerun and no new APK/AAB/IPA was built.

## Gap-free boards, arrow ramp, white departures and brighter music — October 6, 2026

The old generator only added arrows whose exit was already clear, so exit corridors and pockets it could not reach stayed empty (about 15–20% of each board). Generation version 5 gives every arrow an order key instead: a new arrow may slot between existing keys as long as every arrow whose exit it covers has a smaller key and every arrow on its own exit has a larger one, so removing arrows from the highest key down always solves the board. Walks prefer enclosed cells, short arrows fill remaining pockets, tails grow, and stranded single cells join a neighbouring tail or head.

Campaign boards now grow from 30×34 to 46×52, matching the 612×690 board area, so cells are larger than before at every level. Levels are ordered by arrow count and then DifficultyRating: level 1 has 115 arrows, level 100 has 156, level 250 has 215 and level 500 has 371, and no level has fewer arrows than the previous one. Average measured difficulty of levels 451–500 is 2.7× that of levels 1–50. Boards cover 98.7–100% of their cells (about 9 single empty cells on average) and at least 84.8% of arrows start blocked. Daily and weekly boards widen from 23×31 and 24×32 to 27×31 and 28×32 so they also fill the board area, and are gap-free too (at least 98.5%, 91–132 arrows). CampaignOrder.cs now records each level's arrow count, and Checks.Run fails if the generator and catalogue disagree. Checks also cover gap-free layouts, the arrow ramp and rising difficulty, and the greedy solve now tracks blockers instead of rescanning the board for every hint, following the same first-available order. Unfinished attempts reset moves and hearts on the version change; unlocked levels, awards and spent hints are kept.

A correctly tapped arrow is drawn in the new Board View Success color (white) while it leaves, instead of teal. BackgroundLoop.wav is replaced with a 16-second 120 BPM loop (plucked arpeggio, punchy bass, bell hook, crisp drums), about 5 dB louder above 300 Hz than the old ambient loop; it is generated by Assets/ArrowFlow/Audio/Source~/compose_background.py and keeps the same file and GUID, so the scene needs no changes.

Validation: an exact Python port of the C# generator, including .NET System.Random, reproduced the previous level 1 (Builds/PuzzlePreview/level-1.txt) arrow for arrow and matched its rating of 19,593. The new generator was developed in that port and mirrored line for line in Puzzle.cs. In Python, all 500 campaign, 366 daily and 260 weekly boards passed bounds, overlap, connectivity, reverse-order and greedy solvability checks. Not yet compiled or run with dotnet or Unity: run `dotnet run --configuration Release --project Tests/Puzzles/PuzzleTests.csproj`, which also confirms the C# boards match the catalogue. Tournament servers must use the same Puzzle.cs and CampaignOrder.cs. No new APK/AAB/IPA was built.

## Arrow Attack name and app icon — October 6, 2026

The game is renamed Arrow Attack. The new `Editor/Branding.cs` sets Player Settings Product Name to "Arrow Attack", Company Name to "ArrowAttackStudio" and the default icon to `Assets/ArrowFlow/Art/Icon/AppIcon.png`: the supplied Arrow Attack logo on a blue rayed background, 1024×1024 without transparency, imported uncompressed and without mipmaps. The logo's faint full-rectangle haze (alpha below 10) is removed so no box shows behind it. Branding applies itself when Unity first loads it (and again when the icon finishes importing), before every build through BuildTools.Prepare, and from **Arrow Attack → Apply name and icon**.

The editor menu is now **Arrow Attack**, and builds are written to `Builds/Android/ArrowAttack.apk`, `ArrowAttack-TestAds.apk`, `ArrowAttack.aab` and `Builds/Windows/ArrowAttack.exe`. Main.unity and SceneLayout.json rename the controller object, the hidden Home title and the About text, including the disabled backup. Component menus, Android and iOS reminder text, and editor messages use the new name. Reminders.mm replies to the "Arrow Attack" object, and the iOS reminder call gives the controller that name first, so the reply also arrives in scenes built earlier.

Kept on purpose: the `ArrowFlow` folder and C# namespace (button events reference `ArrowFlow.GameApp`), the package identifier `com.arrowflowstudio.arrowflow` (changing it makes stores and phones treat the game as a new app), Android reminder package and preference names, native function names and log markers. AdMob console app names are unchanged and ad unit IDs are unaffected. Unity keeps Editor and Windows saves under the company and product name, so Play mode and Windows preview builds start with fresh progress; phones keep theirs.

Validation: every text replacement was exact and counted. Outside internal identifiers, the historical notes above, logs and Unity's crash-recovery scene, no "Arrow Flow" text remains. Branding.cs uses PlayerSettings name and icon APIs with NamedBuildTarget, as BuildTools.cs does; it was checked structurally, not compiled in Unity. The icon was reviewed with square, rounded and circular masks. No new APK/AAB/IPA was built.

## Easier levels, thicker arrows and soft sounds — October 8, 2026

Supersedes the October 5 and 6 difficulty sections above.

**Levels.** Generation version 6. Campaign boards start at 4×5 and grow in steps to 14×16 (previously 30×34 to 46×52). Arrows start 2–4 cells long and mostly straight, reaching 3–8 cells with more turns on the largest boards. Each board is picked from eight generated layouts by difficulty rank: the easiest at level 1, moving to the fifth of eight by level 500 (previously always the hardest). Only completely filled layouts with at least one blocked arrow compete. The catalogue is still sorted by arrow count, then rating: level 1 has 6 arrows, level 10 has 9, 50 has 15, 100 has 23, 200 has 32, 300 has 41, 400 has 48 and 500 has 67 (previously 115 to 371). On average 39% of arrows start blocked on levels 1–50 and 80% on levels 451–500 (previously at least 85% everywhere). Daily boards are 8×10 with 22–31 arrows and weekly boards 9×11 with 25–36 (previously 27×31 and 28×32, 91–132 arrows). Unfinished attempts reset moves and hearts on the version change; unlocked levels, awards and spent hints are kept.

**Arrows.** Board View draws lines at 24% of the cell size (at least 4 and at most 18 UI units; the cap grows with zoom), up from 12% (3–8 units), with solid triangular heads instead of thin chevrons. Path guides and the red blocked-path ray scale with the line. A tap selects the nearest arrow within 45% of a cell (up to 40 units), up from 38% (up to 22). New Inspector fields on Board View: Arrow Thickness and Max Arrow Thickness. On the 612×690 board area, level 1 draws 18-unit lines on 101-unit cells and level 500 draws 9.3-unit lines on 39-unit cells.

**Audio.** CorrectMove.wav is a 0.24 s rounded "bloop" (440→587 Hz glide, 12 ms fade-in, peak −14 dBFS); it was a 740 Hz ping at −10.6 dBFS. WrongMove.wav is two plucked low notes (E4, B3) over 0.46 s at −22.8 dBFS RMS; it was a sustained 220/140 Hz buzz at −11.2 dBFS RMS. BackgroundLoop.wav is a 25.3 s, 76 BPM mellow loop with no drums: no energy above 2 kHz (was 7.8%), largest sample-to-sample step 0.07 (was 0.70), peak −6 dBFS and RMS −19.1 dBFS (was −1.0 and −17.8). File names and GUIDs are unchanged, so Main.unity needs no edits. Generators: `Audio/Source~/compose_effects.py` (new) and `compose_background.py` (rewritten).

**Validation.** Puzzle.cs, Checks.cs and CampaignOrder.cs were compiled and run with Roslyn on .NET 8 at C# 9. The previous code passed its own checks there first (4,570,079 assertions), confirming the October 6 Python port matched C#. With the new generator and regenerated catalogue, Checks.Run passed 459,233 assertions across 500 campaign, 366 daily and 260 weekly boards in 1.6 s (previously 24 s). Updated checks: level 1 is at most 5 cells wide with 4–8 arrows; levels 10, 50 and 100 have at most 12, 20 and 30 arrows; level 500 at most 80; no board exceeds 14×16; every level has a blocked arrow; levels 250 and 500 have at least half their arrows blocked. SplashDifficultyVerification now compares each sampled level with the catalogue instead of requiring 80+ arrows. BoardView.cs was type-checked against stand-ins for the UnityEngine API it uses, and its mesh was built for level 1 (242 vertices) and level 500 with guides and a hint (5,309 vertices). Not compiled or played in Unity here, and no APK/AAB/IPA was built. Tournament servers sharing Puzzle.cs and CampaignOrder.cs must be updated alongside clients.

## Middle-ground difficulty and line weight — October 8, 2026 (later)

Supersedes the level and arrow numbers in the section above; the audio changes stand. After playing it, the user found that version too simple and the arrows far too thick.

**Levels.** Generation version 7. Campaign boards start at 8×10 and grow in steps to 18×21. Arrows start 2–5 cells long and reach 3–8 cells with more turns on the largest boards. Each board is picked from eight generated layouts by difficulty rank: about the third easiest at level 1 (hardness 250 of 1000) up to about the seventh by level 500 (800). Arrow counts: level 1 has 21 (13 blocked at the start), level 10 has 24, 50 has 32, 100 has 41, 200 has 55, 300 has 72, 400 has 83 and 500 has 95. On average 69% of arrows start blocked on levels 1–50 and 83% on levels 451–500. Daily boards are 11×13 with 36–49 arrows; weekly boards are 12×14 with 43–58. Unfinished attempts from version 6 reset moves and hearts; unlocked levels, awards and spent hints are kept.

**Arrows.** Lines are 16% of the cell size, kept between 6 and 8 UI units (8 on level 1, 6 on the largest boards); this afternoon's version drew up to 18, the October 6 version 3. Arrowheads scale with the line: 3.4 lines long and 4.2 lines wide, never more than 42% / 60% of a cell. The Board View fields are renamed Line Thickness (0.16), Min Line Thickness (6) and Max Line Thickness (8), so values held from the earlier Arrow Thickness field are not reused.

**Validation.** Checks.Run passed 830,625 assertions across 500 campaign, 366 daily and 260 weekly boards in 2.8 s (Roslyn, .NET 8, C# 9). Updated checks: level 1 is at most 9 cells wide with 12–30 arrows and at least a third of them blocked; levels 10, 50 and 100 have at most 35, 45 and 60 arrows; level 500 at most 130. The Tests/Puzzles runner compiled with no warnings, regenerated a byte-identical catalogue and passed. BoardView.cs was type-checked against UnityEngine stand-ins; level 1 builds 1,007 vertices and level 500 with guides and a hint 8,141. Not compiled or played in Unity here, and no APK was built.

## Level-complete stars, jingle and Next level — October 8, 2026

The user asked for the clear popup to celebrate the win: an achievement sound, three stars, and a way to go straight to the next level.

**What changed.** New `Runtime/StarDisplay.cs` draws three gold stars in one graphic; each time the dialog opens they pop in 0.22 s apart with a small overshoot, the middle star larger and raised and the side stars tilted outward. Main.unity gains one object, `Win/Stars` (330×106, between Win/Title and Win/Body), registered in the live SceneUI. The disabled backup UI is untouched. SceneLayout.json gains the same node (kind `stars`), and SceneUIBuilder can build it. `GameAudio.levelComplete` (new LevelComplete.wav, 1.9 s: dings at 0, 0.22 and 0.44 s matching the stars, then a C major chord at 0.66 s, peak −10 dBFS, 0.2% of energy above 2 kHz) is referenced in the scene and assigned by GameAudioAuthoring. `GameApp.Win` shows three stars, plays the jingle, and labels the green button **Next level** for campaign and daily wins; after a daily puzzle it opens the current campaign level. Weekly wins keep **Next puzzle**; **Back home** appears only once all 500 levels are done. Win messages are shortened to one line ("Daily star collected!", "One step closer to a medal!"), because the two-line daily message was cut off. `SceneUI.Stars` skips scenes without the stars object, and the jingle is skipped if no clip is assigned.

**Validation.** GameApp, GameAudio, SceneUI, StarDisplay, HeartDisplay and BoardView with the puzzle core were type-checked by Roslyn at C# 9 against stand-ins for the UnityEngine and project APIs they use: 0 errors (a deliberately broken copy reported both errors). All 3,879 Main.unity documents parse as YAML, file IDs are unique, there are no dangling local references, and the only differences from the previous scene are the four new objects, one child link, one SceneUI entry and one audio reference. SceneLayout.json parses, with 463 nodes. The popup was mocked at 720×1280 with the project's slices and fonts, and a 2.8 s preview with sound was rendered (`Claude outputs/arrowattack-win-popup-preview.mp4`). Not compiled or played in Unity here, and no APK was built.

## A difficulty climb players can feel — October 8, 2026 (evening)

Supersedes the generation-version-7 numbers above. The user found that version still too easy, and the difficulty did not visibly rise from level to level: level 1 had 21 arrows and level 10 only 24, on the same 8×10 board.

**Generation version 8.** Each candidate's place on a hand-tuned difficulty curve (steepest early, still rising at the end) sets both its board area and its layout pick. The area grows from 99 cells (9×11) to 690 (about 25×28). Each board uses the portrait shape (up to 4:5) closest to that area, so neighbouring levels get slightly different boards rather than long runs of one size. Layout picks run from difficulty 350 to 1000 (from about the third easiest of eight layouts to the hardest). Arrows start 2–5 cells long with fewer turns, and reach 3–9 cells with more turns.

**Picking the levels.** The catalogue now draws the 500 levels from 1,000 candidates (`Levels.CandidateCount`). `Tests/Puzzles --catalogue` sorts them by arrow count and rating, takes roughly every second one, and skips ahead to a board with more arrows after three levels with the same count.

**Result.** Levels 1–10 have 28, 30, 31, 32, 35, 36, 36, 39, 41 and 43 arrows. Level 20 has 52, 50 has 72, 100 has 96, 200 has 118, 300 has 134, 400 has 149 and 500 has 175. On average 76% of arrows start blocked on levels 1–50 and 85% on levels 451–500. Daily boards are 13×15 (51–66 arrows), weekly 15×17 (62–79).

**Validation.** Checks.Run passed 1,431,548 assertions across 500 campaign, 366 daily and 260 weekly boards in about 6 s (Roslyn, .NET 8, C# 9). New checks:
- level 1 is at most 10 cells wide, with 20–40 arrows and at least half blocked;
- arrow counts rise across every five levels up to level 101, and no more than four levels in a row share a count up to level 100;
- counts rise across every 20 levels from 100 to 500;
- level 10 has at least 1.4× and level 50 at least 2× the arrows of level 1;
- levels 10, 50 and 100 have at most 60, 100 and 120 arrows, and level 500 at most 180;
- every board stays within the largest campaign area and is portrait.

The Tests/Puzzles runner compiled with no warnings, regenerated a byte-identical catalogue and passed the full run. Runtime scripts still type-check against the UnityEngine stand-ins. Not compiled or played in Unity here, and no APK was built.

## More arrows on every level — October 8, 2026 (late)

The user asked for more arrows on every level. Generation version 9: boards now grow from 130 cells (10×13) to 840 (about 26×32), and arrows are slightly shorter (2–4 cells at first, up to 3–8), so every board holds more arrows. The difficulty curve, layout picks and level picker are unchanged.

All 500 levels have more arrows than in version 8: at least 14 more, and 31% more overall. Levels 1–10 have 43, 44, 47, 50, 51, 52, 54, 56, 57 and 59 arrows. Level 20 has 73, level 50 has 99, level 100 has 127, level 200 has 156, level 300 has 175 and level 500 has 221. Daily boards are 15×17 (76–91 arrows), weekly 17×20 (90–111). The level-to-level climb is kept: no more than three equal counts in a row in the first 100 levels, and growth every 20 levels after that. The largest boards leave cells of about 20 UI units on the 612×690 board area.

**Validation.** Checks.Run passed 1,836,434 assertions in about 9 s. Ramp limits were raised:
- level 1 is at most 12 cells wide, with 30–55 arrows;
- levels 10, 50 and 100 have at most 80, 140 and 170 arrows, and level 500 at most 260;
- level 10 has at least 1.3× and level 50 at least 2.2× the arrows of level 1.

The Tests/Puzzles runner compiled with no warnings, regenerated a byte-identical catalogue and passed. Runtime scripts still type-check. Not compiled or played in Unity here, and no APK was built.

## Missing win stars, gray stars and a lose sound — October 8, 2026 (night)

**Missing win stars.** The user reported that the win stars were not showing. Main.unity on disk still had Win/Stars, its SceneUI entry and the jingle reference, unchanged since the 22:28 edit, and Unity had recompiled the scripts at 23:13. The Unity editor appears to have kept the scene it had open before the file changed on disk, and SceneUI.Stars silently skipped the missing object. The newest APK is from October 6, so testing was in the editor.

**Fix without another outside scene edit.** New `Editor/RewardStarsSetup.cs` checks the open Main scene inside Unity after every script reload, when Main.unity opens and when Play mode ends. It adds Win/Stars (gold, 330×106) and Fail/Stars (gray, 270×86, at y 120 under "Take a fresh look") if they are missing, registers them in the live SceneUI, assigns the Level Complete and Level Failed clips, and saves the scene. It is also on the menu as **Arrow Attack → Add reward stars and jingles**. SceneUI.Stars now logs a warning naming the missing object instead of failing silently.

**Gray stars and lose sound.** StarDisplay draws unearned stars in gray (fill 168/172/178, highlight 214/217/221, outline 62/66/72). `GameApp.ShowFail` shows three gray stars. When the last heart is lost it also plays the new LevelFailed.wav: notes at 0, 0.22, 0.44 and 0.66 s (G4, E♭4, C4, then B3 bending down to B♭3), 1.9 s long, peak −12 dBFS, nothing above 2 kHz. The phrase cuts off the wrong-move sound. Reopening a level that is already out of hearts shows the stars without the sound. GameAudio plays both jingles through one PlayJingle method. GameAudioAuthoring and ReferenceUIPlayCheck include the new clip. SceneLayout.json has the Fail/Stars node (464 nodes), and SceneUIBuilder builds Fail stars with none earned.

**Validation.** Runtime scripts and RewardStarsSetup.cs were type-checked by Roslyn at C# 9 against stand-ins for the UnityEngine and UnityEditor APIs they use: 0 errors (a deliberately broken copy was reported). The Fail popup was mocked with the project's slices and fonts, and a preview with sound was rendered (`Claude outputs/arrowattack-lose-popup-preview.mp4`). Main.unity was not edited from outside this time. Not compiled or played in Unity here.

## Every phone and tablet — October 8, 2026 (later that night)

The user asked for the game to play on any phone or tablet with a fully responsive UI.

**Layout.** New `Runtime/ResponsiveLayout.cs` holds the rules, and the rewritten `Runtime/FitDesign.cs` applies them while playing. GameApp calls `FitDesign.MakeResponsive` first in Awake, before the splash logo stores its position. The Design area takes the shape of the safe area and is never smaller than 720×1280: phones keep the 720 width and get taller, and wider screens keep at least the 1280 height. On screens wider than 9:16 the UI may shrink by up to 1.15 (Tablet Room) so the board gets more room. Direct children of each screen get a role from their authored position: 32 Top, 24 Bottom, 127 Middle (including all dialog content), 1 Stretch (Game/Viewport, with the board), 36 frames, 10 Cover backgrounds and 1 Centre (the oversized Splash background). Backgrounds and dialog backdrops cover the whole screen plus 32 units. Frame corner miters now have fixed sizes (72, 96 and 101.12 units), which is identical at 720×1280. Edit mode and the editor screenshot helpers keep the fixed layout through `FitDesign.FitTo`. Main.unity and SceneLayout.json are unchanged.

**Build settings** (applied by Prepare project): iPad runs full screen (`requiresFullScreen`), since older Xcode versions reject a portrait-only iPad app without it (Apple deprecated the key in iPadOS 26 and plans to ignore it later; the layout adapts to resized windows either way). The Android manifest declares `android:appCategory="game"`: from Android 16, large screens ignore an app's requested orientation, except in games. ARMv7 is built next to ARM64, and the resizable-activity, render-outside-safe-area and iPhone + iPad target-device settings are set explicitly.

**Validation.**
- Runtime and Editor scripts were type-checked by Roslyn at C# 9 against stand-ins for the Unity APIs they use: 0 errors. The one warning (CS0649 on `GameApp.ui`) was already there.
- LayoutSim ran the game's own ResponsiveLayout.cs on SceneLayout.json for 15 screen shapes with their safe areas: iPhone SE, iPhone 15, Galaxy S24, Xperia 1 (21:9), Galaxy Z Fold cover and open, iPad mini, iPad 10th gen, iPad Pro 13", Galaxy Tab S9, an 800×1280 tablet, phone split screen, an iPad landscape window, a 1920×1080 window and a phone with an ad banner. 54,075 checks found 0 problems: no new overlaps, all content inside the safe area and above the banner, and backgrounds and backdrops covering the full screen.
- Sizes on a 15×17 board compared with the old scale-to-fit layout: on phones, board cells and buttons keep their size and the layout fills the screen instead of leaving bands. Board cells and buttons change by +10% and −11% on iPad 10th gen, +8% and −8% on iPad mini, +12% and −13% on iPad Pro 13" and the unfolded Fold, and +5% and −5% on Galaxy Tab S9.
- Before/after previews of Home, Game, Levels and Win on ten devices, and of every other screen and dialog on the Fold cover, iPad 10th gen and an iPad landscape window, were rendered from the simulated layout and inspected. One overlap was found that this change did not cause: on the Tournament screen, Tournament/Subtitle ("Week of … / UTC") and Tournament/Score are authored at the same height.
- Not run in Unity or on devices here, and no APK or IPA was built.

## Daily climb, level-100 weekly puzzles and wrong-tap bump — October 9, 2026

The user asked for three changes and confirmed what the first two mean:
- "Weekly challenge should be a 100 level": every weekly puzzle plays like campaign level 100. Medals stay at 5, 15 and 30 puzzles a week.
- "Daily challenge should start with 10 and go to month dates": the 1st of each month plays like level 10, and each later date like the next level, up to level 40 on the 31st.
- A wrong tap: the arrow moves, hits the arrow in its way, goes back to its place in red and blinks.

**Boards.** New `Levels.LikeLevel(level, seed)` builds a board at that level's point on the difficulty curve, with the same board size, arrow lengths and layout hardness as the campaign uses there, from its own seed. Of up to 12 seeds it keeps the first with the level's arrow count or one more, or else the closest. `Levels.Daily(date)` uses level `9 + day of month` (`DailyLevel`). `Levels.Weekly` uses level 100 (`WeeklyLevel`); tournaments use the same boards, so a tournament server must be built with this Puzzle.cs. Campaign boards are unchanged: `CampaignCandidate` now calls the shared `CandidateBoard` with the same seeds, and `--catalogue` regenerated a byte-identical CampaignOrder.cs.

**Saved games.** Each kind of puzzle now has its own layout version (`Levels.LayoutVersion`). Daily, weekly and tournament puzzles moved to `ChallengeGenerationVersion = 10`, so their unfinished attempts restart once; collected stars, medals and spent hints stay. The campaign stays at `GenerationVersion = 9`, so unfinished campaign levels keep their moves and hearts.

**Wrong-tap bump** (`Runtime/BoardView.cs`, `Error`):
- The tapped arrow turns Error red and slides forward along its own path until its tip touches the first arrow in its way. This takes 0.10–0.25 s, longer for farther blockers.
- The arrow it hits is knocked 0.14 of a cell for 0.2 s.
- It slides back in 0.24 s, then blinks red three times over 0.6 s.
- On a fresh board the blocker is always next to the arrow, so the slide is about 0.8 of a cell; once space opens up, arrows travel farther.
- The red guide line is gone, and the status text now reads "Blocked! Clear the arrow in its way first."
- Other arrows can still be tapped during the animation.
- When the last heart is lost, `GameApp.FailAfterBump` waits until the arrow is back (plus 0.3 s) before opening the out-of-hearts popup, and blocks taps meanwhile.

**Validation.**
- Checks.Run passed 1,914,266 assertions in about 8 s.
  - All 366 dailies of 2028: each has its own board, plays like level 9 + date (within 3 arrows of that level), never drops more than 3 arrows from one day to the next, and ends each month at least 25 arrows above its 1st. At least 90% have the level's arrow count or one more.
  - 260 weekly boards play like level 100 (within 4 arrows; at least 80% have 127 or 128).
  - Layout-version checks: an old daily attempt restarts, and a current campaign attempt resumes.
- Measured over 2026–2028:
  - Dailies: 1,057 of 1,098 dates have their level's arrow count or one more, none is more than 2 off, and no day has more than 2 arrows fewer than the day before. October 2026 runs from 60 arrows on the 1st (13×14) through 80 on the 15th (15×16) to 94 on the 31st (16×18).
  - Weekly boards: 19×23, with 227 of 260 at 127–128 arrows and none more than 3 off.
  - Building one board took about 5 ms (daily) and 11 ms (weekly) in .NET 8 here. Each board is built once, when a puzzle opens.
- The Tests/Puzzles runner compiled and printed the new daily and weekly samples. Runtime and editor scripts type-check with 0 errors (the CS0649 warning on `GameApp.ui` was already there).
- `Claude outputs/arrowattack-oct9-wrong-tap.mp4` was rendered outside Unity with the same timings and geometry: two wrong taps and one correct tap on level 1, then the second bump in slow motion.
- Not played in Unity or on a device here, and no APK was built.
