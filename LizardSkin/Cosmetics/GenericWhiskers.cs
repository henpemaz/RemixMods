using RWCustom;
using UnityEngine;

namespace LizardSkin
{
    internal class GenericWhiskers : GenericCosmeticTemplate
    {
        private CosmeticWhiskersData whiskersData => cosmeticData as CosmeticWhiskersData;
        public GenericWhiskers(ICosmeticsAdaptor iGraphics, LizKinCosmeticData cosmeticData) : base(iGraphics, cosmeticData)
        {
            if (this.cosmeticData.spritesOverlap == LizKinCosmeticData.SpritesOverlapConfig.Default) this.spritesOverlap = GenericCosmeticTemplate.SpritesOverlap.InFront;
            this.amount = whiskersData.count; // UnityEngine.Random.Range(3, 5);
            this.whiskers = new GenericBodyPartAdaptor[2, this.amount];
            this.whiskerDirections = new Vector2[this.amount];
            this.whiskerProps = new float[this.amount, 5];
            this.whiskerLightUp = new float[this.amount, 2, 2];
            for (int i = 0; i < this.amount; i++)
            {
                this.whiskers[0, i] = new GenericBodyPartAdaptor(iGraphics, 1f, 0.6f, 0.9f);
                this.whiskers[1, i] = new GenericBodyPartAdaptor(iGraphics, 1f, 0.6f, 0.9f);
                this.whiskerDirections[i] = Custom.DegToVec(Mathf.Lerp(4f, 100f, UnityEngine.Random.value));
                this.whiskerProps[i, 0] = Custom.ClampedRandomVariation(0.5f, 0.4f, 0.5f) * 40f * whiskersData.length;
                this.whiskerProps[i, 1] = whiskersData.spread * Mathf.Lerp(-0.5f, 0.8f, UnityEngine.Random.value); // depth
                this.whiskerProps[i, 2] = whiskersData.spring * Mathf.Lerp(11f, 720f, Mathf.Pow(UnityEngine.Random.value, 1.5f)) / this.whiskerProps[i, 0];
                this.whiskerProps[i, 3] = UnityEngine.Random.value; // unused ?
                this.whiskerProps[i, 4] = whiskersData.thickness * Mathf.Lerp(0.6f, 1.2f, Mathf.Pow(UnityEngine.Random.value, 1.6f));
                if (i > 0)
                {
                    for (int j = 0; j < 5; j++)
                    {
                        if (j != 1)
                        {
                            this.whiskerProps[i, j] = Mathf.Lerp(this.whiskerProps[i, j], this.whiskerProps[i - 1, j], Mathf.Pow(UnityEngine.Random.value, 0.3f) * 0.6f);
                        }
                    }
                }
            }
            this.numberOfSprites = this.amount * 2;
        }

