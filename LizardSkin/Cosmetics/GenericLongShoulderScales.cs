using UnityEngine;

namespace LizardSkin
{
    internal class GenericLongShoulderScales : GenericLongBodyScales
    {
        private CosmeticLongShoulderScalesData shoulderScalesData => cosmeticData as CosmeticLongShoulderScalesData;
        public GenericLongShoulderScales(ICosmeticsAdaptor iGraphics, LizKinCosmeticData cosmeticData) : base(iGraphics, cosmeticData)
        {
            MakeModeScales();
            float minsize = shoulderScalesData.minSize; // Mathf.Lerp(5f, 15f, UnityEngine.Random.value) * num2;

            this.scaleObjects = new GenericLizardScale[this.scalesPositions.Length];
            this.backwardsFactors = new float[this.scalesPositions.Length];
            float maxy = 0f;
            float miny = 1f;
            for (int i = 0; i < this.scalesPositions.Length; i++)
            {
                if (this.scalesPositions[i].y > maxy)
                {
                    maxy = this.scalesPositions[i].y;
                }
                if (this.scalesPositions[i].y < miny)
                {
                    miny = this.scalesPositions[i].y;
                }
            }
            float p = shoulderScalesData.sizeExponent; //Mathf.Lerp(0.1f, 0.9f, UnityEngine.Random.value);
            for (int j = 0; j < this.scalesPositions.Length; j++)
            {
                this.scaleObjects[j] = new GenericLizardScale(this);
                float num7 = Mathf.Pow(Mathf.InverseLerp(miny, maxy, this.scalesPositions[j].y), p);
                this.scaleObjects[j].length = longBodyScalesData.scale * Mathf.Lerp(minsize, 1f, Mathf.Lerp(Mathf.Sin(num7 * 3.1415927f), 1f, (num7 >= 0.5f) ? 0f : 0.5f));
                this.scaleObjects[j].width = Mathf.Lerp(0.8f, 1.2f, Mathf.Lerp(Mathf.Sin(num7 * 3.1415927f), 1f, (num7 >= 0.5f) ? 0f : 0.5f)) * longBodyScalesData.thickness * longBodyScalesData.scale;
                this.backwardsFactors[j] = this.scalesPositions[j].y * 0.7f;
            }
            this.numberOfSprites = ((!this.colored) ? this.scalesPositions.Length : (this.scalesPositions.Length * 2));
        }
    }
}
