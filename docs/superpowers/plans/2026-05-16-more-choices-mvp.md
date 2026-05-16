# 混沌宝藏多选扩容 MVP 实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 搭建 BepInEx 6 IL2CPP 插件骨架，通过 Harmony patch 把 `LC2.CommonItem._selectItemNum` 抬到 10，并打印关键观测日志，验证编译→部署→生效闭环。

**Architecture:** 单一 BepInEx 插件项目 (`LC2Mod.MoreChoices`)。`csproj` 通过 `BepInEx.AssemblyPublicizer.MSBuild` 自动 publicize `Assembly-CSharp.dll`，使 private 字段 `_itemList` / `_selectItemNum` 可见。`Plugin.cs` 在 `Load()` 里用 `HarmonyLib.Harmony` 给三个目标方法挂 prefix/postfix。本地构建产物自动复制到 `Lost Castle 2/BepInEx/plugins/`。

**Tech Stack:** .NET 6, BepInEx 6.0.0-be.755 (IL2CPP), Il2CppInterop, HarmonyX, BepInEx.AssemblyPublicizer.MSBuild。

参考 spec：[docs/superpowers/specs/2026-05-16-more-choices-mvp-design.md](../specs/2026-05-16-more-choices-mvp-design.md)

---

## 文件结构

要创建：

- `src/LC2Mod.MoreChoices/LC2Mod.MoreChoices.csproj` — 项目文件
- `src/LC2Mod.MoreChoices/Plugin.cs` — 插件主类（BasePlugin + Harmony 加载）
- `src/LC2Mod.MoreChoices/Patches.cs` — 三个 Harmony patch
- `LC2Mod.sln` — 顶层 solution（方便 IDE / dotnet build）

要修改：无（首次开发）

> **注：本项目不写单元测试。** Mod 的"测试"是启动游戏后观察日志与 UI 行为，没办法在常规测试框架里跑 IL2CPP runtime。每个 Task 末尾用"运行游戏 → 看日志"代替"跑测试"。

---

## Task 1: 创建项目骨架（solution + csproj）

**Files:**
- Create: `LC2Mod.sln`
- Create: `src/LC2Mod.MoreChoices/LC2Mod.MoreChoices.csproj`

- [ ] **Step 1: 创建 csproj**

写入 `src/LC2Mod.MoreChoices/LC2Mod.MoreChoices.csproj`：

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>disable</Nullable>
    <AssemblyName>LC2Mod.MoreChoices</AssemblyName>
    <RootNamespace>LC2Mod.MoreChoices</RootNamespace>
    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>

    <!-- 游戏根目录（含 LostCastle2.exe 的目录） -->
    <GameDir>$(MSBuildThisFileDirectory)..\..\Lost Castle 2</GameDir>
    <BepInExCoreDir>$(GameDir)\BepInEx\core</BepInExCoreDir>
    <InteropDir>$(GameDir)\BepInEx\interop</InteropDir>
    <PluginsDir>$(GameDir)\BepInEx\plugins</PluginsDir>
  </PropertyGroup>

  <ItemGroup>
    <!-- BepInEx 6 IL2CPP 运行时所需的引用，全部 Private=false 避免拷到 plugins/ -->
    <Reference Include="BepInEx.Core">
      <HintPath>$(BepInExCoreDir)\BepInEx.Core.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="BepInEx.Unity.IL2CPP">
      <HintPath>$(BepInExCoreDir)\BepInEx.Unity.IL2CPP.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="0Harmony">
      <HintPath>$(BepInExCoreDir)\0Harmony.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="Il2CppInterop.Runtime">
      <HintPath>$(BepInExCoreDir)\Il2CppInterop.Runtime.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="Il2CppInterop.Common">
      <HintPath>$(BepInExCoreDir)\Il2CppInterop.Common.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="MonoMod.Utils">
      <HintPath>$(BepInExCoreDir)\MonoMod.Utils.dll</HintPath>
      <Private>false</Private>
    </Reference>

    <!-- 游戏程序集；下面 Task 2 加 publicize 后会被改写 -->
    <Reference Include="Assembly-CSharp">
      <HintPath>$(InteropDir)\Assembly-CSharp.dll</HintPath>
      <Private>false</Private>
      <Publicize>true</Publicize>
    </Reference>
    <Reference Include="Assembly-CSharp-firstpass">
      <HintPath>$(InteropDir)\Assembly-CSharp-firstpass.dll</HintPath>
      <Private>false</Private>
    </Reference>

    <!-- Unity 模块（仅引用我们用到的） -->
    <Reference Include="UnityEngine.CoreModule">
      <HintPath>$(InteropDir)\UnityEngine.CoreModule.dll</HintPath>
      <Private>false</Private>
    </Reference>
  </ItemGroup>

  <!-- 构建后自动复制到 BepInEx/plugins/ -->
  <Target Name="DeployToPlugins" AfterTargets="Build">
    <MakeDir Directories="$(PluginsDir)" Condition="!Exists('$(PluginsDir)')" />
    <Copy SourceFiles="$(TargetPath)" DestinationFolder="$(PluginsDir)" />
    <Message Importance="high" Text="Deployed $(AssemblyName).dll -> $(PluginsDir)" />
  </Target>

