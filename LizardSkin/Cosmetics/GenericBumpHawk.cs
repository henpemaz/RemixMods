using UnityEngine;

namespace LizardSkin
{
    internal class GenericBumpHawk : GenericCosmeticTemplate
    {
        private CosmeticBumpHawkData cosmeticBumpHawkData => cosmeticData as CosmeticBumpHawkData;
        public GenericBumpHawk(ICosmeticsAdaptor iGraphics, LizKinCosmeticData cosmeticData) : base(iGraphics, cosmeticData)
        {
            if (this.cosmeticData.spritesOverlap == LizKinCosmeticData.SpritesOverlapConfig.Default) this.spritesOverlap = GenericCosmeticTemplate.SpritesOverlap.Behind;

            this.numberOfSprites = cosmeticBumpHawkData.bumps;
        }

        public override void Update()
        {

        }

        public override void InitiateSprites(LeaserAdaptor sLeaser, CameraAdaptor rCam)
        {
            for (int i = this.startSprite + this.numberOfSprites - 1; i >= this.startSprite; i--)
            {
                float num = Mathf.InverseLerp(startSprite, this.startSprite + this.numberOfSprites - 1, i);
                sLeaser.sprites[i] = new FSprite("Circle20", true);
            }
        }

        public override void DrawSprites(LeaserAdaptor sLeaser, CameraAdaptor rCam, float timeStacker, Vector2 camPos)
        {
            for (int i = this.startSprite + this.numberOfSprites - 1; i >= this.startSprite; i--)
            {
                float num = Mathf.InverseLerp(startSprite, this.startSprite + this.numberOfSprites - 1, i);
                sLeaser.sprites[i].scale = Mathf.Lerp(cosmeticBumpHawkData.sizeRangeMin, cosmeticBumpHawkData.sizeRangeMax, Mathf.Lerp(Mathf.Sin(Mathf.Pow(num, cosmeticBumpHawkData.sizeSkewExponent) * 3.1415927f), 1f, (num >= 0.5f) ? 0f : 0.5f));
                float num2 = Mathf.Lerp(0.05f, cosmeticBumpHawkData.spineLength, num);
                SpineData lizardSpineData = this.iGraphics.SpinePosition(num2, false, timeStacker);
                sLeaser.sprites[i].x = lizardSpineData.outerPos.x - camPos.x;
                sLeaser.sprites[i].y = lizardSpineData.outerPos.y - camPos.y;
            }
        }

        public override void ApplyPalette(LeaserAdaptor sLeaser, CameraAdaptor rCam, PaletteAdaptor palette)
        {
            if (cosmeticBumpHawkData.coloredHawk)
            {
                for (int i = this.startSprite + this.numberOfSprites - 1; i >= this.startSprite; i--)
                {
                    float num = Mathf.InverseLerp(startSprite, this.startSprite + this.numberOfSprites - 1, i);
                    float num2 = Mathf.Lerp(0.05f, cosmeticBumpHawkData.spineLength, num);
                    sLeaser.sprites[i].color = Color.Lerp(this.cosmeticData.effectColor, this.cosmeticData.GetBaseColor(iGraphics, num2), num);
                }
            }
            else
            {
                for (int i = this.startSprite; i < this.startSprite + this.numberOfSprites; i++)
                {
                    float f = Mathf.Lerp(0.05f, cosmeticBumpHawkData.spineLength, Mathf.InverseLerp(startSprite, this.startSprite + this.numberOfSprites - 1, i));
                    sLeaser.sprites[i].color = this.cosmeticData.GetBaseColor(iGraphics, f);
                }
            }
        }
    }
}
