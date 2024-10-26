using System;
using System.Security.Permissions;
using System.Security;
using BepInEx;
using RWCustom;
using UnityEngine;
using System.Linq;
using LizardCosmetics;

[module: UnverifiableCode]
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace RemixMods
{
    [BepInPlugin("com.henpemaz.remixmods", "Remix Mods", "0.1.0")]
    public class RemixMods : BaseUnityPlugin
    {
        public void OnEnable()
        {
            Logger.LogInfo("Enabled");
            On.RainWorld.OnModsInit += OnModsInit;
        }
        int nextIssuedId = 0;

        private void OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);

            Logger.LogInfo("Hello world! im new");

            On.Room.Loaded += Room_Loaded;
            On.Room.LoadFromDataString += Room_LoadFromDataString;

            On.Room.Update += Room_Update;
            On.RoomCamera.CamPos += RoomCamera_CamPos;
            On.RoomCamera.DrawUpdate += RoomCamera_DrawUpdate;
            On.RoomCamera.ctor += RoomCamera_ctor;

            On.LizardGraphics.ctor += LizardGraphics_ctor;
            On.LizardGraphics.Update += LizardGraphics_Update;
            On.LizardGraphics.DrawSprites += LizardGraphics_DrawSprites;

            On.RainWorldGame.SpawnPlayers_bool_bool_bool_bool_WorldCoordinate += RainWorldGame_SpawnPlayers_bool_bool_bool_bool_WorldCoordinate;

            On.ShortcutGraphics.Draw += ShortcutGraphics_Draw;
            On.Room.ReadyForAI += Room_ReadyForAI;
            Futile.instance.camera.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);

            On.RoomCamera.NewObjectInRoom += RoomCamera_NewObjectInRoom;

            On.LizardAI.Update += LizardAI_Update;
            On.Lizard.Act += Lizard_Act;

            On.ProcessManager.RequestMainProcessSwitch_ProcessID_float += ProcessManager_RequestMainProcessSwitch_ProcessID_float;
            On.ProcessManager.PostSwitchMainProcess += ProcessManager_PostSwitchMainProcess;

            On.StoryGameSession.ctor += StoryGameSession_ctor;
        }

        private void StoryGameSession_ctor(On.StoryGameSession.orig_ctor orig, StoryGameSession self, SlugcatStats.Name saveStateNumber, RainWorldGame game)
        {
            orig(self, saveStateNumber, game);
            game.nextIssuedId = nextIssuedId;
        }

        private void ProcessManager_PostSwitchMainProcess(On.ProcessManager.orig_PostSwitchMainProcess orig, ProcessManager self, ProcessManager.ProcessID ID)
        {
            orig(self, ID);
            self.blackDelay = 0f;
            self.blackFadeTime = 0.01f;
        }

        private void ProcessManager_RequestMainProcessSwitch_ProcessID_float(On.ProcessManager.orig_RequestMainProcessSwitch_ProcessID_float orig, ProcessManager self, ProcessManager.ProcessID ID, float fadeOutSeconds)
        {
            orig(self, ID, 0.01f);
        }

        private void Lizard_Act(On.Lizard.orig_Act orig, Lizard self)
        {
            return;
        }

        private void LizardAI_Update(On.LizardAI.orig_Update orig, LizardAI self)
        {
            return;
        }

        private void RoomCamera_NewObjectInRoom(On.RoomCamera.orig_NewObjectInRoom orig, RoomCamera self, IDrawable obj)
        {
            //Logger.LogInfo(obj);
            if (obj is DeathFallGraphic) return; // stop
            if (obj is LizardBubble) return;
            orig(self, obj);
        }

        private void RoomCamera_ctor(On.RoomCamera.orig_ctor orig, RoomCamera self, RainWorldGame game, int cameraNumber)
        {
            orig(self, game, cameraNumber);
            //for (int i = 0; i < self.SpriteLayers.Length; i++)
            //{
            //    self.SpriteLayers[i].RemoveAllChildren();
            //}
        }

        private AbstractCreature RainWorldGame_SpawnPlayers_bool_bool_bool_bool_WorldCoordinate(On.RainWorldGame.orig_SpawnPlayers_bool_bool_bool_bool_WorldCoordinate orig, RainWorldGame self, bool player1, bool player2, bool player3, bool player4, WorldCoordinate location)
        {
            AbstractCreature abstractCreature;
            abstractCreature = new AbstractCreature(self.world, StaticWorld.GetCreatureTemplate("Slugcat"), null, location, new EntityID(-1, 0));
            abstractCreature.state = new PlayerState(abstractCreature, 0, self.GetStorySession.saveState.saveStateNumber, false);
            //self.world.GetAbstractRoom(abstractCreature.pos.room).AddEntity(abstractCreature);
            self.session.AddPlayer(abstractCreature);
            return abstractCreature;
        }

        private void Room_LoadFromDataString(On.Room.orig_LoadFromDataString orig, Room self, string[] lines)
        {
            self.abstractRoom.firstTimeRealized = false;
            orig(self, lines);
        }

        private void LizardGraphics_DrawSprites(On.LizardGraphics.orig_DrawSprites orig, LizardGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            self.lizard.stun = 0;
            orig(self, sLeaser, rCam, timeStacker, camPos);
            self.lizard.stun = 20;
        }


        private void Room_ReadyForAI(On.Room.orig_ReadyForAI orig, Room self)
        {
            orig(self);
            for (var x = 0; x < self.TileWidth; x++)
                for (var y = 0; y < self.TileHeight; y++)
                {
                    self.Tiles[x, y].Terrain = Room.Tile.TerrainType.Air;
                }
        }

        private void Room_Loaded(On.Room.orig_Loaded orig, Room self)
        {
            self.roomSettings.placedObjects.Clear();
            self.roomSettings.effects.Clear();
            self.roomSettings.CeilingDrips = 0f;
            self.gravity = 0f;
            orig(self);
        }

        private Vector2 RoomCamera_CamPos(On.RoomCamera.orig_CamPos orig, RoomCamera self, int index)
        {
            return new Vector2(0f, 0f);
        }

        private void RoomCamera_DrawUpdate(On.RoomCamera.orig_DrawUpdate orig, RoomCamera self, float timeStacker, float timeSpeed)
        {
            self.pos = new Vector2(0f, 0f);
            self.currentPalette.darkness = 0f;
            self.lastPos = self.pos;
            orig(self, timeStacker, timeSpeed);
            self.levelGraphic.x = 2000f;
            if (self.waterLight != null) self.waterLight.sprite.isVisible = false;
            if (self.fullScreenEffect != null) self.fullScreenEffect.isVisible = false;
        }

        private void ShortcutGraphics_Draw(On.ShortcutGraphics.orig_Draw orig, ShortcutGraphics self, float timeStacker, Vector2 camPos)
        {
            orig(self, timeStacker, camPos + new Vector2(2000f, 0f));
        }

        private void LizardGraphics_Update(On.LizardGraphics.orig_Update orig, LizardGraphics self)
        {
            self.flicker = 0;

            self.blink = 0.75f;
            self.lastBlink = 0.75f;
            self.headColorSetter = 1f;
            self.blackLizardLightUpHead = 1f;
            self.whiteCamoColorAmount = 0f;

            var wasrot = self.depthRotation;
            if(self.creatureLooker != null)
            {
                self.creatureLooker.LookAtNothing();
                self.creatureLooker.lookFocusDelay = 100;
            }
            self.lizard.AI.focusCreature = null;
            orig(self);
            self.depthRotation = wasrot;
            self.headDepthRotation = wasrot;

            self.blink = 0.75f;
            self.lastBlink = 0.75f;
            self.headColorSetter = 1f;
            self.blackLizardLightUpHead = 1f;
            self.whiteCamoColorAmount = 0f;


            self.DEBUGLABELS[0].label.text = self.lizard.abstractCreature.ID.RandomSeed.ToString();
            self.DEBUGLABELS[1].label.text = "";
            var gah = (Vector3)(Vector4)(self.lizard.effectColor * 255f);
            self.DEBUGLABELS[2].label.text = "";// new Vector3Int((int)gah.x, (int)gah.y, (int)gah.z).ToString();
            //self.DEBUGLABELS[2].label.rotation = 15f;
            self.DEBUGLABELS[3].label.text = "";
        }

        private void LizardGraphics_ctor(On.LizardGraphics.orig_ctor orig, LizardGraphics self, PhysicalObject ow)
        {
            orig(self, ow);
            self.blink = 0.75f;
            self.lastBlink = 0.75f;
            self.headColorSetter = 1f;
            
            if (self.iVars.headSize < 1f) ow.Destroy();
            if (self.iVars.fatness < 0.9f) ow.Destroy();
            if (self.iVars.fatness > 1.1f) ow.Destroy();
            if (self.iVars.tailFatness < 0.9f) ow.Destroy();
            if (self.iVars.tailFatness > 1.05f) ow.Destroy();
            if (self.iVars.tailLength < 0.8f) ow.Destroy();
            if (self.iVars.tailLength > 1.3f) ow.Destroy();

            // pink filter
            //if (self.iVars.tailColor < 0.5f) ow.Destroy();
            //if (!self.cosmetics.Any(c => c is LizardCosmetics.LongShoulderScales)
            //    && !self.cosmetics.Any(c => c is LizardCosmetics.LongHeadScales)
            //    && !self.cosmetics.Any(c => c is LizardCosmetics.TailTuft)
            //    ) ow.Destroy();
            //if (((Vector3)(Vector4)(self.HeadColor(0f) - new Color(255f / 255f, 27f / 255f, 180f / 255f))).magnitude > 0.2f) ow.Destroy();
            //if (self.cosmetics.Any(c => c is LizardCosmetics.TailTuft tt && (tt.scaleObjects.Length > 12 || tt.scaleObjects.Length < 4))) ow.Destroy();
            //if (self.cosmetics.Any(c => c is LizardCosmetics.LongShoulderScales lss && (lss.scaleObjects.Length > 12 || lss.scaleObjects.Length < 7))) ow.Destroy();
            //if (self.cosmetics.Any(c=>c is LizardCosmetics.SpineSpikes
            //                       || c is LizardCosmetics.BumpHawk
            //                       || c is LizardCosmetics.ShortBodyScales
            //                        )) ow.Destroy();

            // blue filter
            //if (self.iVars.headSize < 1.05f) ow.Destroy();
            //if (!self.cosmetics.Any(c => c is LizardCosmetics.ShortBodyScales)
            //    && !self.cosmetics.Any(c => c is LizardCosmetics.TailTuft)
            //    ) ow.Destroy();
            //if (((Vector3)(Vector4)(self.HeadColor(0f) - new Color(0f / 255f, 54f / 255f, 247f / 255f))).magnitude > 0.4f) ow.Destroy();
            ////if (self.cosmetics.Any(c => c is LizardCosmetics.ShortBodyScales sbs && (sbs.scalesPositions.Length > 12 || sbs.scalesPositions.Length < 8))) ow.Destroy();
            //if (!self.cosmetics.Any(c => c is LizardCosmetics.TailTuft tt && (tt.scaleObjects.Length > 8) && tt.colored)) ow.Destroy();
            ////if (self.cosmetics.Any(c=>c is LizardCosmetics.SpineSpikes
            ////                       || c is LizardCosmetics.BumpHawk
            ////                       || c is LizardCosmetics.LongShoulderScales
            ////                        )) ow.Destroy();

            //// Yello filter
            //if (self.iVars.headSize > 1.08f) ow.Destroy();
            //if (!self.cosmetics.Any(c => c is LizardCosmetics.ShortBodyScales)
            //    || !self.cosmetics.Any(c => c is LizardCosmetics.TailTuft) // maybe I want this
            //    ) ow.Destroy();
            //if (((Vector3)(Vector4)(self.HeadColor(0f) - new Color(253f / 255f, 192f / 255f, 30f / 255f))).magnitude > 0.2f) ow.Destroy();
            ////if (self.cosmetics.Any(c => c is LizardCosmetics.ShortBodyScales sbs && (sbs.scalesPositions.Length < 8))) ow.Destroy();
            //if (!self.cosmetics.Any(c => c is LizardCosmetics.ShortBodyScales sbs && sbs.scalesPositions.Length > 6 && (sbs.scalesPositions[sbs.scalesPositions.Length-1].y > 0.44f))) ow.Destroy();
            //if (self.cosmetics.Any(c => c is LizardCosmetics.TailTuft tt && (tt.scaleObjects.Length > 14) )) ow.Destroy();
            //if (self.cosmetics.Any(c => c is LizardCosmetics.TailTuft tt) && !self.cosmetics.Any(c => c is LizardCosmetics.TailTuft tt && tt.colored)) ow.Destroy();
            //if (self.cosmetics.Any(c=>c is LizardCosmetics.SpineSpikes
            //                       || c is LizardCosmetics.BumpHawk
            //                       || c is LizardCosmetics.LongShoulderScales
            //                        )) ow.Destroy();

            //// cyan filter
            //if (self.iVars.tailColor < 0.4f) ow.Destroy();
            //if (self.cosmetics.Any(c => c is LizardCosmetics.TailTuft)
            //    || !self.cosmetics.Any(c => c is LizardCosmetics.TailGeckoScales)
            //    || !self.cosmetics.Any(c => c is LizardCosmetics.LongShoulderScales || c is LizardCosmetics.WingScales)
            //    ) ow.Destroy();
            //if (((Vector3)(Vector4)(self.HeadColor(0f) - new Color(32f / 255f, 237f / 255f, 255f / 255f))).magnitude > 0.2f) ow.Destroy();
            //if (self.cosmetics.Any(c => c is LizardCosmetics.WingScales ws &&
            //(
            //ws.scales.GetLength(1) > 2
            //|| ws.graphic != 0
            //|| ws.scaleLength > 28f
            //// || ws.scaleLength < 12f gecko-tail only with < 10f huh the more you know
            //|| ws.posSqueeze > 0.5f
            //)
            //)) ow.Destroy();

            //// red filter
            //if (self.iVars.tailLength < 1f) ow.Destroy();
            //if (!self.cosmetics.Any(c => c is LongShoulderScales)
            //    &&
            //    !self.cosmetics.Any(c => c is TailTuft)
            //    ) ow.Destroy();

            //if (self.cosmetics.Any(c => c is TailFin)
            //    &&
            //    self.cosmetics.Any(c => c is TailTuft)
            //    ) ow.Destroy();

            //if (((Vector3)(Vector4)(self.HeadColor(0f) - new Color(210f / 255f, 4f / 255f, 45f / 255f))).magnitude > 0.25f) ow.Destroy();
            //if (!self.cosmetics.Any(c => c is LongShoulderScales lss && lss.scaleObjects.Length > 30 && lss.scaleObjects.Length < 60)) ow.Destroy();
            //if (self.cosmetics.Any(c => c is TailTuft tt && tt.scaleObjects.Length < 8)) ow.Destroy();
            //if (self.cosmetics.Any(c=>c is LongHeadScales
            //                       || c is BumpHawk
            //                       || c is ShortBodyScales
            //                        )) ow.Destroy();
            //if (self.cosmetics.Any(c => c is LongBodyScales l && !(l is LongShoulderScales) && !l.colored)
            //    || self.cosmetics.Any(c => c is TailFin l && !l.colored)
            //    || self.cosmetics.Any(c => c is SpineSpikes l && l.colored == 0)
            //    ) ow.Destroy();

            //// black lizard
            //if (self.iVars.fatness < 0.82f) ow.Destroy();
            //if (self.iVars.fatness > 0.98f) ow.Destroy();
            //if (!self.cosmetics.Any(c => c is SpineSpikes)
            //    ) ow.Destroy();
            //if (self.cosmetics.Any(c => c is SpineSpikes ss && ss.bumps < 7)) ow.Destroy();
            //if (self.cosmetics.Any(c => c is Whiskers w && w.whiskerDirections.Any(d => d.y < 0.3))) ow.Destroy();

            //// white
            //if(self.cosmetics.Count < 2) ow.Destroy();

            // salamanders
            self.blackSalamander = true;
            if (self.cosmetics.Any(c=>c is SpineSpikes
                                   || c is BumpHawk
                                   || c is ShortBodyScales
                                    )) ow.Destroy();
            //if (((Vector3)(Vector4)(self.effectColor - new Color(230f / 255f, 0f / 255f, 85f / 255f))).magnitude > 0.25f) ow.Destroy();
            if (self.cosmetics.Any(c => c is TailFin tf && (!tf.colored ||
            (Futile.atlasManager.GetElementWithName("LizardScaleA" + tf.graphic).sourcePixelSize.y * tf.sizeRangeMax is < 10)))) ow.Destroy();

            if (!ow.slatedForDeletetion)
            {
                self.DEBUGLABELS = new DebugLabel[4];
                self.DEBUGLABELS[0] = new DebugLabel(ow, new Vector2(20f, 15f));
                self.DEBUGLABELS[1] = new DebugLabel(ow, new Vector2(20f, 25f));
                self.DEBUGLABELS[2] = new DebugLabel(ow, new Vector2(20f, 35f));
                self.DEBUGLABELS[3] = new DebugLabel(ow, new Vector2(20f, 45f));
            }
        }

        private void Room_Update(On.Room.orig_Update orig, Room self)
        {
            orig(self);
            self.game.world.rainCycle.timer = 200;

            var start = new Vector2(40f, 50f);
            Vector2 pinPoint = start;

            Vector2 faceDir = Custom.DegToVec(90f + 360f * Mathf.InverseLerp(300f, 600f, Input.mousePosition.y));
            int count = 0;
            foreach (AbstractCreature c in self.abstractRoom.creatures)
                if (c.realizedCreature != null && c.creatureTemplate.TopAncestor().type == CreatureTemplate.Type.LizardTemplate)
                {
                    count++;
                    Lizard liz = c.realizedCreature as Lizard;

                    liz.stun = 20;

                    float f = 0f;
                    for (int i = 0; i < 3; i++)
                    {
                        liz.bodyChunks[i].pos = pinPoint + faceDir * f;
                        liz.bodyChunks[i].lastPos = liz.bodyChunks[i].pos;
                        liz.bodyChunks[i].vel *= 0f;
                        if (i < 2) f += liz.bodyChunkConnections[i].distance + 1f;
                    }
                    LizardGraphics grphcs = liz.graphicsModule as LizardGraphics;

                    grphcs.head.pos = pinPoint + faceDir * -(grphcs.headConnectionRad + 10f);
                    grphcs.head.vel *= 0f;
                    grphcs.lightSource?.Destroy();

                    grphcs.depthRotation = Mathf.Lerp(-1f, 1f, Mathf.InverseLerp(0f, 1000f, Input.mousePosition.x));

                    for (int i = 0; i < grphcs.limbs.Length; i++)
                    {
                        grphcs.limbs[i].vel.y += 1.8f * Mathf.InverseLerp(500f, 0f, Input.mousePosition.x);
                        grphcs.limbs[i].vel.x *= 0.8f;
                    }

                    for (int i = 0; i < grphcs.tail.Length; i++)
                    {
                        f += grphcs.tail[i].connectionRad + 1f;
                        grphcs.tail[i].pos = pinPoint + faceDir * f;
                        grphcs.tail[i].vel *= 0f;
                    }

                    pinPoint.x += 135f;
                    if (pinPoint.x > start.x + 1300f)
                    {
                        pinPoint.x = start.x ;
                        pinPoint.y += 60f;
                    }
                }

            if(count < 120 && self.readyForAI)
            {
                while (true)
                {
                    // todo sala axo white
                    // will force sala/axo mode, new IL?
                    var l = new AbstractCreature(self.world, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Salamander), null, self.GetWorldCoordinate(Vector2.zero), self.game.GetNewID());
                    nextIssuedId = self.game.nextIssuedId;
                    
                    l.Realize();
                    l.realizedCreature.InitiateGraphicsModule();
                    if (!l.realizedCreature.slatedForDeletetion)
                    {
                        self.abstractRoom.AddEntity(l);
                        l.realizedCreature.PlaceInRoom(self);
                        break;
                    }
                    l = null;
                }
            }
        }
    }
}
