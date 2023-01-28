using System;
using System.Linq;
using UnityEngine;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace SplitScreenCoop
{
    public partial class SplitScreenCoop
    {
        public delegate void delHandleCoopCamera(Player self, int playerNumber);
        public void FixHandleCoopCamera(delHandleCoopCamera orig, Player self, int playerNumber)
        {
            if (self?.room?.game?.GetStorySession is StoryGameSession sgs && sgs.Players.Count > 1 && self.room.game.cameras.Length > 1)
            {
                var cameras = self.room.game.cameras;
                RoomCamera thecam = null;


                foreach (var cam in cameras)
                {
                    if (cam.followAbstractCreature == self.abstractCreature) return;
                }

                // any dead cams?
                if (cameras.Reverse().FirstOrDefault(c => IsCamDead(c)) is RoomCamera deadcam) thecam = deadcam;
                // any free cams?
                else if (cameras.Reverse().FirstOrDefault(c => cameras.Any(c2 => c != c2 && (c2.followAbstractCreature == c.followAbstractCreature))) is RoomCamera freecam) thecam = freecam;
                // if player doing it has a camera, ignore
                else if (cameras.Any(c => c.followAbstractCreature == self.abstractCreature)) return;
                // any duped cams?
                else if (cameras.Reverse().FirstOrDefault(c => cameras.Any(c2 => c != c2 && (c2.room == c.room && c2.currentCameraPosition == c.currentCameraPosition))) is RoomCamera dupecam) thecam = dupecam;
                // rotate cam2
                else thecam = cameras[1];

                if (thecam != null)
                {
                    AssignCameraToPlayer(thecam, self);
                }
                return;

            }
            orig(self, playerNumber);
        }

        // man
        public delegate void delPlayerMeterDraw(object self, float timeStacker);
        public void FixPlayerMeterDraw(delPlayerMeterDraw orig, object self, float timeStacker)
        {
            //if (self is HUD.HudPart part && part.hud.owner is Player p) JollyCoop.PlayerHK.currentPlayerWithCamera = p.playerState.playerNumber; //if only there was a way to know which player owns the hud...
            orig(self, timeStacker);
        }

        private void FixjollyBodyTransplant(ILContext il) // please do not the slugcat
        {
            var c = new ILCursor(il);
            try
            {
                // do not
                c.GotoNext(
                    i => i.MatchCallOrCallvirt<AbstractCreature>("set_realizedCreature") // oh god oh fuck
                    );
                c.Remove(); // no set_realizedCreature
                c.Emit<PhysicalObject>(OpCodes.Ldfld, "abstractPhysicalObject");
                c.Emit<RoomCamera>(OpCodes.Stfld, "followAbstractCreature");
                c.Index -= 5;
                c.Remove(); // no cam.followabs
            }
            catch (Exception e)
            {
                Debug.LogException(new Exception("Couldn't IL-hook FixjollyBodyTransplant from SplitScreenMod", e)); // deffendisve progrmanig
            }
        }

        private void FixsbcsCheckBorders(ILContext il) // patch up cam scroll boundaries
        {
            var c = new ILCursor(il);
            // buncha gotos, faster like this
            try
            {
                // SplitVertical
                c.GotoNext(i => i.MatchCallOrCallvirt<RoomCamera>("get_sSize"));
                c.Index += 2;
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<float, RoomCamera, float>>((f, cam) =>
                {
                    if (CurrentSplitMode == SplitMode.SplitVertical)
                    {
                        return f - cam.sSize.x / 4f;
                    }
                    return f;
                });
                c.GotoNext(i => i.MatchCallOrCallvirt<RoomCamera>("get_sSize"));
                c.Index += 2;
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<float, RoomCamera, float>>((f, cam) =>
                {
                    if (CurrentSplitMode == SplitMode.SplitVertical)
                    {
                        return f - cam.sSize.x / 4f;
                    }
                    return f;
                });

                c.GotoNext(i => i.MatchLdarg(1));

                c.GotoNext(i => i.MatchLdloc(0));
                c.Index += 2;
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<float, RoomCamera, float>>((f, cam) =>
                {
                    if (CurrentSplitMode == SplitMode.SplitVertical)
                    {
                        return f - cam.sSize.x / 4f;
                    }
                    return f;
                });

                c.GotoNext(i => i.MatchLdloc(0));
                c.Index += 2;
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<float, RoomCamera, float>>((f, cam) =>
                {
                    if (CurrentSplitMode == SplitMode.SplitVertical)
                    {
                        return f - cam.sSize.x / 4f;
                    }
                    return f;
                });

                //// SplitHorizontal
                c.GotoNext(i => i.MatchCallOrCallvirt<RoomCamera>("get_sSize"));
                c.Index += 2;
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<float, RoomCamera, float>>((f, cam) =>
                {
                    if (CurrentSplitMode == SplitMode.SplitHorizontal)
                    {
                        return f - cam.sSize.y / 4f;
                    }
                    return f;
                });
                c.GotoNext(i => i.MatchCallOrCallvirt<RoomCamera>("get_sSize"));
                c.Index += 2;
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<float, RoomCamera, float>>((f, cam) =>
                {
                    if (CurrentSplitMode == SplitMode.SplitHorizontal)
                    {
                        return f - cam.sSize.y / 4f;
                    }
                    return f;
                });

                c.GotoNext(i => i.MatchLdarg(1));

                c.GotoNext(i => i.MatchLdloc(0));
                c.Index += 2;
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<float, RoomCamera, float>>((f, cam) =>
                {
                    if (CurrentSplitMode == SplitMode.SplitHorizontal)
                    {
                        return f - cam.sSize.y / 4f;
                    }
                    return f;
                });

                c.GotoNext(i => i.MatchLdloc(0));
                c.Index += 2;
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<float, RoomCamera, float>>((f, cam) =>
                {
                    if (CurrentSplitMode == SplitMode.SplitHorizontal)
                    {
                        return f - cam.sSize.y / 4f;
                    }
                    return f;
                });

            }
            catch (Exception e)
            {
                Debug.LogException(new Exception("Couldn't IL-hook FixsbcsCheckBorders from SplitScreenMod", e)); // deffendisve progrmanig
            }
        }

        private void FixsbcsApplyPositionChange(ILContext il) // use the right texture please
        {
            var c = new ILCursor(il);
            try
            {
                // LevelTexture -> LevelTexture1
                while (c.TryGotoNext(MoveType.Before,
                    i => i.MatchLdstr("LevelTexture"),
                    i => i.MatchCallOrCallvirt(out _) || (i.MatchLdloc(out _) && i.Next.MatchLdindRef()) || i.MatchLdcI4(out _)
                    ))
                {
                    c.Index++;
                    c.Emit(OpCodes.Ldarg_0);
                    c.EmitDelegate<Func<string, RoomCamera, string>>((t, cam) =>
                    {
                        return t + ((cam.cameraNumber != 0) ? cam.cameraNumber.ToString() : string.Empty);
                    });
                }

            }
            catch (Exception e)
            {
                Debug.Log(new Exception("Couldn't IL-hook FixsbcsApplyPositionChange from SplitScreenMod, but everything is probably fine maybe they fixed it", e));
                // its fine maybe they fixed it
            }
        }
    }
}
