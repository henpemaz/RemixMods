using RWCustom;
using UnityEngine;

namespace LizardSkin
{
    internal class GenericWingScales : GenericCosmeticTemplate
    {
        private CosmeticWingScalesData wingScalesData => cosmeticData as CosmeticWingScalesData;
        public GenericWingScales(ICosmeticsAdaptor iGraphics, LizKinCosmeticData cosmeticData) : base(iGraphics, cosmeticData)
        {
            if (this.cosmeticData.spritesOverlap == LizKinCosmeticData.SpritesOverlapConfig.Default) this.spritesOverlap = GenericCosmeticTemplate.SpritesOverlap.InFront;
            this.scales = new GenericBodyPartAdaptor[2, wingScalesData.count]; // (UnityEngine.Random.value >= 0.2f) ? 2 : 3];
            this.graphic = wingScalesData.graphic; // ((UnityEngine.Random.value >= 0.4f) ? UnityEngine.Random.Range(0, 5) : 0);
                                                   //this.graphicLenght = Futile.atlasManager.GetElementWithName("LizardScaleA" + this.graphic).sourcePixelSize.y;
                                                   //this.sturdy = UnityEngine.Random.value;
                                                   //this.posSqueeze = UnityEngine.Random.value;
                                                   //this.scaleLength = Mathf.Lerp(5f, 40f, Mathf.Pow(UnityEngine.Random.value, 0.75f + 1.25f * this.sturdy));
                                                   // this.frontDir = Mathf.Lerp(-0.1f, 0.2f, UnityEngine.Random.value);
                                                   // this.backDir = Mathf.Lerp(Mathf.Max(0f, this.frontDir), this.frontDir + (float)this.scales.GetLength(1) * 0.2f, UnityEngine.Random.value);
            for (int i = 0; i < this.scales.GetLength(0); i++)
            {
                for (int j = 0; j < this.scales.GetLength(1); j++)
                {
                    // Adjusted manually ??
                    //int middlechunk = (int)Mathf.Floor((iGraphics.graphics.owner.bodyChunks.Length + 1) / 2f) - 1;
                    this.scales[i, j] = new GenericBodyPartAdaptor(iGraphics, 2f, 0.5f, Mathf.Lerp(0.8f, 0.999f, wingScalesData.rigor));
                }
            }
            this.numberOfSprites = this.scales.GetLength(0) * this.scales.GetLength(1);
        }


        public int ScaleSprite(int s, int i)
        {
            return this.startSprite + s * this.scales.GetLength(1) + i;
        }


        public override void Reset()
        {
            base.Reset();
            for (int i = 0; i < this.scales.GetLength(0); i++)
            {
                for (int j = 0; j < this.scales.GetLength(1); j++)
                {
                    //this.scales[i, j].pos = this.scales[i, j].connection.pos;
                    //this.scales[i, j].lastPos = this.scales[i, j].connection.pos;
                    //this.scales[i, j].vel *= 0f;
                    this.scales[i, j].Reset(iGraphics.mainBodyChunkPos);
                }
            }
        }


        public override void Update()
        {
            for (int i = 0; i < this.scales.GetLength(1); i++)
            {
                //float num = Custom.LerpMap((float)i, 0f, (float)(this.scales.GetLength(1) - 1), this.frontDir, this.backDir);
                float factor = this.scales.GetLength(1) > 1 ? Mathf.InverseLerp(0, (this.scales.GetLength(1) - 1), i) : 1f;
                float num = Mathf.Lerp(-0.05f, 0.05f + wingScalesData.roundness, factor);
                //float num = Custom.LerpMap((float)i, 0f, (float)(this.scales.GetLength(1) - 1), -0.05f, wingScalesData.roundness);
                //SpineData spineData = this.iGraphics.SpinePosition(0.025f + (0.025f + 0.15f * (float)i) * this.posSqueeze, 1f);
                SpineData spineData = this.iGraphics.SpinePosition(wingScalesData.start + wingScalesData.length * factor, true, 1f);
                float f = Mathf.Lerp(this.iGraphics.headDepthRotation, spineData.depthRotation, 0.3f + 0.2f * i);
                for (int j = 0; j < this.scales.GetLength(0); j++)
                {
                    Vector2 vector = spineData.pos + spineData.perp * ((j != 0) ? 1f : -1f) * spineData.rad * (1f - Mathf.Abs(f));
                    Vector2 vector2 = spineData.perp * ((j != 0) ? 1f : -1f) * (1f - Mathf.Abs(f));
                    vector2 = Vector3.Slerp(vector2, spineData.dir * num, Mathf.Abs(num));
                    vector2 = Vector3.Slerp(vector2, spineData.perp * Mathf.Sign(f), Mathf.Abs(f) * 0.5f);
                    Vector2 a = vector + vector2 * wingScalesData.scale * 10f * 1.5f;
                    this.scales[j, i].Update();
                    this.scales[j, i].ConnectToPoint(vector, wingScalesData.scale * 10f * ((i <= 1) ? 1f : 0.6f), false, 0f, this.iGraphics.mainBodyChunkVel, 0.1f + 0.2f * wingScalesData.rigor, 0f);
                    this.scales[j, i].vel += (a - this.scales[j, i].pos) * Mathf.Lerp(0.1f, 0.3f, wingScalesData.rigor);
                    this.scales[j, i].pos += (a - this.scales[j, i].pos) * 0.6f * Mathf.Pow(wingScalesData.rigor, 3f);

                    // new :)
                    this.scales[j, i].vel += this.iGraphics.showDominance * (Custom.RNV() * 3f * UnityEngine.Random.value - spineData.dir * 5f * UnityEngine.Random.value);
                    this.scales[j, i].pos += this.iGraphics.showDominance * (Custom.RNV() * 3f * UnityEngine.Random.value);
                }
            }
            //if (this.iGraphics.lizard.animation == Lizard.Animation.PrepareToJump && this.iGraphics.lizard.Consious)
            //{
            //	for (int k = 0; k < this.scales.GetLength(0); k++)
            //	{
            //		for (int l = 0; l < this.scales.GetLength(1); l++)
            //		{
            //			this.scales[k, l].vel += Custom.RNV() * 3f * UnityEngine.Random.value + Custom.DirVec(this.iGraphics.lizard.bodyChunks[1].pos, this.iGraphics.lizard.bodyChunks[0].pos) * UnityEngine.Random.value * 5f;
            //			this.scales[k, l].pos += Custom.RNV() * 3f * UnityEngine.Random.value;
            //		}
            //	}
            //}
        }


