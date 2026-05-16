using HarmonyLib;
using LC2;
using UnityEngine;

namespace LC2Mod.MoreChoices;

internal static class Patches
{
    [HarmonyPatch(typeof(CommonItem), nameof(CommonItem.InitAllItem_Server))]
    internal static class CommonItem_InitAllItem_Server_Patch
    {
        [HarmonyPrefix]
        private static void Prefix(CommonItem __instance)
        {
            int target = Plugin.TargetSelectCount;

            // 找到所有 CommonItemDataAsset 实例（ScriptableObject），
            // 改它们关联的 CommonItemData._selectItemNum，让 InitAllItem 内部循环按 target 生成
            try
            {
                var allAssets = UnityEngine.Resources.FindObjectsOfTypeAll<CommonItemDataAsset>();
                int patched = 0;
                for (int i = 0; i < allAssets.Length; i++)
                {
                    var asset = allAssets[i];
                    if (asset == null) continue;
                    var data = asset.ItemData?.Cast<CommonItemData>();
                    if (data != null && data._selectItemNum < target)
                    {
                        Plugin.Logger.LogInfo($"  CommonItemDataAsset '{asset.name}' _selectItemNum: {data._selectItemNum} -> {target}");
                        data._selectItemNum = target;
                        patched++;
                    }
                }
                if (patched > 0)
                {
                    Plugin.Logger.LogInfo($"[InitAllItem_Server] patched {patched} CommonItemDataAsset");
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Logger.LogWarning($"  FindObjectsOfTypeAll<CommonItemDataAsset> failed: {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(CommonItem), nameof(CommonItem.OnCheckRefreshItem))]
    internal static class CommonItem_OnCheckRefreshItem_Patch
    {
        [HarmonyPostfix]
        private static void Postfix(ref bool __result)
        {
            if (Plugin.CfgFreeRefresh.Value)
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(ForgeAltarChooseUI), nameof(ForgeAltarChooseUI.Show))]
    internal static class ForgeAltarChooseUI_Show_Patch
    {
        [HarmonyPrefix]
        private static void Prefix(ForgeAltarChooseUI __instance)
        {
            if (__instance == null || __instance._unitUIList == null) return;

            // 销毁老版本 mod 残留的 UnitUI_Cloned_* 克隆体（兜底，正常情况列表里不会有）
            int n = __instance._unitUIList.Count;
            int destroyed = 0;
            for (int i = n - 1; i >= 0; i--)
            {
                var unit = __instance._unitUIList[i];
                if (unit == null) continue;
                var go = unit.gameObject;
                if (go != null && go.name.StartsWith("UnitUI_Cloned_"))
                {
                    __instance._unitUIList.RemoveAt(i);
                    UnityEngine.Object.Destroy(go);
                    destroyed++;
                }
            }
            if (destroyed > 0)
            {
                Plugin.Logger.LogInfo($"[ForgeAltarChooseUI.Show] cleaned up {destroyed} stale clones");
            }
        }
    }
}

