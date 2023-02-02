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
        /// Dialogue centered and oncreen
        /// </summary>
        public Vector2 DialogBox_DrawPos(On.HUD.DialogBox.orig_DrawPos orig, HUD.DialogBox self, float timeStacker)
        {
            // maybe instead of cl.roomcamera we could go self.hud.camera ?
            if (CurrentSplitMode == SplitMode.SplitVertical && curCamera >= 0 && cameraListeners[curCamera] is CameraListener cl)
            {
                return orig(self, timeStacker) - new Vector2(cl.roomCamera.sSize.x / 4f, 0f);
            }
            return orig(self, timeStacker);
        }

        /// <summary>
        /// reduce double audio
        /// </summary>
        public void VirtualMicrophone_DrawUpdate(On.VirtualMicrophone.orig_DrawUpdate orig, VirtualMicrophone self, float timeStacker, float timeSpeed)
        {
            if (self.camera.cameraNumber > 0 && self.camera.room == self.camera.game.cameras[0].room)
            {
                self.volumeGroups[0] *= 1f;
                if (self.camera.game.cameras[0].virtualMicrophone is VirtualMicrophone other)
                {
                    self.volumeGroups[0] *= Mathf.InverseLerp(100f, 1000f, (self.listenerPoint - other.listenerPoint).magnitude);
                }
                self.volumeGroups[1] *= 0f;
                self.volumeGroups[2] *= 0f;
            }
            orig(self, timeStacker, timeSpeed);
        }
        
        public bool inpause; // non reentrant
        /// <summary>
        /// Make 2 pause
        /// </summary>
        public void PauseMenu_ctor(On.Menu.PauseMenu.orig_ctor orig, Menu.PauseMenu self, ProcessManager manager, RainWorldGame game)
        {
            orig(self, manager, game);

            if (game.cameras.Length > 1 && !inpause)
            {
                if (CurrentSplitMode == SplitMode.SplitVertical)
                {
                    self.container.SetPosition(manager.rainWorld.screenSize.x / 4f, 0);
                    inpause = true;
                    try
                    {
                        var otherpause = new Menu.PauseMenu(manager, game);
                        otherpause.container.SetPosition(camOffsets[game.cameras[1].cameraNumber] + new Vector2(-manager.rainWorld.screenSize.x / 4f, 0));
                        manager.sideProcesses.Add(otherpause);
                    }
                    finally
                    {
                        inpause = false;
                    }
                }
                else if (CurrentSplitMode == SplitMode.SplitHorizontal)
                {
                    self.container.SetPosition(0, -manager.rainWorld.screenSize.y / 4f);
                    inpause = true;
                    try
                    {
                        var otherpause = new Menu.PauseMenu(manager, game);
                        otherpause.container.SetPosition(camOffsets[game.cameras[1].cameraNumber] + new Vector2(0, manager.rainWorld.screenSize.y / 4f));
                        manager.sideProcesses.Add(otherpause);
                    }
                    finally
                    {
                        inpause = false;
                    }
                }
            }
        }

        /// <summary>
        /// Need to shut down two pause menus
        /// </summary>
        public void PauseMenu_ShutDownProcess(On.Menu.PauseMenu.orig_ShutDownProcess orig, Menu.PauseMenu self)
        {
            orig(self);
            var otherpause = self.manager?.sideProcesses?.FirstOrDefault(t => t is Menu.PauseMenu);
            if (otherpause != null) self.manager.StopSideProcess(otherpause); // removes from sideprocesses list so this isnt a recursive loop
        }

        /// <summary>
        /// Hud is at an offset on splitscreen mode
        /// </summary>
        public void RoomCamera_FireUpSinglePlayerHUD(On.RoomCamera.orig_FireUpSinglePlayerHUD orig, RoomCamera self, Player player)
        {
            orig(self, player);
            OffsetHud(self);
        }

        public delegate bool delget_ShouldBeCulled(GraphicsModule gm);
        /// <summary>
        /// cull should account for more cams
        /// </summary>
        public bool get_ShouldBeCulled(delget_ShouldBeCulled orig, GraphicsModule gm)
        {
            if (gm.owner.room.game.cameras.Length > 1)
            {
                return orig(gm) &&
                !gm.owner.room.game.cameras[1].PositionCurrentlyVisible(gm.owner.firstChunk.pos, gm.cullRange + ((!gm.culled) ? 100f : 0f), true) &&
                !gm.owner.room.game.cameras[1].PositionVisibleInNextScreen(gm.owner.firstChunk.pos, (!gm.culled) ? 100f : 50f, true);
            }
            return orig(gm);
        }

        /// <summary>
        /// water wont move all vertices if the camera is too far to the right, move everything at startup
        /// </summary>
        public void Water_InitiateSprites(On.Water.orig_InitiateSprites orig, Water self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            orig(self, sLeaser, rCam);
            if (CurrentSplitMode != SplitMode.NoSplit)
            {
                var camPos = rCam.pos + rCam.offset;
                float y = -10f;
                if (self.cosmeticLowerBorder > -1f)
                {
                    y = self.cosmeticLowerBorder - camPos.y;
                }
                Vector2 top = new Vector2(1400f, self.fWaterLevel - camPos.y + self.cosmeticSurfaceDisplace);
                Vector2 bottom = new Vector2(1400f, y);
                for (int i = 0; i < self.pointsToRender; i++)
                {
                    int num3 = i * 2;

                    (sLeaser.sprites[0] as WaterTriangleMesh).MoveVertice(num3, top);
                    (sLeaser.sprites[0] as WaterTriangleMesh).MoveVertice(num3 + 1, top);

                    (sLeaser.sprites[1] as WaterTriangleMesh).MoveVertice(num3, top);
                    (sLeaser.sprites[1] as WaterTriangleMesh).MoveVertice(num3 + 1, bottom);
                }
            }
        }

        /// <summary>
        /// implements following player for cam2
        /// </summary>
        public void ShortcutHandler_Update(ILContext il)
        {
            var c = new ILCursor(il);
            int indexLoc = 0;

            // this is loading room if creature followed by camera
            if (c.TryGotoNext(MoveType.Before,
                i => i.MatchCallvirt<AbstractCreature>("FollowedByCamera"),
                i => i.MatchBrfalse(out _),
                i => i.MatchLdarg(0),
                i => i.MatchLdfld<ShortcutHandler>("betweenRoomsWaitingLobby"),
                i => i.MatchLdloc(out indexLoc)
                ))
            {
                c.Index++;
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloc, indexLoc);
                // was A && !B
                // becomes (A || A2) && !B
                // b param here is A
                c.EmitDelegate<Func<bool, ShortcutHandler, int, bool>>((b, sc, k) =>
                {
                    return b || (sc.game.cameras.Length > 1 && sc.betweenRoomsWaitingLobby[k].creature.abstractCreature.FollowedByCamera(1));
                });
            }
            else Logger.LogError(new Exception("Couldn't IL-hook ShortcutHandler_Update part 1 FollowedByCamera from SplitScreenMod")); // deffendisve progrmanig

            // this is actually loading the room, should add to tracked rooms, cmon game
            if (c.TryGotoNext(MoveType.Before,
                i => i.MatchCallvirt<World>("ActivateRoom")
                ))
            {
                c.Remove();
                c.EmitDelegate<Action<World, AbstractRoom>>((w, r) =>
                {
                    w.ActivateRoom(r);
                    if (w?.game?.roomRealizer is RoomRealizer rr) rr.AddNewTrackedRoom(r, true);
                });
            }
            else Logger.LogError(new Exception("Couldn't IL-hook ShortcutHandler_Update part 2 AddNewTrackedRoom from SplitScreenMod")); // deffendisve progrmanig


            // this is moving the camera if the creature is followed by camera
            ILLabel jump = null;
            if (c.TryGotoNext(MoveType.Before,
                i => i.MatchCallvirt<AbstractCreature>("FollowedByCamera"),
                i => i.MatchBrfalse(out jump),
                i => i.MatchLdarg(0),
                i => i.MatchLdfld<ShortcutHandler>("game")
                ))
            {
                c.GotoLabel(jump);

                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloc, indexLoc);
                c.EmitDelegate<Action<ShortcutHandler, int>>((sc, k) =>
                {
                    if (sc.game.cameras.Length > 1 && sc.betweenRoomsWaitingLobby[k].creature.abstractCreature.FollowedByCamera(1))
                        sc.game.cameras[1].MoveCamera(sc.betweenRoomsWaitingLobby[k].room.realizedRoom, sc.betweenRoomsWaitingLobby[k].room.nodes[sc.betweenRoomsWaitingLobby[k].entranceNode].viewedByCamera);
                });
            }
            else Logger.LogError(new Exception("Couldn't IL-hook ShortcutHandler_Update part 3 MoveCamera from SplitScreenMod")); // deffendisve progrmanig
        }

        /// <summary>
        /// activating a room should add it to the tracked active rooms
        /// </summary>
        public void ShortcutHandler_SuckInCreature(ILContext il)
        {
            var c = new ILCursor(il);
            // this is actually loading and entering the room, should add to tracked rooms, cmon game
            if (c.TryGotoNext(MoveType.Before,
                i => i.MatchCallvirt<World>("ActivateRoom")
                ))
            {
                c.Remove();
                c.EmitDelegate<Action<World, AbstractRoom>>((w, r) =>
                {
                    w.ActivateRoom(r);
                    if (w?.game?.roomRealizer is RoomRealizer rr) rr.AddNewTrackedRoom(r, true);
                });
            }
            else Logger.LogError(new Exception("Couldn't IL-hook ShortcutHandler_SuckInCreature AddNewTrackedRoom from SplitScreenMod")); // deffendisve progrmanig
        }

        /// <summary>
        /// fixes draw parameter changing on repeated calls to init, would array-oob on the previous leaser
        /// </summary>
        public void PoleMimicGraphics_InitiateSprites(ILContext il)
        {
            var c = new ILCursor(il);

            if (c.TryGotoNext(MoveType.Before,
                i => i.MatchStfld<PoleMimicGraphics>("leafPairs")
                ))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<int, PoleMimicGraphics, int>>((was, pole) =>
                {
                    return pole.leafPairs > 0 ? pole.leafPairs : was;
                });
            }
            else Logger.LogError(new Exception("Couldn't IL-hook PoleMimicGraphics_InitiateSprites from SplitScreenMod")); // deffendisve progrmanig
        }

        /// <summary>
        /// fixes fsprite names so they can use different textures
        /// </summary>
        public void RoomCamera_ctor(ILContext il)
        {
            var c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.Before,
                i => i.MatchLdstr("LevelTexture"),
                i => i.MatchLdcI4(1),
                i => i.MatchNewobj<FSprite>()
                ))
            {
                c.Index++;
                c.Emit(OpCodes.Ldarg_2);
                c.EmitDelegate<Func<string, int, string>>((name, camnum) =>
                {
                    return camnum > 0 ? name + camnum.ToString() : name;
                });
            }
            else Logger.LogError(new Exception("Couldn't IL-hook RoomCamera_ctor from SplitScreenMod")); // deffendisve progrmanig
        }

        /// <summary>
        /// proper scroll and boundaries for our custom split modes. Originally only supported horiz split
        /// </summary>
        public void RoomCamera_Update1(ILContext il)
        {
            var c = new ILCursor(il);
            ILLabel jump = null;
            if (c.TryGotoNext(MoveType.Before,
                i => i.MatchLdarg(0),
                i => i.MatchLdfld<RoomCamera>("splitScreenMode"),
                i => i.MatchBrfalse(out jump),
                i => i.MatchLdarg(0),
                i => i.MatchLdfld<RoomCamera>("followAbstractCreature"),
                i => i.MatchCallvirt<AbstractCreature>("get_realizedCreature")
                ))
            {
                c.GotoLabel(jump);
                c.MoveAfterLabels();
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Action<RoomCamera>>((rc) =>
                {
                    if (CurrentSplitMode == SplitMode.SplitHorizontal)
                    {
                        float pad = rc.sSize.y / 4f;
                        if (rc.followAbstractCreature != null && rc.followAbstractCreature.realizedCreature is Creature cr)
                        {
                            if (!cr.inShortcut) rc.pos.y = rc.followAbstractCreature.realizedCreature.mainBodyChunk.pos.y - 2 * pad;
                            else
                            {
                                Vector2? vector = rc.room.game.shortcuts.OnScreenPositionOfInShortCutCreature(rc.room, cr);
                                if (vector != null)
                                {
                                    rc.pos.y = vector.Value.y - 2 * pad;
                                }
                            }
                            rc.pos.y += rc.followCreatureInputForward.y * 2f;
                        }
                    }
                    else if (CurrentSplitMode == SplitMode.SplitVertical)
                    {
                        float pad = rc.sSize.x / 4f;
                        if (rc.followAbstractCreature != null && rc.followAbstractCreature.realizedCreature is Creature cr)
                        {
                            if (!cr.inShortcut) rc.pos.x = rc.followAbstractCreature.realizedCreature.mainBodyChunk.pos.x - 2 * pad;
                            else
                            {
                                Vector2? vector = rc.room.game.shortcuts.OnScreenPositionOfInShortCutCreature(rc.room, cr);
                                if (vector != null)
                                {
                                    rc.pos.x = vector.Value.x - 2 * pad;
                                }
                            }
                            rc.pos.x += rc.followCreatureInputForward.x * 2f;
                        }
                    }
                });

                try
                {
                    c.GotoNext(MoveType.After, i => i.MatchCallOrCallvirt<RoomCamera>("get_hDisplace")); // IL_00A7

                    c.Emit(OpCodes.Ldarg_0); // RoomCamera
                    c.EmitDelegate<Func<float, RoomCamera, float>>((v, rc) =>
                    {
                        if (CurrentSplitMode == SplitMode.SplitVertical)
                        {
                            return v - rc.sSize.x / 4f;
                        }
                        return v;
                    });

                    c.GotoNext(MoveType.After, i => i.MatchCallOrCallvirt<RoomCamera>("get_hDisplace")); // IL_00CF

                    c.Emit(OpCodes.Ldarg_0); // RoomCamera
                    c.EmitDelegate<Func<float, RoomCamera, float>>((v, rc) =>
                    {
                        if (CurrentSplitMode == SplitMode.SplitVertical)
                        {
                            return v + rc.sSize.x / 4f;
                        }
                        return v;
                    });

                    c.GotoNext(MoveType.After, i => i.MatchLdfld<RoomCamera>("splitScreenMode"), i => i.MatchBrtrue(out _)); // IL_0116
                    c.MoveAfterLabels();
                    c.Emit(OpCodes.Ldarg_0); // RoomCamera
                    c.EmitDelegate<Func<float, RoomCamera, float>>((v, rc) =>
                    {
                        if (CurrentSplitMode == SplitMode.SplitHorizontal)
                        {
                            return v + rc.sSize.y / 4f;
                        }
                        return v;
                    });


                    c.GotoNext(MoveType.After, i => i.MatchLdfld<RoomCamera>("splitScreenMode"), i => i.MatchBrtrue(out _)); // IL_014C
                    c.MoveAfterLabels();
                    c.Emit(OpCodes.Ldarg_0); // RoomCamera
                    c.EmitDelegate<Func<float, RoomCamera, float>>((v, rc) =>
                    {
                        if (CurrentSplitMode == SplitMode.SplitHorizontal)
                        {
                            return v + rc.sSize.y / 4f;
                        }
                        return v;
                    });
                }
                catch (Exception e)
                {
                    Logger.LogError(new Exception("Couldn't IL-hook RoomCamera_Update1 from SplitScreenMod, inner spot", e)); // deffendisve progrmanig
                    throw;
                }
            }
            else Logger.LogError(new Exception("Couldn't IL-hook RoomCamera_Update1 from SplitScreenMod")); // deffendisve progrmanig


            // Jolly do NOT touch the camera I hecking swear you don't know what you're doing
            try
            {
                //var c = new ILCursor(il);
                c.Index = 0;
                ILLabel doNOT = null;
                c.GotoNext(MoveType.After, // this.game.AlivePlayers.Count > 0 && this.game.FirstAlivePlayer != null
                    i => i.MatchCallOrCallvirt<RainWorldGame>("get_FirstAlivePlayer"),
                    i => i.MatchBrfalse(out doNOT)
                    );

                c.Emit(OpCodes.Br, doNOT); // just don't
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                throw;
            }
        }

        /// <summary>
        /// proper scroll and boundaries for our custom split modes. Originally only supported horiz split
        /// </summary>
        public void RoomCamera_DrawUpdate1(ILContext il)
        {
            var c = new ILCursor(il);
            ILLabel jump = null;
            ILLabel jump2 = null;
            if (c.TryGotoNext(MoveType.Before,
                i => i.MatchLdarg(0),
                i => i.MatchLdfld<RoomCamera>("voidSeaMode"),
                i => i.MatchBrtrue(out jump),
                i => i.MatchLdloca(out _)
                ))
            {
                var b = c.Index;
                c.GotoLabel(jump);
                if (c.Prev.MatchBr(out jump2))
                {
                    try
                    {
                        c.Index = b + 3; // NOT void Sea

                        c.GotoNext(MoveType.After, i => i.MatchCallOrCallvirt<RoomCamera>("get_hDisplace")); // IL_00A7

                        c.Emit(OpCodes.Ldarg_0); // RoomCamera
                        c.EmitDelegate<Func<float, RoomCamera, float>>((v, rc) =>
                        {
                            if (CurrentSplitMode == SplitMode.SplitVertical)
                            {
                                return v - rc.sSize.x / 4f;
                            }
                            return v;
                        });

                        c.GotoNext(MoveType.After, i => i.MatchCallOrCallvirt<RoomCamera>("get_hDisplace")); // IL_00CF

                        c.Emit(OpCodes.Ldarg_0); // RoomCamera
                        c.EmitDelegate<Func<float, RoomCamera, float>>((v, rc) =>
                        {
                            if (CurrentSplitMode == SplitMode.SplitVertical)
                            {
                                return v + rc.sSize.x / 4f;
                            }
                            return v;
                        });

                        c.GotoNext(MoveType.After, i => i.MatchLdfld<RoomCamera>("splitScreenMode"), i => i.MatchBrtrue(out _)); // IL_0116
                        c.MoveAfterLabels();
                        c.Emit(OpCodes.Ldarg_0); // RoomCamera
                        c.EmitDelegate<Func<float, RoomCamera, float>>((v, rc) =>
                        {
                            if (CurrentSplitMode == SplitMode.SplitHorizontal)
                            {
                                return v + rc.sSize.y / 4f;
                            }
                            return v;
                        });


                        c.GotoNext(MoveType.After, i => i.MatchLdfld<RoomCamera>("splitScreenMode"), i => i.MatchBrtrue(out _)); // IL_014C
                        c.MoveAfterLabels();
                        c.Emit(OpCodes.Ldarg_0); // RoomCamera
                        c.EmitDelegate<Func<float, RoomCamera, float>>((v, rc) =>
                        {
                            if (CurrentSplitMode == SplitMode.SplitHorizontal)
                            {
                                return v + rc.sSize.y / 4f;
                            }
                            return v;
                        });
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(new Exception("Couldn't IL-hook RoomCamera_DrawUpdate1 from SplitScreenMod, inner inner spot", e)); // deffendisve progrmanig
                        throw;
                    }
                }
                else Logger.LogError(new Exception("Couldn't IL-hook RoomCamera_DrawUpdate1 from SplitScreenMod, inner spot")); // deffendisve progrmanig
            }
            else Logger.LogError(new Exception("Couldn't IL-hook RoomCamera_DrawUpdate1 from SplitScreenMod")); // deffendisve progrmanig
        }
    }
}