</Project>
```

- [ ] **Step 2: 创建 solution 文件**

写入 `LC2Mod.sln`：

```text
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "LC2Mod.MoreChoices", "src\LC2Mod.MoreChoices\LC2Mod.MoreChoices.csproj", "{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}"
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
EndGlobal
```

- [ ] **Step 3: 验证 dotnet restore 能跑通**

Run: `dotnet restore LC2Mod.sln`
Expected: 不报错（项目还没源文件，只是验证引用路径都对）

- [ ] **Step 4: 提交**

```bash
git add LC2Mod.sln src/LC2Mod.MoreChoices/LC2Mod.MoreChoices.csproj
git commit -m "build: 添加 LC2Mod.MoreChoices 项目骨架与引用配置"
```

---

## Task 2: 加 Publicizer 让私有字段可访问

**Files:**
- Modify: `src/LC2Mod.MoreChoices/LC2Mod.MoreChoices.csproj`（添加 PackageReference）

> **背景：** `CommonItem._itemList` 和 `_selectItemNum` 在 interop dll 里是 `public unsafe`（IL2CPP interop 都生成成 public），但 `InitAllItem_Server` 和 `ShowItemChooseUI` 是 `private`。Harmony patch 用字符串方法名能命中 private，但**字段访问**写起来更顺。`BepInEx.AssemblyPublicizer.MSBuild` 会自动把引用的程序集所有成员改 public，无需手写反射。

- [ ] **Step 1: 在 csproj 里加 PackageReference**

修改 `src/LC2Mod.MoreChoices/LC2Mod.MoreChoices.csproj`，在已有的 `<ItemGroup>` 之前插入：

```xml
  <ItemGroup>
    <PackageReference Include="BepInEx.AssemblyPublicizer.MSBuild" Version="0.4.2" PrivateAssets="all" />
  </ItemGroup>
```

注：上面 Task 1 的 csproj 里 `<Reference Include="Assembly-CSharp">` 已带 `<Publicize>true</Publicize>` 子元素，无需额外改。

- [ ] **Step 2: 运行 restore 验证包能拉到**

Run: `dotnet restore LC2Mod.sln`
Expected: 成功，输出里能看到 `BepInEx.AssemblyPublicizer.MSBuild` 被还原

- [ ] **Step 3: 提交**

```bash
git add src/LC2Mod.MoreChoices/LC2Mod.MoreChoices.csproj
git commit -m "build: 启用 AssemblyPublicizer 让游戏私有成员可访问"
```

---

## Task 3: 写最小 Plugin.cs，先验证 mod 能被 BepInEx 加载

**Files:**
- Create: `src/LC2Mod.MoreChoices/Plugin.cs`

- [ ] **Step 1: 写 Plugin.cs**

写入 `src/LC2Mod.MoreChoices/Plugin.cs`：

```csharp
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using BepInEx.Logging;
using HarmonyLib;

