using BepInEx;
using RainMeadow;
using System;
using System.Security.Permissions;
using UnityEngine;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace TagMod
{
    [BepInPlugin("henpemaz.tag", "Tag", "0.1.0")]
    public partial class TagMod : BaseUnityPlugin
    {
        public static TagMod instance;
        private bool init;
        private bool fullyInit;

        public static RainMeadow.OnlineGameMode.OnlineGameModeType tagGameMode = new("Tag", true);
        public static ProcessManager.ProcessID TagMenu = new("TagMenu", true);

        public void OnEnable()
        {
            instance = this;
            On.RainWorld.OnModsInit += RainWorld_OnModsInit;
        }

        private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            if (init) return;
            init = true;

            try
            {
                On.Menu.MainMenu.ctor += MainMenu_ctor;
                On.ProcessManager.PostSwitchMainProcess += ProcessManager_PostSwitchMainProcess;

                RainMeadow.OnlineGameMode.RegisterType(tagGameMode, typeof(TagGameMode), "Play tag!");
                RainMeadow.LocalMatchmakingManager.localGameMode = "Tag";

                On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;

                On.Rock.HitSomething += Rock_HitSomething;

                On.Player.Collide += Player_Collide;

                fullyInit = true;
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                fullyInit = false;
                //throw;
            }
        }

        private void Player_Collide(On.Player.orig_Collide orig, Player self, PhysicalObject otherObject, int myChunk, int otherChunk)
        {
            orig(self, otherObject, myChunk, otherChunk);
            if (RainMeadow.OnlineManager.lobby != null && RainMeadow.OnlineManager.lobby.gameMode is TagGameMode tag)
            {
                Tagged(tag, self, otherObject);
            }
        }

        private void Tagged(TagGameMode tag, PhysicalObject self, PhysicalObject otherObject)
        {
            if (self.abstractPhysicalObject == self.abstractPhysicalObject.world.game.GetStorySession.Players[0]
                    && self.abstractPhysicalObject.GetOnlineObject() is OnlinePhysicalObject mine
                    && mine.isMine
                    && otherObject is Player
                    && otherObject.abstractPhysicalObject.GetOnlineObject() is OnlinePhysicalObject theirs
                    && !theirs.isMine)
            {
                if (tag.hunterData.hunter && !theirs.GetData<HunterData>().hunter)
                {
                    OnlineManager.lobby.owner.InvokeRPC(NowHunter, theirs.owner);
                }
            }
        }

        [RainMeadow.RPCMethod]
        public static void NowHunter(RPCEvent rpcEvent, OnlinePlayer newHunter)
        {
            var tag = (TagGameMode)OnlineManager.lobby.gameMode;
            if (!tag.tagData.hunters.Contains(newHunter))
            {
                TagMod.Debug(newHunter);
                tag.tagData.hunters.Add(newHunter);
            }
        }

        private bool Rock_HitSomething(On.Rock.orig_HitSomething orig, Rock self, SharedPhysics.CollisionResult result, bool eu)
        {
            if (RainMeadow.OnlineManager.lobby != null && RainMeadow.OnlineManager.lobby.gameMode is TagGameMode tag)
            {
                var hit = orig(self, result, eu);
                if (hit && self.thrownBy is Player thrownBy)
                {
                    Tagged(tag, thrownBy, result.obj);
                }
                return hit;
            }
            return orig(self, result, eu);
        }

        private void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if (RainMeadow.OnlineManager.lobby != null && RainMeadow.OnlineManager.lobby.gameMode is TagGameMode tag)
            {
                var oe = self.player.abstractCreature.GetOnlineObject();
                if (oe != null)
                {
                    var hunterData = oe.GetData<HunterData>();
                    if (UnityEngine.Input.GetKey(KeyCode.L)) { TagMod.Debug($"{oe} hunter?" + hunterData.hunter + ";lasthunter?"+hunterData.lastHunter); }
                    if (hunterData.hunter != hunterData.lastHunter)
                    {
                        TagMod.Debug("hunter change detected");
                        self.ApplyPalette(sLeaser, rCam, rCam.currentPalette);
                    }
                    hunterData.lastHunter = hunterData.hunter;
                }
            }
            orig(self, sLeaser, rCam, timeStacker, camPos);
        }

        private void ProcessManager_PostSwitchMainProcess(On.ProcessManager.orig_PostSwitchMainProcess orig, ProcessManager self, ProcessManager.ProcessID ID)
        {
            if (ID == TagMenu)
            {
                self.currentMainLoop = new TagMenu(self);
            }

            orig(self, ID);
        }

        private void MainMenu_ctor(On.Menu.MainMenu.orig_ctor orig, Menu.MainMenu self, ProcessManager manager, bool showRegionSpecificBkg)
        {
            orig(self, manager, showRegionSpecificBkg);

            if (!fullyInit)
            {
                self.manager.ShowDialog(new Menu.DialogNotify("Tag failed to start", self.manager, null));
                return;
            }
        }
    }
}
