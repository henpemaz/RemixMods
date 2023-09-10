using RWCustom;
using UnityEngine;

namespace LizardSkin
{
    internal class GenericTailFin : GenericCosmeticTemplate
    {
        private CosmeticTailFinData tailFinData => cosmeticData as CosmeticTailFinData;

        public int bumps;
        public int graphic;
        public bool colored;

        // This class behave juuuuuust like LongBodyScales + GenericSpineSpikes but its also nice to keep some variety in I suppose
        // uses CosmeticSpineSpikesData
        public GenericTailFin(ICosmeticsAdaptor iGraphics, LizKinCosmeticData cosmeticData) : base(iGraphics, cosmeticData)
        {
            if (this.cosmeticData.spritesOverlap == LizKinCosmeticData.SpritesOverlapConfig.Default) this.spritesOverlap = GenericCosmeticTemplate.SpritesOverlap.BehindHead;

            //float num = Mathf.Lerp(4f, 7f, Mathf.Pow(UnityEngine.Random.value, 0.7f));
            //this.spineLength = Custom.ClampedRandomVariation(0.5f, 0.17f, 0.5f) * iGraphics.BodyAndTailLength;
            //this.undersideSize = Mathf.Lerp(0.3f, 0.9f, UnityEngine.Random.value);
            //this.sizeRangeMin = Mathf.Lerp(0.1f, 0.3f, Mathf.Pow(UnityEngine.Random.value, 2f));
            //this.sizeRangeMax = Mathf.Lerp(this.sizeRangeMin, 0.6f, UnityEngine.Random.value);
            //this.sizeSkewExponent = Mathf.Lerp(0.5f, 1.5f, UnityEngine.Random.value);
            this.graphic = tailFinData.graphic; // UnityEngine.Random.Range(0, 6);

            this.bumps = tailFinData.count; // (int)(this.spineLength / num);

            this.colored = tailFinData.colored; // (UnityEngine.Random.value > 0.33333334f);
            this.numberOfSprites = ((!this.colored) ? this.bumps : (this.bumps * 2)) * 2;
        }

        public override void Update()
        {
        }

        public override void InitiateSprites(LeaserAdaptor sLeaser, CameraAdaptor rCam)
        {
            for (int i = 0; i < 2; i++)
            {
                int num = i * ((!this.colored) ? this.bumps : (this.bumps * 2));
                for (int j = this.startSprite + this.bumps - 1; j >= this.startSprite; j--)
                {
                    float num2 = Mathf.InverseLerp(startSprite, this.startSprite + this.bumps - 1, j);
                    sLeaser.sprites[j + num] = new FSprite("LizardScaleA" + this.graphic, true);
                    sLeaser.sprites[j + num].anchorY = 0.15f;
                    if (this.colored)
                    {
                        sLeaser.sprites[j + this.bumps + num] = new FSprite("LizardScaleB" + this.graphic, true);
                        sLeaser.sprites[j + this.bumps + num].anchorY = 0.15f;
                    }
                }
            }
        }