namespace LC2Mod.MoreChoices;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public class Plugin : BasePlugin
{
    public const string PluginGuid = "com.ttat.lc2.morechoices";
    public const string PluginName = "LC2 More Choices";
    public const string PluginVersion = "0.1.0";

    internal static ManualLogSource Logger;
    internal static ConfigEntry<int> CfgTargetSelectCount;

    public override void Load()
    {
        Logger = Log;
        Logger.LogInfo($"{PluginName} {PluginVersion} loading");

        CfgTargetSelectCount = Config.Bind(
            "General",
            "TargetSelectCount",
            10,
            "拾起混沌奖励时希望显示的候选数量上限。"
        );

        var harmony = new Harmony(PluginGuid);
        harmony.PatchAll(typeof(Patches));

        Logger.LogInfo($"{PluginName} loaded, target select count = {CfgTargetSelectCount.Value}");
    }
}
```

- [ ] **Step 2: 写一个空的 Patches.cs 让上面 `PatchAll(typeof(Patches))` 能编译**

写入 `src/LC2Mod.MoreChoices/Patches.cs`：

```csharp
namespace LC2Mod.MoreChoices;

internal static class Patches
{
    // 下个 Task 填入真正的 Harmony patch
}
```

- [ ] **Step 3: 编译**

Run: `dotnet build LC2Mod.sln -c Debug`
Expected: 构建成功，最后一行有 `Deployed LC2Mod.MoreChoices.dll -> ...\plugins`

- [ ] **Step 4: 启动游戏验证加载**

让用户执行：双击 `Lost Castle 2/LostCastle2.exe`，进入主菜单后退出游戏。

Run: `cat "Lost Castle 2/BepInEx/LogOutput.log"`
Expected: 日志末尾应有：
```
[Info   :LC2 More Choices] LC2 More Choices 0.1.0 loading
[Info   :LC2 More Choices] LC2 More Choices loaded, target select count = 10
```

如果**没看到这两行**，说明插件加载失败 —— 停下来排查（常见原因：TFM 不对、引用 BepInEx 5 API、csproj 输出路径错），不要继续后续 Task。

- [ ] **Step 5: 提交**

```bash
git add src/LC2Mod.MoreChoices/Plugin.cs src/LC2Mod.MoreChoices/Patches.cs
git commit -m "feat: 添加 BasePlugin 骨架与配置项 TargetSelectCount"
```

---

## Task 4: Patch CommonItem.InitAllItem_Server — 提升 `_selectItemNum`

**Files:**
- Modify: `src/LC2Mod.MoreChoices/Patches.cs`

- [ ] **Step 1: 写 patch 类**

完整替换 `src/LC2Mod.MoreChoices/Patches.cs` 为：

```csharp
using System;
using HarmonyLib;
using LC2;

namespace LC2Mod.MoreChoices;