        public override void InitiateSprites(LeaserAdaptor sLeaser, CameraAdaptor rCam)
        {
            for (int i = 0; i < this.scales.GetLength(0); i++)
            {
                for (int j = 0; j < this.scales.GetLength(1); j++)
                {
                    sLeaser.sprites[this.ScaleSprite(i, j)] = new FSprite("LizardScaleA" + this.graphic, true);
                    sLeaser.sprites[this.ScaleSprite(i, j)].anchorY = 0.05f;
                    sLeaser.sprites[this.ScaleSprite(i, j)].scaleX = ((i != 0) ? 1f : -1f);
                }
            }
        }

        // Token: 0x06001F7E RID: 8062 RVA: 0x001DEAC8 File Offset: 0x001DCCC8
        public override void DrawSprites(LeaserAdaptor sLeaser, CameraAdaptor rCam, float timeStacker, Vector2 camPos)
        {
            for (int i = 0; i < this.scales.GetLength(1); i++)
            {
                //SpineData spineData = this.iGraphics.SpinePosition(0.025f + (0.025f + 0.15f * (float)i) * this.posSqueeze, timeStacker);
                float factor = this.scales.GetLength(1) > 1 ? Mathf.InverseLerp(0, (this.scales.GetLength(1) - 1), i) : 1f;
                //float num = Custom.LerpMap((float)i, 0f, (float)(this.scales.GetLength(1) - 1), -0.05f, wingScalesData.roundness);
                //SpineData spineData = this.iGraphics.SpinePosition(0.025f + (0.025f + 0.15f * (float)i) * this.posSqueeze, 1f);
                SpineData spineData = this.iGraphics.SpinePosition(wingScalesData.start + wingScalesData.length * factor, true, 1f);
                for (int j = 0; j < this.scales.GetLength(0); j++)
                {
                    Vector2 vector = spineData.pos + spineData.perp * ((j != 0) ? 1f : -1f) * spineData.rad * (1f - Mathf.Abs(spineData.depthRotation));
                    Vector2 vector2 = Vector2.Lerp(this.scales[j, i].lastPos, this.scales[j, i].pos, timeStacker);
                    sLeaser.sprites[this.ScaleSprite(j, i)].x = vector.x - camPos.x;
                    sLeaser.sprites[this.ScaleSprite(j, i)].y = vector.y - camPos.y;
                    sLeaser.sprites[this.ScaleSprite(j, i)].scaleY = Vector2.Distance(vector, vector2) / 10f;
                    sLeaser.sprites[this.ScaleSprite(j, i)].scaleX = wingScalesData.scale * wingScalesData.thickness * ((j != 0) ? 1f : -1f);
                    sLeaser.sprites[this.ScaleSprite(j, i)].rotation = Custom.AimFromOneVectorToAnother(vector, vector2);
                }
            }
        }

        // Token: 0x06001F7F RID: 8063 RVA: 0x001DEC3C File Offset: 0x001DCE3C
        public override void ApplyPalette(LeaserAdaptor sLeaser, CameraAdaptor rCam, PaletteAdaptor palette)
        {
            //for (int i = 0; i < this.numberOfSprites; i++)
            //{
            //	sLeaser.sprites[this.startSprite + i].color = cosmeticData.GetBaseColor(iGraphics, )// palette.blackColor;
            //}
            for (int i = 0; i < this.scales.GetLength(1); i++)
            {
                float factor = this.scales.GetLength(1) > 1 ? Mathf.InverseLerp(0, (this.scales.GetLength(1) - 1), i) : 1f;
                float y = wingScalesData.start + wingScalesData.length * factor;
                for (int j = 0; j < this.scales.GetLength(0); j++)
                {
                    sLeaser.sprites[this.ScaleSprite(j, i)].color = wingScalesData.colored ? cosmeticData.effectColor : cosmeticData.GetBaseColor(iGraphics, y);
                }
            }
        }

        // Token: 0x0400220D RID: 8717
        public GenericBodyPartAdaptor[,] scales;

        // Token: 0x0400220E RID: 8718
        private int graphic;

        // Token: 0x0400220F RID: 8719
        //public float scaleLength;

        // Token: 0x04002210 RID: 8720
        //public float graphicLenght;

        // Token: 0x04002211 RID: 8721
        //public float frontDir;

        // Token: 0x04002212 RID: 8722
        //public float backDir;

        // Token: 0x04002213 RID: 8723
        //private float sturdy;

        // Token: 0x04002214 RID: 8724
        //private float posSqueeze;
    }
}
