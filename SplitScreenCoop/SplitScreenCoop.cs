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
using System.Runtime.CompilerServices;

[module: UnverifiableCode]
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace SplitScreenCoop
{
    [BepInPlugin("com.henpemaz.splitscreencoop", "SplitScreen Co-op", "0.1.14")]
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
                On.PersistentData.ctor += PersistentData_ctor; // memory for more cameras
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
        public static bool allowCameraSwapping;
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
        public static bool[] cameraZoomed = new bool[] { false, false, false, false };
        public static Rect[] cameraSourcePositions = new Rect[] { new Rect(0.25f, 0.25f, 0.5f, 0.5f), new Rect(0.25f, 0.25f, 0.5f, 0.5f), new Rect(0.25f, 0.25f, 0.5f, 0.5f), new Rect(0.25f, 0.25f, 0.5f, 0.5f) };
        public static Rect[] cameraTargetPositions = new Rect[] { new Rect(0f, 0.5f, 0.5f, 0.5f), new Rect(0.5f, 0.5f, 0.5f, 0.5f), new Rect(0f, 0f, 0.5f, 0.5f), new Rect(0.5f, 0f, 0.5f, 0.5f) };

        public static RainWorld rainworldGameObject = null;

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
        }

        public void OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            try
            {
                rainworldGameObject = GameObject.FindObjectOfType<RainWorld>();

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
                On.Player.TriggerCameraSwitch += Player_TriggerCameraSwitch1;
                On.Player.JollyInputUpdate += Player_JollyInputUpdate;
                On.MoreSlugcats.HypothermiaMeter.Draw += HypothermiaMeter_Draw;

                On.Player.ctor += Player_ctor;
                IL.HUD.HUD.InitSinglePlayerHud += InitSinglePlayerHud;
                HookEndpointManager.Modify(typeof(JollyCoop.JollyHUD.JollyPlayerSpecificHud).GetProperty("Camera").GetGetMethod(),
                    new ILContext.Manipulator(JollyPlayerSpecificHud_get_Camera));
                IL.JollyCoop.JollyHUD.JollyPlayerSpecificHud.JollyOffRoom.Update += JollyOffRoom_Update1;
                IL.HUD.Map.Draw += HudMap_Draw;
                On.HUD.KarmaMeter.Draw += KarmaMeter_Draw;
                On.HUD.FoodMeter.Draw += FoodMeter_Draw;
                On.HUD.RainMeter.Draw += RainMeter_Draw;
                On.HUD.TextPrompt.Draw += TextPrompt_Draw;

                // CustomDecal
                On.CustomDecal.ctor += CustomDecal_ctor;
                On.CustomDecal.DrawSprites += CustomDecal_DrawSprites;
                On.CustomDecal.InitiateSprites += CustomDecal_InitiateSprites;
                On.CustomDecal.Update += CustomDecal_Update;
                On.CustomDecal.UpdateMesh += CustomDecal_UpdateMesh;
                On.CustomDecal.UpdateAsset += CustomDecal_UpdateAsset;

                // Snow
                On.MoreSlugcats.SnowSource.PackSnowData += SnowSource_PackSnowData;
                On.MoreSlugcats.SnowSource.Update += SnowSource_Update;
                On.RoofTopView.DustpuffSpawner.DustPuff.ApplyPalette += DustPuff_ApplyPalette;

                // Shader shenanigans
                // wrapped calls to store shader globals
                On.RoomCamera.DrawUpdate += RoomCamera_DrawUpdate;
                On.RoomCamera.Update += RoomCamera_Update;
                On.RoomCamera.MoveCamera_int += RoomCamera_MoveCamera_int;
                On.RoomCamera.MoveCamera_Room_int += RoomCamera_MoveCamera_Room_int; // can also colapse to single cam if one of the cams is dead
                On.RoomCamera.UpdateSnowLight += RoomCamera_UpdateSnowLight;

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

        public void JollyPlayerSpecificHud_get_Camera(ILContext il)
        {
            try
            {
                var c = new ILCursor(il);
                c.GotoNext(MoveType.Before,
                    i => i.MatchRet()
                    );

                c.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
                c.EmitDelegate<Func<RoomCamera, JollyCoop.JollyHUD.JollyPlayerSpecificHud, RoomCamera>>((returnValue, self) =>
                {
                    return self.GetSplitScreenCamera();
                });
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                throw;
            }
        }

        public void ReadSettings()
        {
            preferedSplitMode = Options.PreferredSplitMode.Value;
            dualDisplays = Options.DualDisplays.Value;
            alwaysSplit = Options.AlwaysSplit.Value;
            allowCameraSwapping = Options.AllowCameraSwapping.Value;

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
        /// Allocate memory for 4 cameras instead of default 2
        /// </summary>
        private void PersistentData_ctor(On.PersistentData.orig_ctor orig, PersistentData self, RainWorld rainWorld)
        {
            Logger.LogInfo("Allocation");
            orig(self, rainWorld);
            int ntex = Mathf.Max(4, self.cameraTextures.GetLength(0));
            self.cameraTextures = new Texture2D[ntex, 2];
            for (int i = 0; i < ntex; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    self.cameraTextures[i, j] = new Texture2D(1400, 800, TextureFormat.ARGB32, false);
                    self.cameraTextures[i, j].anisoLevel = 0;
                    self.cameraTextures[i, j].filterMode = FilterMode.Point;
                    self.cameraTextures[i, j].wrapMode = TextureWrapMode.Clamp;
                    if (j == 0)
                    {
                        Futile.atlasManager.UnloadAtlas("LevelTexture" + ((i != 0) ? i.ToString() : string.Empty));
                        Futile.atlasManager.LoadAtlasFromTexture("LevelTexture" + ((i != 0) ? i.ToString() : string.Empty), self.cameraTextures[i, j], false);
                    }
                    else
                    {
                        Futile.atlasManager.UnloadAtlas("BackgroundTexture" + ((i != 0) ? i.ToString() : string.Empty));
                        Futile.atlasManager.LoadAtlasFromTexture("BackgroundTexture" + ((i != 0) ? i.ToString() : string.Empty), self.cameraTextures[i, j], false);
                    }
                }
            }
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
                        int ncams = preferedSplitMode == SplitMode.Split4Screen ? 4 : 2;
                        Array.Resize(ref cams, ncams);
                        self.cameras = cams;
                        for(int i = 1; i < ncams; i++)
                        {
                            cams[i] = new RoomCamera(self, i);
                            if (i < self.session.Players.Count)
                                cams[i].followAbstractCreature = self.session.Players[i];
                            else
                                cams[i].followAbstractCreature = self.session.Players[0];
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
                for (int i = 0; i < self.cameras.Length; i++)
                {
                    //Logger.LogInfo($"RainWorldGame_ctor cam {self.cameras[i].cameraNumber} to p {(self.session.Players[i].state as PlayerState).playerNumber}");
                    if (i < self.session.Players.Count)
                        self.cameras[i].followAbstractCreature = self.session.Players[i];
                    else
                        self.cameras[i].followAbstractCreature = self.session.Players[0];
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
            curCamera = cameraNumber;
            orig(self, game, cameraNumber);
            curCamera = -1;
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

        struct RoomTarget : IEquatable<RoomTarget>
        {
            public int room;
            public int camNumber;

            public RoomTarget(int room, int camNumber)
            {
                this.room = room;
                this.camNumber = camNumber;
            }

            public bool Equals(RoomTarget other)
            {
                return room == other.room && camNumber == other.camNumber;
            }

            public override bool Equals(object obj)
            {
                return obj is RoomTarget other && Equals(other);
            }

            public override int GetHashCode()
            {
                return 1000 * room + camNumber;
            }
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
                bool splitTargets = self.cameras.Select(x => x.room != null ? new RoomTarget(x.room.abstractRoom.index, x.currentCameraPosition) : new RoomTarget()).Distinct().Count() != 1;
                if (CurrentSplitMode == SplitMode.NoSplit && preferedSplitMode != SplitMode.NoSplit && splitTargets)
                {
                    SetSplitMode(preferedSplitMode, self);
                }
                else if (CurrentSplitMode != SplitMode.NoSplit && !splitTargets && !alwaysSplit)
                {
                    SetSplitMode(SplitMode.NoSplit, self);
                }

                if (CurrentSplitMode != SplitMode.NoSplit && self.cameras[0].room != null && self.cameras[0].room.abstractRoom.name == "SB_L01") // honestly jolly
                {
                    ConsiderColapsing(self, false);
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

                            for (int i = 0; i < 4; i++)
                            {
                                fcameras[i].enabled = true;
                                cameraListeners[i].direct = false;
                                cameraListeners[i].SetMap(cameraSourcePositions[i], cameraTargetPositions[i]);
                                cameraZoomed[i] = false;
                            }
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
        public void ConsiderColapsing(RainWorldGame game, bool regionSwitch)
        {
            if (game.cameras.Length > 1)
            {
                if (!regionSwitch && (game.Players.Count == game.cameras.Length && alwaysSplit)) return; // I guess
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
            self.hud?.map?.inFrontContainer?.SetPosition(camOffsets[self.cameraNumber]); // map icons
        }

        /// <summary>
        /// Store screen bound camera to an extended field when constructor is called
        /// </summary>
        public void InitSinglePlayerHud(ILContext il)
        {
            try
            {
                var c = new ILCursor(il);
                c.GotoNext(MoveType.After,
                    i => i.MatchNewobj<JollyCoop.JollyHUD.JollyPlayerSpecificHud>(),
                    i => i.MatchStloc(2)
                    );

                c.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_1);
                c.Emit(Mono.Cecil.Cil.OpCodes.Ldloc_2);
                c.EmitDelegate<Action<RoomCamera, JollyCoop.JollyHUD.JollyPlayerSpecificHud>>((cam, self) =>
                {
                    self.SetSplitScreenCamera(cam);
                    return;
                });
                
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                throw;
            }
        }

        public void FoodMeter_Draw(On.HUD.FoodMeter.orig_Draw orig, HUD.FoodMeter self, float timeStacker)
        {
            var oldPos = self.pos;
            var oldLastPos = self.lastPos;
            RoomCamera cam = GetHUDPartCurrentCamera(self);
            if (cam != null)
            {
                var offset = GetGlobalHudOffset(cam);
                self.pos += offset;
                self.lastPos += offset;
            }
            orig(self, timeStacker);
            if (cam != null)
            {
                self.pos = oldPos;
                self.lastPos = oldLastPos;
            }
        }

        public void KarmaMeter_Draw(On.HUD.KarmaMeter.orig_Draw orig, HUD.KarmaMeter self, float timeStacker)
        {
            var oldPos = self.pos;
            var oldLastPos = self.lastPos;
            RoomCamera cam = GetHUDPartCurrentCamera(self);
            if (cam != null)
            {
                var offset = GetGlobalHudOffset(cam);
                self.pos += offset;
                self.lastPos += offset;
            }
            orig(self, timeStacker);
            if (cam != null)
            {
                self.pos = oldPos;
                self.lastPos = oldLastPos;
            }
        }

        public void RainMeter_Draw(On.HUD.RainMeter.orig_Draw orig, HUD.RainMeter self, float timeStacker)
        {
            List<Vector2> oldPoses = new List<Vector2>();
            List<Vector2> oldLastPoses = new List<Vector2>();
            RoomCamera cam = GetHUDPartCurrentCamera(self);
            if (cam != null)
            {
                var offset = GetGlobalHudOffset(cam);
                for (int i = 0; i < self.circles.Length; i++)
                {
                    oldPoses.Add(self.circles[i].pos);
                    oldLastPoses.Add(self.circles[i].lastPos);
                    self.circles[i].pos += offset;
                    self.circles[i].lastPos += offset;
                }
            }
            orig(self, timeStacker);
            if (cam != null)
            {
                for (int j = 0; j < self.circles.Length; j++)
                {
                    self.circles[j].pos = oldPoses[j];
                    self.circles[j].lastPos = oldLastPoses[j];
                }
            }
        }

        public void TextPrompt_Draw(On.HUD.TextPrompt.orig_Draw orig, HUD.TextPrompt self, float timeStacker)
        {
            orig(self, timeStacker);
            RoomCamera cam = GetHUDPartCurrentCamera(self);
            if (cam != null)
            {
                var offset = GetGlobalHudOffset(cam);
                // text
                if (self.label != null)
                {
                    self.label.x += offset.x;
                    self.label.y += offset.y;
                }
                if (self.musicSprite != null)
                {
                    self.musicSprite.x += offset.x;
                    self.musicSprite.y += offset.y;
                }
                // background overlay that appears from top and bottom of the screen
                if (self.sprites != null)
                {
                    if (self.sprites[0] != null)
                        self.sprites[0].y -= offset.y;
                    if (self.sprites.Length > 1 && self.sprites[1] != null)
                        self.sprites[1].y += offset.y;
                }
                for (int j = 0; j < self.symbols.Count; j++)
                {
                    var symbol = self.symbols[j];
                    if (symbol.symbolSprite != null)
                    {
                        self.symbols[j].symbolSprite.x += offset.x;
                        self.symbols[j].symbolSprite.y += offset.y;
                    }
                    if (symbol.shadowSprite1 != null)
                    {
                        self.symbols[j].shadowSprite1.x += offset.x;
                        self.symbols[j].shadowSprite1.y += offset.y;
                    }
                    if (symbol.shadowSprite2 != null)
                    {
                        self.symbols[j].shadowSprite2.x += offset.x;
                        self.symbols[j].shadowSprite2.y += offset.y;
                    }
                }
            }
        }

        public void HypothermiaMeter_Draw(On.MoreSlugcats.HypothermiaMeter.orig_Draw orig, MoreSlugcats.HypothermiaMeter self, float timeStacker)
        {
            List<Vector2> oldPoses = new List<Vector2>();
            List<Vector2> oldLastPoses = new List<Vector2>();
            RoomCamera cam = GetHUDPartCurrentCamera(self);
            if (cam != null)
            {
                var offset = GetGlobalHudOffset(cam);
                for (int i = 0; i < self.circles.Length; i++)
                {
                    oldPoses.Add(self.circles[i].pos);
                    oldLastPoses.Add(self.circles[i].lastPos);
                    self.circles[i].pos += offset;
                    self.circles[i].lastPos += offset;
                }
            }
            orig(self, timeStacker);
            if (cam != null)
            {
                for (int j = 0; j < self.circles.Length; j++)
                {
                    self.circles[j].pos = oldPoses[j];
                    self.circles[j].lastPos = oldLastPoses[j];
                }
            }
        }

        public RoomCamera GetHUDPartCurrentCamera(HUD.HudPart hudPart)
        {
            if (curCamera == -1)
                return null;
            var loop = hudPart.hud.rainWorld.processManager.currentMainLoop;
            if (loop is RainWorldGame)
                return ((RainWorldGame)loop).cameras[curCamera];
            return null;
        }

        public void JollyOffRoom_Update1(ILContext il)
        {
            try
            {
                var c = new ILCursor(il);
                c.GotoNext(MoveType.After,
                    i => i.MatchLdfld<JollyCoop.JollyHUD.JollyPlayerSpecificHud.JollyPointer>("screenEdge")
                    );
                c.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
                c.EmitDelegate<Func<int, JollyCoop.JollyHUD.JollyPlayerSpecificHud.JollyOffRoom, int>>((returnValue, self) =>
                {
                    if (!cameraZoomed[self.jollyHud.Camera.cameraNumber])
                        return returnValue + (int)GetRelativeSplitScreenOffset(self.jollyHud.Camera).x;
                    else
                        return returnValue;
                });
                c.Index++;

                c.GotoNext(MoveType.After,
                    i => i.MatchLdfld<JollyCoop.JollyHUD.JollyPlayerSpecificHud.JollyPointer>("screenEdge")
                    );
                c.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
                c.EmitDelegate<Func<int, JollyCoop.JollyHUD.JollyPlayerSpecificHud.JollyOffRoom, int>>((returnValue, self) =>
                {
                    if (!cameraZoomed[self.jollyHud.Camera.cameraNumber])
                        return returnValue + (int)GetRelativeSplitScreenOffset(self.jollyHud.Camera).x;
                    else
                        return returnValue;
                });
                c.Index++;

                c.GotoNext(MoveType.After,
                    i => i.MatchLdfld<JollyCoop.JollyHUD.JollyPlayerSpecificHud.JollyPointer>("screenEdge")
                    );
                c.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
                c.EmitDelegate<Func<int, JollyCoop.JollyHUD.JollyPlayerSpecificHud.JollyOffRoom, int>>((returnValue, self) =>
                {
                    if (!cameraZoomed[self.jollyHud.Camera.cameraNumber])
                        return returnValue + (int)GetRelativeSplitScreenOffset(self.jollyHud.Camera).y;
                    else
                        return returnValue;
                });
                c.Index++;

                c.GotoNext(MoveType.After,
                    i => i.MatchLdfld<JollyCoop.JollyHUD.JollyPlayerSpecificHud.JollyPointer>("screenEdge")
                    );
                c.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
                c.EmitDelegate<Func<int, JollyCoop.JollyHUD.JollyPlayerSpecificHud.JollyOffRoom, int>>((returnValue, self) =>
                {
                    if (!cameraZoomed[self.jollyHud.Camera.cameraNumber])
                        return returnValue + (int)GetRelativeSplitScreenOffset(self.jollyHud.Camera).y;
                    else
                        return returnValue;
                });

                // Allow icons to appear when other slugcats are in the same room, but not on screen
                c.GotoNext(MoveType.After,
                    i => i.MatchCallvirt<JollyCoop.JollyHUD.JollyPlayerSpecificHud>("get_PlayerRoomBeingViewed")
                    );
                c.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
                c.EmitDelegate<Func<bool, JollyCoop.JollyHUD.JollyPlayerSpecificHud.JollyOffRoom, bool>>((returnValue, self) =>
                {
                    if (self.jollyHud.Camera == null || self.jollyHud.RealizedPlayer == null || self.jollyHud.RealizedPlayer.abstractCreature == null)
                    {
                        return true;
                    }
                    // if result == true - hide slugcat icon
                    var followedCreature = self.jollyHud.Camera.followAbstractCreature;
                    if (followedCreature == self.jollyHud.RealizedPlayer.abstractCreature)
                        return returnValue;
                    if (returnValue)
                    {
                        if (followedCreature == null || followedCreature.realizedCreature == null || followedCreature.Room == null)
                        {
                            return true;
                        }
                        var followedPos = followedCreature.world.RoomToWorldPos(followedCreature.realizedCreature.mainBodyChunk.pos, followedCreature.Room.index);
                        var distanceX = Math.Abs(self.playerPos.x - followedPos.x);
                        var distanceY = Math.Abs(self.playerPos.y - followedPos.y);

                        var magicNumber = 2.6f; // otherwise slugcat icons are sometimes placed weirdly
                        if (cameraZoomed[self.jollyHud.Camera.cameraNumber])
                        {
                            if (distanceX > (self.jollyHud.Camera.sSize.x) || distanceY > (self.jollyHud.Camera.sSize.y))
                            {
                                returnValue = false;
                            }
                        }
                        else if (distanceX > Math.Abs(self.jollyHud.Camera.sSize.x - magicNumber * GetRelativeSplitScreenOffset(self.jollyHud.Camera).x) ||
                                distanceY > Math.Abs(self.jollyHud.Camera.sSize.y - magicNumber * GetRelativeSplitScreenOffset(self.jollyHud.Camera).y))
                        {
                            returnValue = false;
                        }
                    }
                    return returnValue;
                });
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                throw;
            }
        }

        /// <summary>
        /// Show other slugcat icons on the map even when they are in different rooms
        /// </summary>
        public void HudMap_Draw(ILContext il)
        {
            try
            {
                var c = new ILCursor(il);

                // Make a list of creatures to show icons for (including other slugcat rooms)
                List<AbstractCreature> creatures = new List<AbstractCreature>();
                c.GotoNext(MoveType.After,
                  i => i.MatchLdarg(0),
                  i => i.MatchLdfld<HUD.HudPart>("hud"),
                  i => i.MatchLdfld<HUD.HUD>("owner")
                  );
                c.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
                c.EmitDelegate<Action<HUD.Map>>((self) =>
                {
                    List<AbstractCreature> tempCreatures = new List<AbstractCreature>();
                    if(!(self.hud.rainWorld.processManager.currentMainLoop is RainWorldGame))
                    {
                        return;
                    }
                    for (int m = 0; m < ((RainWorldGame)self.hud.rainWorld.processManager.currentMainLoop).session.Players.Count; m++)
                    {
                        Creature cr = ((RainWorldGame)self.hud.rainWorld.processManager.currentMainLoop).session.Players[m].realizedCreature;

                        if (cr == null || cr.room == null)
                        {
                            continue;
                        }

                        List<AbstractCreature> roomCreatures = cr.room.abstractRoom.creatures;
                        for (int n = 0; n < roomCreatures.Count; n++)
                        {
                            tempCreatures.Add(roomCreatures[n]);
                        }
                    }
                    creatures = tempCreatures.Distinct().ToList(); // remove duplicates
                });

                // saving the value of loop iterator
                c.GotoNext(MoveType.After,
                    i => i.MatchCallOrCallvirt<Room>("get_abstractRoom"),
                    i => i.MatchLdfld<AbstractRoom>("creatures")
                    );

                c.Index++;
                int counter = 0;
                c.EmitDelegate<Func<int, int>>((stackVal) =>
                {
                    counter = stackVal;
                    return 0; // replacing with 0 so original calls don't go oob
                });

                // accessing our assembled creature list
                c.Index++;
                c.EmitDelegate<Func<AbstractCreature, AbstractCreature>>((stackVal) =>
                {
                    return creatures[counter];
                });

                // disable vanishing of slugcat icons because of distance
                c.GotoNext(MoveType.Before,
                   i => i.MatchCallOrCallvirt<AbstractWorldEntity>("get_Room"),
                   i => i.MatchLdfld<AbstractRoom>("index"),
                   i => i.MatchLdarg(1)
                   );

                AbstractCreature cr = null;
                c.EmitDelegate<Func<AbstractWorldEntity, AbstractWorldEntity>>((stackVal) =>
                {
                    cr = (AbstractCreature)stackVal;
                    return stackVal;
                });

                c.GotoNext(MoveType.After,
                   i => i.MatchCallOrCallvirt<UnityEngine.Mathf>("InverseLerp")
                   );

                c.EmitDelegate<Func<float, float>>((stackVal) =>
                {
                    if (cr.creatureTemplate.type == CreatureTemplate.Type.Slugcat)
                        return 1f;
                    return stackVal;
                });

                // changing loop bounds
                c.GotoNext(MoveType.After,
                    i => i.MatchCallOrCallvirt<Room>("get_abstractRoom"),
                    i => i.MatchLdfld<AbstractRoom>("creatures")
                    );

                c.Index += 1;
                c.EmitDelegate<Func<int, int>>((stackVal) =>
                {
                    return creatures.Count;
                });

                // clearing creature list
                c.GotoNext(MoveType.After,
                    i => i.MatchLdarg(0),
                    i => i.MatchLdfld<HUD.Map>("visible"),
                    i => i.MatchBrtrue(out _)
                    );

                c.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
                c.EmitDelegate<Action<HUD.Map>>((self) =>
               {
                   creatures.Clear();
               });
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                throw;
            }
        }

        public void ToggleCameraZoom(RoomCamera cam)
        {
            SetCameraZoom(cam, !cameraZoomed[cam.cameraNumber]);
        }

        public void SetCameraZoom(RoomCamera cam, bool enabled)
        {
            var camNum = cam.cameraNumber;
            cameraZoomed[camNum] = enabled;
            if (enabled)
            {
                cameraListeners[camNum].SetMap(new Rect(0f, 0f, 1f, 1f), cameraTargetPositions[camNum]);
            }
            else
            {
                cameraListeners[camNum].SetMap(cameraSourcePositions[camNum], cameraTargetPositions[camNum]);
            }
            OffsetHud(cam);
        }

        public Vector2 GetGlobalHudOffset(RoomCamera camera)
        {
            if (!cameraZoomed[camera.cameraNumber])
                return GetRelativeSplitScreenOffset(camera);
            return new Vector2(0, 0);
        }

        public Vector2 GetSplitScreenHudOffset(RoomCamera camera, int cameraNumber)
        {
            Vector2 offset = camOffsets[cameraNumber];
            if (!cameraZoomed[camera.cameraNumber])
                offset += GetRelativeSplitScreenOffset(camera);
            return offset;
        }

        public Vector2 GetRelativeSplitScreenOffset(RoomCamera camera)
        {
            Vector2 offset = new Vector2();
            if (CurrentSplitMode == SplitMode.SplitHorizontal)
            {
                offset = new Vector2(0, camera.sSize.y / 4f);
            }
            else if (CurrentSplitMode == SplitMode.SplitVertical)
            {
                offset = new Vector2(camera.sSize.x / 4f, 0f);
            }
            else if (CurrentSplitMode == SplitMode.Split4Screen)
            {
                offset = new Vector2(camera.sSize.x / 4f, camera.sSize.y / 4f);
            }
            return offset;
        }

        public static void CustomDecal_ctor(On.CustomDecal.orig_ctor orig, CustomDecal self, PlacedObject placedObject)
        {
            orig(self, placedObject);
            for (int i = 0; i < 4; i++)
            {
                self.SetMeshDirty(i, self.meshDirty);
                self.SetElementDirty(i, self.elementDirty);
            }
        }
		
        public static void CustomDecal_DrawSprites(On.CustomDecal.orig_DrawSprites orig, CustomDecal self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            int cameraNumber = rCam.cameraNumber;
            self.meshDirty = self.IsMeshDirty(cameraNumber);
            self.elementDirty = self.IsElementDirty(cameraNumber);

            orig(self, sLeaser, rCam, timeStacker, camPos);

            self.SetMeshDirty(cameraNumber, self.meshDirty);
            self.SetElementDirty(cameraNumber, self.elementDirty);
        }

        public static void CustomDecal_InitiateSprites(On.CustomDecal.orig_InitiateSprites orig, CustomDecal self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            orig(self, sLeaser, rCam);
            self.SetMeshDirty(rCam.cameraNumber, self.meshDirty);
        }

        public static void CustomDecal_Update(On.CustomDecal.orig_Update orig, CustomDecal self, bool eu)
        {
            orig(self, eu);
            for (int i = 0; i < 4; i++)
                self.SetMeshDirty(i, self.meshDirty);
        }

        public static void CustomDecal_UpdateMesh(On.CustomDecal.orig_UpdateMesh orig, CustomDecal self)
        {
            orig(self);
            for (int i = 0; i < 4; i++)
                self.SetMeshDirty(i, self.meshDirty);
        }

        public static void CustomDecal_UpdateAsset(On.CustomDecal.orig_UpdateAsset orig, CustomDecal self)
        {
            orig(self);
            for (int i = 0; i < 4; i++)
                self.SetElementDirty(i, self.meshDirty);
        }

        public Vector4[] SnowSource_PackSnowData(On.MoreSlugcats.SnowSource.orig_PackSnowData orig, MoreSlugcats.SnowSource self)
        {
            Vector2 vector = self.room.cameraPositions[self.room.game.cameras[curCamera].currentCameraPosition];
            Vector4[] array = new Vector4[3];
            Vector2 vector2 = RWCustom.Custom.EncodeFloatRG((self.pos.x - vector.x) / 1400f * 0.3f + 0.3f);
            Vector2 vector3 = RWCustom.Custom.EncodeFloatRG((self.pos.y - vector.y) / 800f * 0.3f + 0.3f);
            Vector2 vector4 = RWCustom.Custom.EncodeFloatRG(self.rad / 1600f);
            array[0] = new Vector4(vector2.x, vector2.y, vector3.x, vector3.y);
            array[1] = new Vector4(vector4.x, vector4.y, self.intensity, self.noisiness);
            array[2] = new Vector4(0f, 0f, 0f, (float)((int)self.shape) / 5f);
            return array;
        }

        public void SnowSource_Update(On.MoreSlugcats.SnowSource.orig_Update orig, MoreSlugcats.SnowSource self, bool eu)
        {
            bool camChanged = false;
            for (int i = 0; i < self.room.game.cameras.Length; i++)
            {
                RoomCamera cam = self.room.game.cameras[i];
                if (cam.room == self.room)
                {
                    if (self.GetLastCamPos(i) != cam.currentCameraPosition)
                    {
                        camChanged = true;
                        break;
                    }
                }
            }

            bool flag = false;
            if (camChanged || self.shape != self.lastShape || self.noisiness != self.lastNoisiness || self.intensity != self.lastIntensity || self.pos != self.lastPos || self.rad != self.lastRad || (self.room.BeingViewed && self.visibility == 2))
            {
                flag = true;
            }
            if (flag && self.room.snow && self.room.BeingViewed)
            {
                int finalVisibility = self.visibility;
                for (int j = 0; j < self.room.game.cameras.Length; j++)
                {
                    RoomCamera cam = self.room.game.cameras[j];
                    if (cam.room != self.room)
                        continue;
                    int vis = self.CheckVisibility(cam.currentCameraPosition);
                    if (vis != 0)
                        finalVisibility = vis;
                    cam.snowChange = true;
                }
                self.visibility = finalVisibility;
            }

            for (int i = 0; i < self.room.game.cameras.Length; i++)
            {
                RoomCamera cam = self.room.game.cameras[i];
                self.SetLastCamPos(cam.cameraNumber, cam.currentCameraPosition);
            }

            self.lastPos = self.pos;
            self.lastRad = self.rad;
            self.lastIntensity = self.intensity;
            self.lastNoisiness = self.noisiness;
            self.lastShape = self.shape;
        }

        public void DustPuff_ApplyPalette(On.RoofTopView.DustpuffSpawner.DustPuff.orig_ApplyPalette orig, RoofTopView.DustpuffSpawner.DustPuff self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            if (rCam.room.snow)
            {
                int camNum = curCamera;
                if (camNum == -1)
                {
                    for (int i = 0; i < rCam.room.game.cameras.Length; i++)
                    {
                        RoomCamera cam = rCam.room.game.cameras[i];
                        if (rCam.room == cam.room)
                        {
                            camNum = i;
                            break;
                        }
                    }
                }
                Vector2 vector = self.pos - rCam.room.cameraPositions[rCam.room.game.cameras[camNum].currentCameraPosition];
                sLeaser.sprites[0].color = new Color(vector.x / 1400f, vector.y / 800f, 0f);
            }
            else
                orig(self, sLeaser, rCam, palette);
        }

        public static void BlizzardGraphics_TileTexUpdate(On.MoreSlugcats.BlizzardGraphics.orig_TileTexUpdate orig, MoreSlugcats.BlizzardGraphics self)
        {
            if (cameraListeners[curCamera] is CameraListener l)
            {
                l.OnPreRender();
            }
            orig(self);
        }
    }

    public static class JollyHUDExtension
    {
        public class SplitScreenCamera
        {
            public RoomCamera cam;
        }

        private static readonly ConditionalWeakTable<JollyCoop.JollyHUD.JollyPlayerSpecificHud, SplitScreenCamera> _cwt = new();
        public static RoomCamera GetSplitScreenCamera(this JollyCoop.JollyHUD.JollyPlayerSpecificHud hud)
        {
            return _cwt.GetValue(hud, _cwt => new()).cam;
        }
        public static RoomCamera SetSplitScreenCamera(this JollyCoop.JollyHUD.JollyPlayerSpecificHud hud, RoomCamera cam)
        {
            return _cwt.GetValue(hud, _cwt => new()).cam = cam;
        }
    }

    public static class CustomDecalExtension
    {
        public class SplitScreenCustomDecal
        {
            public bool[] meshDirty = new bool[4];
            public bool[] elementDirty = new bool[4];
        }

        private static readonly ConditionalWeakTable<CustomDecal, SplitScreenCustomDecal> _cwt = new();
        public static bool IsMeshDirty(this CustomDecal decal, int cameraNumber)
        {
            return _cwt.GetValue(decal, _cwt => new()).meshDirty[cameraNumber];
        }
        public static bool SetMeshDirty(this CustomDecal decal, int cameraNumber, bool isDirty)
        {
            return _cwt.GetValue(decal, _cwt => new()).meshDirty[cameraNumber] = isDirty;
        }
        public static bool IsElementDirty(this CustomDecal decal, int cameraNumber)
        {
            return _cwt.GetValue(decal, _cwt => new()).elementDirty[cameraNumber];
        }
        public static bool SetElementDirty(this CustomDecal decal, int cameraNumber, bool isDirty)
        {
            return _cwt.GetValue(decal, _cwt => new()).elementDirty[cameraNumber] = isDirty;
        }
    }

    public static class SnowSourceExtension
    {
        public class SplitScreenSnowSource
        {
            public int[] lastCam = new int[4];
        }

        private static readonly ConditionalWeakTable<MoreSlugcats.SnowSource, SplitScreenSnowSource> _cwt = new();
        public static int SetLastCamPos(this MoreSlugcats.SnowSource ss, int cameraNumber, int val)
        {
            return _cwt.GetValue(ss, _cwt => new()).lastCam[cameraNumber] = val;
        }
        public static int GetLastCamPos(this MoreSlugcats.SnowSource ss, int cameraNumber)
        {
            return _cwt.GetValue(ss, _cwt => new()).lastCam[cameraNumber];
        }
    }
}
