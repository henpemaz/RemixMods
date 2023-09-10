using RWCustom;
using UnityEngine;

namespace LizardSkin
{
    internal class GenericTailGeckoScales : GenericCosmeticTemplate
    {
        private CosmeticTailGeckoScalesData tailGeckoScalesData => cosmeticData as CosmeticTailGeckoScalesData;

        public int rows;
        public int lines;
        private bool bigScales;

        public GenericTailGeckoScales(ICosmeticsAdaptor iGraphics, LizKinCosmeticData cosmeticData) : base(iGraphics, cosmeticData)
        {
            if (this.cosmeticData.spritesOverlap == LizKinCosmeticData.SpritesOverlapConfig.Default) this.spritesOverlap = GenericCosmeticTemplate.SpritesOverlap.BehindHead;
            this.rows = tailGeckoScalesData.rows; //UnityEngine.Random.Range(7, 14);
            this.lines = tailGeckoScalesData.lines; //  UnityEngine.Random.Range(3, UnityEngine.Random.Range(3, 4));
            this.bigScales = tailGeckoScalesData.bigScales; // true; // ooopsie

            this.numberOfSprites = this.rows * this.lines;
        }

        public override void Update()
        {
        }

        public override void InitiateSprites(LeaserAdaptor sLeaser, CameraAdaptor rCam)
        {
            for (int i = 0; i < this.rows; i++)
            {
                for (int j = 0; j < this.lines; j++)
                {
                    if (this.bigScales)
                    {
                        sLeaser.sprites[this.startSprite + i * this.lines + j] = new FSprite("Circle20", true);
                        sLeaser.sprites[this.startSprite + i * this.lines + j].scaleY = 0.3f;
                    }
                    else
                    {
                        sLeaser.sprites[this.startSprite + i * this.lines + j] = new FSprite("tinyStar", true);
                    }
                }
            }
        }

