using HarmonyLib;
using LC2;
using UnityEngine;
using UnityEngine.UI;

namespace LC2Mod.MoreChoices;

internal static class Patches
{
    [HarmonyPatch(typeof(CommonItemData), "get_SelectItemNum")]
    internal static class CommonItemData_SelectItemNum_Patch
    {
        [HarmonyPostfix]
        private static void Postfix(CommonItemData __instance, ref int __result)
        {
            int target = Plugin.CfgTargetSelectCount.Value;
            if (__result < target)
            {
                __result = target;
            }
        }
    }

    [HarmonyPatch(typeof(CommonItem), nameof(CommonItem.InitAllItem_Server))]
    internal static class CommonItem_InitAllItem_Server_Patch
    {
        [HarmonyPrefix]
        private static void Prefix(CommonItem __instance)
        {
            int target = Plugin.CfgTargetSelectCount.Value;

            // 找到所有 CommonItemDataAsset 实例（ScriptableObject），改它们关联的 CommonItemData._selectItemNum
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
                Plugin.Logger.LogInfo($"[InitAllItem_Server PREFIX] patched {patched} CommonItemDataAsset; CommonItem._selectItemNum was {__instance._selectItemNum}");
            }
            catch (System.Exception ex)
            {
                Plugin.Logger.LogWarning($"  FindObjectsOfTypeAll<CommonItemDataAsset> failed: {ex.Message}");
            }
        }

        [HarmonyPostfix]
        private static void Postfix(CommonItem __instance)
        {
            int poolSize = __instance._itemList != null ? __instance._itemList.Count : 0;
            Plugin.Logger.LogInfo(
                $"[InitAllItem_Server POST] poolSize={poolSize} selectNum={__instance._selectItemNum}"
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

    [HarmonyPatch(typeof(CommonItem), nameof(CommonItem.BePickedUp))]
    internal static class CommonItem_BePickedUp_Patch
    {
        [HarmonyPostfix]
        private static void Postfix(CommonItem __instance)
        {
            int poolSize = __instance._itemList != null ? __instance._itemList.Count : 0;
            string goName = __instance.gameObject != null ? __instance.gameObject.name : "<null>";
            Plugin.Logger.LogInfo(
                $"[CommonItem.BePickedUp] poolSize={poolSize} selectNum={__instance._selectItemNum} go='{goName}'"
            );
        }
    }

    [HarmonyPatch(typeof(CommonItem), nameof(CommonItem.OnNetworkSpawn))]
    internal static class CommonItem_OnNetworkSpawn_Patch
    {
        [HarmonyPostfix]
        private static void Postfix(CommonItem __instance)
        {
            int poolSize = __instance._itemList != null ? __instance._itemList.Count : 0;
            string goName = __instance.gameObject != null ? __instance.gameObject.name : "<null>";
            Plugin.Logger.LogInfo(
                $"[CommonItem.OnNetworkSpawn] poolSize={poolSize} selectNum={__instance._selectItemNum} go='{goName}'"
            );

            // 列出 GameObject 上所有 component，找出谁决定生成数量
            if (__instance.gameObject != null)
            {
                var comps = __instance.gameObject.GetComponents<UnityEngine.Component>();
                for (int i = 0; i < comps.Length; i++)
                {
                    var c = comps[i];
                    if (c != null)
                    {
                        Plugin.Logger.LogInfo($"  [comp {i}] {c.GetIl2CppType().FullName}");
                    }
                }
            }
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
            int target = Plugin.CfgTargetSelectCount.Value;
            int beforeItem = itemList != null ? itemList.Count : 0;
            int beforeUnit = __instance._unitUIList != null ? __instance._unitUIList.Count : 0;

            // 扩 UI 槽位：克隆 _unitUIList[0] 到 target 个，保持原 cell 尺寸不变
            if (__instance._unitUIList != null && beforeUnit > 0 && beforeUnit < target)
            {
                var template = __instance._unitUIList[0];
                var templateGo = template.gameObject;
                var parent = templateGo.transform.parent;

                int needed = target - beforeUnit;
                for (int i = 0; i < needed; i++)
                {
                    var clone = UnityEngine.Object.Instantiate(templateGo, parent);
                    clone.name = $"UnitUI_Cloned_{i}";
                    var unit = clone.GetComponent<ForgeAltarChooseUnitUI>();
                    if (unit != null)
                    {
                        __instance._unitUIList.Add(unit);
                    }
                }
            }

            int afterItem = itemList != null ? itemList.Count : 0;
            int afterUnit = __instance._unitUIList != null ? __instance._unitUIList.Count : 0;
            Plugin.Logger.LogInfo(
                $"[ForgeAltarChooseUI.ShowChooseUI] itemList: {beforeItem} -> {afterItem}, unitUI: {beforeUnit} -> {afterUnit}"
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
            UpdateVisibleWindow(__instance);
        }
    }

    [HarmonyPatch(typeof(ForgeAltarChooseUI), nameof(ForgeAltarChooseUI.SetSelectState))]
    internal static class ForgeAltarChooseUI_SetSelectState_Patch
    {
        [HarmonyPostfix]
        private static void Postfix(ForgeAltarChooseUI __instance)
        {
            UpdateVisibleWindow(__instance);
        }
    }

    private const int VisibleWindow = 4;

    private static void UpdateVisibleWindow(ForgeAltarChooseUI ui)
    {
        if (ui == null || ui._unitUIList == null) return;
        int n = ui._unitUIList.Count;
        if (n <= VisibleWindow)
        {
            for (int i = 0; i < n; i++)
            {
                var go = ui._unitUIList[i]?.gameObject;
                if (go != null && !go.activeSelf) go.SetActive(true);
            }
            return;
        }

        int cur = ui._curIndex;
        if (cur < 0) cur = 0;
        if (cur >= n) cur = n - 1;

        // 以 cur 为中心，让窗口包含 cur，长度 = VisibleWindow
        int start = cur - VisibleWindow / 2;
        if (start < 0) start = 0;
        int end = start + VisibleWindow - 1;
        if (end >= n)
        {
            end = n - 1;
            start = end - VisibleWindow + 1;
            if (start < 0) start = 0;
        }

        for (int i = 0; i < n; i++)
        {
            var go = ui._unitUIList[i]?.gameObject;
            if (go == null) continue;
            bool shouldShow = i >= start && i <= end;
            if (go.activeSelf != shouldShow) go.SetActive(shouldShow);
        }
    }
}
