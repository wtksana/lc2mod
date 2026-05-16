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

    [HarmonyPatch(typeof(ForgeAltarChooseUI), nameof(ForgeAltarChooseUI.ShowChooseUI))]
    internal static class ForgeAltarChooseUI_ShowChooseUI_Patch
    {
        [HarmonyPrefix]
        private static void Prefix(
            ForgeAltarChooseUI __instance,
            Il2CppSystem.Collections.Generic.List<Item> itemList)
        {
            int inputCount = itemList != null ? itemList.Count : 0;
            Plugin.Logger.LogInfo(
                $"[ForgeAltarChooseUI.ShowChooseUI] inputCount={inputCount}"
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