internal static class Patches
{
    [HarmonyPatch(typeof(CommonItem), nameof(CommonItem.InitAllItem_Server))]
    internal static class CommonItem_InitAllItem_Server_Patch
    {
        [HarmonyPostfix]
        private static void Postfix(CommonItem __instance)
        {
            int target = Plugin.CfgTargetSelectCount.Value;
            int poolSize = __instance._itemList != null ? __instance._itemList.Count : 0;
            int before = __instance._selectItemNum;
            int after = Math.Min(target, poolSize);
            __instance._selectItemNum = after;

            Plugin.Logger.LogInfo(
                $"[InitAllItem_Server] poolSize={poolSize} selectNum: {before} -> {after} (target={target})"
            );
        }
    }
}
```

- [ ] **Step 2: 编译**

Run: `dotnet build LC2Mod.sln -c Debug`
Expected: 构建成功，DLL 自动部署到 plugins。如果报"找不到 `LC2` 命名空间"，到 interop dll 里确认 `CommonItem` 实际命名空间（按 ilspy 输出是 `LC2`）。

- [ ] **Step 3: 进游戏触发一次混沌宝藏**

让用户：启动游戏 → 进入一个有混沌宝藏（混沌的武器/防具/宝藏/首领宝藏）的关卡 → 拾起一个 → 看见选择 UI 后退出游戏。

Run: `grep "InitAllItem_Server" "Lost Castle 2/BepInEx/LogOutput.log"`
Expected: 至少一行：
```
[Info   :LC2 More Choices] [InitAllItem_Server] poolSize=? selectNum: ? -> ? (target=10)
```

记下实际的 `poolSize` 和 `selectNum` 数值，**这是本次 MVP 的核心数据点**。

- [ ] **Step 4: 提交**

```bash
git add src/LC2Mod.MoreChoices/Patches.cs
git commit -m "feat: patch InitAllItem_Server 抬升 _selectItemNum"
```

---

## Task 5: Patch CommonItem.ShowItemChooseUI — 客户端观测日志

**Files:**
- Modify: `src/LC2Mod.MoreChoices/Patches.cs`

- [ ] **Step 1: 在 Patches 类内追加 patch**

修改 `src/LC2Mod.MoreChoices/Patches.cs`，在 `CommonItem_InitAllItem_Server_Patch` 类的下方、外层 `Patches` 类的 `}` 之前插入：

```csharp
    [HarmonyPatch(typeof(CommonItem), nameof(CommonItem.ShowItemChooseUI))]
    internal static class CommonItem_ShowItemChooseUI_Patch
    {
        [HarmonyPrefix]
        private static void Prefix(CommonItem __instance, bool refresh)
        {
            int poolSize = __instance._itemList != null ? __instance._itemList.Count : 0;
            Plugin.Logger.LogInfo(
                $"[ShowItemChooseUI] poolSize={poolSize} selectNum={__instance._selectItemNum} refresh={refresh}"
            );
        }
    }
```

- [ ] **Step 2: 编译**

Run: `dotnet build LC2Mod.sln -c Debug`
Expected: 构建成功

- [ ] **Step 3: 再触发一次混沌宝藏**

让用户：启动游戏 → 触发一次混沌宝藏 → 退出。

Run: `grep -E "ShowItemChooseUI|InitAllItem_Server" "Lost Castle 2/BepInEx/LogOutput.log" | tail -10`
Expected: 同一次拾取至少应能看到一行 `[InitAllItem_Server]` 和一行 `[ShowItemChooseUI]`，且两行的 `selectNum` 一致。如果不一致，说明 `_selectItemNum` 是 NetworkVariable 且回滚了，需要在 spec 复盘中记录。

- [ ] **Step 4: 提交**

```bash
git add src/LC2Mod.MoreChoices/Patches.cs
git commit -m "feat: patch ShowItemChooseUI 添加客户端观测日志"
```

---

## Task 6: Patch ForgeAltarChooseUI.RefreshItemView — UI 端观测

**Files:**
- Modify: `src/LC2Mod.MoreChoices/Patches.cs`

- [ ] **Step 1: 追加 patch**

在 `Patches` 类的尾部追加：

```csharp
    [HarmonyPatch(typeof(ForgeAltarChooseUI), nameof(ForgeAltarChooseUI.RefreshItemView))]
    internal static class ForgeAltarChooseUI_RefreshItemView_Patch
    {
        [HarmonyPostfix]
        private static void Postfix(
            ForgeAltarChooseUI __instance,
            Il2CppSystem.Collections.Generic.List<Item> itemList)
        {
            int inputCount = itemList != null ? itemList.Count : 0;
            int unitUICount = __instance._unitUIList != null ? __instance._unitUIList.Count : 0;
            Plugin.Logger.LogInfo(
                $"[RefreshItemView] inputCount={inputCount} unitUICount={unitUICount}"
            );
        }
    }
