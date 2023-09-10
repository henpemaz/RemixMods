using RWCustom;
using UnityEngine;

namespace LizardSkin
{
    internal class GenericAntennae : GenericCosmeticTemplate
    {
        internal CosmeticAntennaeData antennaeData => cosmeticData as CosmeticAntennaeData;
        public GenericAntennae(ICosmeticsAdaptor iGraphics, LizKinCosmeticData cosmeticData) : base(iGraphics, cosmeticData)
        {
            if (this.cosmeticData.spritesOverlap == LizKinCosmeticData.SpritesOverlapConfig.Default) this.spritesOverlap = GenericCosmeticTemplate.SpritesOverlap.BehindHead;
            //this.length = antennaeData.length; //UnityEngine.Random.value;
            this.segments = antennaeData.segments; //Mathf.FloorToInt(Mathf.Lerp(3f, 8f, Mathf.Pow(this.length, Mathf.Lerp(1f, 6f, this.length))));
                                                   //this.alpha = antennaeData.alpha; // this.length * 0.9f + UnityEngine.Random.value * 0.1f;
            this.antennae = new GenericBodyPartAdaptor[2, this.segments];
            for (int i = 0; i < this.segments; i++)
            {
                this.antennae[0, i] = new GenericBodyPartAdaptor(iGraphics, 1f, 0.6f, 0.9f);
                this.antennae[1, i] = new GenericBodyPartAdaptor(iGraphics, 1f, 0.6f, 0.9f);
            }
            this.numberOfSprites = 4;
        }

        private int Sprite(int side, int part)
        {
            return this.startSprite + part * 2 + side;
        }

