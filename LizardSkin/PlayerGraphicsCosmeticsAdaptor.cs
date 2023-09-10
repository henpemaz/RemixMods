using RWCustom;
using System.Collections.Generic;
using UnityEngine;

namespace LizardSkin
{
    public class PlayerGraphicsCosmeticsAdaptor : GraphicsModuleCosmeticsAdaptor
    {
        public PlayerGraphics pGraphics { get => this.graphics as PlayerGraphics; }
        public Player player { get => this.graphics.owner as Player; }

        // Implementing properties
        public override Vector2 headPos => this.pGraphics.head.pos;
        public override Vector2 headLastPos => this.pGraphics.head.lastPos;
        public override Vector2 baseOfTailPos => this.pGraphics.tail[0].pos;
        public override Vector2 baseOfTailLastPos => this.pGraphics.tail[0].lastPos;
        public override Vector2 mainBodyChunkPos => this.player.mainBodyChunk.pos;
        public override Vector2 mainBodyChunkLastPos => this.player.mainBodyChunk.lastPos;
        public override Vector2 mainBodyChunkVel => this.player.mainBodyChunk.vel;

        public override BodyChunk mainBodyChunckSecret => this.player.mainBodyChunk;

        protected override FNode getOnTopNode(RoomCamera.SpriteLeaser sLeaser) => sLeaser.sprites[6];
        protected override FNode getBehindHeadNode(RoomCamera.SpriteLeaser sLeaser) => sLeaser.sprites[3];
        protected override FNode getBehindNode(RoomCamera.SpriteLeaser sLeaser) => sLeaser.sprites[0];

        public PlayerGraphicsCosmeticsAdaptor(PlayerGraphics pGraphics) : base(pGraphics)
        {
            this.bodyLength = this.pGraphics.player.bodyChunkConnections[0].distance;
            this.tailLength = 0f;

            for (int l = 0; l < this.pGraphics.tail.Length; l++)
            {
                this.tailLength += this.pGraphics.tail[l].connectionRad;
            }

            this.showDominance = 0;
            this.depthRotation = 0;
            this.lastDepthRotation = this.depthRotation;

            List<LizKinCosmeticData> cosmeticDefs = LizardSkin.GetCosmeticsForSlugcat(pGraphics.player.abstractCreature.world.game.IsStorySession, (int)player.slugcatStats.name, (int)player.playerState.slugcatCharacter, player.playerState.playerNumber);

            foreach (LizKinCosmeticData cosmeticData in cosmeticDefs)
            {
                this.AddCosmetic(GenericCosmeticTemplate.MakeCosmetic(this, cosmeticData));
            }
        }

        public override void Update()
        {
            this.bodyLength = this.pGraphics.player.bodyChunkConnections[0].distance;
            this.tailLength = 0f;
            for (int l = 0; l < this.pGraphics.tail.Length; l++)
            {
                this.tailLength += this.pGraphics.tail[l].connectionRad;
            }

            UpdateRotation();
            base.Update();
        }

