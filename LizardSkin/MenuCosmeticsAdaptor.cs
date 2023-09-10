using Menu.Remix.MixedUI;
using System.Collections.Generic;
using UnityEngine;

namespace LizardSkin
{
    internal class MenuCosmeticsAdaptor : OpContainer, ICosmeticsAdaptor
    {
        public RainWorld rainWorld => Menu.manager.rainWorld;

        public MenuCosmeticsAdaptor(Vector2 pos, LizKinProfileData profileData) : base(pos)
        {
            this.profileData = profileData;
            this.cosmetics = new List<GenericCosmeticTemplate>();

            Reset();
        }

        public override void Update()
        {
            base.Update();

            for (int l = 0; l < this.cosmetics.Count; l++)
            {
                this.cosmetics[l].Update();
            }
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            for (int j = 0; j < this.cosmetics.Count; j++)
            {
                this.cosmetics[j].ApplyPalette(leaserAdaptor, cameraAdaptor, paletteAdaptor);
                this.cosmetics[j].DrawSprites(leaserAdaptor, cameraAdaptor, 1f, Vector2.zero);
            }
            this.myContainer.scale = 3f;
        }

        public override void Reset()
        {
            base.Reset();

            myContainer.RemoveAllChildren();
            this.cosmetics.Clear();
            totalSprites = 0;

            foreach (LizKinCosmeticData cosmeticData in profileData.cosmetics)
            {
                this.AddCosmetic(GenericCosmeticTemplate.MakeCosmetic(this, cosmeticData));
            }

            //this.AddCosmetic(new GenericTailTuft(this));
            //this.AddCosmetic(new GenericTailTuft(this));
            //this.AddCosmetic(new GenericLongHeadScales(this));
            //this.AddCosmetic(new GenericAntennae(this));

            this.leaserAdaptor = new LeaserAdaptor(this.firstSprite + totalSprites);
            this.cameraAdaptor = new CameraAdaptor(this.myContainer);
            this.paletteAdaptor = new PaletteAdaptor();
            this.palette = paletteAdaptor;

            for (int j = 0; j < this.cosmetics.Count; j++)
            {
                this.cosmetics[j].InitiateSprites(leaserAdaptor, cameraAdaptor);
                this.cosmetics[j].ApplyPalette(leaserAdaptor, cameraAdaptor, paletteAdaptor);
                this.cosmetics[j].AddToContainer(leaserAdaptor, cameraAdaptor, cameraAdaptor.ReturnFContainer(null));
            }
        }

        public List<GenericCosmeticTemplate> cosmetics { get; protected set; }

        public float BodyAndTailLength => 60f;

        public float bodyLength => 30f;

        public float tailLength => 30f;

        //public Color effectColor => new Color(0.05f, 0.87f, 0.92f);
        //public Color effectColor => profileData.effectColor;

        public PaletteAdaptor palette { get; protected set; }

        public int firstSprite { get; protected set; }

        public Vector2 headPos => Vector2.zero;

        public Vector2 headLastPos => Vector2.zero;

        public Vector2 baseOfTailPos => new Vector2(0f, -40f);

        public Vector2 baseOfTailLastPos => new Vector2(0f, -40f);

        internal Vector2 tipOfTail => new Vector2(0f, -80f);

        public Vector2 mainBodyChunkPos => headPos;

        public Vector2 mainBodyChunkLastPos => headPos;

        public Vector2 mainBodyChunkVel => Vector2.zero;

        public float showDominance => 0f;

        public float depthRotation => _rotation;

        public float headDepthRotation => _rotation;

        public float lastDepthRotation => _rotation;

        public float lastHeadDepthRotation => _rotation;


        private int totalSprites = 0;
        private LeaserAdaptor leaserAdaptor;
        private CameraAdaptor cameraAdaptor;
        private PaletteAdaptor paletteAdaptor;
        private float _rotation;
        private readonly LizKinProfileData profileData;

        public void AddCosmetic(GenericCosmeticTemplate cosmetic)
        {
            this.cosmetics.Add(cosmetic);
            cosmetic.startSprite = this.totalSprites;
            this.totalSprites += cosmetic.numberOfSprites;
        }

        public Color BodyColorFallback(float y)
        {
            return Color.white;
        }

        //public Color HeadColor(float v)
        //{
        //    return Color.white;
        //}

        public float HeadRotation(float timeStacker)
        {
            return 0f;
        }

        public bool PointSubmerged(Vector2 pos)
        {
            return false;
        }

        public SpineData SpinePosition(float spineFactor, bool inFront, float timeStacker)
        {
            Vector2 pos = Vector2.Lerp(headPos, tipOfTail, spineFactor);
            float rad = RWCustom.Custom.LerpMap(spineFactor, 0.5f, 1f, 10f, 1f);
            Vector2 direction = new Vector2(0f, -1f);
            Vector2 perp = new Vector2(1f, 0f);
            float rot = Mathf.Pow(Mathf.Abs(_rotation), Mathf.Lerp(1.2f, 0.3f, Mathf.Pow(spineFactor, 0.5f))) * Mathf.Sign(_rotation);
            if (!inFront) rot *= -1;
            return new SpineData(spineFactor, pos, pos + perp * rad * rot, direction, perp, rot, rad);
        }

        internal void SetRotation(float valueFloat)
        {
            this._rotation = valueFloat;
        }
    }
}