using System;
using BepInEx;

namespace RemixMods
{
    [BepInPlugin("com.henpemaz.remixmods", "Remix Mods", "0.1.0")]
    public class RemixMods : BaseUnityPlugin
    {
        public void OnEnable()
        {
            Logger.LogInfo("Enabled");
            On.RainWorld.OnModsInit += OnModsInit;
        }

        private void OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);

            Logger.LogInfo("Hello world! im new");
            //Logger.LogInfo(SteamFriends.SetRichPresence("connect", "RainWorld.exe -online"));
        }
    }
}