        public override void DrawSprites(LeaserAdaptor sLeaser, CameraAdaptor rCam, float timeStacker, Vector2 camPos)
        {
            if (this.bigScales)
            {
                SpineData lizardSpineData = this.iGraphics.SpinePosition(Mathf.Max(0f, Mathf.Lerp(tailGeckoScalesData.start, 2 * tailGeckoScalesData.start - tailGeckoScalesData.stop, Mathf.Pow(1f / (this.rows - 1), tailGeckoScalesData.exponent))), true, timeStacker);
                for (int i = 0; i < this.rows; i++)
                {
                    float num = Mathf.InverseLerp(0f, this.rows - 1, i);
                    //float num2 = Mathf.Lerp(0.5f, 0.99f, Mathf.Pow(num, 0.8f));
                    float num2 = Mathf.Lerp(tailGeckoScalesData.start, tailGeckoScalesData.stop, Mathf.Pow(num, tailGeckoScalesData.exponent));
                    SpineData lizardSpineData2 = this.iGraphics.SpinePosition(num2, true, timeStacker);
                    Color a = this.cosmeticData.GetBaseColor(iGraphics, num2);
                    for (int j = 0; j < this.lines; j++)
                    {
                        float num3 = (j + ((i % 2 != 0) ? 0f : 0.5f)) / (this.lines - 1);
                        num3 = -1f + 2f * num3;
                        num3 += Mathf.Lerp(this.iGraphics.lastDepthRotation, this.iGraphics.depthRotation, timeStacker);
                        if (num3 < -1f)
                        {
                            num3 += 2f;
                        }
                        else if (num3 > 1f)
                        {
                            num3 -= 2f;
                        }
                        Vector2 vector = lizardSpineData.pos + lizardSpineData.perp * (lizardSpineData.rad + 0.5f) * num3;
                        Vector2 vector2 = lizardSpineData2.pos + lizardSpineData2.perp * (lizardSpineData2.rad + 0.5f) * num3;
                        sLeaser.sprites[this.startSprite + i * this.lines + j].x = (vector.x + vector2.x) * 0.5f - camPos.x;
                        sLeaser.sprites[this.startSprite + i * this.lines + j].y = (vector.y + vector2.y) * 0.5f - camPos.y;
                        sLeaser.sprites[this.startSprite + i * this.lines + j].rotation = Custom.AimFromOneVectorToAnother(vector, vector2);
                        sLeaser.sprites[this.startSprite + i * this.lines + j].scaleX = Custom.LerpMap(Mathf.Abs(num3), 0.4f, 1f, lizardSpineData2.rad * 3.5f / rows, 0f) / 10f;
                        sLeaser.sprites[this.startSprite + i * this.lines + j].scaleY = Vector2.Distance(vector, vector2) * 1.1f / 20f;
                        if (tailGeckoScalesData.shine > 0f)
                        {
                            float num4 = Mathf.InverseLerp(0.5f, 1f, Mathf.Abs(Vector2.Dot(Custom.DirVec(vector2, vector), Custom.DegToVec(-45f + 120f * num3))));
                            //num4 = Custom.LerpMap(Mathf.Abs(num3), 0.5f, 1f, 0.3f, 0f) + 0.7f * Mathf.Pow(num4 * Mathf.Pow(this.iGraphics.iVars.tailColor, 0.3f), Mathf.Lerp(2f, 0.5f, num));
                            num4 = Custom.LerpMap(Mathf.Abs(num3), 0.5f, 1f, 0.3f, 0f) + 0.7f * Mathf.Pow(num4 * Mathf.Pow(tailGeckoScalesData.shine, 0.3f), Mathf.Lerp(2f, 0.5f, num));
                            if (num < 0.5f)
                            {
                                num4 *= Custom.LerpMap(num, 0f, 0.5f, 0.2f, 1f);
                            }
                            num4 = Mathf.Pow(num4, Mathf.Lerp(2f, 0.5f, num));
                            if (num4 < 0.5f)
                            {
                                sLeaser.sprites[this.startSprite + i * this.lines + j].color = Color.Lerp(a, this.cosmeticData.effectColor, Mathf.InverseLerp(0f, 0.5f, num4));
                            }
                            else
                            {
                                sLeaser.sprites[this.startSprite + i * this.lines + j].color = Color.Lerp(this.cosmeticData.effectColor, Color.white, Mathf.InverseLerp(0.5f, 1f, num4));
                            }
                        }
                        else
                        {
                            sLeaser.sprites[this.startSprite + i * this.lines + j].color = Color.Lerp(a, this.cosmeticData.effectColor, Custom.LerpMap(num, 0f, 0.8f, 0.2f, Custom.LerpMap(Mathf.Abs(num3), 0.5f, 1f, 0.8f, 0.4f), 0.8f));
                        }
                    }
                    lizardSpineData = lizardSpineData2;
                }
            }
            else
            {
                for (int k = 0; k < this.rows; k++)
                {
                    float f = Mathf.InverseLerp(0f, this.rows - 1, k);
                    float num5 = Mathf.Lerp(tailGeckoScalesData.start, tailGeckoScalesData.stop, Mathf.Pow(f, tailGeckoScalesData.exponent));
                    SpineData lizardSpineData3 = this.iGraphics.SpinePosition(num5, true, timeStacker);
                    Color color = Color.Lerp(this.cosmeticData.GetBaseColor(iGraphics, num5), this.cosmeticData.effectColor, 0.2f + 0.8f * Mathf.Pow(f, 0.5f));
                    for (int l = 0; l < this.lines; l++)
                    {
                        float num6 = (l + ((k % 2 != 0) ? 0f : 0.5f)) / (this.lines - 1);
                        num6 = -1f + 2f * num6;
                        num6 += Mathf.Lerp(this.iGraphics.lastDepthRotation, this.iGraphics.depthRotation, timeStacker);
                        if (num6 < -1f)
                        {
                            num6 += 2f;
                        }
                        else if (num6 > 1f)
                        {
                            num6 -= 2f;
                        }
                        num6 = Mathf.Sign(num6) * Mathf.Pow(Mathf.Abs(num6), 0.6f);
                        Vector2 vector3 = lizardSpineData3.pos + lizardSpineData3.perp * (lizardSpineData3.rad + 0.5f) * num6;
                        sLeaser.sprites[this.startSprite + k * this.lines + l].x = vector3.x - camPos.x;
                        sLeaser.sprites[this.startSprite + k * this.lines + l].y = vector3.y - camPos.y;
                        //sLeaser.sprites[this.startSprite + k * this.lines + l].color = new Color(1f, 0f, 0f);
                        sLeaser.sprites[this.startSprite + k * this.lines + l].rotation = Custom.VecToDeg(lizardSpineData3.dir);
                        sLeaser.sprites[this.startSprite + k * this.lines + l].scaleX = Custom.LerpMap(Mathf.Abs(num6), 0.4f, 1f, 1f, 0f);
                        sLeaser.sprites[this.startSprite + k * this.lines + l].color = Color.Lerp(color, Color.white, Custom.LerpMap(Mathf.Abs(num6), 0.4f, 1f, 0f, tailGeckoScalesData.shine));
                    }
                }
            }
        }

        //public override void ApplyPalette(LeaserAdaptor sLeaser, CameraAdaptor rCam, PaletteAdaptor palette)
        //{
        //// dynanically colored
        //}
    }
}
