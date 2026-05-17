# LC2 More Choices - 最终设计

- 日期：2026-05-17
- 适用游戏：Lost Castle 2 (Unity 6000.3.12f1, IL2CPP, metadata v39)
- Mod 框架：BepInEx 6.0.0-be.755 (IL2CPP)
- 实现版本：[`63a28e6`](https://github.com/wtksana/lc2mod/commit/63a28e6) `refactor: 改成刷新加硬币 + 删除可配置项`

## 与初版 spec 的差异

最初 [2026-05-16-more-choices-mvp-design.md](2026-05-16-more-choices-mvp-design.md) 的设想：

- 让候选数量从 2/3 提升到 **10**
- 通过修改 `CommonItem._selectItemNum` 实现
- UI 端通过克隆 `_unitUIList[0]` 把槽位扩到 10

实际跑通过程中暴露的问题：

1. **`CommonItem._selectItemNum` 不是真正决定候选数的字段**：真正控制生成的是
   `CommonItemDataAsset` (ScriptableObject) 上挂的 `CommonItemData._selectItemNum`。
   运行时改 `CommonItem` 实例字段无效。
2. **UI 槽位扩容到 10 在 IL2CPP 下不可靠**：克隆 `ForgeAltarChooseUnitUI` 后，
   嵌套的 `LayoutContent_Top/Bottom` 等 LayoutGroup 在第一次显示时没有正确 rebuild，
   导致候选物描述文字两层叠加渲染（必须"返回再开"才会正常）。
   尝试过 `LayoutRebuilder.ForceRebuildLayoutImmediate` / `Canvas.ForceUpdateCanvases` /
   显式调用 `RefreshItemView` 第二次 / 用 `UpdateMenuInput` 标记延迟刷新，
   全部无效。详见 [2026-05-16-more-choices-mvp-findings.md](2026-05-16-more-choices-mvp-findings.md)
   及 git 历史中 `cfaba60` 之前的多次失败 commit。

故最终方案放弃"扩 UI 槽位"，改用游戏原生 4 槽位 + 把候选数从 2/3 提到 **4**。

## 最终目标

1. 拾起混沌奖励（混沌的武器/防具/宝藏/首领宝藏）时显示 **4 个唯一候选**。
2. 玩家点击"刷新候选"按钮时**反向给硬币**：每点一次，"虚灵的硬币" +1。

明确**不在范围内**：

- 联机 / RPC 同步
- 可配置项（mod 装上即生效，无开关）
- 大于 4 的候选数
- 鼠标滚轮浏览（之前尝试过，IL2CPP 下 `Input.mouseScrollDelta` 在 patch 中读不到）

## 关键发现

### 候选物数量来自 ScriptableObject

游戏从 `CommonItemDataAsset.ItemData` (类型为 `CommonItemData`) 上的 `_selectItemNum`
字段读取候选数。每种混沌类型有独立的 asset：

- `EntityData_PassiveProps_CommonWeapon` (混沌武器)
- `EntityData_PassiveProps_CommonArmor` (混沌防具)
- `EntityData_PassiveProps_CommonPassiveProps` (混沌宝藏)
- `EntityData_PassiveProps_CommonPassiveProps_Epic` (混沌宝藏-史诗)
- `EntityData_PassiveProps_CommonPassiveProps_Legend` (混沌宝藏-传说)

`InitAllItem_Server` 内部循环按这个值生成候选，所以**只要在 InitAllItem 调用前**
把这些 asset 的 `_selectItemNum` 改大即可。

### 刷新货币的真名是 "Refresh_WeaponArmor / Refresh_PassiveProp"

UI 上叫"虚灵的硬币"，实际背后是两种 `ItemType` (50, 51)，统一管理在
`BagSystem` 上：

- `BagSystem.Refresh_WeaponArmor` (int) — 刷新武器/防具用
- `BagSystem.Refresh_PassiveProps` (int) — 刷新宝藏/被动用

读这两个字段最简单的办法是用 `BagSystem.ChangeValueItem(ItemType, addValue)`
统一加减。游戏内"扣 1 硬币"的本质就是
`bag.ChangeValueItem(ItemType.Refresh_WeaponArmor, -1)`。

## Patch 设计

3 个 Harmony prefix patch：

### 1. `CommonItem.InitAllItem_Server` (prefix)

服务端要生成候选池前，遍历所有 `CommonItemDataAsset`，把它们关联的
`CommonItemData._selectItemNum` 提到 4（如果当前小于 4）。

```csharp
var allAssets = UnityEngine.Resources.FindObjectsOfTypeAll<CommonItemDataAsset>();
foreach (var asset in allAssets)
{
    var data = asset.ItemData?.Cast<CommonItemData>();
    if (data != null && data._selectItemNum < 4)
        data._selectItemNum = 4;
}
```

幂等：只改"小于 4"的，不会反复加。同进程内只有第一次混沌奖励生成时有效，
之后的实例直接读已被修改的全局 ScriptableObject。

### 2. `BagSystem.ChangeValueItem` (prefix)

拦截所有对 `Refresh_WeaponArmor` / `Refresh_PassiveProp` 的扣减：

```csharp
private static void Prefix(ItemType itemType, ref int addValue)
{
    if (addValue >= 0) return;
    if (itemType == ItemType.Refresh_WeaponArmor || itemType == ItemType.Refresh_PassiveProp)
        addValue = -addValue;
}
```

效果：玩家点刷新游戏想扣 -1 → patch 翻成 +1 → 硬币越刷越多。
副作用：刷新硬币数量随刷新次数单调递增，可作为"已刷新次数"的天然计数器。

### 3. `ForgeAltarChooseUI.Show` (prefix)

兜底：销毁 `_unitUIList` 上残留的 `UnitUI_Cloned_*` GameObject。这是为了
保护从老版本 mod 升级过来的玩家——之前曾尝试把 UI 槽位扩到 10 个，
留下 6 个克隆 GameObject。新装的玩家这步是 no-op。

## 不需要的 patch

设计中曾考虑但最终删除：

- **`CommonItem.OnCheckRefreshItem` postfix `__result = true`**：原想强制让 UI 显示刷新按钮。
  但实测 patch #2 已经覆盖：玩家初始硬币不为 0 时 UI 自然显示按钮，每次刷新硬币只增不减，
  永远会显示按钮。`OnCheckRefreshItem` 强制 true 是冗余。
- **`ForgeAltarChooseUI.ShowChooseUI` 入口垫满 99 硬币**：曾用作"确保 UI 显示刷新按钮"。
  Patch #2 让硬币只增不减后这条也冗余。

唯一边界情况：玩家**真正初始就 0 硬币**（理论上不会发生，虚灵宝库本身会发放硬币）
时不会显示刷新按钮，patch #2 没有触发的机会。这种情况下游戏行为退化为"原生 2-3 选 1
但提到 4 选 1，不能刷新"，仍优于原版。

## 实施清单

| 文件 | 行数 | 内容 |
|------|------|------|
| `src/LC2Mod.MoreChoices/Plugin.cs` | ~36 | BasePlugin + Harmony.PatchAll |
| `src/LC2Mod.MoreChoices/Patches.cs` | ~91 | 3 个 patch 类 |
| `src/LC2Mod.MoreChoices/LC2Mod.MoreChoices.csproj` | ~75 | net6.0, BepInEx + interop 引用，自动部署到 plugins/ |

依赖的游戏 interop 程序集：

- `Assembly-CSharp` (publicized via `BepInEx.AssemblyPublicizer.MSBuild`)
- `Unity.Netcode.Runtime`
- `UnityEngine.CoreModule`
- `Sirenix.Serialization` (CommonItemDataAsset 继承链)
- `Il2Cppmscorlib`、`Il2CppSystem`
- BepInEx 内置: `BepInEx.Core`、`BepInEx.Unity.IL2CPP`、`0Harmony`、`Il2CppInterop.*`、`MonoMod.Utils`

## 验证

启动游戏后 `BepInEx/LogOutput.log` 期望能看到：

```text
[Info   :   BepInEx] Loading [LC2 More Choices 0.1.0]
[Info   :LC2 More Choices] LC2 More Choices 0.1.0 loading
[Info   :LC2 More Choices] Patched: LC2.CommonItem.InitAllItem_Server
[Info   :LC2 More Choices] Patched: LC2.BagSystem.ChangeValueItem
[Info   :LC2 More Choices] Patched: LC2.ForgeAltarChooseUI.Show
[Info   :LC2 More Choices] LC2 More Choices loaded, target select count = 4, patches applied = 3
```

第一次拾起混沌奖励时还会输出：

```text
[Info   :LC2 More Choices]   CommonItemDataAsset 'EntityData_PassiveProps_CommonPassiveProps' _selectItemNum: 2 -> 4
[Info   :LC2 More Choices]   CommonItemDataAsset 'EntityData_PassiveProps_CommonWeapon' _selectItemNum: 2 -> 4
[Info   :LC2 More Choices]   CommonItemDataAsset 'EntityData_PassiveProps_CommonPassiveProps_Epic' _selectItemNum: 2 -> 4
[Info   :LC2 More Choices]   CommonItemDataAsset 'EntityData_PassiveProps_CommonArmor' _selectItemNum: 2 -> 4
[Info   :LC2 More Choices]   CommonItemDataAsset 'EntityData_PassiveProps_CommonPassiveProps_Legend' _selectItemNum: 3 -> 4
[Info   :LC2 More Choices] [InitAllItem_Server] patched 5 CommonItemDataAsset
```

游戏内的人工验证：

1. 进入虚灵宝库或场景里的混沌奖励 → 选择 UI 显示 4 个唯一候选物。
2. 点"刷新武器"或"刷新宝藏"按钮 → 候选物换新一批，"虚灵的硬币"数 +1。
3. 多次刷新 → 硬币持续增加（可以理解为刷新次数计数器）。

## 后续可能的扩展

- 把 `4` 抽成常量配置（如果想让用户能选 5/6/7）。但 UI prefab 槽位上限 4 已经是
  IL2CPP 下能稳定工作的上限。
- 联机兼容（需在 server 端单独装 mod，并同步 RPC）。
- 不限于混沌奖励 —— 游戏其他选择 UI（角色技能升级、词缀替换等）也走 ForgeAltarChooseUI
  类似路径，可以扩展。