```

注：IL2CPP 里集合是 `Il2CppSystem.Collections.Generic.List<T>`，不是 .NET 的 `System.Collections.Generic.List<T>`。Harmony 匹配方法参数类型必须严格一致，否则 patch 无声失败。

- [ ] **Step 2: 编译**

Run: `dotnet build LC2Mod.sln -c Debug`
Expected: 构建成功。如果报 `Item` 类型有歧义，加 `using LC2;` 应该已经覆盖；如果还报错，写完整命名空间 `LC2.Item`。

- [ ] **Step 3: 触发并观察**

让用户：启动游戏 → 触发一次混沌宝藏 → 在 UI 弹出后**截一张图**或记下"屏幕上实际有几个候选格子"→ 退出。

Run: `grep -E "RefreshItemView|ShowItemChooseUI|InitAllItem_Server" "Lost Castle 2/BepInEx/LogOutput.log" | tail -15`
Expected: 一次完整流程能看到三行：
```
[InitAllItem_Server] poolSize=X selectNum: Y -> Z (target=10)
[ShowItemChooseUI] poolSize=X selectNum=Z refresh=False
[RefreshItemView] inputCount=W unitUICount=V
```

- [ ] **Step 4: 提交**

```bash
git add src/LC2Mod.MoreChoices/Patches.cs
git commit -m "feat: patch RefreshItemView 添加 UI 端候选数量日志"
```

---

## Task 7: 编写 MVP 复盘记录

**Files:**
- Create: `docs/superpowers/specs/2026-05-16-more-choices-mvp-findings.md`

- [ ] **Step 1: 把日志观测结果填进复盘文档**

把 Task 4/5/6 收集到的真实数值整理进 `docs/superpowers/specs/2026-05-16-more-choices-mvp-findings.md`，模板：

```markdown
# 混沌宝藏多选 MVP 复盘

- 日期：YYYY-MM-DD
- 测试场景：拾起一次"混沌的XX"（请填具体类型）

## 观测数据

| 来源 | poolSize / inputCount | selectNum / unitUICount | 备注 |
|------|----------------------|--------------------------|------|
| InitAllItem_Server (postfix) | ? | before=? after=? | 服务端 |
| ShowItemChooseUI (prefix)    | ? | ? | 客户端 |
| RefreshItemView (postfix)    | ? | ? | UI |

## UI 实际显示

屏幕上实际渲染的候选格子数：?

是否有溢出/重叠/裁剪：?

## Spec 里 3 个问题的答案

- Q1: `_itemList.Count` 实际是 X，**是否 ≥ 10**：是/否
- Q2: 服务端改 `_selectItemNum` 是否传到客户端：是/否
- Q3: UI `_unitUIList.Count` 与 `inputCount` 关系：相等/被截断/...

## 下一步建议

基于上述事实，下一份 spec 应当聚焦：
- [ ] 选项 A：扩容 `_itemList` 池子（如果 Q1=否）
- [ ] 选项 B：UI 网格自动换行布局（如果 Q3 显示 UI 真能生成 10 个槽位但溢出）
- [ ] 选项 C：...
```

- [ ] **Step 2: 提交**

```bash
git add docs/superpowers/specs/2026-05-16-more-choices-mvp-findings.md
git commit -m "docs: 添加 MVP 观测数据复盘"
```

至此 MVP 完成，可基于 findings 进入下一阶段的 brainstorming/writing-plans 循环。

---

## 完成判定

满足以下全部条件即 MVP 完成：

1. ✅ `Lost Castle 2/BepInEx/plugins/LC2Mod.MoreChoices.dll` 存在且能被 BepInEx 加载（日志有 `loading` 行）
2. ✅ 触发一次混沌宝藏，日志同时出现 `[InitAllItem_Server]`、`[ShowItemChooseUI]`、`[RefreshItemView]` 三行
3. ✅ `[InitAllItem_Server]` 行显示 `selectNum: X -> Y` 且 `Y > X`（说明 patch 真生效）
4. ✅ findings 文档填写完整，明确给出下一步建议
