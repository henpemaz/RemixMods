using System;
using System.Linq;
using UnityEngine;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using HUD;

namespace SplitScreenCoop
{
    public partial class SplitScreenCoop
    {
        /// <summary>
        /// Creates extra RoomRealizer for p2
        /// </summary>
        private void MakeRealizer2(RainWorldGame self)
        {
            Logger.LogInfo("MakeRealizer2");
            if (self.session.Players.Count < 2 || self.roomRealizer == null) return;
            var player = self.session.Players.FirstOrDefault(p => p != self.roomRealizer.followCreature);
            if (player == null) return;
            Logger.LogInfo("MakeRealizer2 making RoomRealizer");
            realizer2 = new RoomRealizer(player, self.world)
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
            ConsiderColapsing(self.game);
            orig(self);
            MakeRealizer2(self.game);
        }

        /// <summary>
        /// Room realizers that aren't the main one dontret re-assigning themselves to cameras[0].followcreature
        /// </summary>
        public void RoomRealizer_Update(ILContext il)
        {
            try
            {
                // dontret this.followCreature = cam[0].followCreature if this != game.roomRealizer
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
        
        /// <summary>
        /// Prevent shelter door close from dontret
        /// </summary>
        public void ShelterDoor_Close(On.ShelterDoor.orig_Close orig, ShelterDoor self)
        {
            if (selfSufficientCoop && !sheltersClose) return;
            orig(self);
        }

        public static bool coopSharedFood = true;
        public static bool sheltersClose;
        public static bool coopActualGameover;

        public void CoopUpdate(RainWorldGame game)
        {
            UpdatePlayerFood(game);

            coopActualGameover = false;
            UpdateCoopGameover(game);

            sheltersClose = false;
            if (SheltersCanClose(game))
            {
                sheltersClose = true;
                CloseShelters(game);
                sheltersClose = false;
            }
        }

        private void UpdateCoopGameover(RainWorldGame game)
        {
            var isGameOver = game.GameOverModeActive;

            if (game.session.Players.All(IsCreatureDead))
            {
                if(!isGameOver)
                    CoopGameOver(game); // Death
                return;
            }
            else if (game.session.Players.All(p => IsCreatureDead(p) || (p.realizedCreature is Player pl && pl.dangerGrasp != null)))
            {
                if (!isGameOver)
                    CoopGameOver(game); // Death?
                return;
            }
            else
            {
                if (isGameOver)
                {
                    // undie!
                    foreach (var c in game.cameras)
                    {
                        if (c?.hud?.textPrompt is TextPrompt texty) texty.gameOverMode = false;
                    }
                }
            }
        }

        void CoopGameOver(RainWorldGame game)
        {
            coopActualGameover = true;
            game.GameOver(null);
            coopActualGameover = false;
        }

        private void CloseShelters(RainWorldGame game)
        {
            foreach (var p in game.session.Players)
            {
                p.realizedCreature?.room?.shelterDoor?.Close();
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
        /// Skip dontret checks in own coop mode
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

        // Jesus fucking christ why can't the game code be like this
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

        /// <summary>
        /// Skip starve detection if in our own coop logic, who the F though this would be a good idea (Joar)
        /// </summary>
        private void ShelterDoor_Update(ILContext il)
        {
            try
            {
                var c = new ILCursor(il);
                c.GotoNext(MoveType.Before,
                    i => i.MatchLdarg(0),
                    i => i.MatchLdfld<UpdatableAndDeletable>("room"),
                    i => i.MatchLdfld<Room>("game"),
                    i => i.MatchCallOrCallvirt<RainWorldGame>("GoToStarveScreen"));
                ILLabel skip = null;
                c.GotoPrev(MoveType.After,
                    i => i.MatchLdarg(0),
                    i => i.MatchLdfld<ShelterDoor>("closeSpeed"),
                    i => i.MatchLdcR4(0f),
                    i => i.MatchBleUn(out skip)
                    );
                c.Emit<SplitScreenCoop>(OpCodes.Ldsfld, "selfSufficientCoop");
                c.Emit(OpCodes.Brtrue, skip);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                throw;
            }
        }

        private void ShelterDoor_DoorClosed(On.ShelterDoor.orig_DoorClosed orig, ShelterDoor self)
        {
            if (selfSufficientCoop)
            {
                if(self.room.game.manager.upcomingProcess == null)
                {
                    CoopWinOrLoose(self.room.game);
                }
            }
            else { orig(self); }
        }

        private void CoopWinOrLoose(RainWorldGame game)
        {
            FixMissingPlayers(game);
            if(game.session.Players.Any(p => !PlayerDeadOrMissing(p)))
            {
                var alive = game.session.Players.Where(p => !PlayerDeadOrMissing(p));
                if (alive.All(p => !PlayerHasEnoughFood(p, true))) {
                    Logger.LogInfo("CoopWinOrLoose -> GoToStarveScreen");
                    game.GoToStarveScreen();
                    return;
                }
                bool starving = coopSharedFood ? !PlayerHasEnoughFood(alive.First(), false)
                    : alive.All(p=> !PlayerHasEnoughFood(p, false));
                Logger.LogInfo($"CoopWinOrLoose -> Win(malnourished:{starving})");
                game.Win(starving);
                return;
            }
            else
            {
                Logger.LogInfo("CoopWinOrLoose -> GoToDeathScreen");
                game.GoToDeathScreen();
            }
        }

        private void FixMissingPlayers(RainWorldGame game)
        {
            var validPlayer = game.Players.First(p => game.world.GetAbstractRoom(p.pos) != null);
            game.Players.ForEach(p => { if (game.world.GetAbstractRoom(p.pos) == null) { p.pos = validPlayer.pos; p.world = validPlayer.world; } });
        }

        public delegate AbstractCreature orig_get_FirstAlivePlayer(RainWorldGame self);
        public AbstractCreature get_FirstAlivePlayer(orig_get_FirstAlivePlayer orig, RainWorldGame self)
        {
            if (selfSufficientCoop) return self.session.Players.FirstOrDefault(p => !PlayerDeadOrMissing(p)) ?? orig(self); // null bad lmao
            return orig(self);
        }

        private void SaveState_SessionEnded(ILContext il)
        {
            try
            {
                var c = new ILCursor(il);
                c.GotoNext(MoveType.Before, // go to food clamped
                    i => i.MatchLdfld<SlugcatStats>("maxFood"),
                    i => i.MatchCallOrCallvirt(out _), // custom.intclamp
                    i => i.MatchStfld<SaveState>("food")
                    );
                c.GotoPrev(MoveType.Before, // go to start of clamp block
                    i => i.MatchLdarg(0),
                    i => i.MatchLdarg(0),
                    i => i.MatchLdfld<SaveState>("food")
                    );
                var skip = c.IncomingLabels.First(); // a jump that skipped dontret
                var vanilla = il.DefineLabel();
                c.GotoPrev(MoveType.After, i => i.MatchBr(out var lab) && lab.Target == skip.Target); // right before dontret block
                c.MoveAfterLabels();
                c.Emit<SplitScreenCoop>(OpCodes.Ldsfld, "selfSufficientCoop");
                c.Emit(OpCodes.Brfalse, vanilla);
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldarg_1);
                c.EmitDelegate(CoopSessionFood);
                c.Emit(OpCodes.Br, skip);
                c.MarkLabel(vanilla);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                throw;
            }
        }

        void CoopSessionFood(SaveState ss, RainWorldGame game)
        {
            Logger.LogInfo($"CoopSessionFood was {ss.food}");
            ss.food += (game.Players.Where(p => !PlayerDeadOrMissing(p)).OrderByDescending(p => (p.realizedCreature as Player).FoodInRoom(false)).First().realizedCreature as Player).FoodInRoom(true);
            Logger.LogInfo($"CoopSessionFood became {ss.food}");
        }

        private void RainWorldGame_ctor2(ILContext il)
        {
            try
            {
                var c = new ILCursor(il);
                int loc = 0;
                c.GotoNext(MoveType.Before, // go to food
                    i => i.MatchLdfld<SaveState>("food"),
                    i => i.MatchStloc(out loc)
                    );

                c.GotoNext(MoveType.Before, // go to start of 'vanilla while'
                    i => i.MatchBr(out _)
                    );
                var vanilla = il.DefineLabel();
                c.MoveAfterLabels();
                c.Emit<SplitScreenCoop>(OpCodes.Ldsfld, "selfSufficientCoop");
                c.Emit(OpCodes.Brfalse, vanilla);
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloc, loc);
                c.EmitDelegate(CoopStartingFood);
                c.Emit(OpCodes.Stloc, loc);
                c.MarkLabel(vanilla);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                throw;
            }
        }

        int CoopStartingFood(RainWorldGame game, int foodInSave)
        {
            Logger.LogInfo("CoopStartingFood");
            if (selfSufficientCoop && coopSharedFood)
            {
                if(foodInSave > 0)
                {
                    Logger.LogInfo($"CoopStartingFood shared {foodInSave} food");
                    game.session.Players.ForEach(p => { (p.state as PlayerState).foodInStomach = foodInSave; });
                    Logger.LogInfo($"CoopStartingFood p0 has {(game.session.Players[0].state as PlayerState).foodInStomach} food");
                    foodInSave = 0;
                }
            }
            return foodInSave;
        }

        // this game is stupid with how it calls gameover before player.base.die (unless you have the DLC installed!)
        // can't detect which players are dead so instead we ignore all calls and update gameover state on game update tick
        private void RainWorldGame_GameOver(ILContext il)
        {
            try
            {
                var c = new ILCursor(il);
                c.GotoNext(MoveType.After, // StorySession If
                    i => i.MatchCallOrCallvirt<RainWorldGame>("get_IsStorySession"),
                    i => i.MatchBrfalse(out _)
                    );

                var dontret = il.DefineLabel();
                c.MoveAfterLabels();
                c.Emit<SplitScreenCoop>(OpCodes.Ldsfld, "selfSufficientCoop");
                c.Emit(OpCodes.Brfalse, dontret);
                c.Emit<SplitScreenCoop>(OpCodes.Ldsfld, "coopActualGameover");
                c.Emit(OpCodes.Brtrue, dontret);
                c.Emit(OpCodes.Ret);
                c.MarkLabel(dontret);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                throw;
            }
        }

        private int RegionGate_PlayersInZone(On.RegionGate.orig_PlayersInZone orig, RegionGate self)
        {
            if (selfSufficientCoop)
            {
                // vanilla logic was just wrong alltogether?
                if (self.room.game.Players.Any(p => (!PlayerDeadOrMissing(p) && p.Room != self.room.abstractRoom))) return -1;
            }
            return orig(self);
        }

    }
}
