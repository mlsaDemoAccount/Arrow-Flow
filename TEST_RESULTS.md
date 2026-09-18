# Validation report

## Passed

- **765,089 logic assertions** across 500 campaign, 366 daily, and 260 weekly boards. Checks include geometry, overlaps, reverse-order solvability, independent solving, hints, unlock/replay rules, attempt separation, leap-year trophies, and week boundaries.
- **External C# compilation against Unity 6 engine/reference assemblies**, including Android conditional code and editor helpers. The available uGUI reference was the installed Unity 2022 uGUI assembly. This checks types and syntax, not editor import or runtime rendering.
- **Android notification Java compilation** against API 36; compiler notes deprecated compatibility APIs.
- **.NET 10 tournament server build**, no errors; three nullable-annotation warnings in the Unity-compatible shared core.
- **51 live local integration checks** covering registration, authentication, invalid proofs, valid cross-runtime proofs, duplicate protection, leaderboard updates, name presentation, expiry, and deletion.
- **Historical medal fixture** covering gold/silver/bronze awards, weekly reset, idempotence, and persisted medals.

## Not verified

- Unity Editor import, Play mode, rendering, gesture behavior, and frame rate.
- APK/AAB compilation, installation, and Android notification delivery.
- iOS native compilation, Xcode export, signing, installation, and notification delivery.
- Public server hosting or load testing.
- Google Mobile Ads Unity Plugin package resolution, the in-editor App ID Settings step, and actual rewarded-ad load/show behavior (see below).

Unity 2022 and Unity 6 rejected the installed license. Unity 6 was retried after activation was reported; its licensing service still reported a machine-binding mismatch.

## Re-run

- Unity menu: **Arrow Flow → Run logic checks**.
- Server: run locally, then execute `TournamentServer/Tests/Integration.ps1`.
- Scene UI: run **Arrow Flow → Validate scene UI**, then enter Play mode and click each screen and dialog button. The earlier procedural UI smoke runner was retired in the scene-based conversion.

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
