using System;

using System.Security;
using System.Security.Permissions;
using BepInEx;
using BepInEx.Logging;
using IL.RWCustom;
using UnityEngine;


// TODO: DISTINGUISH EXITING A REGULAR SHORTCUT AND AN EXITING SHORTCUT

[module: UnverifiableCode]
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace LapMod
{
    [BepInPlugin("com.henpemaz.lapmod", "Lap Mod", "0.1.1")]

    public class LapMod : BaseUnityPlugin
    {
        public void OnEnable()
        {
            Logger.LogInfo("OnEnable");
            On.RainWorld.OnModsInit += OnModsInit;
            On.RainWorld.PostModsInit += RainWorld_PostModsInit;
            sLogger = Logger;
        }


        public bool init;
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
                On.ShortcutHandler.Update += ShortcutHandler_Update;
                On.Player.Update += PlayerUpdateHook;
                On.Player.SpitOutOfShortCut += SpitOutOfShortCutHook;
                On.RainWorldGame.RawUpdate += RainWorldGame_RawUpdate;
                On.RoomCamera.ctor += RoomCamera_ctor;
                On.RoomCamera.ClearAllSprites += RoomCamera_ClearAllSprites;
                On.RainWorldGame.GrafUpdate += RainWorldGame_GrafUpdate;
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
                MachineConnector.SetRegisteredOI("henpemaz_lapmod", LapModRemix.instance);
            }
        }

        private void RainWorld_PostModsInit(On.RainWorld.orig_PostModsInit orig, RainWorld self)
        {
            orig(self);
            try
            {
                Logger.LogInfo("PostModsInit");

            }
            catch (Exception e)
            {
                Logger.LogError(e);
                throw;
            }
        }

        private static void RoomCamera_ctor(On.RoomCamera.orig_ctor orig, RoomCamera self, RainWorldGame game,
            int cameraNumber)
        {
            orig(self, game, cameraNumber);
            Panel.Initialize();
        }

        private static void RoomCamera_ClearAllSprites(On.RoomCamera.orig_ClearAllSprites orig, RoomCamera self)
        {
            Panel.Remove();
            orig(self);
        }

        private static void RainWorldGame_GrafUpdate(On.RainWorldGame.orig_GrafUpdate orig, RainWorldGame self,
            float timeStacker)
        {
            orig(self, timeStacker);
            Panel.Update();
        }


        private Player player;

        static TimeSpan totalTimeTracker;
        static String timeString;

        static int wantsNextRoomCounter = 0;
        public static bool wantsNextRoom = false;

        static int enteredFromNode = -1;
        static int timerWhenEntered = 0;


        public void PlayerUpdateHook(On.Player.orig_Update orig, Player self, bool eu)
        {
            player = self;
            orig(self, eu);
        }

        private void RainWorldGame_RawUpdate(On.RainWorldGame.orig_RawUpdate orig, RainWorldGame self, float dt)
        {
            if (player != null)
            {
                MoreSlugcats.SpeedRunTimer.CampaignTimeTracker timeTracker = MoreSlugcats.SpeedRunTimer.GetCampaignTimeTracker(player.abstractCreature.world.game.GetStorySession.saveStateNumber);
                totalTimeTracker = timeTracker.TotalFreeTimeSpan;
                timeString = totalTimeTracker.ToString("mm'm:'ss's:'fff'ms'");
            }

            KeyCode passthroughKey = LapModRemix.roomPassthroughKey.Value;
            orig(self, dt);
            if (Input.GetKey(passthroughKey) && wantsNextRoomCounter == 0)
            {
                wantsNextRoom = !wantsNextRoom;
                wantsNextRoomCounter = 60;
                Debug("Lap Mod: Player wants next room set to " + wantsNextRoom.ToString());
            }
            else if (wantsNextRoomCounter > 0)
            {
                wantsNextRoomCounter--;
            }
        }


        static TimeSpan time1;
        static TimeSpan time2;
        public static TimeSpan timeDiff;
        static bool isNewRoom = false;

        private void SpitOutOfShortCutHook(On.Player.orig_SpitOutOfShortCut orig, Player self, RWCustom.IntVector2 pos, Room newRoom, bool spitOutAllSticks)
        {
            if (isNewRoom)
            {
                Debug("Lap Mod: Entering room at " + timeString);
                time1 = totalTimeTracker;
            }
            isNewRoom = false;
            orig(self, pos, newRoom, spitOutAllSticks);
        }


        static void ShortcutHandler_Update(On.ShortcutHandler.orig_Update orig, ShortcutHandler self)
        {
            if (!self.game.IsArenaSession)
            {
                for (int i = self.transportVessels.Count - 1; i >= 0; i--)
                {
                    if (self.transportVessels[i].room.realizedRoom != null && self.transportVessels[i].creature is Player) // Found Player
                    {
                        //Debug.Log("found player in pipe");
                        if (self.transportVessels[i].wait <= 0) // About to move
                        {
                            //Debug.Log("about to move");
                            Room realizedRoom = self.transportVessels[i].room.realizedRoom;
                            RWCustom.IntVector2 pos = ShortcutHandler.NextShortcutPosition(self.transportVessels[i].pos, self.transportVessels[i].lastPos, realizedRoom);
                            if (realizedRoom.GetTile(pos).shortCut == 2) // About to exit
                            {
                                //Debug.Log("about to exit");
                                // Looping back
                                isNewRoom = true;
                                time2 = totalTimeTracker;
                                timeDiff = time2.Subtract(time1);
                                Debug("LapMod: Exiting room at: " + timeString);
                                if (timeDiff != null)
                                {
                                    Debug("Lap Mod: Total room time " + timeDiff.ToString("mm'm:'ss's:'fff'ms'"));
                                }

                                int num = Array.IndexOf<RWCustom.IntVector2>(realizedRoom.exitAndDenIndex, pos);
                                if (!wantsNextRoom && enteredFromNode > -1 && !self.transportVessels[i].room.shelter && !self.transportVessels[i].room.gate && self.transportVessels[i].room.connections.Length > 1)
                                {
                                    //Debug.Log("looping");
                                    realizedRoom.PlaySound(SoundID.Player_Tick_Along_In_Shortcut, 0f, 1f, 1f);

                                    self.transportVessels[i].PushNewLastPos(self.transportVessels[i].pos);
                                    self.transportVessels[i].pos = pos;

                                    self.transportVessels[i].creature.abstractCreature.pos.abstractNode = num;
                                    Debug("LapMod: redirecting vessel at " + timeString);
                                    if (self.transportVessels[i].room.connections.Length > 0)
                                    {
                                        if (num >= self.transportVessels[i].room.connections.Length)
                                        {
                                            self.transportVessels[i].PushNewLastPos(self.transportVessels[i].pos);
                                            self.transportVessels[i].pos = pos;
                                            Debug("faulty room exit");
                                        }
                                        else
                                        {
                                            int num2 = self.transportVessels[i].room.connections[num];
                                            if (num2 <= -1)
                                            {
                                                break; // Huh
                                            }
                                            self.transportVessels[i].entranceNode = enteredFromNode;
                                            self.game.world.rainCycle.timer = timerWhenEntered;
                                            //instance.transportVessels[i].room = instance.game.world.GetAbstractRoom(num2);
                                            self.betweenRoomsWaitingLobby.Add(self.transportVessels[i]);
                                        }
                                    }
                                    self.transportVessels.RemoveAt(i);
                                }
                                else // About to enter new room, store info
                                {
                                    if (num < self.transportVessels[i].room.connections.Length)
                                    {
                                        int num2 = self.transportVessels[i].room.connections[num];
                                        enteredFromNode = self.game.world.GetAbstractRoom(num2).ExitIndex(self.transportVessels[i].room.index);
                                        timerWhenEntered = self.game.world.rainCycle.timer;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            orig(self);
        }
    }
}
