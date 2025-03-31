using System;
using System.Linq;
using UnityEngine;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using HUD;
using System.Collections.Generic;

namespace SplitScreenCoop
{
    public partial class SplitScreenCoop
    {
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
                CloseShelters(game);
            }
        }

        public void UpdateCoopGameover(RainWorldGame game)
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

        /// <summary>
        /// Prevent shelter door close from vanilla
        /// </summary>
        public void ShelterDoor_Close(On.ShelterDoor.orig_Close orig, ShelterDoor self)
        {
            if (selfSufficientCoop && !sheltersClose) return;
            orig(self);
        }

        public void CloseShelters(RainWorldGame game)
        {
            sheltersClose = true;
            foreach (var p in game.session.Players)
            {
                p.realizedCreature?.room?.shelterDoor?.Close();
            }
            sheltersClose = false;
        }

        public bool SheltersCanClose(RainWorldGame game)
        {
            return ReadyForWin(game) || ReadyForStarve(game);
        }

        public bool ReadyForWin(RainWorldGame game)
        {
            return game.session.Players.Any(p => !PlayerDeadOrMissing(p) && PlayerReadyForWin(p)) 
                && game.session.Players.All(p => PlayerDeadOrMissing(p) || PlayerReadyForWin(p));
        }

        public bool ReadyForStarve(RainWorldGame game)
        {
            return game.session.Players.Any(p => !PlayerDeadOrMissing(p) && PlayerReadyForStarve(p))
                && game.session.Players.All(p => PlayerDeadOrMissing(p) || PlayerReadyForWin(p) || PlayerReadyForStarve(p));
        }
        
        public bool PlayerReadyForWin(AbstractCreature p)
        {
            return PlayerInShelter(p) && p.realizedCreature is Player pl && pl.readyForWin && pl.touchedNoInputCounter > (ModManager.MMF ? 40 : 20);
        }

        public bool PlayerReadyForStarve(AbstractCreature p)
        {
            return PlayerInShelter(p) && p.realizedCreature is Player pl && pl.forceSleepCounter > 260;
        }

        // Player considered dead or missing if dead or missing or in a grasp for longer than a second
        public bool PlayerDeadOrMissing(AbstractCreature absPlayer)
        {
            return IsCreatureDead(absPlayer) || (absPlayer.realizedCreature is Player p && p.dangerGrasp != null && p.dangerGraspTime > 40);
        }

        public bool PlayerHasEnoughFood(AbstractCreature p, bool toStarve)
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

        public bool PlayerInShelter(AbstractCreature p)
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

        public static void UpdatePlayerFood(RainWorldGame game)
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
        public void Player_Update(ILContext il)
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
        public void ShelterUpdate(Player self)
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

        public bool PlayerCanSleep(Player self)
        {
            return (!self.stillInStartShelter && !self.Stunned && PlayerHasEnoughFood(self.abstractCreature, false)) || (self.room.world.rainCycle.timer > self.room.world.rainCycle.cycleLength);
        }

        public bool PlayerCanForceSleep(Player self)
        {
            return (!self.abstractCreature.world.game.GetStorySession.saveState.malnourished && PlayerHasEnoughFood(self.abstractCreature, true));
        }

        /// <summary>
        /// Skip starve detection if in our own coop logic, who the F though this would be a good idea (Joar)
        /// </summary>
        public void ShelterDoor_Update(ILContext il)
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

        public void ShelterDoor_DoorClosed(On.ShelterDoor.orig_DoorClosed orig, ShelterDoor self)
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

        public void CoopWinOrLoose(RainWorldGame game)
        {
            FixMissingPlayers(game); // SessionEnd doesn't like when players are in a different region
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
                game.Win(starving, false); // is true for watcher warps, should investigate this later
                return;
            }
            else
            {
                Logger.LogInfo("CoopWinOrLoose -> GoToDeathScreen");
                game.GoToDeathScreen();
            }
        }

        public void FixMissingPlayers(RainWorldGame game)
        {
            var validPlayer = game.Players.First(p => game.world.GetAbstractRoom(p.pos) != null);
            game.Players.ForEach(p => { if (game.world.GetAbstractRoom(p.pos) == null) { p.pos = validPlayer.pos; p.world = validPlayer.world; } });
        }

        // this game is stupid with how it calls gameover before player.base.die (unless you have the DLC installed!)
        // can't detect which players are dead so instead we ignore all calls and update gameover state on game update tick
        public void RainWorldGame_GameOver(ILContext il)
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

    }
}
