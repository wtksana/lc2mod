using HarmonyLib;
using LC2;
using UnityEngine;
using UnityEngine.UI;

namespace LC2Mod.MoreChoices;

internal static class Patches
{
    [HarmonyPatch(typeof(CommonItem), nameof(CommonItem.InitAllItem_Server))]
    internal static class CommonItem_InitAllItem_Server_Patch
    {
        [HarmonyPostfix]
        private static void Postfix(CommonItem __instance)
        {
            int poolSize = __instance._itemList != null ? __instance._itemList.Count : 0;
            Plugin.Logger.LogInfo(
                $"[InitAllItem_Server] poolSize={poolSize} selectNum={__instance._selectItemNum}"
            );
        }
    }

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

    [HarmonyPatch(typeof(RewardPedestalGroup), nameof(RewardPedestalGroup.Init))]
    internal static class RewardPedestalGroup_Init_Patch
    {
        [HarmonyPrefix]
        private static void Prefix(RewardPedestalGroup __instance)
        {
            int target = Plugin.CfgTargetSelectCount.Value;
            int before = __instance._rewardCount;
            __instance._rewardCount = target;
            Plugin.Logger.LogInfo(
                $"[RewardPedestalGroup.Init] _rewardCount: {before} -> {target}"
            );
        }
    }

    [HarmonyPatch(typeof(RewardPedestalGroup), nameof(RewardPedestalGroup.GeneratePedestalAndInitItemData_Server))]
    internal static class RewardPedestalGroup_Generate_Patch
    {
        [HarmonyPrefix]
        private static void Prefix(RewardPedestalGroup __instance)
        {
            int target = Plugin.CfgTargetSelectCount.Value;
            int before = __instance._rewardCount;
            __instance._rewardCount = target;
            Plugin.Logger.LogInfo(
                $"[RewardPedestalGroup.GeneratePedestalAndInitItemData_Server] _rewardCount: {before} -> {target}, _pedestalList.Count={__instance._pedestalList?.Count ?? -1}"
            );
        }
    }

    [HarmonyPatch(typeof(RewardPedestalGroup), nameof(RewardPedestalGroup.CheckGeneratePedestalItem))]
    internal static class RewardPedestalGroup_CheckGen_Patch
    {
        [HarmonyPrefix]
        private static void Prefix(RewardPedestalGroup __instance)
        {
            int target = Plugin.CfgTargetSelectCount.Value;
            int before = __instance._rewardCount;
            __instance._rewardCount = target;
            Plugin.Logger.LogInfo(
                $"[RewardPedestalGroup.CheckGeneratePedestalItem] _rewardCount: {before} -> {target}, _pedestalList.Count={__instance._pedestalList?.Count ?? -1}"
            );
        }
    }

    [HarmonyPatch(typeof(RewardPedestalGroup), nameof(RewardPedestalGroup.ShowForgeAltarChooseUI))]
    internal static class RewardPedestalGroup_ShowForge_Patch
    {
        [HarmonyPrefix]
        private static void Prefix(RewardPedestalGroup __instance, Player player, bool refresh)
        {
            Plugin.Logger.LogInfo(
                $"[RewardPedestalGroup.ShowForgeAltarChooseUI] _rewardCount={__instance._rewardCount}, _pedestalList.Count={__instance._pedestalList?.Count ?? -1}, refresh={refresh}"
            );
        }
    }

    [HarmonyPatch(typeof(ForgeAltarChooseUI), nameof(ForgeAltarChooseUI.ShowChooseUI))]
    internal static class ForgeAltarChooseUI_ShowChooseUI_Patch
    {
        [HarmonyPrefix]
        private static void Prefix(
            ForgeAltarChooseUI __instance,
            Il2CppSystem.Collections.Generic.List<Item> itemList)
        {
            int beforeItem = itemList != null ? itemList.Count : 0;
            int beforeUnit = __instance._unitUIList != null ? __instance._unitUIList.Count : 0;
            Plugin.Logger.LogInfo(
                $"[ForgeAltarChooseUI.ShowChooseUI] itemList={beforeItem}, unitUI={beforeUnit}"
            );
        }
    }

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
}
