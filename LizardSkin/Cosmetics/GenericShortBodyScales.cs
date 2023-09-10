using RWCustom;
using UnityEngine;

namespace LizardSkin
{
    internal class GenericShortBodyScales : GenericBodyScales
    {
        private CosmeticShortBodyScalesData shortBodyScalesData => cosmeticData as CosmeticShortBodyScalesData;
        public GenericShortBodyScales(ICosmeticsAdaptor iGraphics, LizKinCosmeticData cosmeticData) : base(iGraphics, cosmeticData)
        {
            MakeModeScales();
            this.numberOfSprites = this.scalesPositions.Length;
        }

        public override void InitiateSprites(LeaserAdaptor sLeaser, CameraAdaptor rCam)
        {
            for (int i = this.startSprite + this.scalesPositions.Length - 1; i >= this.startSprite; i--)
            {
                sLeaser.sprites[i] = new FSprite("pixel", true);
                sLeaser.sprites[i].scaleX = shortBodyScalesData.scale;
                sLeaser.sprites[i].scaleY = shortBodyScalesData.scale * shortBodyScalesData.thickness;
            }
        }

        public override void DrawSprites(LeaserAdaptor sLeaser, CameraAdaptor rCam, float timeStacker, Vector2 camPos)
        {
            for (int i = this.startSprite + this.scalesPositions.Length - 1; i >= this.startSprite; i--)
            {
                SpineData backPos = base.GetBackPos(i - this.startSprite, timeStacker);
                sLeaser.sprites[i].x = backPos.outerPos.x - camPos.x;
                sLeaser.sprites[i].y = backPos.outerPos.y - camPos.y;
                sLeaser.sprites[i].rotation = Custom.AimFromOneVectorToAnother(backPos.dir, -backPos.dir);
                sLeaser.sprites[i].color = this.cosmeticData.effectColor; //this.cosmeticData.GetBaseColor(iGraphics, 0);
            }
        }
    }
}
