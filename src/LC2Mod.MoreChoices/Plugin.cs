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
