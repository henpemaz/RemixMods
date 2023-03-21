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
                var skip = c.IncomingLabels.First(); // a jump that skipped vanilla
                var vanilla = il.DefineLabel();
                c.GotoPrev(MoveType.After, i => i.MatchBr(out var lab) && lab.Target == skip.Target); // right before vanilla block
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
                if (foodInSave > 0)
                {
                    Logger.LogInfo($"CoopStartingFood shared {foodInSave} food");
                    game.session.Players.ForEach(p => { (p.state as PlayerState).foodInStomach = foodInSave; });
                    Logger.LogInfo($"CoopStartingFood p0 has {(game.session.Players[0].state as PlayerState).foodInStomach} food");
                    foodInSave = 0;
                }
            }
            return foodInSave;
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


        private void Creature_FlyAwayFromRoom(On.Creature.orig_FlyAwayFromRoom orig, Creature self, bool carriedByOther)
        {
            if (self is Player pl && selfSufficientCoop && !pl.isNPC && carriedByOther) pl.Die();
            orig(self, carriedByOther);
        }

        // vanilla assumes players[0].realizedcreature not null
        private void RegionGate_get_MeetRequirement(ILContext il)
        {
            try
            {
                var c = new ILCursor(il);
                c.GotoNext(MoveType.Before, // StorySession If
                    i => i.MatchCallOrCallvirt<RainWorldGame>("get_Players"),
                    i => i.MatchLdcI4(0),
                    i => i.MatchCallOrCallvirt(out _), // get_item
                    i => i.MatchCallOrCallvirt<AbstractCreature>("get_realizedCreature")
                    );

                c.Index++;
                c.EmitDelegate((List<AbstractCreature> players) =>
                {
                    if (selfSufficientCoop)
                    {
                        return players.Where(p => (!PlayerDeadOrMissing(p))).ToList();
                    }
                    return players;
                });
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                throw;
            }
        }

        // we are multiplayer
        private bool ProcessManager_IsGameInMultiplayerContext(On.ProcessManager.orig_IsGameInMultiplayerContext orig, ProcessManager self)
        {
            if (self.currentMainLoop is RainWorldGame game && game.IsStorySession && selfSufficientCoop) return true;
            return orig(self);
        }

        // Jolly still doesn't know how to do it proper
        private void RoomCamera_ChangeCameraToPlayer(On.RoomCamera.orig_ChangeCameraToPlayer orig, RoomCamera self, AbstractCreature cameraTarget)
        {
            Logger.LogInfo("RoomCamera_ChangeCameraToPlayer");
            if (cameraTarget.realizedCreature is Player player)
            {
                AssignCameraToPlayer(self, player);
                // ChangeCameraToPlayer gets called before the camera gets a chance to initialize
                if (self.hud == null) { self.FireUpSinglePlayerHUD(player); } 
            }
            orig(self, cameraTarget);
        }

        private void Player_TriggerCameraSwitch(ILContext il)
        {
            try
            {
                var c = new ILCursor(il);
                c.GotoNext(MoveType.Before, // roomcam = 
                    i => i.MatchStloc(out var _)
                    );

                // rcam on stack
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((RoomCamera rc, Player self) => // use the cam that's my own or a cam that is free or cam0, in this order
                {
                    var wasrc = rc;
                    rc = self.abstractCreature.world.game.cameras.FirstOrDefault(c => c.followAbstractCreature == self.abstractCreature);
                    if (rc == null) self.abstractCreature.world.game.cameras.FirstOrDefault(c => IsCreatureDead(c.followAbstractCreature));
                    if (rc == null) rc = wasrc;
                    return rc;
                });
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                throw;
            }
        }

        private void SlugcatSelectMenu_StartGame(On.Menu.SlugcatSelectMenu.orig_StartGame orig, Menu.SlugcatSelectMenu self, SlugcatStats.Name storyGameCharacter)
        {
            if (selfSufficientCoop)
            {
                Logger.LogInfo("Requesting p2 rewired signin");
                self.manager.rainWorld.RequestPlayerSignIn(1, null);
            }
            orig(self, storyGameCharacter);
        }

    }
}
