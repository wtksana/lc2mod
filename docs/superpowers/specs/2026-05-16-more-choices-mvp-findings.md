# 混沌宝藏多选 MVP 复盘

- 日期：2026-05-16
- 测试场景：拾起一次混沌奖励（具体类型已忘记，未细分；后续测试需要在不同混沌类型下分别取样）

## 观测数据

| 来源 | poolSize / inputCount | selectNum / unitUICount | 备注 |
|------|----------------------|--------------------------|------|
| `CommonItem.InitAllItem_Server` (postfix) | 0 | 2 | postfix 时机偏早，候选物未填进 `_itemList` |
| `CommonItem.ShowItemChooseUI` (prefix)    | **未触发** | — | 真实路径不经过此方法 |
| `ForgeAltarChooseUI.ShowChooseUI` (prefix) | inputCount=2 | — | UI 入口直接收到长度为 2 的列表 |
| `ForgeAltarChooseUI.RefreshItemView` (postfix) | inputCount=2 | unitUICount=4 | UI 槽位 prefab 预留了 4 个 |

## UI 实际显示

屏幕上实际渲染的候选格子数：2（plan 写的样本即为此次）

## 关键发现

1. **真实路径不是 `CommonItem.ShowItemChooseUI`**：拾起混沌奖励 → 直接到 `ForgeAltarChooseUI.ShowChooseUI(player, itemType, itemList)`。
   `CommonItem` 只是网络实体；**它的 `_selectItemNum` / `ShowItemChooseUI` 跟混沌奖励路径无关**（可能只用于其他场景，如祭坛刷新）。

2. **候选数量受 `itemList` 控制**：`ShowChooseUI` 收到的列表长度就是"几选 1"的"几"。要扩 10 选 1，必须**让传进来的 itemList 长度为 10**，或者**在 prefix 里把 itemList 扩展到 10**。

3. **UI 槽位 prefab 预留了 4 个**：`_unitUIList.Count=4` 而 inputCount=2，说明 prefab 自带 4 个槽位 GameObject，UI 只会按 itemList 长度取前 N 个显示。**直接把 itemList 给到 10，超过 4 的部分会显示不全 / 越界**，需要在 UI 上动态克隆扩容。

4. **改动 `CommonItem._selectItemNum=10` 引发卡顿**：因为 `_itemList` 实际未填充就被强制设为 10，下游可能在循环里抽不出物品。这条路径已确认是死路，不应继续走。

## Spec 里 3 个问题的答案

- **Q1 `_itemList.Count` 是否 ≥ 10**：不适用 —— 真实路径不在 `CommonItem`，候选物在 `ForgeAltarChooseUI` 入口已经只有 2 个；上游必有人在生成"2 件物品"。**下一步要找的是这个上游生成方**。
- **Q2 服务端改 `_selectItemNum` 是否传到客户端**：不适用 —— 路径不经过此字段。
- **Q3 UI 槽位与 inputCount 关系**：prefab 固定 4 个槽位，只显示 `itemList` 的前 N 个。**Mod 必须扩容 prefab 的 `_unitUIList` 才能显示 ≥ 5 个候选**。

## 下一步建议

下一份 spec 的工作量分两部分，互相独立但都要做：

**A. 找候选物生成方，让它生成 10 个**
- 候选 `ShowChooseUI` 调用方有：`CommonItem.ShowItemChooseUI`（这次没触发）、`ChestAltar`、`TreasureAltar`、`RewardChestGroup`、`ExchangeItemAltar` 等。
- 反编译 dump 里有 `Class LC2.Power.PFAM_ShowSpecialItemUI`、`Class LC2.Chest._OpenChestAsync_Server_d__34` 等，"混沌奖励"的实际类型还需要进一步验证（用 prefix 加 stack trace 能直接定位）。
- **建议下一步先在 `ShowChooseUI` 的 prefix 里 `Plugin.Logger.LogInfo(System.Environment.StackTrace)`**，一次就知道是谁调用的。

**B. UI 扩容**
- `ForgeAltarChooseUI._unitUIList` 是 List 而不是定长数组，但元素 GameObject 来自 prefab 预先实例化的 4 个。要做到 10 槽位，需要在 UI 显示前 `Instantiate` 现有槽位的 prefab 副本插进列表，并调整布局。
- `_unitUIList[0]` 的 GameObject 可作为模板。布局看上去走的是 LayoutGroup / GridLayoutGroup，需要进一步看 prefab 结构。

## 风险记录

- 之前误以为 `CommonItem._selectItemNum=10` 能生效 —— spec 阶段对路径的假设错了。**下一份 spec 应优先用 stack trace 把"谁调用谁"画清楚再动手**。
- 卡顿现象虽然在回滚后消失，但要记得：之后任何"先改字段后看效果"的尝试都需要先确认调用链完整再下手。

## 完成判定

按 plan 的判定表：

1. ✅ DLL 部署成功，BepInEx 加载（4 个 patch 全部 applied）
2. ✅ 触发一次混沌宝藏，4 个观测点中 3 个有数据（`ShowItemChooseUI` 未触发本身就是发现）
3. ❌ `[InitAllItem_Server]` 改 `_selectItemNum` 未生效（路径不经过此方法），但**这正是 MVP 想发现的负面结论**
4. ✅ 本文档填写完整，明确给出下一步两条工作线（A 找上游 / B UI 扩容）
