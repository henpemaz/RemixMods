using System;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;


[module: UnverifiableCode]
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace LapMod
{
    [BepInPlugin("com.henpemaz.lapmod", "Lap Mod", "0.1.3")]

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
                On.RainWorldGame.Update += RainWorldGame_UpdateHook;
                On.RoomCamera.ClearAllSprites += RoomCamera_ClearAllSprites;
                On.RainWorldGame.ctor += RainWorldGame_ctor;
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

        private static TimeSpan defaultTime;

        private Player player;

        private static TimeSpan totalTimeTracker;
        private static String timeString;

        private int wantsNextRoomCounter = 0;
        public static bool wantsNextRoom = false;

        private static int enteredFromNode = -1;
        private static int timerWhenEntered = 0;

        private static TimeSpan time1;
        private static TimeSpan time2;
        public static TimeSpan timeDiff;

        private static bool isNewRoom = false;

        private void RainWorldGame_ctor(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
        {
            orig(self, manager);
            if (!self.IsArenaSession)
            {
                Panel.Initialize();
                // Reset times on new room/campaign
                time1 = defaultTime;
                time2 = defaultTime;
                timeDiff = defaultTime;
            }
        }

        private static void RoomCamera_ClearAllSprites(On.RoomCamera.orig_ClearAllSprites orig, RoomCamera self)
        {
            orig(self);
            if (Panel.initialized)
            {
                Panel.Remove();
            }
        }

        public void PlayerUpdateHook(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);
            if (Panel.initialized)
            {
                player = self;
            }
        }

        private void RainWorldGame_UpdateHook(On.RainWorldGame.orig_Update orig, RainWorldGame self)
        {
            if (Panel.initialized)
            {
                Panel.Update();
                if (player != null)
                {
                    MoreSlugcats.SpeedRunTimer.CampaignTimeTracker timeTracker = MoreSlugcats.SpeedRunTimer.GetCampaignTimeTracker(player.abstractCreature.world.game.GetStorySession.saveStateNumber);
                    totalTimeTracker = timeTracker.TotalFreeTimeSpan;
                    timeString = totalTimeTracker.ToString("mm'm:'ss's:'fff'ms'");
                }

                KeyCode passthroughKey = LapModRemix.roomPassthroughKey.Value;
                KeyCode resetKey = LapModRemix.resetKey.Value;
                if (Input.GetKey(passthroughKey) && wantsNextRoomCounter == 0)
                {
                    wantsNextRoom = !wantsNextRoom;
                    wantsNextRoomCounter = 12; // ~1/3 sec before another press is accepted
                    Debug("Lap Mod: Player wants next room set to " + wantsNextRoom.ToString());
                }
                else if (wantsNextRoomCounter > 0)
                {
                    wantsNextRoomCounter--;
                }
            }
            orig(self);
        }

        static void ShortcutHandler_Update(On.ShortcutHandler.orig_Update orig, ShortcutHandler self)
        {
            if (Panel.initialized)
            {
                for (int i = self.transportVessels.Count - 1; i >= 0; i--)
                {
                    if (self.transportVessels[i].room.realizedRoom != null && self.transportVessels[i].creature is Player) // Found Player
                    {
                        if (self.transportVessels[i].wait <= 0) // About to move
                        {
                            Room realizedRoom = self.transportVessels[i].room.realizedRoom;
                            RWCustom.IntVector2 pos = ShortcutHandler.NextShortcutPosition(self.transportVessels[i].pos, self.transportVessels[i].lastPos, realizedRoom);
                            
                            if (realizedRoom.GetTile(pos).shortCut == 2) // About to exit
                            {
                                GetTimes();
                                int num = Array.IndexOf<RWCustom.IntVector2>(realizedRoom.exitAndDenIndex, pos);
                                if (!wantsNextRoom && enteredFromNode > -1 && !self.transportVessels[i].room.shelter && !self.transportVessels[i].room.gate && self.transportVessels[i].room.connections.Length > 1) // Looping
                                {
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
        private void SpitOutOfShortCutHook(On.Player.orig_SpitOutOfShortCut orig, Player self, RWCustom.IntVector2 pos, Room newRoom, bool spitOutAllSticks)
        {
            if (Panel.initialized)
            {
                if (isNewRoom)
                {
                    Debug("Lap Mod: Entering room at " + timeString);
                    time1 = totalTimeTracker;
                }
                isNewRoom = false;
            }
            orig(self, pos, newRoom, spitOutAllSticks);
        }

        private static void GetTimes()
        {
            isNewRoom = true;
            time2 = totalTimeTracker;
            timeDiff = time2.Subtract(time1);
            Debug("LapMod: Exiting/looping room at: " + timeString);
            if (timeDiff != null)
            {
                Debug("Lap Mod: Total room time " + timeDiff.ToString("mm'm:'ss's:'fff'ms'"));
            }
        }
    }
}
