using BepInEx;
using BepInEx.Logging;
using Menu.Remix;
using MonoMod.Cil;
using Newtonsoft.Json;
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
            On.Menu.MainMenu.ctor += MainMenu_ctor;
            sLogger = Logger;
        }


        public bool init;
        private LizardSkinOI options;
        private static ManualLogSource sLogger;
        private bool fullyInit;
        private bool errorOnce = true;

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

                On.Menu.Remix.ConfigContainer._ReloadItfs += ConfigContainer__ReloadItfs;
                IL.Menu.Remix.ConfigContainer.ctor += ConfigContainer_ctor;
                On.Menu.Remix.ConfigContainer._SwitchMode += ConfigContainer__SwitchMode;

                // great functionality
                On.OptionInterface.ConfigHolder.Reload += ConfigHolder_Reload;

                Futile.atlasManager.LoadAtlas("Atlases/LizKinIcons");
                Logger.LogInfo("OnModsInit done");
                fullyInit = true;
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
            finally
            {
                orig(self);
            }
        }

        private void ConfigHolder_Reload(On.OptionInterface.ConfigHolder.orig_Reload orig, OptionInterface.ConfigHolder self)
        {
            if(self.owner is LizardSkinOI lsoi)
            {
                lsoi.OnConfigReload();
            }
            orig(self);
        }

        // surely they tested this
        private void ConfigContainer__SwitchMode(On.Menu.Remix.ConfigContainer.orig__SwitchMode orig, Menu.Remix.ConfigContainer self, Menu.Remix.ConfigContainer.Mode newMode)
        {
            orig(self, newMode);
            ConfigContainer.menuTab.tabCtrler._tabCount = -1;
            ConfigContainer.menuTab.tabCtrler.Change();
        }

        private void ConfigContainer_ctor(ILContext il)
        {
            try
            {
                ILCursor c = new ILCursor(il);
                c.GotoNext( MoveType.AfterLabel,
                    i=>i.MatchLdcI4(0),
                    i=>i.MatchCall<Menu.Remix.ConfigContainer>("set_ActiveItfIndex")
                    );
                if (c.Prev.MatchCall<Menu.Remix.ConfigContainer>("_ReloadItfs"))
                {
                    c.RemoveRange(2);
                }
                else
                {
                    Debug("set_ActiveItfIndex 0 not found, maybe it's been patched");
                }
            }
            catch (Exception e)
            {
                Debug(e);
                throw;
            }
        }

        private void ConfigContainer__ReloadItfs(On.Menu.Remix.ConfigContainer.orig__ReloadItfs orig, Menu.Remix.ConfigContainer self)
        {
            // nobody tested reloads I guess
            // everything is uninitialized on shutdown needs loading from scratch
            if (Menu.Remix.MenuModList.ModButton._thumbD == null)
            {
                self._LoadItfs();
            }
            orig(self);
        }

        private void RainWorld_PostModsInit(On.RainWorld.orig_PostModsInit orig, RainWorld self)
        {
            orig(self);
            try
            {
                Logger.LogInfo("PostModsInit");
                if (!fullyInit) return;
                // Register OptionsInterface
                options ??= new LizardSkinOI();
                MachineConnector.SetRegisteredOI("henpemaz_lizardskin", options);

                LizardSkinOI.LoadLizKinData();
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                fullyInit = false;
            }
        }

        private void MainMenu_ctor(On.Menu.MainMenu.orig_ctor orig, Menu.MainMenu self, ProcessManager manager, bool showRegionSpecificBkg)
        {
            orig(self, manager, showRegionSpecificBkg);

            if (!fullyInit && errorOnce)
            {
                errorOnce = false;
                self.manager.ShowDialog(new Menu.DialogNotify("LizardSkin failed to start", self.manager, null));
                return;
            }
        }

        internal static JsonSerializerSettings jsonSerializerSettings = new() { Converters =
        [
            new UnityColorConverter(),
            new LizKinCosmeticData.LizKinCosmeticDataConverter()
        ],
            ObjectCreationHandling = ObjectCreationHandling.Replace
        };

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
