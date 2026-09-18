# Arrow Flow — Unity game source

Original arrow-removal puzzles for Android and iOS. No purchases, subscriptions, or paid hints. The only monetization is one optional Google AdMob rewarded-video ad, offered when a life refill is needed (see **Ads (rewarded video)** below); the refill is granted either way, so play is never blocked on ad availability.

## Open and play

1. Add this `ArrowFlow` folder in Unity Hub. Use **Unity 6000.4.4f1** with an active editor license.
2. Allow the package manifest to resolve and the scripts to compile.
3. Choose **Arrow Flow → Prepare project** from the editor menu.
4. Open `Assets/Scenes/Main.unity` and press Play.

**All UI is saved in Main.unity**, including every screen, dialog and persistent Button On Click event. The runtime reuses these objects and updates only game data. Read **SCENE_UI_GUIDE.md** for hierarchy editing, screen preview, Inspector callbacks, and migration of existing scenes.

## Features implemented in source

- 500 original deterministic campaign puzzles with guaranteed solutions.
- Winding arrows, directional obstruction checks, animated departures, and three lives.
- Unlimited free hints, free path guides, red blocked-path feedback, and free life restarts. Life refills offer a rewarded video ad (Google AdMob) and grant the refill regardless of whether an ad played.
- Pinch zoom, drag/two-finger pan, mouse-wheel zoom, and Fit view.
- Local move-by-move progress, unfinished attempts, backup recovery, and unlocked-level replay.
- Daily calendar, past-day replay, daily stars, monthly trophies, and leap-year handling.
- Weekly solo puzzles with bronze/silver/gold milestones and saved medal totals.
- Optional real online tournaments: generated anonymous names, rankings, validated solutions, and final top-three medals.
- Optional 6 pm daily notification on Android/iOS, with permission requested only when enabled.
- Sound/haptic settings, tutorial, privacy explanation, and confirmed deletion of progress.
- Portrait layout fitted to device safe areas.

## Android build

Install Android Build Support with SDK, NDK, and OpenJDK through Unity Hub.

- **Arrow Flow → Build Android APK** creates `Builds/Android/ArrowFlow.apk`.
- **Arrow Flow → Build Android App Bundle** creates `Builds/Android/ArrowFlow.aab`.
- Configuration: ARM64, IL2CPP, minimum API 26, newest installed target SDK, portrait orientation, Activity entry point.
- Before release, change the placeholder package identifier `com.arrowflowstudio.arrowflow` and configure your signing keystore. Build setup currently assigns that identifier each time; change it in `Editor/BuildTools.cs`.

## iOS build

On a Mac, install Unity iOS Build Support and Xcode. Open this project and select **Arrow Flow → Export iOS Xcode project**. Open the generated project in `Builds/iOS`, select your signing team and identifier, and build/run or archive through Xcode.

The source includes a native notification bridge and a post-build script to link UserNotifications.framework. iOS native compilation and signing still need verification on a Mac.

## Ads (rewarded video)

The **Refill** button on the Fail dialog shows a Google AdMob rewarded-video ad; when the player finishes it, `Runtime/Ads.cs` reports it back to `GameApp.RefillLives()`, which refills to three lives and updates the status text. If no ad is loaded (no network, no fill, or the player taps before one finishes loading), the refill is granted anyway and the status text says so — the game never makes a player wait on ad inventory.

The Google Mobile Ads Unity Plugin is distributed through Unity's Package Manager rather than a `.unitypackage`. `Packages/manifest.json` already points at it:

```json
{
  "scopedRegistries": [
    { "name": "google", "url": "https://package.openupm.com", "scopes": ["com.google"] }
  ],
  "dependencies": { "com.google.ads.mobile": "11.5.0" }
}
```

Opening the project in Unity resolves this automatically. After it resolves, do the one step Google's plugin requires in-editor: **Assets → Google Mobile Ads → Settings**, and paste in your Android and iOS AdMob **App IDs**. Ad unit IDs (a separate value from the App ID) are read at runtime from `Assets/Resources/AdsConfiguration.json`:

```json
{
  "androidAppId": "...",
  "androidRewardedUnitId": "...",
  "iosAppId": "...",
  "iosRewardedUnitId": "...",
  "testDeviceIds": []
}
```

By default this file ships with **Google's public test IDs** on both platforms, so the game builds and shows real (non-monetized) test ads immediately. Replace all four IDs — here and in the Settings asset — with your own AdMob App ID and rewarded ad unit ID before release; test ads never generate revenue and Google will reject production apps still pointed at them. While testing on a real device with your own IDs, add that device's advertising ID to `testDeviceIds` so your own taps don't create invalid-traffic flags on your account.

Before publishing with ads live, also handle AdMob's own requirements, which are outside this game's code: user consent (Google's User Messaging Platform / a consent management platform) if you serve EEA/UK/Swiss/California users, and Apple's App Tracking Transparency prompt on iOS if you want personalized ads there. Neither is wired up yet.

## Online tournaments

The sibling `TournamentServer` folder contains the server. Keep both folders beside each other because they share `Core/Puzzle.cs`.

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
| Core/Checks.cs | Reproducible logic tests |
| Runtime/GameApp.cs | Public button handlers, navigation and game-data updates |
| Runtime/Ads.cs | Rewarded-video ad loading/showing (Google Mobile Ads Unity Plugin) for the life refill |
| Runtime/SceneUI.cs | Serialized references to every saved UI object |
| Editor/SceneUIBuilder.cs | Optional editor-time UI generation and persistent event wiring |
| Runtime/BoardView.cs | Vector rendering, animation, gestures and safe-area layout |
| Runtime/SaveStore.cs | Local JSON persistence and backup recovery |
| Runtime/TournamentClient.cs | Online identity, rankings, submission and deletion |
| Runtime/Reminders.cs and Assets/Plugins | Mobile notifications |
| Editor/BuildTools.cs | Setup, tests and platform builds |

## Validation and limitations

See **TEST_RESULTS.md**. Logic, external C# compilation, Android Java compilation, and local server integration tests passed. Unity Editor execution, visual QA, and mobile builds remain unverified because the installed editor license was rejected with a machine-binding mismatch. No verified APK, AAB, IPA, or gameplay screenshot is included.

This is original artwork/UI and generated level content inspired by the reference mechanics, not Easybrain's exact assets, levels, or private implementation. Cloud-save/device transfer and localization are not implemented. On-device testing is still required for touch accuracy, save recovery, suspend/resume, safe areas and notifications.

The optional server is suitable as a small single-instance starter; review its README before hosting publicly.


