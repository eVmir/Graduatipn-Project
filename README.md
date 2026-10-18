# Graduation Project

Unity graduation project with an illustrated factory blockout and a point-and-click prototype.

## Requirements

- Unity `6000.3.8f1` (the version recorded in `ProjectSettings/ProjectVersion.txt`)
- Git

Open the repository root in Unity Hub. On first open, Unity resolves the packages in `Packages/manifest.json` and creates the local `Library/` cache.

## Scenes

- `Assets/Scenes/FactoryIllustratedBlockout.unity` — illustrated 2.5D factory prototype; this is the recommended scene to open first.
- `Assets/Scenes/FactoryBlockout.unity` — 3D factory blockout.
- `Assets/Scenes/SampleScene.unity` — original Unity sample scene.

The current Build Settings scene list contains `SampleScene`. Add the scene you want to build from **File > Build Profiles** before making a player build.

## Controls

- Left-click a walkable floor to move.
- Left-click a marked object to approach and inspect it.
- Right-click to cancel the current command.

## Blockout generators

In Unity, use **Tools > Crew > Generate Factory Blockout** or **Tools > Crew > Generate Illustrated Factory Blockout**. Each generator saves its corresponding scene under `Assets/Scenes/`, keeps baked NavMesh data in `Assets/Blockout/NavMeshes/`, and writes preview images to `Builds/BlockoutPreviews/` (ignored by Git). Running a generator replaces that generator's scene.

## Team workflow

1. Update your local `main` branch before starting a task.
2. Create a branch for the change, for example `feature/interactions`, `fix/movement`, or `art/factory-props`.
3. Keep commits focused and include the matching `.meta` file whenever you add, move, or remove an asset.
4. Push the branch and open a Pull Request into `main` so the other person can review it.

The project is already configured to serialize Unity scenes as text and to keep visible `.meta` files. Keep those settings enabled. On Windows, run `.\scripts\Configure-UnityYamlMerge.ps1` once after cloning to set up Unity's scene and prefab merge tool for this checkout. On macOS or Linux, configure UnityYAMLMerge using the [Smart Merge guide](https://docs.unity3d.com/6000.3/Documentation/Manual/SmartMerge.html). Coordinate edits to the same scene or prefab; Unity scene files can still conflict when both people change the same objects.

Do not commit Unity's generated `Library/`, `Temp/`, `Logs/`, `obj/`, `UserSettings/`, or local build output. Git ignores these folders.
