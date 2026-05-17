# LC2 More Choices

[![BepInEx](https://img.shields.io/badge/BepInEx-6.0.0--be.755-blue)](https://builds.bepinex.dev/projects/bepinex_be) ![Game](https://img.shields.io/badge/Lost%20Castle%202-Unity%206000.3.12f1-green)

A small BepInEx mod for **Lost Castle 2 (失落城堡 2)** that improves the chaos reward chest experience: more candidates per pick, and a refresh mechanic that pays you instead of charging you.

> 失落城堡 2 的 BepInEx mod：拾起混沌奖励时给更多候选；刷新不仅免费，还会反向给你硬币。

## Features

- **4-choice chaos rewards.** When picking up a chaos weapon / armor / treasure / boss treasure, the choose UI shows **4 unique items** instead of the vanilla 2-3.
- **Reverse-cost refresh.** Each refresh click *adds* 1 spirit coin (虚灵的硬币) instead of consuming one. Effectively unlimited refreshes, with the coin counter doubling as a refresh-count tracker.
- **Zero configuration.** Drop in and play. No settings file, no toggles.
- **Native UI.** Uses the game's original 4-slot picker. No overlays, no layout hacks.

## Installation

1. Install [BepInEx 6 IL2CPP (bleeding-edge)](https://builds.bepinex.dev/projects/bepinex_be) into your Lost Castle 2 directory.
2. Launch the game once so BepInEx generates `BepInEx/interop/`.
3. Drop `LC2Mod.MoreChoices.dll` (from [Releases](../../releases) or built locally) into `<game>/BepInEx/plugins/`.
4. Launch the game.

Verify the load by checking `BepInEx/LogOutput.log` for:

```text
[Info   :   BepInEx] Loading [LC2 More Choices 0.1.0]
[Info   :LC2 More Choices] LC2 More Choices 0.1.0 loading
[Info   :LC2 More Choices] LC2 More Choices loaded, target select count = 4, patches applied = 3
```

## Building from Source

Requires .NET SDK 6+ and a working BepInEx 6 setup with `interop/` already generated (launch the game once).

```bash
git clone https://github.com/wtksana/lc2mod.git
cd lc2mod
dotnet build LC2Mod.sln -c Release
```

The output DLL is automatically copied to `Lost Castle 2/BepInEx/plugins/` after build (the project assumes the game lives at the symlink/path `./Lost Castle 2/`). Adjust `GameDir` in the csproj if your layout differs.

## How it Works

Three Harmony patches against game internals (IL2CPP via Il2CppInterop):

| Patch target | Effect |
| --- | --- |
| `CommonItem.InitAllItem_Server` (prefix) | Bumps `CommonItemData._selectItemNum` on all `CommonItemDataAsset` ScriptableObjects to 4 before the server fills the candidate pool. |
| `BagSystem.ChangeValueItem` (prefix) | When a negative `addValue` is passed for `ItemType.Refresh_WeaponArmor` or `Refresh_PassiveProp`, flips the sign so refresh cost becomes refresh reward. |
| `ForgeAltarChooseUI.Show` (prefix) | Cleans up `UnitUI_Cloned_*` stale GameObjects left over from earlier mod versions (defensive, no-op for fresh installs). |

See [docs/superpowers/specs/](docs/superpowers/specs/) for the design notes and reverse-engineering findings that led to this implementation.

## Compatibility

- **Game version:** Lost Castle 2 on Unity `6000.3.12f1`, IL2CPP, metadata v39 (current Steam build at the time of writing). Future game updates may shift internal field names — open an issue if you see `Patched: ...` lines fail to appear in the BepInEx log.
- **Multiplayer:** Designed for single-player. Multiplayer behavior was never tested. Patches affect both server and client logic, so all parties would need the mod for consistent results.
- **Other mods:** No known conflicts. The patches touch only the chaos-reward path.

## Limitations

- The `ChangeValueItem` patch flips sign for *any* deduction of `Refresh_WeaponArmor` / `Refresh_PassiveProp`, not just refresh button clicks. If a future update or another mod consumes these item types for an unrelated purpose, that consumption will also be reversed.
- 4 is hardcoded. The game's UI prefab has exactly 4 slot GameObjects; trying to push past that (we tried — see commit history) ran into nested `LayoutGroup` rebuild issues on cloned cells under IL2CPP.

## Acknowledgments

- [BepInEx](https://github.com/BepInEx/BepInEx) — IL2CPP plugin framework
- [Cpp2IL](https://github.com/SamboyCoding/Cpp2IL) — for reverse-engineering `GameAssembly.dll`
- [BepInEx.AssemblyPublicizer.MSBuild](https://github.com/BepInEx/BepInEx.AssemblyPublicizer) — exposes private game members at build time