        // Token: 0x06001F5D RID: 8029 RVA: 0x001DBD9C File Offset: 0x001D9F9C
        public override void Reset()
        {
            base.Reset();
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < this.amount; j++)
                {
                    this.whiskers[i, j].Reset(this.AnchorPoint(i, j, 1f));
                }
            }
        }

        // Token: 0x06001F5E RID: 8030 RVA: 0x001DBDF8 File Offset: 0x001D9FF8
        public override void Update()
        {
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < this.amount; j++)
                {
                    this.whiskers[i, j].vel += this.whiskerDir(i, j, 1f) * this.whiskerProps[j, 2];
                    if (this.iGraphics.PointSubmerged(this.whiskers[i, j].pos))
                    {
                        this.whiskers[i, j].vel *= 0.8f;
                    }
                    else
                    {
                        GenericBodyPartAdaptor genericBodyPart = this.whiskers[i, j];
                        genericBodyPart.vel.y = genericBodyPart.vel.y - 0.6f;
                    }
                    this.whiskers[i, j].Update();
                    this.whiskers[i, j].ConnectToPoint(this.AnchorPoint(i, j, 1f), this.whiskerProps[j, 0], false, 0f, this.iGraphics.mainBodyChunkVel, 0f, 0f);
                    if (!Custom.DistLess(this.iGraphics.headPos, this.whiskers[i, j].pos, 200f))
                    {
                        this.whiskers[i, j].pos = this.iGraphics.headPos;
                    }
                    this.whiskerLightUp[j, i, 1] = this.whiskerLightUp[j, i, 0];
                    if (this.whiskerLightUp[j, i, 0] < Mathf.InverseLerp(0f, 0.3f, this.iGraphics.showDominance))
                    {
                        this.whiskerLightUp[j, i, 0] = Mathf.Lerp(this.whiskerLightUp[j, i, 0], Mathf.InverseLerp(0f, 0.3f, this.iGraphics.showDominance), 0.7f) + 0.05f;
                    }
                    else
                    {
                        this.whiskerLightUp[j, i, 0] -= 0.025f;
                    }
                    this.whiskerLightUp[j, i, 0] += Mathf.Lerp(-1f, 1f, UnityEngine.Random.value) * 0.03f * this.iGraphics.showDominance;
                    this.whiskerLightUp[j, i, 0] = Mathf.Clamp(this.whiskerLightUp[j, i, 0], 0f, 1f);
                }
            }
        }

        // Token: 0x06001F5F RID: 8031 RVA: 0x001DC0A4 File Offset: 0x001DA2A4
        private Vector2 whiskerDir(int side, int m, float timeStacker)
        {
            float num = Mathf.Lerp(this.iGraphics.lastHeadDepthRotation, this.iGraphics.headDepthRotation, timeStacker);
            Vector2 vector = new Vector2(((side != 0) ? 1f : -1f) * (1f - Mathf.Abs(num)) * this.whiskerDirections[m].x + num * this.whiskerProps[m, 1], this.whiskerDirections[m].y);
            return Custom.RotateAroundOrigo(vector.normalized, Custom.AimFromOneVectorToAnother(Vector2.Lerp(this.iGraphics.mainBodyChunkLastPos, this.iGraphics.mainBodyChunkPos, timeStacker), Vector2.Lerp(this.iGraphics.headLastPos, this.iGraphics.headPos, timeStacker)));
        }

        // Token: 0x06001F60 RID: 8032 RVA: 0x001DC18C File Offset: 0x001DA38C
        private Vector2 AnchorPoint(int side, int m, float timeStacker)
        {
            return Vector2.Lerp(this.iGraphics.headLastPos, this.iGraphics.headPos, timeStacker) + this.whiskerDir(side, m, timeStacker) * 3f;
        }

        // Token: 0x06001F61 RID: 8033 RVA: 0x001DC1EC File Offset: 0x001DA3EC
        public override void InitiateSprites(LeaserAdaptor sLeaser, CameraAdaptor rCam)
        {
            for (int i = this.startSprite + this.amount * 2 - 1; i >= this.startSprite; i--)
            {
                sLeaser.sprites[i] = TriangleMesh.MakeLongMesh(4, true, true);
            }
        }

        // Token: 0x06001F62 RID: 8034 RVA: 0x001DC230 File Offset: 0x001DA430
        public override void DrawSprites(LeaserAdaptor sLeaser, CameraAdaptor rCam, float timeStacker, Vector2 camPos)
        {
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            Vector2 b = iGraphics.SpinePosition(0f, true, timeStacker).dir; // Custom.DegToVec(this.iGraphics.HeadRotation(timeStacker));
            for (int i = 0; i < this.amount; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    Vector2 vector = Vector2.Lerp(this.whiskers[j, i].lastPos, this.whiskers[j, i].pos, timeStacker);
                    Vector2 a = this.whiskerDir(j, i, timeStacker);
                    Vector2 vector2 = this.AnchorPoint(j, i, timeStacker);
                    a = (a + b).normalized;
                    Vector2 vector3 = vector2;
                    float num = this.whiskerProps[i, 4];
                    float num2 = 1f;
                    for (int k = 0; k < 4; k++)
                    {
                        Vector2 vector4;
                        if (k < 3)
                        {
                            vector4 = Vector2.Lerp(vector2, vector, (k + 1) / 4f);
                            vector4 += a * num2 * this.whiskerProps[i, 0] * 0.2f;
                        }
                        else
                        {
                            vector4 = vector;
                        }
                        num2 *= 0.7f;
                        Vector2 normalized = (vector4 - vector3).normalized;
                        Vector2 a2 = Custom.PerpendicularVector(normalized);
                        float d = Vector2.Distance(vector4, vector3) / ((k != 0) ? 5f : 1f);
                        float num3 = Custom.LerpMap(k, 0f, 3f, this.whiskerProps[i, 4], 0.5f);
                        for (int l = k * 4; l < k * 4 + ((k != 3) ? 4 : 3); l++)
                        {
                            (sLeaser.sprites[this.startSprite + i * 2 + j] as TriangleMesh).verticeColors[l] = Color.Lerp(this.cosmeticData.GetBaseColor(iGraphics, 0), new Color(1f, 1f, 1f), (k - 1) / 2f * Mathf.Lerp(this.whiskerLightUp[i, j, 1], this.whiskerLightUp[i, j, 0], timeStacker));
                        }
                        (sLeaser.sprites[this.startSprite + i * 2 + j] as TriangleMesh).MoveVertice(k * 4, vector3 - a2 * (num3 + num) * 0.5f + normalized * d - camPos);
                        (sLeaser.sprites[this.startSprite + i * 2 + j] as TriangleMesh).MoveVertice(k * 4 + 1, vector3 + a2 * (num3 + num) * 0.5f + normalized * d - camPos);
                        if (k < 3)
                        {
                            (sLeaser.sprites[this.startSprite + i * 2 + j] as TriangleMesh).MoveVertice(k * 4 + 2, vector4 - a2 * num3 - normalized * d - camPos);
                            (sLeaser.sprites[this.startSprite + i * 2 + j] as TriangleMesh).MoveVertice(k * 4 + 3, vector4 + a2 * num3 - normalized * d - camPos);
                        }
                        else
                        {
                            (sLeaser.sprites[this.startSprite + i * 2 + j] as TriangleMesh).MoveVertice(k * 4 + 2, vector4 + normalized * 2.1f - camPos);
                        }
                        num = num3;
                        vector3 = vector4;
                    }
                }
            }
        }

        // Token: 0x06001F63 RID: 8035 RVA: 0x001DC5FC File Offset: 0x001DA7FC
        public override void ApplyPalette(LeaserAdaptor sLeaser, CameraAdaptor rCam, PaletteAdaptor palette)
        {
            base.ApplyPalette(sLeaser, rCam, palette);
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < this.amount; j++)
                {
                    for (int k = 0; k < (sLeaser.sprites[this.startSprite + j * 2 + i] as TriangleMesh).verticeColors.Length; k++)
                    {
                        (sLeaser.sprites[this.startSprite + j * 2 + i] as TriangleMesh).verticeColors[k] = palette.blackColor;
                    }
                }
            }
        }

        // Token: 0x04002200 RID: 8704
        public GenericBodyPartAdaptor[,] whiskers;

        // Token: 0x04002201 RID: 8705
        public Vector2[] whiskerDirections;

        // Token: 0x04002202 RID: 8706
        public float[,] whiskerProps;

        // Token: 0x04002203 RID: 8707
        public float[,,] whiskerLightUp;

        // Token: 0x04002204 RID: 8708
        public int amount;
    }
}
