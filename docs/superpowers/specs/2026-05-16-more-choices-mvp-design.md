# 失落城堡 2 - 混沌宝藏多选扩容 MVP 设计

- 日期：2026-05-16
- 适用游戏：Lost Castle 2 (Unity 6000.3.12f1, IL2CPP)
- Mod 框架：BepInEx 6.0.0-be.755 (IL2CPP)
- 目标用户：单机玩家（不考虑联机/RPC 同步）

## 背景

游戏中拾取"混沌的武器 / 防具 / 宝藏 / 首领宝藏"时，会弹出 2 选 1 或 3 选 1 面板。需求是改成 10 选 1。

通过反编译 `Assembly-CSharp.dll` 已定位到核心实现：

- `LC2.CommonItem`：地上的"奖励物体"网络实体，承载候选物列表
  - `List<Item> _itemList`：候选物池
  - `int _selectItemNum`：实际显示给玩家选的数量（即"几选 1"的"几"）
  - `void InitAllItem_Server(ulong, int, List<ItemData>)`：服务端初始化
  - `void ShowItemChooseUI(Player, bool)`：弹出 UI 入口
- `LC2.ForgeAltarChooseUI`：选择 UI
  - `List<ForgeAltarChooseUnitUI> _unitUIList`：UI 槽位（动态 List，非定长数组）
  - `void ShowChooseUI(Player, ItemType, List<Item>)`
  - `void RefreshItemView(ItemType, List<Item>)`

`_itemList` 与 `_selectItemNum` 的真实关系（池容量 vs 显示数量）目前从 interop 看不到方法体，必须靠运行时日志确认。

## 目标

1. 改造 mod 项目骨架，建立"编译 → 自动部署到 `BepInEx/plugins/` → 验证加载"的可重复闭环。
2. 在最小代价下，把混沌奖励的"几选 1"从 2/3 改成 10。
3. 用日志摸清 `_itemList.Count` / `_selectItemNum` / `_unitUIList.Count` 的真实关系，作为 MVP 后完整方案的判断依据。

明确**不在 MVP 范围**：

- 联机 / RPC 同步
- UI 布局调整（网格化、缩放、滚动）
- 候选物池洗牌、稀有度调整
- 改 `LC2.PerkChooseUI`（那是词缀选择，不是混沌宝藏）

## 项目结构

```
src/
└── LC2Mod.MoreChoices/
    ├── LC2Mod.MoreChoices.csproj   # net6.0
    └── Plugin.cs                    # BasePlugin + Harmony patches
```

`csproj` 关键设置：

- `TargetFramework = net6.0`
- 引用 `BepInEx/core/` 下的 BepInEx + Il2CppInterop + 0Harmony
- 引用 `BepInEx/interop/Assembly-CSharp.dll`（用于编译期类型检查）
- `Publicize` 处理（如需访问私有字段）：用 `BepInEx.AssemblyPublicizer.MSBuild` 自动 publicize 引用程序集
- 输出后 `Copy` 到 `Lost Castle 2/BepInEx/plugins/`

## Patch 设计

所有 patch 通过 Harmony 注入。Mod 唯一插件类 `LC2Mod.MoreChoices.Plugin`：

```csharp
[BepInPlugin("com.ttat.lc2.morechoices", "LC2 More Choices", "0.1.0")]
public class Plugin : BasePlugin
{
    public override void Load() {
        Log.LogInfo("LC2 More Choices loaded");
        ClassInjector... // 如需
        Harmony.CreateAndPatchAll(typeof(Patches));
    }
}
```

### Patch 1: `CommonItem.InitAllItem_Server` (postfix)

**作用**：服务端初始化完候选物后，强制把 `_selectItemNum` 提到目标值；同时打印观测日志。

```
postfix(CommonItem __instance):
    int before = __instance._selectItemNum
    int poolSize = __instance._itemList?.Count ?? 0
    int target = Math.Min(config.TargetSelectCount, poolSize)
    __instance._selectItemNum = target
    Log: "[InitAllItem_Server] poolSize={poolSize} selectNum: {before} -> {target}"
```

注意：如果 `poolSize < TargetSelectCount`（很可能在 MVP 阶段就出现），日志会立刻暴露这个事实。

### Patch 2: `CommonItem.ShowItemChooseUI` (prefix, 仅日志)

**作用**：客户端展示 UI 之前再观测一次，确认数据是否被服务端的修改正确传到客户端。

```
prefix(CommonItem __instance, Player player, bool refresh):
    Log: "[ShowItemChooseUI] poolSize={_itemList.Count} selectNum={_selectItemNum} refresh={refresh}"
```

### Patch 3: `ForgeAltarChooseUI.RefreshItemView` (postfix, 仅日志)

**作用**：UI 真正拿到候选列表后，看实际生成了几个槽位。

```
postfix(ForgeAltarChooseUI __instance, ItemType itemType, List<Item> itemList):
    Log: "[RefreshItemView] inputCount={itemList.Count} unitUICount={_unitUIList.Count}"
```

## 配置

文件：`Lost Castle 2/BepInEx/config/com.ttat.lc2.morechoices.cfg`

```ini
[General]
TargetSelectCount = 10   # 期望的候选数量上限
```

MVP 阶段不实现热重载，启动时读取一次。

## 验证流程

1. `dotnet build` 项目，DLL 自动出现在 `BepInEx/plugins/`。
2. 启动 `LostCastle2.exe`，关掉游戏后查 `BepInEx/LogOutput.log`：
   - 看到 `LC2 More Choices loaded` → mod 加载成功
3. 进入游戏，触发混沌奖励（武器/防具/宝藏/首领宝藏任一）。
4. 在 UI 弹出前后，日志应有 3 组观测点：
   - `[InitAllItem_Server] poolSize=? selectNum: ? -> ?`
   - `[ShowItemChooseUI] poolSize=? selectNum=? refresh=?`
   - `[RefreshItemView] inputCount=? unitUICount=?`
5. 观察实际 UI 是否显示了多于 3 个候选格子（即便溢出/重叠也算）。

## MVP 输出 / 决策点

跑完后基于日志回答：

- Q1：`_itemList` 是否本身就 ≥ 10？
  - 是 → 只需改 `_selectItemNum` 就够，下一步聚焦 UI 布局
  - 否 → 需要在生成阶段扩容池子（patch `InitAllItem_Server` 内部或上游生成方法）
- Q2：服务端改 `_selectItemNum` 是否传到客户端？
- Q3：UI 槽位是按 list 长度动态生成，还是有 prefab 写死的上限？

这三个问题的答案决定下一份 spec 的范围：UI 布局工作 vs 候选池扩容工作 vs 两者都要做。

## 风险与备注

- `interop/` DLL 是 Il2CppInterop 生成的代理程序集，方法体为空。Harmony patch 可以正常打到 IL2CPP 实方法上（BepInEx 6 内置 Il2CppInterop.HarmonySupport），但**不要**期待能从 interop dll 反编译出实际逻辑。
- `_selectItemNum` 是否被序列化到客户端：如果它是 NetworkBehaviour 上的 `NetworkVariable`，服务端改了能同步；如果只是普通字段，纯单机下也无所谓（服务端 = 客户端是同一进程）。**MVP 单机场景下两种情况都能跑**。
- `BasePlugin` 是 BepInEx 6 IL2CPP 版的基类（不是 BepInEx 5 的 `BaseUnityPlugin`），写错会无法加载。