        public override void Reset()
        {
            base.Reset();
            SpineData spine = iGraphics.SpinePosition(antennaeData.spinepos, false, 1f);
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < this.segments; j++)
                {
                    this.antennae[i, j].Reset(this.AnchorPoint(i, 1f, spine));
                }
            }
        }

        public override void Update()
        {
            float flicker = this.iGraphics.showDominance; //  this.iGraphics.lizard.AI.yellowAI.commFlicker;
            float lengthPerSegment = Mathf.Max(0f, antennaeData.length - 4f * (this.segments - 1)) / (this.segments - 1);
            SpineData spine = iGraphics.SpinePosition(antennaeData.spinepos, false, 1f);
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < this.segments; j++)
                {
                    float tipFactor = j / (float)(this.segments - 1);
                    tipFactor = Mathf.Lerp(tipFactor, Mathf.InverseLerp(0f, 5f, j), 0.2f); // accelerate tippening
                    this.antennae[i, j].vel += this.AntennaDir(i, 1f, spine) * (1f - tipFactor + 0.6f * flicker); // Pull outwards
                    if (this.iGraphics.PointSubmerged(this.antennae[i, j].pos))
                    {
                        this.antennae[i, j].vel *= 0.8f; // floaty
                    }
                    else
                    {
                        this.antennae[i, j].vel.y -= 0.4f * tipFactor * (1f - flicker); // droopey
                    }
                    this.antennae[i, j].Update();
                    this.antennae[i, j].pos += Custom.RNV() * 3f * flicker;
                    Vector2 previousPrevious;
                    if (j == 0)
                    {
                        this.antennae[i, j].vel += this.AntennaDir(i, 1f, spine) * 5f;
                        //p = this.iGraphics.headPos;
                        previousPrevious = spine.pos;
                        this.antennae[i, j].ConnectToPoint(this.AnchorPoint(i, 1f, spine), lengthPerSegment * 0.2f, true, 0f, this.iGraphics.mainBodyChunkVel, 0f, 0f);
                    }
                    else
                    {
                        if (j == 1)
                        {
                            previousPrevious = this.AnchorPoint(i, 1f, spine);
                        }
                        else
                        {
                            previousPrevious = this.antennae[i, j - 2].pos;
                        }
                        Vector2 toPrevious = Custom.DirVec(this.antennae[i, j].pos, this.antennae[i, j - 1].pos);
                        float dist = Vector2.Distance(this.antennae[i, j].pos, this.antennae[i, j - 1].pos);
                        this.antennae[i, j].pos -= toPrevious * (lengthPerSegment - dist) * 0.5f;
                        this.antennae[i, j].vel -= toPrevious * (lengthPerSegment - dist) * 0.5f;
                        this.antennae[i, j - 1].pos += toPrevious * (lengthPerSegment - dist) * 0.5f;
                        this.antennae[i, j - 1].vel += toPrevious * (lengthPerSegment - dist) * 0.5f;
                    }
                    this.antennae[i, j].vel += Custom.DirVec(previousPrevious, this.antennae[i, j].pos) * 3f * Mathf.Pow(1f - tipFactor, 0.3f);
                    if (j > 1)
                    {
                        this.antennae[i, j - 2].vel += Custom.DirVec(this.antennae[i, j].pos, this.antennae[i, j - 2].pos) * 3f * Mathf.Pow(1f - tipFactor, 0.3f);
                    }
                    // But wheres the fun in this
                    //if (!Custom.DistLess(this.iGraphics.headPos, this.antennae[i, j].pos, 200f))
                    //{
                    //	this.antennae[i, j].pos = this.iGraphics.headPos;
                    //}
                }
            }
        }

        private Vector2 AntennaDir(int side, float timeStacker, SpineData spine)
        {
            //float num = Mathf.Lerp(this.iGraphics.lastHeadDepthRotation, this.iGraphics.headDepthRotation, timeStacker);
            Vector2 vector = new Vector2(((side != 0) ? 1f : -1f) * (1f - Mathf.Abs(spine.depthRotation)) * 1.5f + spine.depthRotation * 3.5f, -1f);
            return Custom.RotateAroundOrigo(vector.normalized, ((side != 0) ? 90f : -90f) * antennaeData.angle + Custom.VecToDeg(-spine.dir)); // Custom.AimFromOneVectorToAnother(Vector2.Lerp(this.iGraphics.mainBodyChunkLastPos, this.iGraphics.mainBodyChunkPos, timeStacker), Vector2.Lerp(this.iGraphics.headLastPos, this.iGraphics.headPos, timeStacker)));
        }

        private Vector2 AnchorPoint(int side, float timeStacker, SpineData spine)
        {
            // anchor should be offsetable
            //float num = Mathf.Lerp(this.iGraphics.lastHeadDepthRotation, this.iGraphics.headDepthRotation, timeStacker);
            return spine.pos + spine.perp * (((side != 0) ? -1f : 1f) * (1f - Mathf.Abs(spine.depthRotation)) * 1.5f + spine.depthRotation * 3.5f) * antennaeData.offset + this.AntennaDir(side, timeStacker, spine) * antennaeData.distance;
        }

        public override void InitiateSprites(LeaserAdaptor sLeaser, CameraAdaptor rCam)
        {
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    sLeaser.sprites[this.Sprite(i, j)] = TriangleMesh.MakeLongMesh(this.segments, true, true);
                }
            }
            for (int k = 0; k < 2; k++)
            {
                sLeaser.sprites[this.Sprite(k, 1)].shader = iGraphics.rainWorld.Shaders["LizardAntenna"];
            }
        }

        public override void DrawSprites(LeaserAdaptor sLeaser, CameraAdaptor rCam, float timeStacker, Vector2 camPos)
        {
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            float flicker = Mathf.Pow(UnityEngine.Random.value, 1f - 0.5f * this.iGraphics.showDominance) * this.iGraphics.showDominance;
            //if (!this.iGraphics.lizard.Consious)
            //{
            //	flicker = 0f;
            //}
            //Vector2 vector = Custom.DegToVec(this.iGraphics.HeadRotation(timeStacker));
            SpineData spine = iGraphics.SpinePosition(antennaeData.spinepos, false, timeStacker);
            for (int i = 0; i < 2; i++)
            {
                sLeaser.sprites[this.startSprite + i].color = this.cosmeticData.GetBaseColor(iGraphics, 0);
                Vector2 vector2 = Vector2.Lerp(spine.pos, this.AnchorPoint(i, timeStacker, spine), 0.5f);
                float num = 1f;
                float num2 = 0f;
                for (int j = 0; j < this.segments; j++)
                {
                    float num3 = j / (float)(this.segments - 1);
                    Vector2 vector3 = Vector2.Lerp(this.antennae[i, j].lastPos, this.antennae[i, j].pos, timeStacker);
                    Vector2 normalized = (vector3 - vector2).normalized;
                    Vector2 a = Custom.PerpendicularVector(normalized);
                    float d = Vector2.Distance(vector3, vector2) / 5f;
                    float num4 = Mathf.Lerp(antennaeData.width, 1f, Mathf.Pow(num3, 0.8f));
                    for (int k = 0; k < 2; k++)
                    {
                        (sLeaser.sprites[this.Sprite(i, k)] as TriangleMesh).MoveVertice(j * 4, vector2 - a * (num + num4) * 0.5f + normalized * d - camPos);
                        (sLeaser.sprites[this.Sprite(i, k)] as TriangleMesh).MoveVertice(j * 4 + 1, vector2 + a * (num + num4) * 0.5f + normalized * d - camPos);
                        (sLeaser.sprites[this.Sprite(i, k)] as TriangleMesh).verticeColors[j * 4] = this.EffectColor(k, (num3 + num2) / 2f, timeStacker, flicker);
                        (sLeaser.sprites[this.Sprite(i, k)] as TriangleMesh).verticeColors[j * 4 + 1] = this.EffectColor(k, (num3 + num2) / 2f, timeStacker, flicker);
                        (sLeaser.sprites[this.Sprite(i, k)] as TriangleMesh).verticeColors[j * 4 + 2] = this.EffectColor(k, num3, timeStacker, flicker);
                        if (j < this.segments - 1)
                        {
                            (sLeaser.sprites[this.Sprite(i, k)] as TriangleMesh).MoveVertice(j * 4 + 2, vector3 - a * num4 - normalized * d - camPos);
                            (sLeaser.sprites[this.Sprite(i, k)] as TriangleMesh).MoveVertice(j * 4 + 3, vector3 + a * num4 - normalized * d - camPos);
                            (sLeaser.sprites[this.Sprite(i, k)] as TriangleMesh).verticeColors[j * 4 + 3] = this.EffectColor(k, num3, timeStacker, flicker);
                        }
                        else
                        {
                            (sLeaser.sprites[this.Sprite(i, k)] as TriangleMesh).MoveVertice(j * 4 + 2, vector3 - camPos);
                        }
                    }
                    num = num4;
                    vector2 = vector3;
                    num2 = num3;
                }
            }
        }

        public Color EffectColor(int part, float tip, float timeStacker, float flicker)
        {
            tip = Mathf.Pow(Mathf.InverseLerp(0f, 0.6f, tip), 0.5f);
            if (part == 0)
            {
                return Color.Lerp(this.cosmeticData.GetBaseColor(iGraphics, 0), Color.Lerp(this.cosmeticData.effectColor, this.iGraphics.palette.blackColor, flicker), tip);
            }
            Color tint = Color.Lerp(this.cosmeticData.effectColor, antennaeData.tintColor, 0.66f);
            return Color.Lerp(new Color(tint.r, tint.g, tint.b, antennaeData.alpha), new Color(1f, 1f, 1f, antennaeData.alpha), flicker);
        }

        public override void ApplyPalette(LeaserAdaptor sLeaser, CameraAdaptor rCam, PaletteAdaptor palette)
        {
            base.ApplyPalette(sLeaser, rCam, palette);
        }

        public GenericBodyPartAdaptor[,] antennae;

        private int segments;
    }
}
