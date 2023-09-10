using RWCustom;
using UnityEngine;

namespace LizardSkin
{
    internal class GenericJumpRings : GenericCosmeticTemplate
    {
        private int numberOfRings;

        private CosmeticJumpRingsData jumpRingsData => cosmeticData as CosmeticJumpRingsData;

        public GenericJumpRings(ICosmeticsAdaptor iGraphics, LizKinCosmeticData cosmeticData) : base(iGraphics, cosmeticData)
        {
            if (this.cosmeticData.spritesOverlap == LizKinCosmeticData.SpritesOverlapConfig.Default) this.spritesOverlap = GenericCosmeticTemplate.SpritesOverlap.InFront;
            this.numberOfRings = jumpRingsData.count;
            this.numberOfSprites = 4 * numberOfRings;
        }


        public int RingSprite(int ring, int side, int part)
        {
            if (jumpRingsData.invertOverlap) return this.startSprite + part + side * 2 + numberOfSprites - 4 - ring * 4;
            return this.startSprite + part + side * 2 + ring * 4;
        }

        public override void Update()
        {
        }

        public override void InitiateSprites(LeaserAdaptor sLeaser, CameraAdaptor rCam)
        {
            for (int i = 0; i < numberOfRings; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    for (int k = 0; k < 2; k++)
                    {
                        sLeaser.sprites[this.RingSprite(i, j, k)] = new FSprite("Circle20", true);
                    }
                }
            }
        }

