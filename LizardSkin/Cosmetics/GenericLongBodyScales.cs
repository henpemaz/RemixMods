using RWCustom;
using UnityEngine;

namespace LizardSkin
{
    public abstract class GenericLongBodyScales : GenericBodyScales
    {
        internal LongBodyScalesData longBodyScalesData => this.cosmeticData as LongBodyScalesData;

        public GenericLizardScale[] scaleObjects;
        public float[] backwardsFactors;

        // Moved from GenericBodyScales
        public int graphic;
        public bool colored;

        public GenericLongBodyScales(ICosmeticsAdaptor iGraphics, LizKinCosmeticData cosmeticData) : base(iGraphics, cosmeticData)
        {
            this.graphic = longBodyScalesData.graphic;
            this.colored = longBodyScalesData.colored;
        }

        public override void Update()
        {
            SpineData headSpine = iGraphics.SpinePosition(0f, this.spritesOverlap != SpritesOverlap.Behind, 1f);
            for (int i = 0; i < this.scaleObjects.Length; i++)
            {
                SpineData backPos = base.GetBackPos(i, 1f);
                Vector2 vector = Vector2.Lerp(backPos.dir, Custom.DirVec(backPos.pos, backPos.outerPos), Mathf.Abs(backPos.depthRotation));
                if (this.scalesPositions[i].y < 0.2f)
                {
                    vector -= headSpine.dir * Mathf.Pow(Mathf.InverseLerp(0.2f, 0f, this.scalesPositions[i].y), 2f) * 2f;
                }
                vector = Vector2.Lerp(vector, backPos.dir, Mathf.Pow(this.backwardsFactors[i], Mathf.Lerp(1f, 15f, this.iGraphics.showDominance))).normalized;
                Vector2 vector2 = backPos.outerPos + vector * this.scaleObjects[i].length * 10f;
                if (!Custom.DistLess(this.scaleObjects[i].pos, vector2, this.scaleObjects[i].length * 10f / 2f))
                {
                    Vector2 a = Custom.DirVec(this.scaleObjects[i].pos, vector2);
                    float num = Vector2.Distance(this.scaleObjects[i].pos, vector2);
                    float num2 = this.scaleObjects[i].length * 10f / 2f;
                    this.scaleObjects[i].pos += a * (num - num2);
                    this.scaleObjects[i].vel += a * (num - num2);
                }
                this.scaleObjects[i].vel += Vector2.ClampMagnitude(vector2 - this.scaleObjects[i].pos, Mathf.Lerp(10f, 20f, this.iGraphics.showDominance)) / Mathf.Lerp(5f, 1.5f, longBodyScalesData.rigor);
                this.scaleObjects[i].vel *= Mathf.Lerp(1f, 0.8f, longBodyScalesData.rigor);
                if (this.iGraphics.showDominance > 0f)
                {
                    this.scaleObjects[i].vel += Custom.DegToVec(Random.value * 360f) * Mathf.Lerp(0f, 6f, this.iGraphics.showDominance);
                }
                this.scaleObjects[i].ConnectToPoint(backPos.outerPos, this.scaleObjects[i].length * 10f, false, 0f, new Vector2(0f, 0f), 0f, 0f);
                this.scaleObjects[i].Update();
            }
        }

        public override void InitiateSprites(LeaserAdaptor sLeaser, CameraAdaptor rCam)
        {
            for (int i = this.startSprite + this.scalesPositions.Length - 1; i >= this.startSprite; i--)
            {
                sLeaser.sprites[i] = new FSprite("LizardScaleA" + this.graphic, true);
                sLeaser.sprites[i].anchorY = 0.1f;
                if (this.colored)
                {
                    sLeaser.sprites[i + this.scalesPositions.Length] = new FSprite("LizardScaleB" + this.graphic, true);
                    sLeaser.sprites[i + this.scalesPositions.Length].anchorY = 0.1f;
                }
            }
        }

        public override void DrawSprites(LeaserAdaptor sLeaser, CameraAdaptor rCam, float timeStacker, Vector2 camPos)
        {
            for (int i = this.startSprite + this.scalesPositions.Length - 1; i >= this.startSprite; i--)
            {
                SpineData backPos = base.GetBackPos(i - this.startSprite, timeStacker);
                sLeaser.sprites[i].x = backPos.outerPos.x - camPos.x;
                sLeaser.sprites[i].y = backPos.outerPos.y - camPos.y;
                sLeaser.sprites[i].rotation = Custom.AimFromOneVectorToAnother(backPos.outerPos, Vector2.Lerp(this.scaleObjects[i - this.startSprite].lastPos, this.scaleObjects[i - this.startSprite].pos, timeStacker));
                sLeaser.sprites[i].scaleY = this.scaleObjects[i - this.startSprite].length;
                sLeaser.sprites[i].scaleX = this.scaleObjects[i - this.startSprite].width * Mathf.Sign(backPos.depthRotation);
                if (this.colored)
                {
                    sLeaser.sprites[i + this.scalesPositions.Length].x = backPos.outerPos.x - camPos.x;
                    sLeaser.sprites[i + this.scalesPositions.Length].y = backPos.outerPos.y - camPos.y;
                    sLeaser.sprites[i + this.scalesPositions.Length].rotation = Custom.AimFromOneVectorToAnother(backPos.outerPos, Vector2.Lerp(this.scaleObjects[i - this.startSprite].lastPos, this.scaleObjects[i - this.startSprite].pos, timeStacker));
                    sLeaser.sprites[i + this.scalesPositions.Length].scaleY = this.scaleObjects[i - this.startSprite].length;
                    sLeaser.sprites[i + this.scalesPositions.Length].scaleX = this.scaleObjects[i - this.startSprite].width * Mathf.Sign(backPos.depthRotation);
                }
            }
        }

        public override void ApplyPalette(LeaserAdaptor sLeaser, CameraAdaptor rCam, PaletteAdaptor palette)
        {
            for (int i = this.startSprite + this.scalesPositions.Length - 1; i >= this.startSprite; i--)
            {
                sLeaser.sprites[i].color = this.cosmeticData.GetBaseColor(iGraphics, this.scalesPositions[i - this.startSprite].y);
                if (this.colored)
                {
                    sLeaser.sprites[i + this.scalesPositions.Length].color = this.cosmeticData.effectColor;
                }
            }
            base.ApplyPalette(sLeaser, rCam, palette);
        }
    }
}
