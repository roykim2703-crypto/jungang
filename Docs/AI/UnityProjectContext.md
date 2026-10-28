# Unity project context

Inspected 2026-10-29 at base commit 6f1459c. Project: jungang.

- Unity 6000.6.0f1 (ProjectSettings/ProjectVersion.txt).
- URP package 17.6.0 installed; GraphicsSettings has no global render pipeline override. Per-quality pipeline selection was not inspected.
- Input handling is Both; Home and Game use InputSystemUIInputModule.
- Build startup is Assets/Scenes/Home.unity, followed by Game.unity. HomeMenu loads Game asynchronously.
- UI uses uGUI, TMP, serialized MonoBehaviour references, UnityEvents and coroutines. HomeMenu and TitleFadeIn live in Assets/문지후/scripts. Runtime-built loading/tutorial overlays use separate canvases.
- Runtime UI is in Assembly-CSharp; editor setup tools live in Assets/Editor. Rules use 육목규칙.asmdef; EditMode rule tests use 육목규칙테스트.asmdef.
- Card and augmentation systems live in Assets/김민균/Script/Card. Netcode and multiplayer packages are installed; multiplayer implementation was outside this UI inspection.
- Existing code uses public component types without namespaces, four-space indentation, and private SerializeField configuration for title UI.
- Unity MCP is connected to this project. Recompile, Console, asset inspection and editor menu invocation are available. Eval intermittently fails with a Roslyn illegal byte sequence error; play commands did not retain Play Mode during this task.
- Preserve existing local changes in HomeMenu.cs, Home.unity and Fonts/CardUI SDF.asset. Do not regenerate title button layout to configure tutorial content.
- Tutorial: select Home/Canvas, edit HomeTutorial.tutorialPages (title, description, Sprite cutscenes), frameImage, cutsceneInterval and whiteFadeDuration. Tools/Home/Setup Tutorial initializes missing example content and connects HomeMenu without replacing nonempty pages.
- Validation: Unity script compilation passed. Runtime playback and visual interaction require Editor verification because Play Mode was unavailable through the connected tools. Existing Console errors before this feature concerned destroyed Image previews; existing serialization/obsolete API warnings were observed.

Evidence: Packages/manifest.json, ProjectSettings/EditorBuildSettings.asset, ProjectSettings/ProjectSettings.asset, ProjectSettings/GraphicsSettings.asset, Assets/Scenes/Home.unity, Assets/Scenes/Game.unity, HomeMenu.cs, TitleFadeIn.cs, CardAcquisitionUI.cs, and first-party assembly inventory. Target-platform and CI configuration remain unverified.
