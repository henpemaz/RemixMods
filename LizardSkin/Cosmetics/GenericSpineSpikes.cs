using RWCustom;
using UnityEngine;

namespace LizardSkin
{
    internal class GenericSpineSpikes : GenericCosmeticTemplate
    {
        private CosmeticSpineSpikesData spineSpikesData => cosmeticData as CosmeticSpineSpikesData;

        public int bumps;
        public int graphic;
        public bool colored;

        // This class behave juuuuuust like LongBodyScales will all scales at x:0 but its also nice to keep some variety in I suppose
        // uses LongBodyScalesData because screw it im lazy
        public GenericSpineSpikes(ICosmeticsAdaptor iGraphics, LizKinCosmeticData cosmeticData) : base(iGraphics, cosmeticData)
        {
            if (this.cosmeticData.spritesOverlap == LizKinCosmeticData.SpritesOverlapConfig.Default) this.spritesOverlap = GenericCosmeticTemplate.SpritesOverlap.Behind;

            this.bumps = spineSpikesData.count; // (int)(this.spineLength / num);
            this.graphic = spineSpikesData.graphic; // UnityEngine.Random.Range(0, 5);
            this.colored = spineSpikesData.colored; // UnityEngine.Random.Range(0, 3);

            this.numberOfSprites = ((!this.colored) ? this.bumps : (this.bumps * 2));
        }

        public override void Update()
        {
        }

        public override void InitiateSprites(LeaserAdaptor sLeaser, CameraAdaptor rCam)
        {
            for (int i = this.startSprite + this.bumps - 1; i >= this.startSprite; i--)
            {
                // float num = Mathf.InverseLerp((float)this.startSprite, (float)(this.startSprite + this.bumps - 1), (float)i);
                sLeaser.sprites[i] = new FSprite("LizardScaleA" + this.graphic, true);
                sLeaser.sprites[i].anchorY = 0.15f;
                if (this.colored)
                {
                    sLeaser.sprites[i + this.bumps] = new FSprite("LizardScaleB" + this.graphic, true);
                    sLeaser.sprites[i + this.bumps].anchorY = 0.15f;
                }
            }
        }

        public override void DrawSprites(LeaserAdaptor sLeaser, CameraAdaptor rCam, float timeStacker, Vector2 camPos)
        {
            for (int i = this.startSprite + this.bumps - 1; i >= this.startSprite; i--)
            {
                float num = Mathf.InverseLerp(startSprite, this.startSprite + this.bumps - 1, i);
                SpineData lizardSpineData = this.iGraphics.SpinePosition(Mathf.Lerp(spineSpikesData.start, spineSpikesData.start + spineSpikesData.length, num), false, timeStacker);
                sLeaser.sprites[i].x = lizardSpineData.outerPos.x - camPos.x;
                sLeaser.sprites[i].y = lizardSpineData.outerPos.y - camPos.y;
                sLeaser.sprites[i].rotation = Custom.AimFromOneVectorToAnother(-lizardSpineData.perp * lizardSpineData.depthRotation, lizardSpineData.perp * lizardSpineData.depthRotation);
                float num2 = spineSpikesData.scale * Mathf.Lerp(spineSpikesData.sizeMin, 1f, Mathf.Sin(Mathf.Pow(num, spineSpikesData.sizeExponent) * 3.1415927f));
                sLeaser.sprites[i].scaleX = Mathf.Sign(this.iGraphics.depthRotation) * spineSpikesData.thickness * num2;
                sLeaser.sprites[i].scaleY = num2 * Mathf.Max(0.2f, Mathf.InverseLerp(0f, 0.5f, Mathf.Abs(this.iGraphics.depthRotation)));
                if (this.colored)
                {
                    sLeaser.sprites[i + this.bumps].x = lizardSpineData.outerPos.x - camPos.x;
                    sLeaser.sprites[i + this.bumps].y = lizardSpineData.outerPos.y - camPos.y;
                    sLeaser.sprites[i + this.bumps].rotation = Custom.AimFromOneVectorToAnother(-lizardSpineData.perp * lizardSpineData.depthRotation, lizardSpineData.perp * lizardSpineData.depthRotation);
                    sLeaser.sprites[i + this.bumps].scaleX = Mathf.Sign(this.iGraphics.depthRotation) * spineSpikesData.thickness * num2;
                    sLeaser.sprites[i + this.bumps].scaleY = num2 * Mathf.Max(0.2f, Mathf.InverseLerp(0f, 0.5f, Mathf.Abs(this.iGraphics.depthRotation)));
                }
            }
        }

        public override void ApplyPalette(LeaserAdaptor sLeaser, CameraAdaptor rCam, PaletteAdaptor palette)
        {
            for (int i = this.startSprite; i < this.startSprite + this.bumps; i++)
            {
                float f = Mathf.Lerp(spineSpikesData.start, spineSpikesData.start + spineSpikesData.length, Mathf.InverseLerp(startSprite, this.startSprite + this.bumps - 1, i));
                sLeaser.sprites[i].color = this.cosmeticData.GetBaseColor(iGraphics, f);
                if (this.colored && !spineSpikesData.colorFade)
                {
                    sLeaser.sprites[i + this.bumps].color = this.cosmeticData.effectColor;
                }
                else if (this.colored && spineSpikesData.colorFade)
                {
                    float f2 = Mathf.InverseLerp(startSprite, this.startSprite + this.bumps - 1, i);
                    sLeaser.sprites[i + this.bumps].color = Color.Lerp(this.cosmeticData.effectColor, this.cosmeticData.GetBaseColor(iGraphics, f), Mathf.Pow(f2, 0.5f)); // Could make this controlable exponent
                }
            }
        }
    }
}
