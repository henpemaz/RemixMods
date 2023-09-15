using System;

using System.Security;
using System.Security.Permissions;
using BepInEx;
using BepInEx.Logging;

[module: UnverifiableCode]
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace LapMod
{
    [BepInPlugin("com.henpemaz.lapmod", "Lap Mod", "0.1.0")]

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
                On.RainWorldGame.RawUpdate += RainWorldGame_RawUpdate;
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

            }
            catch (Exception e)
            {
                Logger.LogError(e);
                throw;
            }
        }

        static int wantsNextRoomCounter = 0;
        static int enteredFromNode = -1;
        static int timerWhenEntered = 0;

        private void RainWorldGame_RawUpdate(On.RainWorldGame.orig_RawUpdate orig, RainWorldGame self, float dt)
        {
            orig(self, dt);
            if (RWInput.CheckSpecificButton(0, 11, self.rainWorld)) // god I love magic numbers if only they invented something like enums or what have you
            {
                Debug("LapMod: player wants out!");
                wantsNextRoomCounter = 60;
            }
            else if (wantsNextRoomCounter > 0)
            {
                wantsNextRoomCounter--;
            }
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
                                int num = Array.IndexOf<RWCustom.IntVector2>(realizedRoom.exitAndDenIndex, pos);
                                if (wantsNextRoomCounter <= 0 && enteredFromNode > -1 && !self.transportVessels[i].room.shelter && !self.transportVessels[i].room.gate && self.transportVessels[i].room.connections.Length > 1)
                                {
                                    //Debug.Log("looping");
                                    realizedRoom.PlaySound(SoundID.Player_Tick_Along_In_Shortcut, 0f, 1f, 1f);

                                    self.transportVessels[i].PushNewLastPos(self.transportVessels[i].pos);
                                    self.transportVessels[i].pos = pos;

                                    self.transportVessels[i].creature.abstractCreature.pos.abstractNode = num;

                                    Debug("LapMod: redirecting vessel");
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
                                    Debug("LapMod: passing through");
                                    if (num < self.transportVessels[i].room.connections.Length)
                                    {
                                        int num2 = self.transportVessels[i].room.connections[num];
                                        enteredFromNode = self.game.world.GetAbstractRoom(num2).ExitIndex(self.transportVessels[i].room.index);
                                        timerWhenEntered = self.game.world.rainCycle.timer;
                                        wantsNextRoomCounter = 0;
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
