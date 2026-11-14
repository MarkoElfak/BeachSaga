# Beach Tycoon release checklist

Everything below the line "Done in the project" is already set up. The steps under "Yours, once" need your
accounts, keys or decisions, so the game can't do them for you. `Beach Tycoon > Release > Check Google Play
Readiness` lists whatever is still missing.

## Done in the project

- App id `com.markoelfak.beachtycoon`, version 1.0.0 (`ReleaseBuilder.Version`), portrait, IL2CPP ARM64.
- Rewarded ads through LevelPlay 9.6. They start only after the privacy question is answered, with the answer
  passed on as GDPR consent. Until the keys are set, the game keeps its mock ads.
- Remove ads and restore through Unity IAP (`remove_ads`), analytics only after Accept.
- `Build Google Play Bundle (AAB)` makes a signed bundle from a clean cache in `Builds/Android/`, named
  `BeachTycoon-<version>-<code>.aab`, then raises the version code, because Google Play accepts each code once.
- Slower phones lower the render scale (down to 0.6), then shadows, while they can't hold 60 fps
  (`PresentationConfig`, builds only).
- Phone test builds (`build android-dev`) show the frame rate, the worst frame of each second and a
  **Fill beach** button that brings all 120 visitors at once.

## Yours, once

1. **LevelPlay keys.** In the LevelPlay dashboard (platform.ironsrc.com), add the Android app with the id
   above, then create one Rewarded ad unit. Paste the app key and the rewarded ad unit id into
   `Assets/_Project/ScriptableObjects/Config/MonetizationConfig.asset` (Android fields; the iOS ones later).
2. **Upload key.** In Project Settings > Player > Android > Publishing Settings, open Keystore Manager and
   create a keystore and key. Keep the file and both passwords somewhere safe outside the project: Google
   Play needs the same key for every update. Unity forgets the passwords when it restarts, so enter them
   again before a store build. `*.keystore` and `*.jks` files are kept out of git.
3. **Privacy policy.** Host a privacy policy page that mentions ads (LevelPlay and its networks), purchases and
   analytics, and put its address in `privacyPolicyUrl` in the same config. The game links to it from the
   privacy question and Settings.
4. **Google Play Console.** Create the app, turn on Play App Signing, add a one-time product with the id
   `remove_ads`, and fill in the Ads declaration (contains ads), Data safety (device ids and ad data through
   the ad networks, purchase history, app interactions for analytics), content rating and target audience
   (not children).
5. **First upload.** Build the bundle and upload it to Internal testing first. Install it from the Play Store on
   the Xiaomi and try a rewarded ad and Remove ads with a licence tester account.

## Before iOS

The App Store build needs the iOS LevelPlay keys, the App Tracking Transparency prompt and the ad networks'
SKAdNetwork ids. That is a separate step once Android is live.
