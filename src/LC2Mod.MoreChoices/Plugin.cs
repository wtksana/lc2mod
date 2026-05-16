using System.Reflection;
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
    internal static ConfigEntry<bool> CfgFreeRefresh;

    public const int TargetSelectCount = 4;

    public override void Load()
    {
        Logger = Log;
        Logger.LogInfo($"{PluginName} {PluginVersion} loading");

        CfgFreeRefresh = Config.Bind(
            "General",
            "FreeRefresh",
            true,
            "刷新候选时不消耗虚灵硬币、不限次数。"
        );

        var harmony = new Harmony(PluginGuid);
        harmony.PatchAll(Assembly.GetExecutingAssembly());

        int patchedCount = 0;
        foreach (var method in harmony.GetPatchedMethods())
        {
            patchedCount++;
            Logger.LogInfo($"Patched: {method.DeclaringType?.FullName}.{method.Name}");
        }
        Logger.LogInfo($"{PluginName} loaded, target select count = {TargetSelectCount}, free refresh = {CfgFreeRefresh.Value}, patches applied = {patchedCount}");
    }
}
