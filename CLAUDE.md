# CLAUDE.md

Unity 6 (6000.6) mobile game template (URP, Android/iOS). Every new mobile project is cloned from this one, so these rules apply to all of them. Follow them without being asked.

## Packages you must use

| Need | Use | Never |
|---|---|---|
| Wiring dependencies | **VContainer** (`jp.hadashikick.vcontainer`) | Singletons, static managers, service locators, `FindObjectOfType`, `GameObject.Find` |
| Transform / UI / value animation, delays, sequences | **PrimeTween** (`com.kyrylokuzyk.primetween`) | DOTween, `Animator` for simple motion, coroutine or `Update` lerps |
| Vibration / haptic feedback | **RichHaptic** via `IHapticPlayer` (`Assets/Scripts/Core/Haptic`) | `Handheld.Vibrate`, native plugins, the static `RichHaptic.Haptics` API |

Other installed packages: Google Mobile Ads (AdMob), Unity IAP, Mobile Notifications + Push Notifications, Input System, TextMesh Pro, Unity Pipeline (CLI control of the Editor).

## 1. Architecture: DI, SOLID, OOP

### Dependency injection with VContainer
- **Root scope:** `Assets/Scripts/Core/ProjectLifetimeScope.cs`, on the prefab `Assets/Prefabs/ProjectLifetimeScope.prefab`. `Assets/Settings/VContainerSettings.asset` points VContainer at that prefab and is in Player Settings' Preloaded Assets. It survives scene loads. Register app-wide services here (save, audio, haptics, ads, IAP, analytics).
- **Scene scopes:** each scene gets its own `LifetimeScope` for scene-only services. VContainer parents it to the root scope automatically.
- **One installer per feature.** Group a feature's registrations in a class that implements `IInstaller` (see `HapticInstaller`) and call it from a scope's `Configure`. Don't add long registration lists directly to a scope.
- **Register against interfaces.** Consumers depend on `IFoo`, not `Foo`: `builder.Register<IFoo, Foo>(Lifetime.Singleton)`.
- **Prefer plain C# classes over MonoBehaviours.** Put logic in plain classes that take their dependencies through the constructor. For lifecycle hooks, use VContainer entry points (`IInitializable`, `IStartable`, `ITickable`, `IDisposable`) registered with `builder.RegisterEntryPoint<T>()`. Don't give a MonoBehaviour an `Update` just to run logic.
- **MonoBehaviours are views.** They hold serialized references, forward input and show state. They receive dependencies through an `[Inject] public void Construct(...)` method. Register scene components with `RegisterComponentInHierarchy` / `RegisterComponent`, or add them to the scope's Auto Inject list. Spawn injected prefabs with `IObjectResolver.Instantiate(prefab)`, never plain `Object.Instantiate`.
- **Configuration lives in ScriptableObjects** registered with `RegisterInstance`, not in hard-coded constants or scene objects.
- **Gotcha:** VContainer rewrites any newly created file whose name ends in `LifetimeScope.cs` into an empty template. `DisableScriptModifier` is on in `VContainerSettings.asset` to prevent this. If a new `*LifetimeScope.cs` still comes out empty, rewrite it after its `.meta` exists.

### SOLID / OOP
- **Single responsibility:** one reason to change per class. Split "manager" god classes into focused services.
- **Open/closed:** extend through new implementations, data assets (like `HapticProfile`) or strategies, not by adding `switch` branches to existing classes.
- **Interface segregation:** small, role-specific interfaces. `IHapticPlayer` and `IHapticSettings` are split because gameplay and the settings menu need different things.
- **Dependency inversion:** depend on abstractions that are injected, never `new` a service inside another service.
- Classes are `sealed` unless they are designed for inheritance. Fields are `private` and exposed through properties or methods. Use `[SerializeField] private` instead of public fields.
- Prefer composition to inheritance. Use events or C# delegates (or a message broker registered in DI) to decouple systems instead of direct cross-references.

### Optimization (mobile first; this is a requirement)
- **No per-frame allocations.** In `Update`/`Tick`/hot paths avoid LINQ, `new` collections, closures capturing locals, string building/concatenation, boxing, `foreach` over interfaces, and `params` calls. Reuse buffers and collections.
- **Cache everything:** `GetComponent`, `Camera.main`, `transform`, `WaitForSeconds`, and animator/shader property IDs (`Animator.StringToHash`, `Shader.PropertyToID`).
- **Pool frequently spawned objects** with `UnityEngine.Pool.ObjectPool<T>`, never repeated `Instantiate`/`Destroy`.
- Use the non-allocating physics APIs (`Physics.RaycastNonAlloc`, `OverlapSphereNonAlloc`) and `CompareTag`. Never use `tag ==`.
- Fewer `Update` methods. One `ITickable` that iterates a list beats hundreds of MonoBehaviour `Update`s.
- **UI:** split static and dynamic elements into separate Canvases. Turn off `Raycast Target` on non-interactive graphics. Never change layout every frame.
- Avoid `SendMessage`, reflection, and `Resources.Load` at runtime. Load addressable or serialized references instead.
- Event-driven over polling. Profile before micro-optimizing, but never write an obviously allocating hot path.

