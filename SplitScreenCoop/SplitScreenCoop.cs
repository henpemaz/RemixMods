using System;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using UnityEngine;
using BepInEx;
using MonoMod.RuntimeDetour;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using BepInEx.Logging;
using MonoMod.RuntimeDetour.HookGen;
using System.Collections.Generic;

[module: UnverifiableCode]
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace SplitScreenCoop
{
    [BepInPlugin("com.henpemaz.splitscreencoop", "SplitScreen Co-op", "0.1.8")]
    public partial class SplitScreenCoop : BaseUnityPlugin
    {
        public static SplitScreenCoopOptions Options;
        
        public void OnEnable()
        {
            Logger.LogInfo("OnEnable");
            sLogger = Logger;
            On.RainWorld.OnModsInit += OnModsInit;
            On.RainWorld.PostModsInit += RainWorld_PostModsInit;

            try
            {
                // need this early
                On.Futile.Init += Futile_Init; // turn on cam2
                On.Futile.UpdateCameraPosition += Futile_UpdateCameraPosition; // handle custom switcheroos
                On.FScreen.ReinitRenderTexture += FScreen_ReinitRenderTexture; // new tech huh
            }
            catch (Exception e)
            {
                Logger.LogError("Failed to initialize");
                Logger.LogError(e);
                throw;
            }
        }

        private void RainWorld_PostModsInit(On.RainWorld.orig_PostModsInit orig, RainWorld self)
        {
            orig(self);
            ReadSettings();
        }

        public enum SplitMode
        {
            NoSplit,
            SplitHorizontal, // top bottom screens
            SplitVertical, // left right screens
            Split4Screen // 4 players
        }

        public static SplitMode CurrentSplitMode;
        public static SplitMode preferedSplitMode = SplitMode.SplitVertical;
        public static bool alwaysSplit;
        public static bool dualDisplays;

        public static Camera[] fcameras = new Camera[4];
        public static CameraListener[] cameraListeners = new CameraListener[4];
        public static List<DisplayExtras> displayExtras = new();

        public static Camera camera2;
        public static Camera camera3;
        public static Camera camera4;
        public static GameObject cameraHolder2;
        public static GameObject cameraHolder3;
        public static GameObject cameraHolder4;

        public static Vector2[] camOffsets = new Vector2[] { new Vector2(0, 0), new Vector2(32000, 0), new Vector2(0, 32000), new Vector2(32000, 32000) }; // one can dream

        public static int curCamera = -1;
        public static RoomRealizer realizer2;

        public bool init;
        public static ManualLogSource sLogger;
        public static bool selfSufficientCoop;

        public void Update()
        {
            if (Input.GetKeyDown("f8"))
            {
                if (preferedSplitMode == SplitMode.SplitHorizontal) preferedSplitMode = SplitMode.SplitVertical;
                else if (preferedSplitMode == SplitMode.SplitVertical) preferedSplitMode = SplitMode.SplitHorizontal;
                else return;
                if (CurrentSplitMode != SplitMode.NoSplit && GameObject.FindObjectOfType<RainWorld>()?.processManager?.currentMainLoop is RainWorldGame game)
                    SetSplitMode(preferedSplitMode, game);
            }
            if (Input.GetKeyDown("f9"))
            {
                RainWorldGame game = (RainWorldGame)GameObject.FindObjectOfType<RainWorld>()?.processManager?.currentMainLoop;
                CurrentSplitMode = preferedSplitMode = SplitMode.Split4Screen;
                SetSplitMode(preferedSplitMode, game);
                for (int i = 0; i < game.session.Players.Count; i++)
                    AssignCameraToPlayer(game.cameras[i], (Player)game.session.Players[i].realizedCreature);
            }
        }

        public void OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            try
            {
                // Register OptionsInterface
                Options ??= new SplitScreenCoopOptions();
                MachineConnector.SetRegisteredOI("henpemaz_splitscreencoop", Options);

                if (init) return;
                init = true;
                Logger.LogInfo("OnModsInit");

                // splitscreen functionality
                IL.RainWorldGame.ctor += RainWorldGame_ctor1;
                On.RainWorldGame.ctor += RainWorldGame_ctor; // make roomcam2, follow fix

                On.RoomCamera.ctor += RoomCamera_ctor1; // bind cam to camlistener
                On.RainWorldGame.Update += RainWorldGame_Update; // split unsplit
                On.RainWorldGame.ShutDownProcess += RainWorldGame_ShutDownProcess; // unbind camlistener

                // fixes in fixes file
                On.RoomCamera.FireUpSinglePlayerHUD += RoomCamera_FireUpSinglePlayerHUD;// displace cam2 map
                On.Menu.PauseMenu.ctor += PauseMenu_ctor;// displace pause menu
                On.Menu.PauseMenu.ShutDownProcess += PauseMenu_ShutDownProcess;// kill dupe pause menu
                On.Water.InitiateSprites += Water_InitiateSprites; // move water somewhere near final position
                On.VirtualMicrophone.DrawUpdate += VirtualMicrophone_DrawUpdate; // mic from 2nd cam should not pic up while on same cam
                On.HUD.DialogBox.DrawPos += DialogBox_DrawPos; // center dialog in half screen

                IL.RoomCamera.ctor += RoomCamera_ctor; // create sprite with the right name
                IL.RoomCamera.Update += RoomCamera_Update1; // follow critter, clamp to proper values
                IL.RoomCamera.DrawUpdate += RoomCamera_DrawUpdate1; // clamp to proper values
                IL.ShortcutHandler.Update += ShortcutHandler_Update; // activate room if followed, move cam1 if p moves
                IL.ShortcutHandler.SuckInCreature += ShortcutHandler_SuckInCreature; // the vanilla room loading system is fragile af
                IL.PoleMimicGraphics.InitiateSprites += PoleMimicGraphics_InitiateSprites; // dont resize on re-init

                // creature culling has to take into account cam2
                new Hook(typeof(GraphicsModule).GetMethod("get_ShouldBeCulled", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic),
                    typeof(SplitScreenCoop).GetMethod("get_ShouldBeCulled"), this);

                // co-op in co-op file
                On.OverWorld.WorldLoaded += OverWorld_WorldLoaded; // roomrealizer 2 
                IL.RoomRealizer.Update += RoomRealizer_Update; // they broke roomrealizer
                On.RoomRealizer.CanAbstractizeRoom += RoomRealizer_CanAbstractizeRoom; // two checks
                On.ShelterDoor.Close += ShelterDoor_Close; // custom close logic
                IL.Player.Update += Player_Update; // custom sleep update
                On.ShelterDoor.DoorClosed += ShelterDoor_DoorClosed; // custom win condition
                IL.ShelterDoor.Update += ShelterDoor_Update; // custom win/starve detection
                new Hook(typeof(RainWorldGame).GetMethod("get_FirstAlivePlayer", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic),
                    typeof(SplitScreenCoop).GetMethod("get_FirstAlivePlayer"), this);
                IL.SaveState.SessionEnded += SaveState_SessionEnded; // food math
                IL.RainWorldGame.ctor += RainWorldGame_ctor2; // food math
                IL.RainWorldGame.GameOver += RainWorldGame_GameOver; // custom gameover detection
                On.RegionGate.PlayersInZone += RegionGate_PlayersInZone; // joar please TEST your own code
                On.Creature.FlyAwayFromRoom += Creature_FlyAwayFromRoom; // Player taken by vulture? die quicker please
                HookEndpointManager.Modify(typeof(RegionGate).GetProperty("MeetRequirement").GetGetMethod(), // don't assume player[0].realizedcreature
                    new ILContext.Manipulator(RegionGate_get_MeetRequirement));
                On.ProcessManager.IsGameInMultiplayerContext += ProcessManager_IsGameInMultiplayerContext; // :pensive:

                // jolly why
                On.Menu.SlugcatSelectMenu.StartGame += SlugcatSelectMenu_StartGame;
                On.RoomCamera.ChangeCameraToPlayer += RoomCamera_ChangeCameraToPlayer;
                IL.Player.TriggerCameraSwitch += Player_TriggerCameraSwitch;
                On.Player.ctor += Player_ctor;

                // Shader shenanigans
                // wrapped calls to store shader globals
                On.RoomCamera.DrawUpdate += RoomCamera_DrawUpdate;
                On.RoomCamera.Update += RoomCamera_Update;
                On.RoomCamera.MoveCamera_int += RoomCamera_MoveCamera_int;
                On.RoomCamera.MoveCamera_Room_int += RoomCamera_MoveCamera_Room_int; // can also colapse to single cam if one of the cams is dead
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
                selfSufficientCoop = !ModManager.JollyCoop;
                if (selfSufficientCoop)
                {
                    try
                    {
                        self.RequestPlayerSignIn(1, null);
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e);
                    }
                }
            }
        }

        public void ReadSettings()
        {
            preferedSplitMode = Options.PreferredSplitMode.Value;
            dualDisplays = Options.DualDisplays.Value;
            alwaysSplit = Options.AlwaysSplit.Value;

            if (dualDisplays && DualDisplaySupported())
            {
                InitSecondDisplay();
                preferedSplitMode = SplitMode.NoSplit;
                alwaysSplit = false;
            }
            else
            {
                cameraListeners[1]?.BindToDisplay(Display.main);
                dualDisplays = false;
            }
        }

        public static bool DualDisplaySupported()
        {
            return Display.displays.Length >= 2;
        }

        public static void InitSecondDisplay()
        {
            if (!Display.displays[1].active)
                Display.displays[1].Activate();
            cameraListeners[1].BindToDisplay(Display.displays[1]);
            cameraListeners[1].mirrorMain = true;
        }

        /// <summary>
        /// Init unity camera 2
        /// </summary>
        public void Futile_Init(On.Futile.orig_Init orig, Futile self, FutileParams futileParams)
        {
            orig(self, futileParams);

            Logger.LogInfo("Futile_Init creating camera2");

            cameraHolder2 = new GameObject();
            cameraHolder2.transform.parent = self.gameObject.transform;
            camera2 = cameraHolder2.AddComponent<Camera>();
            self.InitCamera(camera2, 2);

            cameraHolder3 = new GameObject();
            cameraHolder3.transform.parent = self.gameObject.transform;
            camera3 = cameraHolder3.AddComponent<Camera>();
            self.InitCamera(camera3, 3);

            cameraHolder4 = new GameObject();
            cameraHolder4.transform.parent = self.gameObject.transform;
            camera4 = cameraHolder4.AddComponent<Camera>();
            self.InitCamera(camera4, 4);

            fcameras = new Camera[] { self.camera, camera2, camera3, camera4 };

            for (int i = 0; i < fcameras.Length; i++)
            {
                var listener = fcameras[i].gameObject.AddComponent<CameraListener>();
                cameraListeners[i] = listener;
                listener.AttachTo(fcameras[i], Display.main);
            }

            camera2.enabled = false;
            camera3.enabled = false;
            camera4.enabled = false;
            self.UpdateCameraPosition();
            Logger.LogInfo("Futile_Init camera2 success");
        }

        /// <summary>
        /// CameraListeners need to keep up
        /// </summary>
        public void FScreen_ReinitRenderTexture(On.FScreen.orig_ReinitRenderTexture orig, FScreen self, int displayWidth)
        {
            orig(self, displayWidth);

            foreach (var l in cameraListeners)
            {
                l?.ReinitRenderTexture();
            }
        }

        /// <summary>
        /// Apply better offsets in multicam mode, vanilla ones weren't good
        /// </summary>
        public void Futile_UpdateCameraPosition(On.Futile.orig_UpdateCameraPosition orig, Futile self)
        {
            orig(self);

            Logger.LogInfo("Futile_UpdateCameraPosition");
            for (int i = 0; i < fcameras.Length; i++)
            {
                if (fcameras[i] == null) continue;
                var offset = camOffsets[i];
                var x = (Futile.screen.originX - 0.5f) * -Futile.screen.pixelWidth * Futile.displayScaleInverse + Futile.screenPixelOffset.x + offset.x;
                var y = (Futile.screen.originY - 0.5f) * -Futile.screen.pixelHeight * Futile.displayScaleInverse - Futile.screenPixelOffset.y + offset.y;
                fcameras[i].transform.position = new Vector3(x, y, -10f);
            }
        }


        /// <summary>
        /// Hookpoint for loading in more cameras, right before first room force-loads
        /// </summary>
        public void RainWorldGame_ctor1(ILContext il)
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
                    Logger.LogInfo("RainWorldGame_ctor1 hookpoint");
                    if (self.IsStorySession && self.session.Players.Count > 1 && (preferedSplitMode != SplitMode.NoSplit || dualDisplays))
                    {
                        Logger.LogInfo("RainWorldGame_ctor1 creating roomcamera2");
                        var cams = self.cameras;
                        Array.Resize(ref cams, 4);
                        self.cameras = cams;
                        for(int i = 1; i < 4; i++)
                        {
                            cams[i] = new RoomCamera(self, i);
                            if(self.session.Players.Count > i)
                                cams[i].followAbstractCreature = self.session.Players[i];
                        }
                        cams[0].followAbstractCreature = self.session.Players[0];
                    }
                    Logger.LogInfo("RainWorldGame_ctor1 hookpoint done");
                });
            }
            else Logger.LogError(new Exception("Couldn't IL-hook RainWorldGame_ctor1 from SplitScreenMod")); // deffendisve progrmanig
        }

        /// <summary>
        /// Setups, cleanups, move cam2, realizer2, set initial split mode
        /// </summary>
        public void RainWorldGame_ctor(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
        {
            Logger.LogInfo("RainWorldGame_ctor");
            ReadSettings();
            if (selfSufficientCoop)
            {
                Logger.LogInfo("enabling player 2");
                manager.rainWorld.setup.player2 = true;
            }

            realizer2 = null;
            CurrentSplitMode = SplitMode.NoSplit;

            orig(self, manager);

            if (self.cameras.Length > 1)
            {
                Logger.LogInfo("camera2 detected");
                for(int i = 1; i < self.session.Players.Count; i++)
                {
                    self.cameras[i].MoveCamera(self.world.activeRooms[0], 0);
                    self.cameras[i].followAbstractCreature = self.session.Players[i];
                }
                SetSplitMode(alwaysSplit ? preferedSplitMode : SplitMode.NoSplit, self);
            }
            else
            {
                Logger.LogInfo("no camera2");
                SetSplitMode(SplitMode.NoSplit, self);
            }
            Logger.LogInfo("RainWorldGame_ctor done");
        }

        /// <summary>
        /// adds a listener for render events so shader globals can be set
        /// </summary>
        public void RoomCamera_ctor1(On.RoomCamera.orig_ctor orig, RoomCamera self, RainWorldGame game, int cameraNumber)
        {
            Logger.LogInfo("RoomCamera_ctor1 for camera #" + cameraNumber);
            orig(self, game, cameraNumber);
            self.splitScreenMode = false; // don't, mine is better
            self.offset = Vector2.zero;
            foreach (var c in self.SpriteLayers) c.SetPosition(camOffsets[self.cameraNumber]);
        }

        /// <summary>
        /// Cleanup cameralisteners and realizers
        /// </summary>
        public void RainWorldGame_ShutDownProcess(On.RainWorldGame.orig_ShutDownProcess orig, RainWorldGame self)
        {
            Logger.LogInfo("RainWorldGame_ShutDownProcess cleanups");
            SetSplitMode(SplitMode.NoSplit, self);
            if (dualDisplays && DualDisplaySupported())
            {
                cameraListeners[1].mirrorMain = true;
            }
            realizer2 = null;
            orig(self);
        }

        /// <summary>
        /// Check for needed split mode changes, update realizers
        /// </summary>
        public void RainWorldGame_Update(On.RainWorldGame.orig_Update orig, RainWorldGame self)
        {
            orig(self);

            if (!self.IsStorySession) return;
            if (self.GamePaused) return;

            if (self.cameras.Length > 1)
            {
                var main = self.cameras[0];
                var other = self.cameras[1];
                if (CurrentSplitMode == SplitMode.NoSplit && preferedSplitMode != SplitMode.NoSplit && main.followAbstractCreature != other.followAbstractCreature && (other.room != main.room || other.currentCameraPosition != main.currentCameraPosition))
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

            if(self.Players.Count > 1)
            {
                if (realizer2 != null)
                {
                    realizer2.Update();
                }
                else
                {
                    if (self.roomRealizer?.followCreature != null) MakeRealizer2(self);
                }
            }

            if (selfSufficientCoop)
            {
                CoopUpdate(self);
            }
        }

        /// <summary>
        /// Switches between split modes, only call from outside of camera-related code? untested if that actually breaks anything
        /// </summary>
        public void SetSplitMode(SplitMode split, RainWorldGame game)
        {
            Logger.LogInfo("SetSplitMode");
            if (game.cameras.Length > 1)
            {
                Logger.LogInfo("multicam");
                CurrentSplitMode = split;
                for(int i = 0; i < game.cameras.Length; i++)
                    OffsetHud(game.cameras[i]);

                if (dualDisplays)
                {
                    cameraListeners[0].direct = true;
                    cameraListeners[1].fcamera.enabled = true;
                    cameraListeners[1].mirrorMain = false;
                    cameraListeners[1].direct = true;
                }
                else
                {
                    switch (CurrentSplitMode)
                    {
                        case SplitMode.NoSplit:
                            Logger.LogInfo("NoSplit");
                            for (int i = 1; i < fcameras.Length; i++)
                            {
                                fcameras[i].enabled = false;
                            }
                            cameraListeners[0].direct = true;
                            break;
                        case SplitMode.SplitHorizontal:
                            Logger.LogInfo("SplitHorizontal");
                            cameraListeners[0].direct = false;
                            cameraListeners[0].SetMap(new Rect(0f, 0.25f, 1f, 0.5f), new Rect(0f, 0.5f, 1f, 0.5f));
                            fcameras[1].enabled = true;
                            cameraListeners[1].direct = false;
                            cameraListeners[1].SetMap(new Rect(0f, 0.25f, 1f, 0.5f), new Rect(0f, 0f, 1f, 0.5f));
                            break;
                        case SplitMode.SplitVertical:
                            Logger.LogInfo("SplitVertical");
                            cameraListeners[0].direct = false;
                            cameraListeners[0].SetMap(new Rect(0.25f, 0f, 0.5f, 1f), new Rect(0f, 0f, 0.5f, 1f));
                            fcameras[1].enabled = true;
                            cameraListeners[1].direct = false;
                            cameraListeners[1].SetMap(new Rect(0.25f, 0f, 0.5f, 1f), new Rect(0.5f, 0f, 0.5f, 1f));
                            break;
                        case SplitMode.Split4Screen:
                            Logger.LogInfo("Split4Screen");
                            cameraListeners[0].direct = false;
                            cameraListeners[0].SetMap(new Rect(0.25f, 0.25f, 0.5f, 0.5f), new Rect(0f, 0.5f, 0.5f, 0.5f));
                            fcameras[1].enabled = true;
                            cameraListeners[1].direct = false;
                            cameraListeners[1].SetMap(new Rect(0.25f, 0.25f, 0.5f, 0.5f), new Rect(0.5f, 0.5f, 0.5f, 0.5f));
                            fcameras[2].enabled = true;
                            cameraListeners[2].direct = false;
                            cameraListeners[2].SetMap(new Rect(0.25f, 0.25f, 0.5f, 0.5f), new Rect(0f, 0f, 0.5f, 0.5f));
                            fcameras[3].enabled = true;
                            cameraListeners[3].direct = false;
                            cameraListeners[3].SetMap(new Rect(0.25f, 0.25f, 0.5f, 0.5f), new Rect(0.5f, 0f, 0.5f, 0.5f));
                            break;
                        default:
                            break;
                    }
                }
            }
            else
            {
                Logger.LogInfo("single cam NoSplit");
                for (int i = 1; i < fcameras.Length; i++)
                {
                    fcameras[i].enabled = false;
                }
                cameraListeners[0].direct = true;
            }
        }

        /// <summary>
        /// null or dead or deleted creature
        /// </summary>
        public bool IsCreatureDead(AbstractCreature critter)
        {
            return (critter?.state?.dead ?? true) || (critter?.realizedCreature?.slatedForDeletetion ?? true);
        }

        /// <summary>
        /// consider changing camera targets if someones dead or deleted
        /// </summary>
        public void ConsiderColapsing(RainWorldGame game)
        {
            if (game.cameras.Length > 1)
            {
                if (game.Players.Count == 2 && alwaysSplit) return; // I guess
                foreach (var cam in game.cameras)
                {
                    // if following dead critter, switch!
                    if (IsCreatureDead(cam.followAbstractCreature))
                    {
                        if (cam.game.Players.ToArray().Reverse().FirstOrDefault(cr => !IsCreatureDead(cr))?.realizedCreature is Player p)
                            AssignCameraToPlayer(cam, p);
                        else if (cam.game.cameras.FirstOrDefault(c => !IsCreatureDead(c.followAbstractCreature))?.followAbstractCreature?.realizedCreature is Player pp)
                            AssignCameraToPlayer(cam, pp);
                    }
                }
            }
        }

        /// <summary>
        /// Update camera.follow but also properly switch current room and hud owner
        /// </summary>
        public void AssignCameraToPlayer(RoomCamera camera, Player player)
        {
            Logger.LogInfo($"AssignCameraToPlayer cam {camera.cameraNumber} to p {player.playerState.playerNumber}");
            //Logger.LogInfo(Environment.StackTrace);
            camera.followAbstractCreature = player.abstractCreature;
            var newroom = player.room ?? player.abstractCreature.Room.realizedRoom;
            if (newroom != null && camera.room != null && camera.room != newroom)
            {
                int node = player.abstractCreature.pos.abstractNode;
                camera.MoveCamera(newroom, newroom.CameraViewingNode(node != -1 ? node : 0));
            }
            if (camera.hud != null) camera.hud.owner = player;
        }

        /// <summary>
        /// Move HUD onscreen for current split mode
        /// </summary>
        public void OffsetHud(RoomCamera self)
        {
            Vector2 offset = camOffsets[self.cameraNumber];
            if (CurrentSplitMode == SplitMode.SplitHorizontal)
            {
                offset += new Vector2(0, self.sSize.y / 4f);
            }
            else if(CurrentSplitMode == SplitMode.SplitVertical)
            {
                offset += new Vector2(self.sSize.x / 4f, 0f);
            }
            else if(CurrentSplitMode == SplitMode.Split4Screen)
            {
                offset += new Vector2(self.sSize.x / 4f, self.sSize.y / 4f);
            }
            self.ReturnFContainer("HUD").SetPosition(offset);
            self.ReturnFContainer("HUD2").SetPosition(offset);
            self.hud?.map?.inFrontContainer?.SetPosition(offset);
        }
    }
}
