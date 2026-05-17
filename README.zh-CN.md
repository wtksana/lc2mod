# LC2 More Choices

[![BepInEx](https://img.shields.io/badge/BepInEx-6.0.0--be.755-blue)](https://builds.bepinex.dev/projects/bepinex_be) ![Game](https://img.shields.io/badge/Lost%20Castle%202-Unity%206000.3.12f1-green)

[English](README.md) | **中文**

失落城堡 2（Lost Castle 2）的一个 BepInEx mod：拾起混沌奖励时给更多候选；刷新不仅免费，还会反向给你硬币。

## 功能

- **混沌奖励 4 选 1**。拾起混沌的武器 / 防具 / 宝藏 / 首领宝藏时，选择界面显示 **4 个唯一候选物**，而不是原版的 2-3 个。
- **刷新反给硬币**。每次点击刷新会让"虚灵的硬币" **+1**，而不是 -1。等于无限刷新，硬币计数器顺便变成"已刷新次数"统计。
- **零配置**。装上即用，无设置文件、无开关。
- **原生 UI**。复用游戏自带的 4 槽位选择界面，不做布局改造，不加遮罩。

## 安装

1. 把 [BepInEx 6 IL2CPP (bleeding-edge)](https://builds.bepinex.dev/projects/bepinex_be) 装到你的失落城堡 2 安装目录。
2. 启动一次游戏，让 BepInEx 生成 `BepInEx/interop/` 目录。
3. 把 `LC2Mod.MoreChoices.dll`（从 [Releases](../../releases) 下载或自行编译）放进 `<游戏目录>/BepInEx/plugins/`。
4. 启动游戏。

确认加载成功 —— 检查 `BepInEx/LogOutput.log` 是否有：

```text
[Info   :   BepInEx] Loading [LC2 More Choices 0.1.0]
[Info   :LC2 More Choices] LC2 More Choices 0.1.0 loading
[Info   :LC2 More Choices] LC2 More Choices loaded, target select count = 4, patches applied = 3
```

## 从源码编译

需要 .NET SDK 6+，以及一份装好 BepInEx 6 并已生成 `interop/` 的游戏（启动一次游戏即可）。

```bash
git clone https://github.com/wtksana/lc2mod.git
cd lc2mod
dotnet build LC2Mod.sln -c Release
```

构建成功后 DLL 会自动复制到 `Lost Castle 2/BepInEx/plugins/`（项目假设游戏在 `./Lost Castle 2/` 这个相对路径，可以是符号链接）。如果你的目录结构不同，改 csproj 里的 `GameDir` 即可。

## 工作原理

三个针对游戏内部（IL2CPP，通过 Il2CppInterop）的 Harmony patch：

| 目标方法 | 效果 |
| --- | --- |
| `CommonItem.InitAllItem_Server`（prefix） | 在服务端填候选池前，把所有 `CommonItemDataAsset` ScriptableObject 上的 `CommonItemData._selectItemNum` 提到 4。 |
| `BagSystem.ChangeValueItem`（prefix） | 当传入 `ItemType.Refresh_WeaponArmor` 或 `Refresh_PassiveProp` 且 `addValue` 是负数时翻转符号，让"刷新成本"变成"刷新奖励"。 |
| `ForgeAltarChooseUI.Show`（prefix） | 兜底清理 `UnitUI_Cloned_*` 这种残留 GameObject（来自老 mod 版本，全新安装下是 no-op）。 |

完整设计与逆向过程见 [docs/superpowers/specs/2026-05-17-final-design.md](docs/superpowers/specs/2026-05-17-final-design.md)；同目录里的几份 MVP 文档记录了当时走过的弯路。

## 兼容性

- **游戏版本**：失落城堡 2，Unity `6000.3.12f1`，IL2CPP，metadata v39（撰写时的 Steam 版本）。游戏后续更新可能会改字段名 —— 如果发现 BepInEx 日志里 `Patched: ...` 那几行没出现，请提 issue。
- **联机**：仅按单机设计，没测过联机。Patch 同时影响 server 与 client 路径，所以联机时所有人都装 mod 才能保证一致。
- **其他 mod**：暂无已知冲突。Patch 仅触及混沌奖励链路。

## 已知限制

- `ChangeValueItem` patch 会反转**所有**对 `Refresh_WeaponArmor` / `Refresh_PassiveProp` 的扣减，不只是刷新按钮那一次。如果将来游戏更新或别的 mod 出于不同目的扣这两种货币，那些扣减也会被反转。
- 候选数 4 是写死的。游戏 UI prefab 只有 4 个槽位 GameObject —— 想做更多我们试过（见 commit 历史），IL2CPP 下嵌套 `LayoutGroup` 在克隆体上的 rebuild 不可靠。

## 鸣谢

- [BepInEx](https://github.com/BepInEx/BepInEx) — IL2CPP 插件框架
- [Cpp2IL](https://github.com/SamboyCoding/Cpp2IL) — 用于反编译 `GameAssembly.dll`
- [BepInEx.AssemblyPublicizer.MSBuild](https://github.com/BepInEx/BepInEx.AssemblyPublicizer) — 编译期把游戏私有成员暴露成 public