## 2. Animation: PrimeTween
- Use PrimeTween for every transform, UI, color, alpha, and value animation, and for delays and timed sequences. Don't write `Update` lerps or coroutines for these.
- Common calls: `Tween.LocalPosition`, `Tween.Position`, `Tween.Scale`, `Tween.Rotation` / `LocalRotation`, `Tween.UIAnchoredPosition`, `Tween.Alpha` (CanvasGroup, Image, …), `Tween.Color`, `Tween.Custom` (any float/vector), `Tween.PunchScale`, `Tween.ShakeLocalPosition`, `Tween.ShakeScale`, `Tween.Delay` (instead of `Invoke` or `WaitForSeconds`).
- Sequences: `Sequence.Create().Chain(a).Group(b).ChainDelay(t).ChainCallback(...)`.
- Await with `await tween;` or `yield return tween.ToYieldInstruction();`.
- UI and pause menus use `useUnscaledTime: true` so they still animate when `Time.timeScale == 0`.
- **Lifecycle:** keep the `Tween`/`Sequence` handle when an animation may be interrupted. Call `.Stop()` or `.Complete()` before starting a conflicting one, and check `isAlive`. In `OnDisable`/`OnDestroy` (or `IDisposable.Dispose`) call `Tween.StopAll(onTarget: target)` so no tween runs on a destroyed object.
- PrimeTween allocates nothing per tween. Keep it that way: no capturing lambdas in hot paths, and use the `target` overloads of `OnComplete`/`ChainCallback` instead of closures.
- If the game runs many concurrent tweens, raise capacity once at startup with `PrimeTweenConfig.SetTweensCapacity(n)`.

## 3. Haptics: RichHaptic through the core system
The core system lives in `Assets/Scripts/Core/Haptic` (namespace `Template.Core.Haptic`) and is registered by `HapticInstaller` in the root scope.
- **Play:** inject `IHapticPlayer`.
  - `Play(HapticProfile)` is the normal path. Create profiles with *Create > Template > Haptic Profile* and assign them in the Inspector. A profile holds a preset or an authored `HapticClip`, a priority, and a cooldown.
  - `Play(PresetType, priority)` is for quick one-offs.
  - `StartContinuous` / `UpdateContinuous` / `StopContinuous` are for held or charging effects. Always stop a continuous session in `OnDisable`/`Dispose`.
- **Settings:** inject `IHapticSettings`. Its `Enabled` and `Intensity` are saved to PlayerPrefs and applied at startup. Use `IsSupported` to hide the option on devices without a vibrator. Bind the settings menu toggle and slider to it.
- **Priority:** 0 is the most important. Use `HapticPriority.Critical/High/Default/Low`. A lower-priority haptic never interrupts a higher-priority one that is still playing.
- Never call the static `RichHaptic.Haptics` API outside `HapticInstaller`. Its instance is registered in DI as `RichHaptic.IHapticService`.
- Don't name a namespace `Haptics`; it collides with the `RichHaptic.Haptics` class.
- Add new haptic features (for example a `HapticButton` view or haptics driven by game events) as new classes that depend on `IHapticPlayer`. Don't change the core.

## 4. Ads: AdMob through the core system
The core system lives in `Assets/Scripts/Core/Ads` (namespace `Template.Core.Ads`) and is registered by `AdInstaller` in the root scope. Configuration is `Assets/Settings/AdConfig.asset` (master `Ads Enabled` toggle, ad unit IDs per platform, banner position, interstitial cooldown, consent, test devices), assigned on the `ProjectLifetimeScope` prefab.
- **Show ads:** inject `IAdController`. `ShowBanner` / `HideBanner`, `ShowInterstitial(onFinished)`, `ShowRewarded(earned => ...)`. The callbacks always fire, immediately if no ad was shown, so flows never stall. Grant rewards only when `earned` is true.
- Pause gameplay and mute audio on `FullscreenAdOpened` / `FullscreenAdClosed`; Android doesn't pause the app for you. Drive "watch ad" buttons from `IsRewardedReady` / `RewardedReadyChanged`.
- **Settings:** inject `IAdSettings`. Set `AdsRemoved = true` from the "remove ads" purchase (hides the banner and blocks interstitials; rewarded ads stay). Show a "Privacy settings" button only when `IsPrivacyOptionsRequired`, and open it with `ShowPrivacyOptions()`.
- UMP consent runs before the SDK initializes; never call `MobileAds.Initialize` or load ads yourself. Never call the static `MobileAds` / `InterstitialAd` / `RewardedAd` APIs outside `Assets/Scripts/Core/Ads`.
- Add a new format (app open, rewarded interstitial) as its own unit class behind an interface, register it in `AdInstaller`, and add its ad unit IDs to `AdUnitIds` and the Checklist.

## Project layout
- `Assets/Scripts/Core/` holds reusable template code (asmdef `Template.Core`; references VContainer, RichHaptic.Runtime, PrimeTween.Runtime).
- Put game-specific code in feature folders under `Assets/Scripts/`, each with its own asmdef when it grows. Keep the dependency direction Game → Core, never Core → Game.
- `Assets/Editor/` holds editor-only tools. **Tools > Checklist** (`Assets/Editor/Checklist/ProjectChecklistWindow.cs`) is the pre-release checklist. Add a section there for every release-critical setting a new system introduces.
- Code style: 4-space indent, `private` camelCase fields without an underscore, PascalCase members, block-style `namespace` declarations under `Template.*`. Comment only what the code can't say.

## Release notes for this template
- AdMob App IDs (`Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset`) default to **Google's test IDs**. Replace them with the real IDs before release; the Checklist window warns while test IDs are set.
- Ad unit IDs (`Assets/Settings/AdConfig.asset`) also default to **Google's test units**. The Checklist warns while they are test IDs, and reports an error if one is actually an App ID or belongs to a different AdMob account than the App ID.

## Working with the Editor
- When a Unity Editor has this project open, drive it with the `unity` CLI (`unity status`, `unity command …`, pass `--project-path` when several Editors are open). Don't hand-edit `.unity`, `.prefab` or `.asset` YAML while it's open.
- After changing C#, run `unity command recompile` and poll `unity command recompile_status` until `compilationFailed` is false before reporting the work as done.