        protected void UpdateRotation()
        {
            /*
            Completely re-work the rotation system
            Add z-depth info to cosmetic so they render in-front or behind slugcat based on rotation

            OR don't worry about overlap
            but change the logic to sprites that behave like they're on top vs sprites that behave like they're behind (rot *= -1)
             
            note: currently no vanilla cosmetics use SpritesOverlap.Behind, everything is BehindHead or InFront
             
            TODO
            better body depth rotation 
            using face direction is influenced wayyy to much by look direction


            tail rotation
            increase tail rotation towards +- 1 depending on its perpendicularity to updir, correctly handle 180 turns on long tails
             */

            //if (this.pGraphics.player.input[0].jmp)
            //{
            //    this.showDominance += 0.05f;
            //}
            if (this.pGraphics.player.input[0].thrw)
            {
                this.showDominance += 0.2f;
            }
            if (showDominance > 0)
            {
                this.showDominance = Mathf.Clamp(this.showDominance - 1f / Mathf.Lerp(60f, 120f, UnityEngine.Random.value), 0f, 1f);
            }
            this.lastDepthRotation = this.depthRotation;
            this.lastHeadDepthRotation = this.headDepthRotation;
            //this.lastHeadRotation = this.headRotation;


            float newDepth;
            float newHeadDepth;
            //float newHeadRotation;

            // From playergraphics.draw
            Vector2 neck = this.pGraphics.drawPositions[0, 0];
            Vector2 hips = this.pGraphics.drawPositions[1, 0];
            Vector2 head = this.pGraphics.head.pos;
            float breathfac = 0.5f + 0.5f * Mathf.Sin(this.pGraphics.breath * 3.1415927f * 2f);
            //if (this.player.aerobicLevel > 0.5f)
            //{
            //    neck += Custom.DirVec(hips, neck) * Mathf.Lerp(-1f, 1f, breathfac) * Mathf.InverseLerp(0.5f, 1f, this.player.aerobicLevel) * 0.5f;
            //    head -= Custom.DirVec(hips, neck) * Mathf.Lerp(-1f, 1f, breathfac) * Mathf.Pow(Mathf.InverseLerp(0.5f, 1f, this.player.aerobicLevel), 1.5f) * 0.75f;
            //}
            //float tilt = Custom.AimFromOneVectorToAnother(Vector2.Lerp(hips, neck, 0.5f), head);

            if (this.player.aerobicLevel > 0.5f)
            {
                neck += Custom.DirVec(hips, neck) * Mathf.Lerp(-1f, 1f, breathfac) * Mathf.InverseLerp(0.5f, 1f, this.player.aerobicLevel) * 0.5f;
                head -= Custom.DirVec(hips, neck) * Mathf.Lerp(-1f, 1f, breathfac) * Mathf.Pow(Mathf.InverseLerp(0.5f, 1f, this.player.aerobicLevel), 1.5f) * 0.75f;
            }
            float skew = Mathf.InverseLerp(0.3f, 0.5f, Mathf.Abs(Custom.DirVec(hips, neck).y));
            //sLeaser.sprites[0].x = neck.x - camPos.x;
            //sLeaser.sprites[0].y = neck.y - camPos.y - this.player.sleepCurlUp * 4f + Mathf.Lerp(0.5f, 1f, this.player.aerobicLevel) * breathfac * (1f - skew);
            //sLeaser.sprites[0].rotation = Custom.AimFromOneVectorToAnother(hips, neck);
            //sLeaser.sprites[0].scaleX = 1f + Mathf.Lerp(Mathf.Lerp(Mathf.Lerp(-0.05f, -0.15f, this.malnourished), 0.05f, breathfac) * skew, 0.15f, this.player.sleepCurlUp);
            //sLeaser.sprites[1].x = (hips.x * 2f + neck.x) / 3f - camPos.x;
            //sLeaser.sprites[1].y = (hips.y * 2f + neck.y) / 3f - camPos.y - this.player.sleepCurlUp * 3f;
            //sLeaser.sprites[1].rotation = Custom.AimFromOneVectorToAnother(neck, Vector2.Lerp(this.tail[0].lastPos, this.tail[0].pos, timeStacker));
            //sLeaser.sprites[1].scaleY = 1f + this.player.sleepCurlUp * 0.2f;
            //sLeaser.sprites[1].scaleX = 1f + this.player.sleepCurlUp * 0.2f + 0.05f * breathfac - 0.05f * this.malnourished;
            Vector2 previoustailpos = (hips * 3f + neck) / 4f;
            float d = 1f - 0.2f * this.pGraphics.malnourished;
            float d2 = 6f;
            for (int i = 0; i < this.pGraphics.tail.Length; i++)
            {
                Vector2 tailpos = this.pGraphics.tail[i].pos;
                Vector2 taildir = (tailpos - previoustailpos).normalized;
                Vector2 perptaildir = Custom.PerpendicularVector(taildir);
                float d3 = Vector2.Distance(tailpos, previoustailpos) / 5f;
                if (i == 0)
                {
                    d3 = 0f;
                }
                //(sLeaser.sprites[2] as TriangleMesh).MoveVertice(i * 4, previoustailpos - perptaildir * d2 * d + taildir * d3 - camPos);
                //(sLeaser.sprites[2] as TriangleMesh).MoveVertice(i * 4 + 1, previoustailpos + perptaildir * d2 * d + taildir * d3 - camPos);
                if (i < 3)
                {
                    //(sLeaser.sprites[2] as TriangleMesh).MoveVertice(i * 4 + 2, tailpos - perptaildir * this.tail[i].StretchedRad * d - taildir * d3 - camPos);
                    //(sLeaser.sprites[2] as TriangleMesh).MoveVertice(i * 4 + 3, tailpos + perptaildir * this.tail[i].StretchedRad * d - taildir * d3 - camPos);
                }
                else
                {
                    //(sLeaser.sprites[2] as TriangleMesh).MoveVertice(i * 4 + 2, tailpos - camPos);
                }
                d2 = this.pGraphics.tail[i].StretchedRad;
                previoustailpos = tailpos;
            }
            float tilt = Custom.AimFromOneVectorToAnother(Vector2.Lerp(hips, neck, 0.5f), head);
            int tiltIndex = Mathf.RoundToInt(Mathf.Abs(tilt / 360f * 34f));
            if (this.player.sleepCurlUp > 0f)
            {
                tiltIndex = 7;
                tiltIndex = Custom.IntClamp((int)Mathf.Lerp(tiltIndex, 4f, this.player.sleepCurlUp), 0, 8);
            }
            Vector2 lookdirx3 = this.pGraphics.lookDirection * 3f * (1f - this.player.sleepCurlUp);
            if (this.player.sleepCurlUp > 0f)
            {
                //sLeaser.sprites[9].element = Futile.atlasManager.GetElementWithName("Face" + ((this.pGraphics.blink <= 0) ? "A" : "B") + Custom.IntClamp((int)Mathf.Lerp((float)tiltIndex, 1f, this.player.sleepCurlUp), 0, 8));
                //sLeaser.sprites[9].scaleX = Mathf.Sign(neck.x - hips.x);
                //sLeaser.sprites[9].rotation = tilt * (1f - this.player.sleepCurlUp);

                newHeadDepth = Mathf.Clamp(Mathf.Lerp(Mathf.Abs(tilt / 360f * 34f), 1f, this.player.sleepCurlUp) / 8f, 0f, 1f);
                newHeadDepth *= Mathf.Sign(newHeadDepth) * Mathf.Sign(neck.x - hips.x);
                tilt = Mathf.Lerp(tilt, 45f * Mathf.Sign(neck.x - hips.x), this.player.sleepCurlUp);

                newDepth = Mathf.Clamp(Mathf.Lerp(Mathf.Abs(tilt / 360f * 34f), 1f, this.player.sleepCurlUp) / 8f, 0f, 1f);
                newDepth *= Mathf.Sign(newDepth) * Mathf.Sign(neck.x - hips.x);

                head.y += 1f * this.player.sleepCurlUp;
                head.x += Mathf.Sign(neck.x - hips.x) * 2f * this.player.sleepCurlUp;
                lookdirx3.y -= 2f * this.player.sleepCurlUp;
                lookdirx3.x -= 4f * Mathf.Sign(neck.x - hips.x) * this.player.sleepCurlUp;
            }
            else if (base.owner.owner.room != null && base.owner.owner.room.gravity == 0f)
            {
                tiltIndex = 0;
                newHeadDepth = 0;
                newDepth = 0;
                //sLeaser.sprites[9].rotation = tilt;
                //if (this.player.Consious)
                //{
                //    sLeaser.sprites[9].element = Futile.atlasManager.GetElementWithName("Face" + ((this.blink <= 0) ? "A" : "B") + "0");
                //}
                //else
                //{
                //    sLeaser.sprites[9].element = Futile.atlasManager.GetElementWithName("Face" + ((!this.player.dead) ? "Stunned" : "Dead"));
                //}
            }
            else if (this.player.Consious)
            {
                if ((this.player.bodyMode == Player.BodyModeIndex.Stand && this.player.input[0].x != 0) || this.player.bodyMode == Player.BodyModeIndex.Crawl)
                {
                    newDepth = 1.0f;
                    newDepth *= Mathf.Sign(newDepth) * Mathf.Sign(neck.x - hips.x);
                    if (this.player.bodyMode == Player.BodyModeIndex.Crawl)
                    {
                        newHeadDepth = 0.88f;
                        newHeadDepth *= Mathf.Sign(newHeadDepth) * Mathf.Sign(neck.x - hips.x);
                        //tiltIndex = 7;
                        //sLeaser.sprites[9].scaleX = Mathf.Sign(neck.x - hips.x);
                    }
                    else
                    {
                        newHeadDepth = 0.66f;
                        newHeadDepth *= Mathf.Sign(newHeadDepth) * ((tilt >= 0f) ? 1f : -1f);
                        //tiltIndex = 6;
                        //sLeaser.sprites[9].scaleX = ((tilt >= 0f) ? 1f : -1f);
                    }
                    lookdirx3.x = 0f;
                    //sLeaser.sprites[9].y += 1f;
                    //sLeaser.sprites[9].element = Futile.atlasManager.GetElementWithName("Face" + ((this.blink <= 0) ? "A" : "B") + "4");
                }
                else
                {
                    Vector2 animationtilt = head - hips;
                    animationtilt.x *= 1f - lookdirx3.magnitude / 6f; // /3f;
                    animationtilt = animationtilt.normalized;
                    newHeadDepth = Mathf.Clamp(Custom.VecToDeg(animationtilt) / 180f, -1, 1);


                    //sLeaser.sprites[9].element = Futile.atlasManager.GetElementWithName("Face" + ((this.blink <= 0) ? "A" : "B") + Mathf.RoundToInt(Mathf.Abs(Custom.AimFromOneVectorToAnother(new Vector2(0f, 0f), animationtilt) / 22.5f)));
                    if (Mathf.Abs(lookdirx3.x) < 0.1f)
                    {
                        newHeadDepth *= Mathf.Sign(newHeadDepth) * ((tilt >= 0f) ? 1f : -1f);
                        //sLeaser.sprites[9].scaleX = ((tilt >= 0f) ? 1f : -1f);
                    }
                    else
                    {
                        newHeadDepth *= Mathf.Sign(newHeadDepth) * Mathf.Sign(lookdirx3.x);
                        //sLeaser.sprites[9].scaleX = Mathf.Sign(lookdirx3.x);
                    }
                    newDepth = newHeadDepth;
                }
                //sLeaser.sprites[9].rotation = 0f;
            }
            else
            {
                newHeadDepth = 0;
                newDepth = 0;
                lookdirx3 *= 0f;
                tiltIndex = 0;
                //sLeaser.sprites[9].element = Futile.atlasManager.GetElementWithName("Face" + ((!this.player.dead) ? "Stunned" : "Dead"));
                //sLeaser.sprites[9].rotation = tilt;
            }


            this.depthRotation = Mathf.Lerp(this.depthRotation, Mathf.Clamp(newDepth, -1f, 1f), 0.2f);
            this.headDepthRotation = Mathf.Lerp(this.headDepthRotation, Mathf.Clamp(newHeadDepth, -1f, 1f), 0.2f);

            //if (this.pGraphics.DEBUGLABELS != null)
            //{
            //    this.pGraphics.DEBUGLABELS[0].label.text = "depthRotation: " + depthRotation;
            //    this.pGraphics.DEBUGLABELS[1].label.text = "headDepthRotation: " + depthRotation;
            //    SpineData spinehead = SpinePosition(0f, true, 1f);
            //    SpineData spinetail = SpinePosition(1f, true, 1f);
            //    this.pGraphics.DEBUGLABELS[2].label.text = "spineDepthAtHead: " + spinehead.depthRotation;
            //    this.pGraphics.DEBUGLABELS[3].label.text = "spineDepthAtTail: " + spinetail.depthRotation;
            //    this.pGraphics.DEBUGLABELS[4].label.text = "spineAngleAtHead: " + Custom.VecToDeg(spinehead.dir);
            //    this.pGraphics.DEBUGLABELS[5].label.text = "spineAngleAtTail: " + Custom.VecToDeg(spinetail.dir);
            //}
        }

