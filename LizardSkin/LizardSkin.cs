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
                if (init) return;
                init = true;

                Logger.LogInfo("OnModsInit");

                ApplyHooksToPlayerGraphics();

                // Json goes brrrr
                On.Json.Serializer.SerializeOther += Serializer_SerializeOther;

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

                // Register OptionsInterface
                options ??= new LizardSkinOI();
                MachineConnector.SetRegisteredOI("henpemaz_lizardskin", options);

                LizardSkinOI.LoadLizKinData();
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                throw;
            }
        }

        private void Serializer_SerializeOther(On.Json.Serializer.orig_SerializeOther orig, Json.Serializer self, object value)
        {
            if (value is IJsonSerializable) self.SerializeObject((value as IJsonSerializable).ToJson());
            else orig(self, value);
        }


        internal static List<LizKinCosmeticData> GetCosmeticsForSlugcat(bool isStorySession, int name, int slugcatCharacter, int playerNumber)
        {
            if (LizardSkinOI.configBeingUsed == null)
            {
                sLogger.LogError(new Exception("Lizardskin requested but hasn't loaded yet!"));
                LizardSkinOI.LoadLizKinData();
            }
            return LizardSkinOI.configBeingUsed.GetCosmeticsForSlugcat(name, slugcatCharacter, playerNumber);
        }
    }
}
