using RWCustom;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace LizardSkin
{
    public class GenericEartlers : GenericCosmeticTemplate
    {
        public GenericEartlers(ICosmeticsAdaptor iGraphics, LizKinCosmeticData cData) : base(iGraphics, cData)
        {

            if (this.cosmeticData.spritesOverlap == LizKinCosmeticData.SpritesOverlapConfig.Default) this.spritesOverlap = SpritesOverlap.BehindHead;
            this.GenerateSegments();
            this.numberOfSprites = this.TotalSprites;
        }

        public int TotalSprites
        {
            get
            {
                return this.points.Count;
            }
        }

        private void GenerateSegments()
        {
            //#warning native seeding; maybe change to something controllable later
            //or maybe leave it as is to allow people to get eartlers of their favourite scooc
            //decided to yes
            int oldseed = UnityEngine.Random.seed;
            UnityEngine.Random.seed = this.eartlersData.seed;
            this.points = new List<ScavengerGraphics.Eartlers.Vertex[]>();
            List<ScavengerGraphics.Eartlers.Vertex> vertices = new List<ScavengerGraphics.Eartlers.Vertex>();
            vertices.Clear();
            vertices.Add(new ScavengerGraphics.Eartlers.Vertex(new Vector2(0f, 0f), 1f));
            vertices.Add(new ScavengerGraphics.Eartlers.Vertex(Custom.DegToVec(Mathf.Lerp(40f, 90f, UnityEngine.Random.value)) * 0.4f, 1f));
            Vector2 vector = Custom.DegToVec(Mathf.Lerp(15f, 45f, UnityEngine.Random.value));
            Vector2 pos = vector - Custom.DegToVec(Mathf.Lerp(40f, 90f, UnityEngine.Random.value)) * 0.4f;
            if (pos.x < 0.2f)
            {
                pos.x = Mathf.Lerp(pos.x, vector.x, 0.4f);
            }
            vertices.Add(new ScavengerGraphics.Eartlers.Vertex(pos, 1.5f));
            vertices.Add(new ScavengerGraphics.Eartlers.Vertex(vector, 2f));
            this.DefineBranch(vertices);
            vertices.Clear();
            vertices.Add(new ScavengerGraphics.Eartlers.Vertex(this.points[0][1].pos, 1f));
            int num = ((double)Vector2.Distance(this.points[0][1].pos, this.points[0][2].pos) <= 0.6 || UnityEngine.Random.value >= 0.5f) ? 1 : 2;
            float2 vector2 = Unity.Mathematics.math.lerp(this.points[0][1].pos, this.points[0][2].pos, Mathf.Lerp(0f, (num != 1) ? 0.25f : 0.7f, UnityEngine.Random.value));
            vertices.Add(new ScavengerGraphics.Eartlers.Vertex(vector2, 1.2f));
            vertices.Add(new ScavengerGraphics.Eartlers.Vertex(vector2 + this.points[0][3].pos - this.points[0][2].pos + Custom.DegToFloat2(UnityEngine.Random.value * 360f) * 0.1f, 1.75f));
            this.DefineBranch(vertices);
            if (num == 2)
            {
                vertices.Clear();
                vector2 = Vector2.Lerp(this.points[0][1].pos, this.points[0][2].pos, Mathf.Lerp(0.45f, 0.7f, UnityEngine.Random.value));
                vertices.Add(new ScavengerGraphics.Eartlers.Vertex(vector2, 1.2f));
                vertices.Add(new ScavengerGraphics.Eartlers.Vertex(vector2 + this.points[0][3].pos - this.points[0][2].pos + Custom.DegToFloat2(UnityEngine.Random.value * 360f) * 0.1f, 1.75f));
                this.DefineBranch(vertices);
            }
            bool flag = UnityEngine.Random.value < 0.5f;
            if (flag)
            {
                vertices.Clear();
                Vector2 vector3 = Custom.DegToVec(90f + Mathf.Lerp(-20f, 20f, UnityEngine.Random.value)) * Mathf.Lerp(0.2f, 0.5f, UnityEngine.Random.value);
                if (vector3.y > this.points[0][1].pos.y - 0.1f)
                {
                    vector3.y -= 0.2f;
                }
                float num2 = Mathf.Lerp(0.8f, 2f, UnityEngine.Random.value);
                if (UnityEngine.Random.value < 0.5f)
                {
                    vector3 += Custom.DegToVec(Mathf.Lerp(120f, 170f, UnityEngine.Random.value)) * Mathf.Lerp(0.1f, 0.3f, UnityEngine.Random.value);
                    vertices.Add(new ScavengerGraphics.Eartlers.Vertex(new Vector2(0f, 0f), num2));
                    vertices.Add(new ScavengerGraphics.Eartlers.Vertex(vector3, num2));
                }
                else
                {
                    vertices.Add(new ScavengerGraphics.Eartlers.Vertex(new Vector2(0f, 0f), 1f));
                    vertices.Add(new ScavengerGraphics.Eartlers.Vertex(vector3, (1f + num2) / 2f));
                    vertices.Add(new ScavengerGraphics.Eartlers.Vertex(vector3 + Custom.DegToVec(Mathf.Lerp(95f, 170f, UnityEngine.Random.value)) * Mathf.Lerp(0.1f, 0.2f, UnityEngine.Random.value), num2));
                }
                this.DefineBranch(vertices);
            }
            if (UnityEngine.Random.value > 0.25f || !flag)
            {
                vertices.Clear();
                float num3 = 1f + UnityEngine.Random.value * 1.5f;
                bool flag2 = UnityEngine.Random.value < 0.5f;
                vertices.Add(new ScavengerGraphics.Eartlers.Vertex(new Vector2(0f, 0f), 1f));
                float num4 = Mathf.Lerp(95f, 135f, UnityEngine.Random.value);
                float num5 = Mathf.Lerp(0.25f, 0.4f, UnityEngine.Random.value);
                vertices.Add(new ScavengerGraphics.Eartlers.Vertex(Custom.DegToVec(num4) * num5, (!flag2) ? Mathf.Lerp(1f, num3, 0.3f) : 0.8f));
                vertices.Add(new ScavengerGraphics.Eartlers.Vertex(Custom.DegToVec(num4 + Mathf.Lerp(5f, 35f, UnityEngine.Random.value)) * Mathf.Max(num5 + 0.1f, Mathf.Lerp(0.3f, 0.6f, UnityEngine.Random.value)), (!flag2) ? Mathf.Lerp(1f, num3, 0.6f) : 0.8f));
                vertices.Add(new ScavengerGraphics.Eartlers.Vertex(vertices[vertices.Count - 1].pos.normalized() * (vertices[vertices.Count - 1].pos.magnitude() + Unity.Mathematics.math.lerp(0.15f, 0.25f, UnityEngine.Random.value)), num3));
                this.DefineBranch(vertices);
            }
            UnityEngine.Random.seed = oldseed;
        }
        private void DefineBranch(List<ScavengerGraphics.Eartlers.Vertex> vList)
        {
            this.points.Add(vList.ToArray());
            for (int i = 0; i < vList.Count; i++)
            {
                vList[i] = new ScavengerGraphics.Eartlers.Vertex(new Vector2(-vList[i].pos.x, vList[i].pos.y), vList[i].rad);
            }
            this.points.Add(vList.ToArray());
        }

        public override void Update()
        {
            base.Update();
            lastBodyAxis = bodyAxis;
            bodyAxis = (iGraphics.headPos - iGraphics.baseOfTailPos).normalized;
        }

        public override void InitiateSprites(LeaserAdaptor sLeaser, CameraAdaptor rCam)
        {
            int num = this.startSprite;
            for (int i = 0; i < this.points.Count; i++)
            {
                //cet
                sLeaser.sprites[num] = TriangleMesh.MakeLongMesh(this.points[i].Length, false, this.eartlersData.overrideEffectColor);
                num++;
            }
        }
        public override void ApplyPalette(LeaserAdaptor sLeaser, CameraAdaptor rCam, PaletteAdaptor palette)
        {
            base.ApplyPalette(sLeaser, rCam, palette);
            try
            {
                for (int i = 0; i < this.points.Count; i++)
                {
                    sLeaser.sprites[this.startSprite + i].color = eartlersData.GetBaseColor(iGraphics, 0f);
                }
                if (this.eartlersData.overrideEffectColor)
                {
                    for (int j = 0; j < this.points.Count; j++)
                    {
                        for (int k = 0; k < 2; k++)
                        {
                            if (sLeaser.sprites[this.startSprite + j] is TriangleMesh mesh)
                                mesh.verticeColors[(this.points[j].Length - 1) * 4 + 3 - k] = eartlersData.effectColor;

                        }
                    }
                }
            }
            catch (Exception e) { LizardSkin.Debug(e); }

        }
        public override void DrawSprites(LeaserAdaptor sLeaser, CameraAdaptor rCam, float timeStacker, Vector2 camPos)
        {
            Vector2 headPos = iGraphics.SpinePosition(0f, false, timeStacker).pos;
            Vector2 hd = Custom.RotateAroundOrigo(new Vector2(0f, -1f), -GetBodyAxis(timeStacker));
            Vector2 headDir = hd + Custom.PerpendicularVector(hd).normalized * iGraphics.depthRotation * 0.85f;
            float lookUpFac = Mathf.Lerp(-1f, 1f, Mathf.InverseLerp(-1f, 1f, hd.y));
            float rotat = Custom.VecToDeg(-headDir);
            float num = 1f - Mathf.Pow(Mathf.Abs(Custom.RotateAroundOrigo(headDir, this.GetBodyAxis(timeStacker)).x), 1.5f) * Mathf.Lerp(1f, 0.5f, lookUpFac);
            float ySqueeze = Mathf.Lerp(1f, -0.25f, lookUpFac);

            int num2 = this.startSprite;
            for (int i = 0; i < this.points.Count; i++)
            {
                Vector2 vector = this.RotatedPosOfVertex(i, 0, headPos, rotat, num, ySqueeze);
                Vector2 v = Custom.PerpendicularVector(this.points[i][1].pos, this.points[i][0].pos);
                float num3 = this.points[i][0].rad;
                for (int j = 0; j < this.points[i].Length; j++)
                {
                    float num4 = j / (float)(this.points[i].Length - 1);
                    Vector2 vector2 = this.RotatedPosOfVertex(i, j, headPos, rotat, num, ySqueeze);
                    Vector2 normalized = (vector - vector2).normalized;
                    Vector2 vector3 = Custom.PerpendicularVector(normalized);
                    float d = Vector2.Distance(vector2, vector) / 10f;
                    float num5 = Mathf.Lerp(1f, 2f, this.eartlersData.eartlerWidth) * this.points[i][j].rad * Mathf.Lerp(0.5f, 1f, num);
                    //#warning weird cast shenanigans went on here, might need to recheck
                    //seems to be working fine
                    (sLeaser.sprites[num2] as TriangleMesh).MoveVertice(j * 4, vector - (Vector2)(Vector3.Slerp(v, vector3, 0.5f) * (num5 + num3) * 0.5f) - normalized * d - camPos);
                    (sLeaser.sprites[num2] as TriangleMesh).MoveVertice(j * 4 + 1, vector + (Vector2)(Vector3.Slerp(v, vector3, 0.5f) * (num5 + num3) * 0.5f) - normalized * d - camPos);
                    if (num4 == 1f)
                    {
                        num5 /= 4f;
                    }
                    (sLeaser.sprites[num2] as TriangleMesh).MoveVertice(j * 4 + 2, vector2 - vector3 * num5 + normalized * d - camPos);
                    (sLeaser.sprites[num2] as TriangleMesh).MoveVertice(j * 4 + 3, vector2 + vector3 * num5 + normalized * d - camPos);
                    vector = vector2;
                    v = vector3;
                    num3 = num5;
                }
                num2++;
            }
        }

        private Vector2 RotatedPosOfVertex(int segment, int vert, Vector2 headPos, float rotat, float xSqueeze, float ySqueeze)
        {
            return headPos + Custom.RotateAroundOrigo(new Vector2(this.points[segment][vert].pos.x * xSqueeze, this.points[segment][vert].pos.y * ySqueeze), rotat) * Mathf.Lerp(15f, 35f, this.eartlersData.dominance);
        }


        private GenericEartlersCosmeticData eartlersData => cosmeticData as GenericEartlersCosmeticData;
        private float GetBodyAxis(float ts)
        {
            //figure out bodyaxis math
            return -Custom.VecToDeg(Vector3.Slerp(this.lastBodyAxis, this.bodyAxis, ts).normalized);
        }
        private Vector2 bodyAxis;
        private Vector2 lastBodyAxis;

        private List<ScavengerGraphics.Eartlers.Vertex[]> points;


        internal class GenericEartlersCosmeticData : LizKinCosmeticData
        {
            public GenericEartlersCosmeticData()
            {
                this.dominance = 0.3f;
                this.eartlerWidth = 0.5f;

            }
            public override CosmeticInstanceType instanceType => CosmeticInstanceType.ScavEartlers;
            public float dominance = 0.5f;
            public float eartlerWidth = 0.3f;
            internal override CosmeticPanel MakeEditPanel(LizardSkinOI.ProfileManager manager)
            {
                return new EartlersConfigPanel(this, manager);
            }

            internal override void ReadEditPanel(CosmeticPanel panel)
            {
                base.ReadEditPanel(panel);
                if (panel is EartlersConfigPanel eartlersPanel)
                {

                }
            }
        }
        internal class EartlersConfigPanel : LizKinCosmeticData.CosmeticPanel
        {
            //config panel is a stub
            internal EartlersConfigPanel(GenericEartlersCosmeticData cd, LizardSkinOI.ProfileManager mngr) : base(cd, mngr)
            {

            }
        }
    }
}