using CounterStrikeSharp.API.Modules.Utils;
using RetakesAllocatorCore.Config;

namespace RetakesAllocatorCore;

public static class PluginInfo
{
    public const string Version = "2.3.2";

    public static readonly string LogPrefix = $"[RetakesAllocator {Version}] ";

    public static string MessagePrefix
    {
        get
        {
            var name = "Retakes";
            if (Configs.IsLoaded())
            {
                if (Configs.GetConfigData().ChatMessagePluginPrefix is not null)
                {
                    return Translator.Color(Configs.GetConfigData().ChatMessagePluginPrefix!);
                }

                name = Configs.GetConfigData().ChatMessagePluginName;
            }

            return $"[{ChatColors.Green}{name}{ChatColors.White}] ";
        }
    }
}