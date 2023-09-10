using UnityEngine;

namespace LizardSkin
{
    internal class GenericLongHeadScales : GenericLongBodyScales
    {
        private CosmeticLongHeadScalesData longHeadScalesData => cosmeticData as CosmeticLongHeadScalesData;
        public GenericLongHeadScales(ICosmeticsAdaptor iGraphics, LizKinCosmeticData cosmeticData) : base(iGraphics, cosmeticData)
        {
            this.scalesPositions = new Vector2[2];
            this.scaleObjects = new GenericLizardScale[this.scalesPositions.Length];
            this.backwardsFactors = new float[this.scalesPositions.Length];
            for (int i = 0; i < this.scalesPositions.Length; i++)
            {
                this.scaleObjects[i] = new GenericLizardScale(this);
            }
            this.numberOfSprites = ((!this.colored) ? this.scalesPositions.Length : (this.scalesPositions.Length * 2));
            PositionStuff();
        }

        private void PositionStuff()
        {
            // copypasted code to keep things inline with the settings
            float y = longHeadScalesData.spinePos; // Mathf.Lerp(0f, 0.07f, UnityEngine.Random.value);
            float num = longHeadScalesData.offset; // Mathf.Lerp(0.5f, 1.5f, UnityEngine.Random.value);
            float num2 = longBodyScalesData.scale; // Mathf.Pow(UnityEngine.Random.value, 0.7f);
            float value = longBodyScalesData.thickness; // UnityEngine.Random.value;
            float num3 = longHeadScalesData.angle; // Mathf.Pow(UnityEngine.Random.value, 0.85f);
            for (int i = 0; i < this.scalesPositions.Length; i++)
            {
                this.scalesPositions[i] = new Vector2((i != 0) ? num : (-num), y);
                this.scaleObjects[i].length = num2; // Mathf.Lerp(5f, 35f, num2);
                this.scaleObjects[i].width = value * num2; // Mathf.Lerp(0.65f, 1.2f, value * num2);
                this.backwardsFactors[i] = num3;
            }
        }

        public override void Update()
        {
            PositionStuff();
            base.Update();
        }
    }
}
