using System.Collections.Generic;
using UnityEngine;

namespace LizardSkin
{
    public interface ICosmeticsAdaptor
    {
        RainWorld rainWorld { get; }
        List<GenericCosmeticTemplate> cosmetics { get; }
        float BodyAndTailLength { get; }
        float bodyLength { get; }
        float tailLength { get; }
        int firstSprite { get; }

        Vector2 headPos { get; }
        Vector2 headLastPos { get; }
        Vector2 baseOfTailPos { get; }
        Vector2 baseOfTailLastPos { get; }
        Vector2 mainBodyChunkPos { get; }
        Vector2 mainBodyChunkLastPos { get; }
        Vector2 mainBodyChunkVel { get; }


        float showDominance { get; }
        float depthRotation { get; }
        float headDepthRotation { get; }
        float lastDepthRotation { get; }
        float lastHeadDepthRotation { get; }

        bool PointSubmerged(Vector2 pos);
        void AddCosmetic(GenericCosmeticTemplate cosmetic);
        //void ApplyPalette(LeaserAdaptor sLeaser, CameraAdaptor rCam, PaletteAdaptor palette);
        //void InitiateSprites(LeaserAdaptor sLeaser, CameraAdaptor rCam);
        //void DrawSprites(LeaserAdaptor sLeaser, CameraAdaptor rCam, float timeStacker, Vector2 camPos);
        //void Reset();
        //void Update();


        PaletteAdaptor palette { get; }
        // Color now comes from cosmeticData, falls back to adaptor if unset in the profile
        // Color effectColor { get;}
        Color BodyColorFallback(float y);
        //Color HeadColor(float v);
        //float HeadRotation(float timeStacker);
        SpineData SpinePosition(float spineFactor, bool inFront, float timeStacker);
    }

    public class LeaserAdaptor
    {
        private RoomCamera.SpriteLeaser sLeaser;
        private FSprite[] _sprites;
        public FSprite[] sprites
        {
            get
            {
                if (this.sLeaser != null) return this.sLeaser.sprites;
                return this._sprites;
            }
            set
            {
                if (this.sLeaser != null) this.sLeaser.sprites = value;
                else this._sprites = value;
            }
        }


        public LeaserAdaptor(int totalSprites)
        {
            _sprites = new FSprite[totalSprites];
        }

        public LeaserAdaptor(FSprite[] fSprites)
        {
            _sprites = fSprites;
        }

        public LeaserAdaptor(RoomCamera.SpriteLeaser sLeaser)
        {
            this.sLeaser = sLeaser;
            //this.sprites = sLeaser.sprites;
        }

        public bool IsAdaptorForLeaser(RoomCamera.SpriteLeaser sLeaser)
        {
            return sLeaser == this.sLeaser;
        }
    }

    public class CameraAdaptor
    {
        private RoomCamera rCam;
        private FContainer _defaultContainer;

        public CameraAdaptor(RoomCamera rCam)
        {
            this.rCam = rCam;
        }

        public CameraAdaptor(FContainer defaultContainer)
        {
            this._defaultContainer = defaultContainer;
        }

        public FContainer ReturnFContainer(string v)
        {
            if (rCam != null) return rCam.ReturnFContainer(v);
            return _defaultContainer;
        }

        public bool IsAdaptorForCamera(RoomCamera rCam)
        {
            return rCam == this.rCam;
        }
    }

    public class PaletteAdaptor
    {
        //internal RoomPalette? palette;
        public Color blackColor;
        public Color skyColor;
        public float darkness;

        public PaletteAdaptor()
        {
            blackColor = new Color(0.05f, 0.05f, 0.05f);
            skyColor = Color.white;
            darkness = 0.5f;
        }

        public PaletteAdaptor(RoomPalette palette)
        {
            //this.palette = palette;
            blackColor = palette.blackColor;
            skyColor = palette.skyColor;
            darkness = palette.darkness;
        }
    }