        public override SpineData SpinePosition(float spineFactor, bool inFront, float timeStacker)
        {
            // float num = this.pGraphics.player.bodyChunkConnections[0].distance + this.pGraphics.player.bodyChunkConnections[1].distance;
            Vector2 topPos;
            float fromRadius;
            Vector2 direction;
            Vector2 bottomPos;
            float toRadius;
            float t;
            if (spineFactor < this.bodyLength / this.BodyAndTailLength)
            {
                float inBodyFactor = Mathf.InverseLerp(0f, this.bodyLength / this.BodyAndTailLength, spineFactor);

                topPos = Vector2.Lerp(this.pGraphics.drawPositions[0, 1], this.pGraphics.drawPositions[0, 0], timeStacker);
                fromRadius = this.pGraphics.player.bodyChunks[0].rad * 0.9f;

                bottomPos = Vector2.Lerp(this.pGraphics.drawPositions[1, 1], this.pGraphics.drawPositions[1, 0], timeStacker);
                toRadius = this.pGraphics.player.bodyChunks[1].rad * 0.95f;
                direction = Custom.DirVec(topPos, bottomPos);

                t = inBodyFactor;
            }
            else
            {
                float inTailFactor = Mathf.InverseLerp(this.bodyLength / this.BodyAndTailLength, 1f, spineFactor);
                int num6 = Mathf.FloorToInt(inTailFactor * pGraphics.tail.Length - 1f);
                int num7 = Mathf.FloorToInt(inTailFactor * pGraphics.tail.Length);
                if (num7 > this.pGraphics.tail.Length - 1)
                {
                    num7 = this.pGraphics.tail.Length - 1;
                }
                if (num6 < 0)
                {
                    topPos = Vector2.Lerp(this.pGraphics.drawPositions[1, 1], this.pGraphics.drawPositions[1, 0], timeStacker);
                    fromRadius = this.pGraphics.player.bodyChunks[1].rad;
                }
                else
                {
                    topPos = Vector2.Lerp(this.pGraphics.tail[num6].lastPos, this.pGraphics.tail[num6].pos, timeStacker);
                    fromRadius = this.pGraphics.tail[num6].StretchedRad;
                }
                Vector2 nextPos = Vector2.Lerp(this.pGraphics.tail[Mathf.Min(num7 + 1, this.pGraphics.tail.Length - 1)].lastPos, this.pGraphics.tail[Mathf.Min(num7 + 1, this.pGraphics.tail.Length - 1)].pos, timeStacker);
                bottomPos = Vector2.Lerp(this.pGraphics.tail[num7].lastPos, this.pGraphics.tail[num7].pos, timeStacker);
                toRadius = this.pGraphics.tail[num7].StretchedRad;
                t = Mathf.InverseLerp(num6 + 1, num7 + 1, inTailFactor * pGraphics.tail.Length);
                direction = Vector2.Lerp(bottomPos - topPos, nextPos - bottomPos, t).normalized;
                if (direction.x == 0f && direction.y == 0f)
                {
                    direction = (this.pGraphics.tail[this.pGraphics.tail.Length - 1].pos - this.pGraphics.tail[this.pGraphics.tail.Length - 2].pos).normalized;
                }
            }

            Vector2 perp = Custom.PerpendicularVector(direction);
            float rad = Mathf.Lerp(fromRadius, toRadius, t);
            float rot = Mathf.Lerp(this.lastDepthRotation, this.depthRotation, timeStacker);
            if (!inFront)
            {
                rot = -rot;
                // perp = -perp;
            }
            rot = Mathf.Pow(Mathf.Abs(rot), Mathf.Lerp(1.2f, 0.3f, Mathf.Pow(spineFactor, 0.5f))) * Mathf.Sign(rot);
            Vector2 pos = Vector2.Lerp(topPos, bottomPos, t);
            Vector2 outerPos = pos + perp * rot * rad;
            return new SpineData(spineFactor, pos, outerPos, direction, perp, rot, rad);
        }

        public override Color BodyColorFallback(float y)
        {
            Color color = PlayerGraphics.SlugcatColor((pGraphics.player.State as PlayerState).slugcatCharacter);

            // Vanilla and Jolly
            if (pGraphics.malnourished > 0f)
            {
                float num = (!pGraphics.player.Malnourished) ? Mathf.Max(0f, pGraphics.malnourished - 0.005f) : pGraphics.malnourished;
                color = Color.Lerp(color, Color.gray, 0.4f * num);
            }

            return color;
        }
    }
}