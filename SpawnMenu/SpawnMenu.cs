using System;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using UnityEngine;
using BepInEx;
using BepInEx.Configuration;
using MonoMod.RuntimeDetour;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using BepInEx.Logging;
using System.Runtime.InteropServices;
using MonoMod.RuntimeDetour.HookGen;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using RWCustom;
using Menu;

[module: UnverifiableCode]
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace SpawnMenu
{
    [BepInPlugin("com.henpemaz.spawnmenu", "Spawn Menu", "0.1.0")]
    public class SpawnMenu : BaseUnityPlugin
    {
        public void OnEnable()
        {
            Logger.LogInfo("OnEnable");
            sLogger = Logger;
            On.RainWorld.OnModsInit += OnModsInit;

            
        }
        public bool init;
        public static ManualLogSource sLogger;
        private int currentRoomIndex;

        public void OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            try
            {
                if (init) return;
                init = true;
                Logger.LogInfo("OnModsInit");

                On.Menu.PauseMenu.ctor += PauseMenu_ctor;
                On.Menu.PauseMenu.Update += PauseMenu_Update;
                On.Menu.PauseMenu.GrafUpdate += PauseMenu_GrafUpdate;
                On.Menu.PauseMenu.ShutDownProcess += PauseMenu_ShutDownProcess;

                new Hook(typeof(ArenaBehaviors.ArenaGameBehavior).GetProperty("room").GetGetMethod(), typeof(SpawnMenu).GetMethod("get_room"), this);

                On.ArenaBehaviors.SandboxEditor.LoadConfig += SandboxEditor_LoadConfig;
                On.ArenaBehaviors.SandboxEditor.AddIcon_IconSymbolData_Vector2_EntityID_bool_bool += SandboxEditor_AddIcon_IconSymbolData_Vector2_EntityID_bool_bool;
                On.ArenaBehaviors.SandboxEditor.CreatureOrItemIcon.DrawSprites += CreatureOrItemIcon_DrawSprites;

                On.ArenaBehaviors.SandboxEditor.EditCursor.Update += EditCursor_Update;
                On.ArenaBehaviors.SandboxEditor.EditCursor.OverseerEyePos += EditCursor_OverseerEyePos;
                new Hook(typeof(ArenaBehaviors.SandboxEditor.EditCursor).GetProperty("OverseerActive").GetGetMethod(), typeof(SpawnMenu).GetMethod("get_OverseerActive"), this);
                IL.ArenaBehaviors.SandboxEditor.EditCursor.Update += EditCursor_Update1;

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

        private void EditCursor_Update1(ILContext il)
        {
            try
            {
                var c = new ILCursor(il);
                ILLabel skip = null;
                c.GotoNext(i => i.MatchLdfld<ArenaBehaviors.SandboxEditor.EditCursor>("overseer"));
                c.GotoNext(i => i.MatchBrfalse(out skip));
                c.GotoPrev(i => i.MatchLdfld<ArenaBehaviors.SandboxEditor.EditCursor>("overseer"));
                c.Index--;
                c.Emit(OpCodes.Ldarg_0);
                c.Emit<ArenaBehaviors.SandboxEditor.EditCursor>(OpCodes.Ldfld, "overseer");
                c.Emit(OpCodes.Brfalse, skip);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                throw;
            }
        }

        private Vector2 EditCursor_OverseerEyePos(On.ArenaBehaviors.SandboxEditor.EditCursor.orig_OverseerEyePos orig, ArenaBehaviors.SandboxEditor.EditCursor self, float timeStacker)
        {
            if(self.room.game.session is StoryGameSession)
            {
                if (self.room.game.Players.Count > 0 && self.room.game.Players[0].realizedCreature is Creature c) return c.firstChunk.pos;
                return Vector2.zero;
            }
            return orig(self, timeStacker);
        }

        private void EditCursor_Update(On.ArenaBehaviors.SandboxEditor.EditCursor.orig_Update orig, ArenaBehaviors.SandboxEditor.EditCursor self, bool eu)
        {
            orig(self, eu);
            if (self.room.game.session is StoryGameSession)
            {
                self.quality = 1f;
            }
        }

        public delegate bool orig_OverseerActive(ArenaBehaviors.SandboxEditor.EditCursor self);
        public bool get_OverseerActive(orig_OverseerActive orig, ArenaBehaviors.SandboxEditor.EditCursor self)
        {
            if (self.room.game.session is StoryGameSession)
            {
                return false;
            }
            return orig(self);
        }


        // support being drawn by more than 1 cam
        private void CreatureOrItemIcon_DrawSprites(On.ArenaBehaviors.SandboxEditor.CreatureOrItemIcon.orig_DrawSprites orig, ArenaBehaviors.SandboxEditor.CreatureOrItemIcon self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if (self.symbol == null) // deleted
            {
                sLeaser.CleanSpritesAndRemove();
                return;
            }
            orig(self, sLeaser, rCam, timeStacker, camPos);
        }

        private void SandboxEditor_LoadConfig(On.ArenaBehaviors.SandboxEditor.orig_LoadConfig orig, ArenaBehaviors.SandboxEditor self)
        {
            if (self.sandboxSession.game.IsStorySession)
            {
                self.ClearAll();
                // dont attempt to load
                return;
            }
            orig(self);
        }

        public class SpawnExtra
        {
            public bool dead;
            public bool like;
            public int? seed;
        }

        public ConditionalWeakTable<ArenaBehaviors.SandboxEditor.PlacedIcon, SpawnExtra> spawnExtras = new ConditionalWeakTable<ArenaBehaviors.SandboxEditor.PlacedIcon, SpawnExtra>();
        private bool doDetour;

        private ArenaBehaviors.SandboxEditor.PlacedIcon SandboxEditor_AddIcon_IconSymbolData_Vector2_EntityID_bool_bool(On.ArenaBehaviors.SandboxEditor.orig_AddIcon_IconSymbolData_Vector2_EntityID_bool_bool orig, ArenaBehaviors.SandboxEditor self, IconSymbol.IconSymbolData iconData, Vector2 pos, EntityID ID, bool fadeCircle, bool updatePerfEstimate)
        {
            var ico = orig(self, iconData, pos, ID, fadeCircle, updatePerfEstimate);
            if (self.sandboxSession.game.IsStorySession)
            {
                var extra = new SpawnExtra();
                spawnExtras.Add(ico, extra);
                //spawnExtras[ico] = new SpawnExtra();
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    extra.like = true;
                }
                if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt) || Input.GetKey(KeyCode.AltGr))
                {
                    extra.dead = true;
                }
                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                {
                    self.room.AddObject(new SeedPicker(self.room, ico, this));
                }
            }

            return ico;
        }

        private void PauseMenu_ctor(On.Menu.PauseMenu.orig_ctor orig, Menu.PauseMenu self, ProcessManager manager, RainWorldGame game)
        {
            orig(self, manager, game);

            Logger.LogDebug("PauseMenu_ctor");
            if (!game.IsStorySession || self.game.cameras[0].room == null) return;
            try
            {
                if (manager.arenaSetup == null) // this is required
                {
                    manager.arenaSetup = new ArenaSetup(manager);
                }
                var room = self.game.cameras[0].room;

                SandboxGameSession sb = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(SandboxGameSession)) as SandboxGameSession;
                sb.arenaSitting = new ArenaSitting(manager.arenaSetup.GetOrInitiateGameTypeSetup(ArenaSetup.GameTypeID.Sandbox), new MultiplayerUnlocks(manager.rainWorld.progression, new List<string>()));
                sb.arenaSitting.gameTypeSetup.saveCreatures = true;
                sb.game = game;
                
                room.AddObject(new SandboxOverlayOwner(room, sb, !sb.PlayMode));
                var session = game.session;
                game.session = sb; // MSC tries to game.GetArenaSession unlike the basegame
                sb.overlay.Initiate(false);
                game.session = session;
                for (int l = 0; l < SandboxEditorSelector.Width; l++)
                {
                    for (int m = 0; m < SandboxEditorSelector.Height; m++)
                    {
                        var btn = sb.overlay.sandboxEditorSelector.buttons[l, m];
                        // no slugcat and no actions
                        if ((sb.overlay.sandboxEditorSelector.buttons[l, m] is SandboxEditorSelector.CreatureOrItemButton coib
                            && coib.data.itemType == AbstractPhysicalObject.AbstractObjectType.Creature
                            && coib.data.critType == CreatureTemplate.Type.Slugcat)
                            ||
                            (sb.overlay.sandboxEditorSelector.buttons[l, m] is SandboxEditorSelector.ActionButton ab
                            && (
                                ab.action == SandboxEditorSelector.ActionButton.Action.Play ||
                                ab.action == SandboxEditorSelector.ActionButton.Action.Randomize ||
                                ab.action == SandboxEditorSelector.ActionButton.Action.ConfigA ||
                                ab.action == SandboxEditorSelector.ActionButton.Action.ConfigB ||
                                ab.action == SandboxEditorSelector.ActionButton.Action.ConfigC
                            )))
                        {
                            sb.overlay.sandboxEditorSelector.buttons[l, m] = null;
                            sb.overlay.sandboxEditorSelector.RemoveSubObject(btn);
                            btn.RemoveSprites();
                        }
                    }
                }

                sb.editor = new ArenaBehaviors.SandboxEditor(sb);
                sb.editor.currentConfig = -1;


                var c = new ArenaBehaviors.SandboxEditor.EditCursor(sb.editor, null, 0, new Vector2(-1000, -1000));
                sb.editor.cursors.Add(c);
                room.AddObject(c);

                sb.overlay.sandboxEditorSelector.ConnectToEditor(sb.editor);

                sb.sandboxInitiated = true;
                sb.overlay.fadingOut = true;
                Logger.LogDebug("PauseMenu_ctor trully really fully done");
            }
            catch (Exception e)
            {
                Logger.LogError("SpawnMenuMod failed to create sandbox menu");
                Logger.LogError(e);
            }
        }

        private void PauseMenu_Update(On.Menu.PauseMenu.orig_Update orig, Menu.PauseMenu self)
        {
            if (!self.game.IsStorySession || self.game.pauseMenu == null || self.game.cameras[0].room == null) // none of my bisness
            {
                orig(self);
                return;
            }
            var was = self.game.pauseMenu;
            self.game.pauseMenu = null; // several menu thinghies check this and stop working :(
            // some room thinghies need updating
            var room = self.game.cameras[0].room;
            var overlayowner = room.updateList.First(o => o is SandboxOverlayOwner) as SandboxOverlayOwner;
            foreach (var uad in room.updateList.ToList())
            {
                if (uad is ArenaBehaviors.SandboxEditor.PlacedIcon
                    || uad is SeedPicker
                    || uad is SandboxOverlayOwner
                    || uad is ArenaBehaviors.SandboxEditor.EditCursor
                    )
                {
                    uad.Update(false);
                }

            }
            self.game.pauseMenu = was; // done here
            // grab processed by our menus not pause menu
            // if doing anything, pause buttons shut down
            if ((overlayowner.overlay.sandboxEditorSelector.currentlyVisible
                    || overlayowner.overlay.sandboxEditorSelector.editor.cursors[0].homeInIcon != null
                    || overlayowner.overlay.sandboxEditorSelector.editor.cursors[0].dragIcon != null)
                && self.manager.upcomingProcess == null) // menus freeze input if there's an upcoming process, we use that here to pause the pause menu
            {
                self.pressButton = false; //  prevent processing the grab input that brought up the menu                
                self.manager.upcomingProcess = self.ID;
                orig(self);
                self.manager.upcomingProcess = null;
            }
            else
            {
                orig(self);
            }

            // fade ctrls on menuing
            if (self.controlMap != null && (overlayowner.selector.visFac > 0f || overlayowner.selector.lastVisFac > 0f))
            {
                // this could be rewritten into a hook for ctrlmap update or something.
                self.controlMap.fade = Mathf.Clamp01(self.controlMap.fade - overlayowner.selector.visFac);
                self.controlMap.lastFade = Mathf.Clamp01(self.controlMap.lastFade - overlayowner.selector.lastVisFac);
                // re calc everything
                if(self.controlMap.controlsMap != null)
                {
                    self.controlMap.controlsMap.setAlpha = new float?(self.controlMap.fade);
                }

                if(self.controlMap.controlsMap2 != null)
                {
                    self.controlMap.controlsMap2.setAlpha = new float?(Mathf.Min(self.controlMap.fade, Custom.SCurve(Mathf.InverseLerp(5f, 80f, (float)self.controlMap.counter), 0.8f)));
                }

                if (self.controlMap.controlsMap3 != null)
                {
                    self.controlMap.controlsMap3.setAlpha = new float?(Mathf.Min(self.controlMap.fade, Custom.SCurve(Mathf.InverseLerp(5f, 80f, (float)self.controlMap.counter), 0.8f)) * 0.5f);
                }

                if (self.controlMap.pickupButtonInstructions != null)
                {
                    self.controlMap.pickupFade = Mathf.Clamp01(Custom.SCurve(Mathf.InverseLerp(40f, 120f, (float)self.controlMap.counter), 0.5f) - overlayowner.selector.visFac);
                    self.controlMap.lastPickupFade = Mathf.Clamp01(Custom.SCurve(Mathf.InverseLerp(40f, 120f, (float)self.controlMap.counter - 1f), 0.5f) - overlayowner.selector.lastVisFac);
                }
            }
        }

        private void PauseMenu_GrafUpdate(On.Menu.PauseMenu.orig_GrafUpdate orig, Menu.PauseMenu self, float timeStacker)
        {
            orig(self, timeStacker);
            if (!self.game.IsStorySession || self.game.pauseMenu == null) return;
            foreach (var cam in self.game.cameras) cam.DrawUpdate(0f, 1f);
            // so icons and cursor also update otherwise this would get quite verbose in here
            // timespeed 1 so audio doesnt glitch out
        }

        // close everything and actually spawn stuff
        private void PauseMenu_ShutDownProcess(On.Menu.PauseMenu.orig_ShutDownProcess orig, Menu.PauseMenu self)
        {
            var wasSingleWorld = self.game.world.singleRoomWorld;
            try
            {
                if (self.game.IsStorySession)
                {
                    var room = self.game.cameras[0].room;
                    var overlayowner = room.updateList.FirstOrDefault(o => o is SandboxOverlayOwner && !o.slatedForDeletetion) as SandboxOverlayOwner;

                    if (overlayowner is null) return;
                    var editor = overlayowner.gameSession.editor;
                    overlayowner.gameSession.PlayMode = true;

                    // buncha fixes
                    On.World.GetAbstractRoom_int += World_GetAbstractRoom_int;
                    doDetour = false;
                    On.WorldCoordinate.ctor_int_int_int_int += WorldCoordinate_ctor;

                    currentRoomIndex = room.abstractRoom.index;

                    foreach (var ico in editor.icons)
                    {
                        if (ico is ArenaBehaviors.SandboxEditor.CreatureOrItemIcon coii)
                        {
                            var data = new ArenaBehaviors.SandboxEditor.PlacedIconData(coii.pos, coii.iconData, coii.ID);
                            var extras = spawnExtras.GetOrCreateValue(ico);
                            if (extras != null && extras.seed != null) data.ID.number = extras.seed.Value;
                            Logger.LogInfo($"SpawnMenu spawning {coii.iconData.itemType} {coii.iconData.critType}");

                            doDetour = true;
                            room.world.singleRoomWorld = true; // deer ai checks this, miros too
                            overlayowner.gameSession.SpawnEntity(data);
                            room.world.singleRoomWorld = false; //
                            doDetour = false;

                            if (room.abstractRoom.entities.Last() is AbstractPhysicalObject apo)
                            {
                                if (extras != null && apo is AbstractCreature ac)
                                {
                                    if (extras.like && ac.state is CreatureState cs && cs.socialMemory != null)
                                    {
                                        foreach (var p in self.game.Players)
                                        {
                                            var rel = cs.socialMemory.GetOrInitiateRelationship(p.ID);
                                            rel.InfluenceLike(1000f);
                                            rel.InfluenceTempLike(1000f);
                                            rel.InfluenceKnow(1000f);
                                        }
                                    }
                                }

                                apo.RealizeInRoom();

                                if (extras != null && apo is AbstractCreature ac2)
                                {
                                    if (extras.dead && ac2.realizedCreature != null)
                                    {
                                        ac2.realizedCreature.Die();
                                    }
                                }
                            }
                        }
                        ico.Fade();
                    }

                    overlayowner.Destroy();
                    overlayowner.overlay.ShutDownProcess();

                    (room.updateList.FirstOrDefault(o => o is ArenaBehaviors.SandboxEditor.EditCursor && !o.slatedForDeletetion) as ArenaBehaviors.SandboxEditor.EditCursor)?.Destroy();
                    // with splitscreen, slugbase nullrefs on a hook if one of these is deleted, removed from room, then drawn. not too sure how
                    // pretty crazy ngl
                    foreach (var cam in self.game.cameras)
                        foreach (var sl in cam.spriteLeasers.ToList())
                            if (sl.drawableObject is ArenaBehaviors.SandboxEditor.EditCursor ec && ec.slatedForDeletetion
                                || sl.drawableObject is SandboxOverlayOwner ow && ow.slatedForDeletetion)
                            {
                                sl.CleanSpritesAndRemove();
                                cam.spriteLeasers.Remove(sl);
                            }
                }
            }
            finally
            {
                self.game.world.singleRoomWorld = wasSingleWorld;
                On.WorldCoordinate.ctor_int_int_int_int -= WorldCoordinate_ctor;
                On.World.GetAbstractRoom_int -= World_GetAbstractRoom_int;
                doDetour = false;

                orig(self);
            }
        }

        // temp hooks during spawning
        private void WorldCoordinate_ctor(On.WorldCoordinate.orig_ctor_int_int_int_int orig, ref WorldCoordinate self, int room, int x, int y, int abstractNode)
        {
            if (doDetour)
                orig(ref self, currentRoomIndex, x, y, abstractNode);
            else
                orig(ref self, room, x, y, abstractNode);
        }
        // temp hooks during spawning
        private AbstractRoom World_GetAbstractRoom_int(On.World.orig_GetAbstractRoom_int orig, World self, int room)
        {
            if (doDetour)
                return self.game.cameras[0].room.abstractRoom;
            return orig(self, room);
        }

        public delegate Room orig_get_room(ArenaBehaviors.ArenaGameBehavior bhv);
        public Room get_room(orig_get_room orig, ArenaBehaviors.ArenaGameBehavior self)
        {
            if (self.gameSession.game.IsStorySession)
                return self.gameSession.game.cameras[0].room;
            return orig(self);
        }

        internal class SeedPicker : UpdatableAndDeletable, IDrawable
        {
            private ArenaBehaviors.SandboxEditor.PlacedIcon ico;
            private readonly SpawnMenu spawnMenuMod;
            private float lastAlpha;
            private float alpha;
            private Vector2 iconOffset;
            private int counter;
            private FLabel label;
            private string text;
            private bool active;

            public SeedPicker(Room room, ArenaBehaviors.SandboxEditor.PlacedIcon ico, SpawnMenu spawnMenuMod)
            {
                this.room = room;
                this.ico = ico;
                this.spawnMenuMod = spawnMenuMod;
                this.lastAlpha = 1f;
                this.alpha = 1f;

                this.text = "";

                this.active = true;
                foreach (var uad in room.updateList) if (uad is SeedPicker picker) picker.active = false;

                iconOffset = new Vector2(22f, 0f);

                room.game.rainWorld.StartCoroutine(TextUpdater()); // engine update rate inputs
            }

            public override void Update(bool eu)
            {
                base.Update(eu);
                if (slatedForDeletetion) return;

                this.counter++;

                lastAlpha = alpha;
                alpha = 0.5f * alpha + 0.5f * (active ? Mathf.Clamp01(Mathf.Pow(UnityEngine.Random.Range(0.5f, 1.4f), 0.75f)) : Mathf.Pow(UnityEngine.Random.Range(0.6f, 0.9f), 0.5f));

                iconOffset = new Vector2(22f, 0f);
                if (active && alpha < 0.85f) iconOffset += UnityEngine.Random.insideUnitCircle * 1.5f;
                var extra = spawnMenuMod.spawnExtras.GetOrCreateValue(ico);
                if(extra == null)
                {
                    Destroy();
                    return;
                }
                if (int.TryParse(text, out int res)) extra.seed = res;
                else extra.seed = null;
            }

            public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner) { }

            public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette) { }

            public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
            {
                if (slatedForDeletetion || room != rCam.room)
                {
                    label = null;
                    sLeaser.CleanSpritesAndRemove();
                    return;
                }

                if (ico.slatedForDeletetion)
                {
                    this.Destroy();
                    return;
                }

                var blendedalpha = Mathf.Lerp(lastAlpha, alpha, timeStacker);
                var refPos = -camPos + ico.DrawPos(timeStacker) + iconOffset;

                var backdrop = sLeaser.sprites[0];
                var cursor = sLeaser.sprites[1];
                backdrop.SetPosition(refPos);
                backdrop.alpha = active ? 0.92f : 0.4f;

                cursor.isVisible = active && (counter / 20) % 2 == 0;
                cursor.SetPosition(refPos + new Vector2(text.Length * 12f, 0));
                cursor.alpha = blendedalpha;

                label.text = text;
                label.SetPosition(refPos);
                label.alpha = blendedalpha;
            }

            public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                var hud = rCam.ReturnFContainer("HUD");
                var cursor = new FSprite("Futile_White")
                {
                    scaleX = 2f / 16f,
                    scaleY = 24f / 16f,
                    shader = rCam.game.rainWorld.Shaders["Hologram"]
                };
                hud.AddChild(cursor);
                var backdrop = new FSprite("Futile_White")
                {
                    color = new Color(0, 0, 0),
                    alpha = 0.8f,
                    anchorX = 0.2f,
                    anchorY = 0.5f,
                    scaleX = 100f / 16f,
                    scaleY = 32f / 16f,
                    shader = rCam.game.rainWorld.Shaders["FlatLight"]
                };
                hud.AddChild(backdrop);

                sLeaser.sprites = new FSprite[] { backdrop, cursor };
                FContainer c;
                sLeaser.containers = new FContainer[1] { c = new FContainer() };
                hud.AddChild(c);
                label = new FLabel("DisplayFont", "ASDF")
                {
                    shader = rCam.game.rainWorld.Shaders["Hologram"],
                    alignment = FLabelAlignment.Left,
                    color = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.MediumGrey)
                };
                c.AddChild(label);
            }

            System.Collections.IEnumerator TextUpdater() // runs at engine update rate
            {
                yield return null;

                while (!this.slatedForDeletetion && room.game.processActive)
                {
                    if (this.active)
                    {
                        foreach (char c in Input.inputString)
                        {
                            if (c == '\b')
                            {
                                if (text.Length != 0)
                                {
                                    text = text.Substring(0, text.Length - 1);
                                }
                            }
                            else if (c == '\n' || c == '\r')
                            {
                                this.active = false;
                            }
                            else
                            {
                                if (char.IsDigit(c) || (text.Length == 0 && c == '-'))
                                {
                                    text += c;
                                }
                            }
                        }
                    }
                    yield return null;
                }
            }
        }
    }
}
