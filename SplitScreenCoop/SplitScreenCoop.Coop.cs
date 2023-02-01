using System;
using System.Linq;
using UnityEngine;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace SplitScreenCoop
{
    public partial class SplitScreenCoop
    {
        /// <summary>
        /// Creates extra RoomRealizer for p2
        /// </summary>
        private void MakeRealizer2(RainWorldGame self)
        {
            if (self.session.Players.Count < 2 || self.roomRealizer == null) return;
            var player = self.session.Players.FirstOrDefault(p => p != self.roomRealizer.followCreature);
            if (player == null) return;
            realizer2 = new RoomRealizer(self.session.Players.First(p => p != self.roomRealizer.followCreature), self.world)
            {
                realizedRooms = self.roomRealizer.realizedRooms,
                recentlyAbstractedRooms = self.roomRealizer.recentlyAbstractedRooms,
                realizeNeighborCandidates = self.roomRealizer.realizeNeighborCandidates
            };
        }

        /// <summary>
        /// Realizer2 in new world
        /// </summary>
        public void OverWorld_WorldLoaded(On.OverWorld.orig_WorldLoaded orig, OverWorld self)
        {
            orig(self);
            MakeRealizer2(self.game);
        }

        /// <summary>
        /// Room realizers that aren't the main one skip re-assigning themselves to cameras[0].followcreature
        /// </summary>
        public void RoomRealizer_Update(ILContext il)
        {
            try
            {
                // skip this.followCreature = cam[0].followCreature if this != game.roomRealizer
                var c = new ILCursor(il);
                c.GotoNext(MoveType.Before,
                    i => i.MatchStfld<RoomRealizer>("followCreature"),
                    i => i.MatchLdarg(0),
                    i => i.MatchLdfld<RoomRealizer>("followCreature"));
                c.Index++;
                c.MoveAfterLabels();
                var skip = c.MarkLabel();
                c.GotoPrev(MoveType.Before,
                    i => i.MatchLdarg(0),
                    i => i.MatchLdarg(0),
                    i => i.MatchLdfld<RoomRealizer>("world"));
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((RoomRealizer self) =>
                {
                    if (self != self.world?.game?.roomRealizer)
                    {
                        return true;
                    }
                    return false;
                });
                c.Emit(OpCodes.Brtrue, skip);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                throw;
            }
        }

        public bool rrNestedLock;
        /// <summary>
        /// Realizers work together
        /// </summary>
        public bool RoomRealizer_CanAbstractizeRoom(On.RoomRealizer.orig_CanAbstractizeRoom orig, RoomRealizer self, RoomRealizer.RealizedRoomTracker tracker)
        {
            var r = orig(self, tracker);
            if (!rrNestedLock && realizer2 != null) // if other exists, not recursive
            {
                RoomRealizer other;
                RoomRealizer prime = self?.world?.game?.roomRealizer;
                if (prime == self) other = realizer2;
                else other = prime;
                if (other != null)
                {
                    rrNestedLock = true;
                    r = r && other.CanAbstractizeRoom(tracker);
                    rrNestedLock = false;
                }
            }
            return r;
        }
        
        public void ShelterDoor_Close(On.ShelterDoor.orig_Close orig, ShelterDoor self)
        {
            if (selfSufficientCoop && !sheltersClose) return;
            orig(self);
        }


        public static bool coopSharedFood = true;
        public static bool sheltersClose;

        public void CoopUpdate(RainWorldGame game)
        {
            UpdatePlayerFood(game);

            sheltersClose = false;

            if (SheltersCanClose(game))
            {
                sheltersClose = true;
                CloseShelters(game);
                sheltersClose = false;
            }
        }

        private void CloseShelters(RainWorldGame game)
        {
            foreach (var p in game.session.Players)
            {
                p.realizedCreature?.room?.shelterDoor.Close();
            }
        }

        public bool SheltersCanClose(RainWorldGame game)
        {
            return ReadyForWin(game) || ReadyForStarve(game);
        }

        private bool ReadyForWin(RainWorldGame game)
        {
            return game.session.Players.Any(p => !PlayerDeadOrMissing(p) && PlayerReadyForWin(p)) 
                && game.session.Players.All(p => PlayerDeadOrMissing(p) || PlayerReadyForWin(p));
        }

        private bool ReadyForStarve(RainWorldGame game)
        {
            return game.session.Players.Any(p => !PlayerDeadOrMissing(p) && PlayerReadyForStarve(p))
                && game.session.Players.All(p => PlayerDeadOrMissing(p) || PlayerReadyForWin(p) || PlayerReadyForStarve(p));
        }
        
        private bool PlayerReadyForWin(AbstractCreature p)
        {
            return PlayerInShelter(p) && p.realizedCreature is Player pl && pl.readyForWin && pl.touchedNoInputCounter > (ModManager.MMF ? 40 : 20);
        }

        private bool PlayerReadyForStarve(AbstractCreature p)
        {
            return PlayerInShelter(p) && p.realizedCreature is Player pl && pl.forceSleepCounter > 260;
        }

        // Player considered dead or missing if dead or missing or in a grasp for longer than a second
        public bool PlayerDeadOrMissing(AbstractCreature absPlayer)
        {
            return IsCreatureDead(absPlayer) || (absPlayer.realizedCreature is Player p && p.dangerGrasp != null && p.dangerGraspTime > 40);
        }

        private bool PlayerHasEnoughFood(AbstractCreature p, bool toStarve)
        {
            // if good on their own, good
            bool starving = p.world.game.GetStorySession.saveState.malnourished;
            int needed = ((starving ? (p.realizedCreature as Player).slugcatStats.maxFood : toStarve ? 1 : (p.realizedCreature as Player).slugcatStats.foodToHibernate));
            if (needed <= (p.state as PlayerState).foodInStomach)
            {
                return true;
            }
            // if shared foodbar, simple check
            if (coopSharedFood) return needed <= (p.realizedCreature as Player).FoodInRoom(false);

            // otherwise check if room has food for everyone
            int foodNeeded = p.Room.creatures.Where(c => c.state is PlayerState && c.realizedCreature is Player)
                .Sum(c => Mathf.Max(0, (starving ? (c.realizedCreature as Player).slugcatStats.maxFood : toStarve ? 1 : (c.realizedCreature as Player).slugcatStats.foodToHibernate) - (c.state as PlayerState).foodInStomach));
            return (p.realizedCreature as Player).FoodInRoom(false) >= foodNeeded;
        }

        private bool PlayerInShelter(AbstractCreature p)
        {
            return p.realizedCreature is Player pl
                && pl.room is Room room
                && room.abstractRoom.shelter
                && room.shelterDoor != null
                && !room.shelterDoor.Broken
                && (RWCustom.Custom.ManhattanDistance(p.pos.Tile, room.shortcuts[0].StartTile) > 6
                && (!ModManager.MMF || pl.timeSinceInCorridorMode > 10)
                && ShelterDoor.CoordInsideShelterRange(p.pos.Tile, room.shelterDoor.isAncient));
        }

        private static void UpdatePlayerFood(RainWorldGame game)
        {
            if (coopSharedFood)
            {
                // synch food, players share a food meter
                int food = game.session.Players.Max(p => (p.state as PlayerState).foodInStomach);
                int quarters = game.session.Players.Where(p => (p.state as PlayerState).foodInStomach == food).Max(p => (p.state as PlayerState).quarterFoodPoints);
                game.session.Players.ForEach(p => (p.state as PlayerState).foodInStomach = food);
                game.session.Players.ForEach(p => (p.state as PlayerState).quarterFoodPoints = quarters);
            }
        }

        /// <summary>
        /// Skip vanilla checks in own coop mode
        /// </summary>
        private void Player_Update(ILContext il)
        {
            try
            {
                var c = new ILCursor(il);
                c.GotoNext(MoveType.Before,
                    i => i.MatchLdarg(0),
                    i => i.MatchLdarg(1),
                    i => i.MatchCallOrCallvirt<Creature>("Update"));
                ILLabel end = null;
                c.GotoPrev(MoveType.After,
                    i => i.MatchCallOrCallvirt<AbstractRoom>("get_shelter"), i=>i.MatchBrfalse(out end));
                
                c.Emit<SplitScreenCoop>(OpCodes.Ldsfld, "selfSufficientCoop");
                c.Emit(OpCodes.Brtrue, end);

                c.Goto(end.Target);
                c.MoveAfterLabels();
                
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate(ShelterUpdate);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                throw;
            }
        }

        private void ShelterUpdate(Player self)
        {
            if (!selfSufficientCoop) return;
            if (self.room.abstractRoom.shelter && self.AI == null && self.room.game.IsStorySession && !self.dead && !self.Sleeping && self.room.shelterDoor != null && !self.room.shelterDoor.Broken)
            {
                if (PlayerCanSleep(self))
                {
                    self.readyForWin = true;
                    self.forceSleepCounter = 0;
                }
                else if (PlayerCanForceSleep(self) && self.input[0].y < 0 && !self.input[0].jmp && !self.input[0].thrw && !self.input[0].pckp && self.IsTileSolid(1, 0, -1) && (self.input[0].x == 0 || ((!self.IsTileSolid(1, -1, -1) || !self.IsTileSolid(1, 1, -1)) && self.IsTileSolid(1, self.input[0].x, 0))))
                {
                    self.forceSleepCounter++;
                }
                else
                {
                    self.forceSleepCounter = 0;
                }
            }
        }

        private bool PlayerCanSleep(Player self)
        {
            return (!self.stillInStartShelter && !self.Stunned && PlayerHasEnoughFood(self.abstractCreature, false)) || (self.room.world.rainCycle.timer > self.room.world.rainCycle.cycleLength);
        }

        private bool PlayerCanForceSleep(Player self)
        {
            return (!self.abstractCreature.world.game.GetStorySession.saveState.malnourished && PlayerHasEnoughFood(self.abstractCreature, true));
        }
    }
}
