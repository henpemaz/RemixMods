using System;
using UnityEngine;

namespace LizardSkin
{
    public abstract partial class GenericCosmeticTemplate
    {
        public GenericCosmeticTemplate(ICosmeticsAdaptor iGraphics, LizKinCosmeticData cosmeticData)
        {
            this.iGraphics = iGraphics;
            this.cosmeticData = cosmeticData;
            UnityEngine.Random.seed = cosmeticData.seed;

            if (this.cosmeticData.spritesOverlap != LizKinCosmeticData.SpritesOverlapConfig.Default) spritesOverlap = (SpritesOverlap)this.cosmeticData.spritesOverlap;
        }

        public static GenericCosmeticTemplate MakeCosmetic(ICosmeticsAdaptor iGraphics, LizKinCosmeticData cosmeticData)
        {
            switch (cosmeticData.instanceType)
            {
                case LizKinCosmeticData.CosmeticInstanceType.Antennae:
                    return new GenericAntennae(iGraphics, cosmeticData);
                case LizKinCosmeticData.CosmeticInstanceType.AxolotlGills:
                    return new GenericAxolotlGills(iGraphics, cosmeticData);
                case LizKinCosmeticData.CosmeticInstanceType.BumpHawk:
                    return new GenericBumpHawk(iGraphics, cosmeticData);
                case LizKinCosmeticData.CosmeticInstanceType.JumpRings:
                    return new GenericJumpRings(iGraphics, cosmeticData);
                case LizKinCosmeticData.CosmeticInstanceType.LongHeadScales:
                    return new GenericLongHeadScales(iGraphics, cosmeticData);
                case LizKinCosmeticData.CosmeticInstanceType.LongShoulderScales:
                    return new GenericLongShoulderScales(iGraphics, cosmeticData);
                case LizKinCosmeticData.CosmeticInstanceType.ShortBodyScales:
                    return new GenericShortBodyScales(iGraphics, cosmeticData);
                case LizKinCosmeticData.CosmeticInstanceType.SpineSpikes:
                    return new GenericSpineSpikes(iGraphics, cosmeticData);
                case LizKinCosmeticData.CosmeticInstanceType.TailFin:
                    return new GenericTailFin(iGraphics, cosmeticData);
                case LizKinCosmeticData.CosmeticInstanceType.TailGeckoScales:
                    return new GenericTailGeckoScales(iGraphics, cosmeticData);
                case LizKinCosmeticData.CosmeticInstanceType.TailTuft:
                    return new GenericTailTuft(iGraphics, cosmeticData);
                case LizKinCosmeticData.CosmeticInstanceType.Whiskers:
                    return new GenericWhiskers(iGraphics, cosmeticData);
                case LizKinCosmeticData.CosmeticInstanceType.WingScales:
                    return new GenericWingScales(iGraphics, cosmeticData);
                case LizKinCosmeticData.CosmeticInstanceType.ScavEartlers:
                    return new GenericEartlers(iGraphics, cosmeticData);
                default:
                    throw new Exception("Unsupported cosmetic data type");
            }
        }

        public virtual void Update()
        {
        }

        public virtual void Reset()
        {
        }

        public virtual void InitiateSprites(LeaserAdaptor sLeaser, CameraAdaptor rCam)
        {
        }
        public virtual void DrawSprites(LeaserAdaptor sLeaser, CameraAdaptor rCam, float timeStacker, Vector2 camPos)
        {
        }
        public virtual void ApplyPalette(LeaserAdaptor sLeaser, CameraAdaptor rCam, PaletteAdaptor palette)
        {
            this.palette = palette;
        }

        public void AddToContainer(LeaserAdaptor sLeaser, CameraAdaptor rCam, FContainer newContatiner)
        {
            if (this.startSprite + this.numberOfSprites <= sLeaser.sprites.Length)
            {
                for (int i = this.startSprite; i < this.startSprite + this.numberOfSprites; i++)
                {

                    newContatiner.AddChild(sLeaser.sprites[i]);
                }
            }
            else
            {
                throw new System.Exception("sprite indexes f'd up, henpe you goof");
            }
        }

        public ICosmeticsAdaptor iGraphics;
        public LizKinCosmeticData cosmeticData;

        // Token: 0x040021D8 RID: 8664
        public int numberOfSprites;

        // Token: 0x040021D9 RID: 8665
        public int startSprite { get => this.iGraphics.firstSprite + this._startSprite; set => this._startSprite = value; }

        public int _startSprite;

        // Token: 0x040021DA RID: 8666
        public PaletteAdaptor palette;

        // Token: 0x040021DB RID: 8667
        public GenericCosmeticTemplate.SpritesOverlap spritesOverlap;

        // Token: 0x020004BB RID: 1211
        public enum SpritesOverlap
        {
            // Token: 0x040021DD RID: 8669
            Behind,
            // Token: 0x040021DE RID: 8670
            BehindHead,
            // Token: 0x040021DF RID: 8671
            InFront
        }
    }
}