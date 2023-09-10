using BepInEx;
using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Security;
using System.Security.Permissions;

[module: UnverifiableCode]
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]


namespace LizardSkin
{
    [BepInPlugin("com.henpemaz.lizardskin", "LizardSkin", "0.1.0")]
    public partial class LizardSkin : BaseUnityPlugin
    {
        public void OnEnable()
        {
            Logger.LogInfo("OnEnable");
            On.RainWorld.OnModsInit += OnModsInit;
            On.RainWorld.PostModsInit += RainWorld_PostModsInit;
            sLogger = Logger;
        }

        public bool init;
        private LizardSkinOI options;
        private static ManualLogSource sLogger;

        public static void Debug(object data)
        {
            sLogger.LogInfo(data);
        }

        public void OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            try
            {
                // Register OptionsInterface
                options ??= new LizardSkinOI();
                MachineConnector.SetRegisteredOI("henpemaz_lizardskin", options);

                if (init) return;
                init = true;

                Logger.LogInfo("OnModsInit");

                ApplyHooksToPlayerGraphics();
                Futile.atlasManager.LoadAtlas("Atlases/LizKinIcons");
                Logger.LogInfo("OnModsInit done");
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                throw;
            }
            finally
            {
                orig(self);
            }
        }

        private void RainWorld_PostModsInit(On.RainWorld.orig_PostModsInit orig, RainWorld self)
        {
            orig(self);
            try
            {
                Logger.LogInfo("PostModsInit");
                ReadSettings();
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                throw;
            }
        }

        private void ReadSettings()
        {
            //throw new NotImplementedException();
        }

        internal static List<LizKinCosmeticData> GetCosmeticsForSlugcat(bool isStorySession, int name, int slugcatCharacter, int playerNumber)
        {
            if (LizardSkinOI.configBeingUsed == null) // CM hasn't run yet and we're in the game, huh :/
            {
                LizardSkinOI.LoadLizKinData();
            }
            return LizardSkinOI.configBeingUsed.GetCosmeticsForSlugcat(name, slugcatCharacter, playerNumber);
        }
    }
}
