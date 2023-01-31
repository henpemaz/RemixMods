using System;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using UnityEngine;
using BepInEx;
using BepInEx.Configuration;
using MonoMod.RuntimeDetour;
using MonoMod.RuntimeDetour.HookGen;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System.IO;
using System.Security.Cryptography;
using BepInEx.Logging;

[module: UnverifiableCode]
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace SplitScreenCoop
{
    [BepInPlugin("com.henpemaz.splitscreencoop", "SplitScreen Co-op", "0.1.0")]
    public partial class SplitScreenCoop : BaseUnityPlugin
    {
        public void OnEnable()
        {
            sLogger = Logger;
            On.RainWorld.OnModsInit += OnModsInit;

            // need this early
            On.Futile.Init += Futile_Init; // turn on splitscreen
            On.Futile.UpdateCameraPosition += Futile_UpdateCameraPosition; // handle custom switcheroos
            On.FScreen.ReinitRenderTexture += FScreen_ReinitRenderTexture; // new tech huh

            confPreferedSplitMode = Config.Bind("General",
                                         "preferedSplitMode",
                                         SplitMode.SplitVertical,
                                         "Preferred split mode");
            confAlwaysSplit = Config.Bind("General",
                                         "alwaysSplit",
                                         false,
                                         "Permanent split mode");
        }

        private ConfigEntry<SplitMode> confPreferedSplitMode;
        private ConfigEntry<bool> confAlwaysSplit;

        public enum SplitMode
        {
            NoSplit,
            SplitHorizontal, // top bottom screens
            SplitVertical // left right screens
        }

        public static SplitMode CurrentSplitMode;
        public static SplitMode preferedSplitMode = SplitMode.SplitVertical;
        public static bool alwaysSplit;
        public static Camera[] fcameras = new Camera[2];
        public static Vector2[] camOffsets = new Vector2[] { new Vector2(0, 0), new Vector2(32000, 0), new Vector2(0, 32000), new Vector2(32000, 32000) }; // one can dream

        static int curCamera = -1;
        public static CameraListener[] cameraListeners = new CameraListener[2];
        public static RoomRealizer realizer2;
        private bool init;

        private static ManualLogSource sLogger;

        void Update()
        {
            if (Input.GetKeyDown("f8"))
            {
                if (preferedSplitMode == SplitMode.NoSplit) return;
                if (preferedSplitMode == SplitMode.SplitHorizontal) preferedSplitMode = SplitMode.SplitVertical;
                else if (preferedSplitMode == SplitMode.SplitVertical) preferedSplitMode = SplitMode.SplitHorizontal;
                if (CurrentSplitMode != SplitMode.NoSplit && GameObject.FindObjectOfType<RainWorld>()?.processManager?.currentMainLoop is RainWorldGame game)
                    SetSplitMode(preferedSplitMode, game);
            }
        }

        private void OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            try
            {
                if (init) return;
                init = true;

                On.RoomCamera.ctor += RoomCamera_ctor1; // bind cam to camlistener
                On.RainWorldGame.Update += RainWorldGame_Update; // split unsplit
                On.RainWorldGame.ShutDownProcess += RainWorldGame_ShutDownProcess; // unbind camlistener

                // wrapped calls to store shader globals
                On.RoomCamera.DrawUpdate += RoomCamera_DrawUpdate;
                On.RoomCamera.Update += RoomCamera_Update;
                On.RoomCamera.MoveCamera_int += RoomCamera_MoveCamera_int;
                On.RoomCamera.MoveCamera_Room_int += RoomCamera_MoveCamera_Room_int; // can also colapse to single cam if one of the cams is dead 
                
                // fixes in fixes file
                On.OverWorld.WorldLoaded += OverWorld_WorldLoaded; // roomrealizer 2 
                On.RoomCamera.FireUpSinglePlayerHUD += RoomCamera_FireUpSinglePlayerHUD;// displace cam2 map
                On.Menu.PauseMenu.ctor += PauseMenu_ctor;// displace pause menu
                On.Menu.PauseMenu.ShutDownProcess += PauseMenu_ShutDownProcess;// kill dupe pause menu
                On.Water.InitiateSprites += Water_InitiateSprites; // move water somewhere near final position
                On.VirtualMicrophone.DrawUpdate += VirtualMicrophone_DrawUpdate; // mic from 2nd cam should not pic up while on same cam
                On.HUD.DialogBox.DrawPos += DialogBox_DrawPos; // center dialog in half screen
                On.RoomRealizer.CanAbstractizeRoom += RoomRealizer_CanAbstractizeRoom;

                IL.RainWorldGame.ctor += RainWorldGame_ctor1;
                On.RainWorldGame.ctor += RainWorldGame_ctor; // make roomcam2, follow fix

                // fixes in fixes file
                IL.RoomCamera.ctor += RoomCamera_ctor; // create sprite with the right name
                IL.RoomCamera.Update += RoomCamera_Update1; // follow critter, clamp to proper values
                IL.RoomCamera.DrawUpdate += RoomCamera_DrawUpdate1; // clamp to proper values
                IL.ShortcutHandler.Update += ShortcutHandler_Update; // activate room if followed, move cam1 if p moves
                IL.ShortcutHandler.SuckInCreature += ShortcutHandler_SuckInCreature; // the vanilla room loading system is fragile af
                IL.PoleMimicGraphics.InitiateSprites += PoleMimicGraphics_InitiateSprites; // dont resize on re-init

                // creature culling has to take into account cam2
                new Hook(typeof(GraphicsModule).GetMethod("get_ShouldBeCulled", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic),
                    typeof(SplitScreenCoop).GetMethod("get_ShouldBeCulled"), this);

                // unity hooks
                // set shader variables into a dict so it can be set per-camera
                new Hook(typeof(Shader).GetMethod("SetGlobalColor", new Type[] { typeof(string), typeof(Color) }),
                    typeof(SplitScreenCoop).GetMethod("Shader_SetGlobalColor"), this);
                new Hook(typeof(Shader).GetMethod("SetGlobalVector", new Type[] { typeof(string), typeof(Vector4) }),
                    typeof(SplitScreenCoop).GetMethod("Shader_SetGlobalVector"), this);
                new Hook(typeof(Shader).GetMethod("SetGlobalFloat", new Type[] { typeof(string), typeof(float) }),
                    typeof(SplitScreenCoop).GetMethod("Shader_SetGlobalFloat"), this);
                new Hook(typeof(Shader).GetMethod("SetGlobalTexture", new Type[] { typeof(string), typeof(Texture) }),
                    typeof(SplitScreenCoop).GetMethod("Shader_SetGlobalTexture"), this);

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

        // Hookpoint for loading in more cameras, right before first room force-loads
        private void RainWorldGame_ctor1(ILContext il)
        {
            var c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.Before,
                i => i.MatchLdarg(0),
                i => i.MatchCallOrCallvirt<RainWorldGame>("get_world"),
                i => i.MatchLdloc(0),
                i => i.MatchCallOrCallvirt<World>("ActivateRoom")
                ))
            {
                c.MoveAfterLabels();
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Action<RainWorldGame>>((self) =>
                {
                    if (self.session.Players.Count > 1 && preferedSplitMode != SplitMode.NoSplit)
                    {
                        var cams = self.cameras;
                        Array.Resize(ref cams, 2);
                        self.cameras = cams;
                        cams[1] = new RoomCamera(self, 1);

                        cams[0].followAbstractCreature = self.session.Players[0];
                        cams[1].followAbstractCreature = self.session.Players[1];
                    }
                });
            }
            else Logger.LogError(new Exception("Couldn't IL-hook RainWorldGame_ctor1 from SplitScreenMod")); // deffendisve progrmanig
        }

        private void RainWorldGame_ctor(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
        {
            manager.rainWorld.setup.player2 = true;
            // manager.rainWorld.setup.startMap = "SU_S01";

            preferedSplitMode = confPreferedSplitMode.Value;
            alwaysSplit = confAlwaysSplit.Value;

            for (int i = 0; i < cameraListeners.Length; i++)
            {
                cameraListeners[i]?.Destroy();
                cameraListeners[i] = null;
            }
            realizer2 = null;

            orig(self, manager);

            if (self.session.Players.Count > 1 && preferedSplitMode != SplitMode.NoSplit)
            {
                var cams = self.cameras;
                cams[1].MoveCamera(self.world.activeRooms[0], 0);

                realizer2 = new RoomRealizer(self.session.Players.First(p => p != self.roomRealizer.followCreature), self.world)
                {
                    realizedRooms = self.roomRealizer.realizedRooms,
                    recentlyAbstractedRooms = self.roomRealizer.recentlyAbstractedRooms,
                    realizeNeighborCandidates = self.roomRealizer.realizeNeighborCandidates
                };
            }

            CurrentSplitMode = SplitMode.NoSplit;
            SetSplitMode(alwaysSplit ? preferedSplitMode : SplitMode.NoSplit, self);
        }

        // adds a listener for render events so shader globals can be set
        private void RoomCamera_ctor1(On.RoomCamera.orig_ctor orig, RoomCamera self, RainWorldGame game, int cameraNumber)
        {
            orig(self, game, cameraNumber);
            cameraListeners[cameraNumber]?.Destroy();
            var listener = fcameras[cameraNumber].gameObject.AddComponent<CameraListener>();
            cameraListeners[cameraNumber] = listener;
            listener.AttachTo(self);
            foreach (var c in self.SpriteLayers) c.SetPosition(camOffsets[self.cameraNumber]);
            self.offset = Vector2.zero; // nulla zero niente don't use it
                                        // so many drawables don't ever fucking move or don't take into account the offset its infuriating
                                        // so we don't relly on any of that
        }

        private void RainWorldGame_ShutDownProcess(On.RainWorldGame.orig_ShutDownProcess orig, RainWorldGame self)
        {
            SetSplitMode(SplitMode.NoSplit, self);

            for (int i = 0; i < cameraListeners.Length; i++)
            {
                cameraListeners[i]?.Destroy();
                cameraListeners[i] = null;
            }
            realizer2 = null;
            orig(self);
        }

        private void RainWorldGame_Update(On.RainWorldGame.orig_Update orig, RainWorldGame self)
        {
            orig(self);
            if (self.cameras.Length > 1)
            {
                var main = self.cameras[0];
                var other = self.cameras[1];
                if (CurrentSplitMode == SplitMode.NoSplit && main.followAbstractCreature != other.followAbstractCreature && (other.room != main.room || other.currentCameraPosition != main.currentCameraPosition))
                {
                    SetSplitMode(preferedSplitMode, self);
                }

                if (CurrentSplitMode != SplitMode.NoSplit && (other.room == main.room && other.currentCameraPosition == main.currentCameraPosition) && !alwaysSplit)
                {
                    SetSplitMode(SplitMode.NoSplit, self);
                }

                if (CurrentSplitMode != SplitMode.NoSplit && main.room.abstractRoom.name == "SB_L01") // honestly jolly
                {
                    ConsiderColapsing(self);
                }
            }

            realizer2?.Update();
        }

        public void SetSplitMode(SplitMode split, RainWorldGame game)
        {
            if (game.cameras.Length > 1)
            {
                var main = game.cameras[0];
                var other = game.cameras[1];
                CurrentSplitMode = split;
                OffsetHud(main);
                OffsetHud(other);

                if (CurrentSplitMode == SplitMode.NoSplit)
                {
                    for (int i = 1; i < fcameras.Length; i++)
                    {
                        fcameras[i].enabled = false;
                    }
                    cameraListeners[0].skip = true;
                    cameraListeners[0].SetMap(new Rect(0f, 0f, 1f, 1f), new Rect(0f, 0f, 1f, 1f));
                }
                else if (CurrentSplitMode == SplitMode.SplitHorizontal)
                {
                    cameraListeners[0].SetMap(new Rect(0f, 0.25f, 1f, 0.5f), new Rect(0f, 0.5f, 1f, 0.5f));
                    cameraListeners[0].skip = false;
                    fcameras[1].enabled = true;
                    cameraListeners[1].SetMap(new Rect(0f, 0.25f, 1f, 0.5f), new Rect(0f, 0f, 1f, 0.5f));
                }
                else if (CurrentSplitMode == SplitMode.SplitVertical)
                {
                    cameraListeners[0].SetMap(new Rect(0.25f, 0f, 0.5f, 1f), new Rect(0f, 0f, 0.5f, 1f));
                    cameraListeners[0].skip = false;
                    fcameras[1].enabled = true;
                    cameraListeners[1].SetMap(new Rect(0.25f, 0f, 0.5f, 1f), new Rect(0.5f, 0f, 0.5f, 1f));
                }
            }
        }

        // following null or deleted or dead
        private bool IsCamDead(RoomCamera cam)
        {
            return IsCreatureDead(cam.followAbstractCreature);
        }

        // null or dead or deleted
        private bool IsCreatureDead(AbstractCreature critter)
        {
            return (critter?.state?.dead ?? true) || (critter?.realizedCreature?.slatedForDeletetion ?? true);
        }

        // consider changing if someones dead or deleted
        private void ConsiderColapsing(RainWorldGame game)
        {
            if (game.cameras.Length > 1)
            {
                if (game.Players.Count == 2 && alwaysSplit) return; // I guess
                foreach (var cam in game.cameras)
                {
                    // if following dead critter, switch!
                    if (IsCamDead(cam))
                    {
                        if (cam.game.Players.ToArray().Reverse().FirstOrDefault(cr => !IsCreatureDead(cr))?.realizedCreature is Player p)
                            AssignCameraToPlayer(cam, p);
                        else if (cam.game.cameras.FirstOrDefault(c => !IsCamDead(c))?.followAbstractCreature?.realizedCreature is Player pp)
                            AssignCameraToPlayer(cam, pp);
                    }
                }
            }
        }

        public void AssignCameraToPlayer(RoomCamera camera, Player player)
        {
            camera.followAbstractCreature = player.abstractCreature;
            var newroom = player.room ?? player.abstractCreature.Room.realizedRoom;
            if (newroom != null && camera.room != null && camera.room != newroom)
            {
                int node = player.abstractCreature.pos.abstractNode;
                camera.MoveCamera(newroom, newroom.CameraViewingNode(node != -1 ? node : 0));
            }
            if (camera.hud != null) camera.hud.owner = player;
        }

        private void OffsetHud(RoomCamera self)
        {
            Vector2 offset = camOffsets[self.cameraNumber];
            if (CurrentSplitMode != SplitMode.NoSplit)
            {
                offset += (CurrentSplitMode == SplitMode.SplitHorizontal ? new Vector2(0, self.sSize.y / 4f) : new Vector2(self.sSize.x / 4f, 0));
            }
            self.ReturnFContainer("HUD").SetPosition(offset);
            self.ReturnFContainer("HUD2").SetPosition(offset);
            self.hud?.map?.inFrontContainer?.SetPosition(offset);
        }
    }
}