        public override void DrawSprites(LeaserAdaptor sLeaser, CameraAdaptor rCam, float timeStacker, Vector2 camPos)
        {
            for (int i = 0; i < 2; i++)
            {
                int num = i * ((!this.colored) ? this.bumps : (this.bumps * 2));
                for (int j = this.startSprite + this.bumps - 1; j >= this.startSprite; j--)
                {
                    float num2 = Mathf.InverseLerp(startSprite, this.startSprite + this.bumps - 1, j);
                    SpineData lizardSpineData = this.iGraphics.SpinePosition(Mathf.Lerp(1f - tailFinData.length, 1f, num2), true, timeStacker);
                    if (i == 0)
                    {
                        sLeaser.sprites[j + num].x = lizardSpineData.outerPos.x - camPos.x;
                        sLeaser.sprites[j + num].y = lizardSpineData.outerPos.y - camPos.y;
                    }
                    else if (i == 1)
                    {
                        sLeaser.sprites[j + num].x = lizardSpineData.pos.x + (lizardSpineData.pos.x - lizardSpineData.outerPos.x) * 0.85f - camPos.x;
                        sLeaser.sprites[j + num].y = lizardSpineData.pos.y + (lizardSpineData.pos.y - lizardSpineData.outerPos.y) * 0.85f - camPos.y;
                    }
                    sLeaser.sprites[j + num].rotation = Custom.VecToDeg(Vector2.Lerp(lizardSpineData.perp * lizardSpineData.depthRotation, lizardSpineData.dir * ((i != 1) ? 1 : -1), num2));
                    float num3 = tailFinData.scale * Mathf.Lerp(tailFinData.sizeMin, 1f, Mathf.Sin(Mathf.Pow(num2, tailFinData.sizeExponent) * 3.1415927f));
                    sLeaser.sprites[j + num].scaleX = Mathf.Sign(this.iGraphics.depthRotation) * tailFinData.thickness * num3;
                    sLeaser.sprites[j + num].scaleY = num3 * Mathf.Max(0.2f, Mathf.InverseLerp(0f, 0.5f, Mathf.Abs(this.iGraphics.depthRotation))) * ((i != 1) ? 1f : (-tailFinData.undersideSize));
                    if (this.colored)
                    {
                        if (i == 0)
                        {
                            sLeaser.sprites[j + this.bumps + num].x = lizardSpineData.outerPos.x - camPos.x;
                            sLeaser.sprites[j + this.bumps + num].y = lizardSpineData.outerPos.y - camPos.y;
                        }
                        else if (i == 1)
                        {
                            sLeaser.sprites[j + this.bumps + num].x = lizardSpineData.pos.x + (lizardSpineData.pos.x - lizardSpineData.outerPos.x) * 0.85f - camPos.x;
                            sLeaser.sprites[j + this.bumps + num].y = lizardSpineData.pos.y + (lizardSpineData.pos.y - lizardSpineData.outerPos.y) * 0.85f - camPos.y;
                        }
                        sLeaser.sprites[j + this.bumps + num].rotation = Custom.VecToDeg(Vector2.Lerp(lizardSpineData.perp * lizardSpineData.depthRotation, lizardSpineData.dir * ((i != 1) ? 1 : -1), num2));
                        sLeaser.sprites[j + this.bumps + num].scaleX = Mathf.Sign(this.iGraphics.depthRotation) * tailFinData.thickness * num3;
                        sLeaser.sprites[j + this.bumps + num].scaleY = num3 * Mathf.Max(0.2f, Mathf.InverseLerp(0f, 0.5f, Mathf.Abs(this.iGraphics.depthRotation))) * ((i != 1) ? 1f : (-tailFinData.undersideSize));
                        if (i == 1)
                        {
                            sLeaser.sprites[j + this.bumps + num].alpha = Mathf.Pow(Mathf.InverseLerp(0.3f, 1f, Mathf.Abs(this.iGraphics.depthRotation)), 0.2f);
                        }
                    }
                }
            }
        }

        public override void ApplyPalette(LeaserAdaptor sLeaser, CameraAdaptor rCam, PaletteAdaptor palette)
        {
            for (int i = 0; i < 2; i++)
            {
                int num = i * ((!this.colored) ? this.bumps : (this.bumps * 2));
                for (int j = this.startSprite; j < this.startSprite + this.bumps; j++)
                {
                    float f = Mathf.Lerp(tailFinData.start, tailFinData.length, Mathf.InverseLerp(startSprite, this.startSprite + this.bumps - 1, i));
                    sLeaser.sprites[j + num].color = this.cosmeticData.GetBaseColor(iGraphics, f);
                    if (this.colored && !tailFinData.colorFade)
                    {
                        sLeaser.sprites[j + this.bumps + num].color = this.cosmeticData.effectColor;
                    }
                    else if (this.colored && tailFinData.colorFade)
                    {
                        float f2 = Mathf.InverseLerp(startSprite, this.startSprite + this.bumps - 1, i);
                        sLeaser.sprites[j + this.bumps + num].color = Color.Lerp(this.cosmeticData.effectColor, this.cosmeticData.GetBaseColor(iGraphics, f), Mathf.Pow(f2, 0.5f)); // Could make this controlable exponent
                    }
                }
            }
        }
    }
}
