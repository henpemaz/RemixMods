using System;
using BepInEx;
using RWCustom;
using UnityEngine;

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

        private void OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);

            Logger.LogInfo("Hello world! im new");

            On.Room.Update += Room_Update;

            On.LizardGraphics.ctor += LizardGraphics_ctor;
            On.LizardGraphics.Update += LizardGraphics_Update;

            On.RoomCamera.DrawUpdate += RoomCamera_DrawUpdate;

        }

        private void RoomCamera_DrawUpdate(On.RoomCamera.orig_DrawUpdate orig, RoomCamera self, float timeStacker, float timeSpeed)
        {
            orig(self, timeStacker, timeSpeed);
            self.levelGraphic.x = 2000f;
        }

        private void LizardGraphics_Update(On.LizardGraphics.orig_Update orig, LizardGraphics self)
        {
            orig(self);
            self.DEBUGLABELS[0].label.text = self.lizard.abstractCreature.ID.RandomSeed.ToString();
            self.DEBUGLABELS[1].label.text = "";
            self.DEBUGLABELS[2].label.text = "";
            self.DEBUGLABELS[3].label.text = "";
        }

        private void LizardGraphics_ctor(On.LizardGraphics.orig_ctor orig, LizardGraphics self, PhysicalObject ow)
        {
            orig(self, ow);
            self.DEBUGLABELS = new DebugLabel[4];
            self.DEBUGLABELS[0] = new DebugLabel(ow, new Vector2(40f, 50f));
            self.DEBUGLABELS[1] = new DebugLabel(ow, new Vector2(40f, 65f));
            self.DEBUGLABELS[2] = new DebugLabel(ow, new Vector2(40f, 35f));
            self.DEBUGLABELS[3] = new DebugLabel(ow, new Vector2(40f, 5f));
        }

        private void Room_Update(On.Room.orig_Update orig, Room self)
        {

            Vector2 pinPoint = new Vector2(-150f, 680f);
            

            Vector2 faceDir = Custom.DegToVec(90f + 360f * Mathf.InverseLerp(300f, 600f, Input.mousePosition.y));
            foreach (AbstractCreature c in self.abstractRoom.creatures)
                if (c.realizedCreature != null && c.creatureTemplate.TopAncestor().type == CreatureTemplate.Type.LizardTemplate)
                {
                    Lizard liz = c.realizedCreature as Lizard;

                    liz.Stun(20);

                    float f = 0f;
                    for (int i = 0; i < 3; i++)
                    {
                        liz.bodyChunks[i].pos = pinPoint + faceDir * f;
                        liz.bodyChunks[i].vel *= 0f;
                        if (i < 2) f += liz.bodyChunkConnections[i].distance + 1f;
                    }
                    LizardGraphics grphcs = liz.graphicsModule as LizardGraphics;

                    grphcs.head.pos = pinPoint + faceDir * -(grphcs.headConnectionRad + 10f);
                    grphcs.head.vel *= 0f;

                    grphcs.depthRotation = Mathf.Lerp(-1f, 1f, Mathf.InverseLerp(0f, 1000f, Input.mousePosition.x));

                    for (int i = 0; i < grphcs.limbs.Length; i++)
                        grphcs.limbs[i].vel.y += 1.8f * Mathf.InverseLerp(500f, 0f, Input.mousePosition.x);


                    for (int i = 0; i < grphcs.tail.Length; i++)
                    {
                        f += grphcs.tail[i].connectionRad + 1f;
                        grphcs.tail[i].pos = pinPoint + faceDir * f;
                        grphcs.tail[i].vel *= 0f;

                    }

                    pinPoint.x += 140f;
                    if (pinPoint.x > 1000f)
                    {
                        pinPoint.x = -150f;
                        pinPoint.y -= 60f;
                    }
                }


            orig(self);

        }
    }
}