        public override void DrawSprites(LeaserAdaptor sLeaser, CameraAdaptor rCam, float timeStacker, Vector2 camPos)
        {
            float to = Mathf.Lerp(this.iGraphics.lastDepthRotation, this.iGraphics.depthRotation, timeStacker);
            float from = Mathf.Lerp(this.iGraphics.lastHeadDepthRotation, this.iGraphics.headDepthRotation, timeStacker);

            Color color = this.cosmeticData.effectColor;
            float num = (1f - 0.5f * iGraphics.showDominance); // 1f;
                                                               //if (this.iGraphics.lizard.animation == Lizard.Animation.PrepareToJump)
                                                               //{
                                                               //	num = 0.5f + 0.5f * Mathf.InverseLerp((float)this.iGraphics.lizard.timeToRemainInAnimation, 0f, (float)this.iGraphics.lizard.timeInAnimation);
                                                               //	color = Color.Lerp(this.iGraphics.HeadColor(timeStacker), Color.Lerp(Color.white, this.iGraphics.effectColor, num), UnityEngine.Random.value);
                                                               //}
            for (int i = 0; i < numberOfRings; i++)
            {
                float s = numberOfRings == 1 ? jumpRingsData.spineStart : Mathf.Lerp(jumpRingsData.spineStart, jumpRingsData.spineStop, Mathf.Pow(i / (float)(numberOfRings - 1), jumpRingsData.spineExponent)); //0.06f + 0.12f * (float)i;
                float scale = numberOfRings == 1 ? jumpRingsData.scaleStart : Mathf.Lerp(jumpRingsData.scaleStart, jumpRingsData.scaleStop, Mathf.Pow(i / (float)(numberOfRings - 1), jumpRingsData.scaleExponent));
                Color color2 = this.cosmeticData.GetBaseColor(iGraphics, s);
                SpineData lizardSpineData = this.iGraphics.SpinePosition(s, true, timeStacker);
                Vector2 vector = lizardSpineData.dir;
                Vector2 pos = lizardSpineData.pos;
                if (i == 0)
                {
                    vector = (vector + this.iGraphics.SpinePosition(0f, true, timeStacker).dir).normalized;
                }
                Vector2 a = Custom.PerpendicularVector(vector);
                float num2 = 50f * Mathf.Lerp(from, to, ((i != 0 && numberOfRings > 1) ? 0.5f : 0.33f) * (1f - s));
                for (int j = 0; j < 2; j++)
                {
                    Vector2 vector2 = Custom.DegToVec(num2 + ((j != 0f) ? 40f : -40f));
                    Vector2 vector3 = pos + a * lizardSpineData.rad * vector2.x;
                    Vector2 vector4 = vector;
                    // Added support to adjustable amount of rings
                    //if (i == 0)
                    //{
                    //	vector4 = (vector4 - 2f * Custom.DirVec(vector3, Vector2.Lerp(this.iGraphics.headLastPos, this.iGraphics.headPos, timeStacker)) * Mathf.Abs(vector2.y)).normalized;
                    //}
                    //else
                    //{
                    //	vector4 = (vector4 + 2f * Custom.DirVec(vector3, Vector2.Lerp(this.iGraphics.baseOfTailLastPos, this.iGraphics.baseOfTailPos, timeStacker)) * Mathf.Abs(vector2.y)).normalized;
                    //}
                    if (numberOfRings > 1)
                    {
                        Vector2 headPos = Vector2.Lerp(this.iGraphics.headLastPos, this.iGraphics.headPos, timeStacker);
                        Vector2 tailPos = Vector2.Lerp(this.iGraphics.baseOfTailLastPos, this.iGraphics.baseOfTailPos, timeStacker);
                        vector4 = (vector4 + jumpRingsData.spread * Vector2.Lerp(Custom.DirVec(headPos, vector3), Custom.DirVec(vector3, tailPos), i / (float)(numberOfRings - 1)) * Mathf.Abs(vector2.y)).normalized;
                    }
                    else
                    {
                        vector4 = Custom.RotateAroundOrigo(vector4, ((j != 0f) ? 9f : -9f) * jumpRingsData.spread);
                    }

                    sLeaser.sprites[this.RingSprite(i, j, 0)].x = vector3.x - camPos.x;
                    sLeaser.sprites[this.RingSprite(i, j, 0)].y = vector3.y - camPos.y;
                    sLeaser.sprites[this.RingSprite(i, j, 0)].rotation = Custom.VecToDeg(vector4);
                    vector3 = pos + a * (lizardSpineData.rad + 2f * Mathf.Pow(Mathf.Clamp01(Mathf.Abs(vector2.x) * Mathf.Abs(vector2.y)), 0.5f)) * vector2.x;
                    vector3 -= vector4 * (1f - num) * 4f * jumpRingsData.innerScale * scale; /// ???????????
					sLeaser.sprites[this.RingSprite(i, j, 1)].x = vector3.x - camPos.x;
                    sLeaser.sprites[this.RingSprite(i, j, 1)].y = vector3.y - camPos.y;
                    sLeaser.sprites[this.RingSprite(i, j, 1)].rotation = Custom.VecToDeg(vector4);
                    float t = Mathf.Pow(Mathf.Clamp01(Mathf.Abs(vector2.x)), 2f);
                    sLeaser.sprites[this.RingSprite(i, j, 0)].scaleX = scale * jumpRingsData.thickness * ((vector2.y <= 0f) ? 0f : Mathf.Lerp(0.45f, 0f, t));
                    sLeaser.sprites[this.RingSprite(i, j, 0)].scaleY = scale * 0.55f;
                    //sLeaser.sprites[this.RingSprite(i, j, 0)].color = new Color(1f, 0f, 0f);
                    sLeaser.sprites[this.RingSprite(i, j, 0)].color = color;
                    sLeaser.sprites[this.RingSprite(i, j, 1)].scaleX = jumpRingsData.innerScale * scale * jumpRingsData.thickness * ((vector2.y <= 0f) ? 0f : (Mathf.Lerp(0.27f, 0f, t) * num));
                    sLeaser.sprites[this.RingSprite(i, j, 1)].scaleY = jumpRingsData.innerScale * scale * 0.33f * num;
                    sLeaser.sprites[this.RingSprite(i, j, 1)].color = color2;
                }
            }
        }

        public override void ApplyPalette(LeaserAdaptor sLeaser, CameraAdaptor rCam, PaletteAdaptor palette)
        {
            for (int i = 0; i < numberOfRings; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    sLeaser.sprites[this.RingSprite(i, j, 1)].color = palette.blackColor;
                }
            }
        }
    }
}
