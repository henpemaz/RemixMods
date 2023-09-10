using RWCustom;
using UnityEngine;

namespace LizardSkin
{
    public abstract class GenericBodyScales : GenericCosmeticTemplate
    {
        internal BodyScalesData bodyScalesData => this.cosmeticData as BodyScalesData;

        public Vector2[] scalesPositions;

        public GenericBodyScales(ICosmeticsAdaptor iGraphics, LizKinCosmeticData cosmeticData) : base(iGraphics, cosmeticData)
        {
            //this.spritesOverlap = ((!(this is SlugcatLongHeadScales)) ? SlugcatCosmeticsTemplate.SpritesOverlap.BehindHead : SlugcatCosmeticsTemplate.SpritesOverlap.InFront);
            //this.spritesOverlap = GenericCosmeticTemplate.SpritesOverlap.BehindHead;
            if (this.cosmeticData.spritesOverlap == LizKinCosmeticData.SpritesOverlapConfig.Default) this.spritesOverlap = GenericCosmeticTemplate.SpritesOverlap.Behind;
        }

        protected void MakeModeScales()
        {
            switch (bodyScalesData.mode)
            {
                case BodyScalesData.GenerationMode.Patch:
                    GeneratePatchPattern(bodyScalesData.start, bodyScalesData.length, bodyScalesData.count, bodyScalesData.roundness);
                    break;
                case BodyScalesData.GenerationMode.Lines:
                    GenerateTwoLines(bodyScalesData.start, bodyScalesData.length, bodyScalesData.count, bodyScalesData.roundness);
                    break;
                case BodyScalesData.GenerationMode.Segments:
                    GenerateSegments(bodyScalesData.start, bodyScalesData.length, bodyScalesData.count, bodyScalesData.roundness);
                    break;
            }
        }

        protected void GeneratePatchPattern(float startPoint, float length, int numOfScales, float roundness)
        {
            //todo fix this; how can I achieve "parametric randomness"
            this.scalesPositions = new Vector2[numOfScales];
            float stopPoint = startPoint + Mathf.Max(0.1f, length);// Mathf.Lerp(startPoint + 0.1f, Mathf.Max(startPoint + 0.2f, length), Mathf.Pow(Random.value, lengthExponent));
            for (int i = 0; i < this.scalesPositions.Length; i++)
            {
                // huh, this spreads nicely now that I think about it, good job joar
                Vector2 vector = Custom.DegToVec(Random.value * 360f) * roundness;// Random.value;
                this.scalesPositions[i].y = Mathf.Lerp(startPoint * this.iGraphics.bodyLength / this.iGraphics.BodyAndTailLength, stopPoint * this.iGraphics.bodyLength / this.iGraphics.BodyAndTailLength, (vector.y + 1f) / 2f);
                this.scalesPositions[i].x = vector.x;
            }
        }

        protected void GenerateTwoLines(float startPoint, float length, int numOfScales, float roundness)
        {
            float spineLenght = Mathf.Max(0.1f, length); // Mathf.Lerp(startPoint + 0.1f, Mathf.Max(startPoint + 0.2f, maxLength), Mathf.Pow(Random.value, lengthExponent));
                                                         //float pixelLenght = spineLenght * this.iGraphics.BodyAndTailLength;
                                                         //float pixelSpacing = Mathf.Lerp(2f, 9f, Random.value);
                                                         //pixelSpacing = 2f;
                                                         //pixelSpacing *= spacingScale;
            int count = numOfScales; // (int)(pixelLenght / spacing);
            if (count < 2)
            {
                count = 2;
            }
            this.scalesPositions = new Vector2[count * 2];
            for (int i = 0; i < count; i++)
            {
                float y = startPoint + Mathf.Lerp(0f, spineLenght, i / (float)(count - 1));
                float num5 = (1 - roundness) + roundness * Mathf.Sin(i / (float)(count - 1) * 3.1415927f);
                this.scalesPositions[i * 2] = new Vector2(num5, y);
                this.scalesPositions[i * 2 + 1] = new Vector2(-num5, y);
            }
        }

        protected void GenerateSegments(float startPoint, float length, int numOfScales, float roundness)
        {
            float stopPoint = startPoint + Mathf.Max(0.1f, length); // Mathf.Lerp(startPoint + 0.1f, Mathf.Max(startPoint + 0.2f, length), Mathf.Pow(Random.value, lengthExponent));
                                                                    //float num2 = stopPoint * this.iGraphics.BodyAndTailLength;
                                                                    //float num3 = Mathf.Lerp(7f, 14f, Random.value);
                                                                    //if (this.pGraphics.lizard.abstractCreature.creatureTemplate.type == CreatureTemplate.Type.RedLizard)
                                                                    //{
                                                                    //	num3 = Mathf.Min(num3, 11f) * 0.75f;
                                                                    //}
                                                                    // uuuh could use one more param here...
            int segments = Mathf.Max(2, Mathf.CeilToInt(numOfScales * roundness / 2f)); // Mathf.Max(3, (int)(num2 / num3));
            int scalesPerSegment = numOfScales / segments; // Random.Range(1, 4) * 2;
            this.scalesPositions = new Vector2[segments * scalesPerSegment];
            for (int i = 0; i < segments; i++)
            {
                float y = Mathf.Lerp(startPoint, stopPoint, i / (float)(segments - 1));
                for (int j = 0; j < scalesPerSegment; j++)
                {
                    float num6 = (1 - roundness) + roundness * Mathf.Sin(i / (float)(segments - 1) * 3.1415927f);
                    num6 *= Mathf.Lerp(-1f, 1f, j / (float)(scalesPerSegment - 1));
                    this.scalesPositions[i * scalesPerSegment + j] = new Vector2(num6, y);
                }
            }
        }

        protected SpineData GetBackPos(int shoulderScale, float timeStacker)
        {
            SpineData result = this.iGraphics.SpinePosition(this.scalesPositions[shoulderScale].y, this.spritesOverlap != SpritesOverlap.Behind, timeStacker);

            float num = Mathf.Clamp(this.scalesPositions[shoulderScale].x + result.depthRotation, -1f, 1f);
            result.outerPos = result.pos + result.perp * num * result.rad;
            result.depthRotation = num;
            return result;
        }
    }
}