    public class GenericBodyPartAdaptor
    {
        private GenericBodyPart _genericBodyPart;
        private Vector2 _pos;
        private Vector2 _lastPos;
        private Vector2 _vel;
        //private BodyChunk _connection;
        private float _rad;
        private float _airFriction;

        public GenericBodyPartAdaptor(ICosmeticsAdaptor iG, float rd, float sfFric, float aFric)
        {
            if (iG is GraphicsModuleCosmeticsAdaptor)
            {
                this._genericBodyPart = new GenericBodyPart((iG as GraphicsModuleCosmeticsAdaptor).graphics, rd, sfFric, aFric, (iG as GraphicsModuleCosmeticsAdaptor).mainBodyChunckSecret);
            }
            else
            {
                this._rad = rd;
                this._airFriction = aFric;
                //BodyChunk con = new BodyChunk(null, 0, pos, rd, 1f);
                //this._connection = con;
                this.Reset(iG.mainBodyChunkPos);
            }
        }

        public ref Vector2 vel
        {
            get
            {
                if (this._genericBodyPart != null) return ref this._genericBodyPart.vel;
                return ref this._vel;
            }
            //internal set {
            //    if (this._genericBodyPart != null) this._genericBodyPart.vel = value;
            //    else this._vel = value;
            //}
        }

        public ref Vector2 pos
        {
            get
            {
                if (this._genericBodyPart != null) return ref this._genericBodyPart.pos;
                return ref this._pos;
            }
        }

        public ref Vector2 lastPos
        {
            get
            {
                if (this._genericBodyPart != null) return ref this._genericBodyPart.lastPos;
                return ref this._lastPos;
            }
        }

        public void ConnectToPoint(Vector2 pnt, float connectionRad, bool push, float elasticMovement, Vector2 hostVel, float adaptVel, float exaggerateVel)
        {
            if (this._genericBodyPart != null) this._genericBodyPart.ConnectToPoint(pnt, connectionRad, push, elasticMovement, hostVel, adaptVel, exaggerateVel);
            else
            {
                if (elasticMovement > 0f)
                {
                    this.vel += RWCustom.Custom.DirVec(this.pos, pnt) * Vector2.Distance(this.pos, pnt) * elasticMovement;
                }
                this.vel += hostVel * exaggerateVel;
                if (push || !RWCustom.Custom.DistLess(this.pos, pnt, connectionRad))
                {
                    float num = Vector2.Distance(this.pos, pnt);
                    Vector2 a = RWCustom.Custom.DirVec(this.pos, pnt);
                    this.pos -= (connectionRad - num) * a * 1f;
                    this.vel -= (connectionRad - num) * a * 1f;
                }
                this.vel -= hostVel;
                this.vel *= 1f - adaptVel;
                this.vel += hostVel;
            }
        }

        public void Reset(Vector2 vector2)
        {
            if (this._genericBodyPart != null)
            {
                this._genericBodyPart.Reset(vector2);
            }
            else
            {
                this._pos = vector2 + RWCustom.Custom.DegToVec(UnityEngine.Random.value * 360f);
                this._lastPos = this._pos;
                this._vel = new Vector2(0f, 0f);
            }
        }

        public void Update()
        {
            if (this._genericBodyPart != null)
            {
                this._genericBodyPart.Update();
            }
            else
            {
                this.lastPos = this.pos;
                this.pos += this.vel;
                this.vel *= this._airFriction;
            }
        }
    }

    public struct SpineData
    {
        public SpineData(float f, Vector2 pos, Vector2 outerPos, Vector2 dir, Vector2 perp, float depthRotation, float rad)
        {
            this.f = f;
            this.pos = pos;
            this.outerPos = outerPos;
            this.dir = dir;
            this.perp = perp;
            this.depthRotation = depthRotation;
            this.rad = rad;
        }
        public float f;
        public Vector2 pos;
        public Vector2 outerPos;
        public Vector2 dir;
        public Vector2 perp;
        public float depthRotation;
        public float rad;
    }
}