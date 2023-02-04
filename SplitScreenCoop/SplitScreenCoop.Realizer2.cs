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
            if (realizer2 != null) MakeRealizer2(self.game);
        }

        /// <summary>
        /// Room realizers that aren't the main one re-assigning themselves to cameras[0].followcreature
        /// dont reasign if cam.followcreature is null, you dumb fuck
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
                    if (self != self.world?.game?.roomRealizer || self.world?.game?.cameras[0].followAbstractCreature == null)
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
                if (other != null && other.followCreature != null)
                {
                    rrNestedLock = true;
                    r = r && other.CanAbstractizeRoom(tracker);
                    rrNestedLock = false;
                }
            }
            return r;
        }
    }
}
