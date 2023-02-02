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
    }
}
