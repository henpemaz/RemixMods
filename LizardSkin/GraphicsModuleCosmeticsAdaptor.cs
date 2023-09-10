using System.Collections.Generic;
using UnityEngine;

namespace LizardSkin
{
    public abstract class GraphicsModuleCosmeticsAdaptor : BodyPart, ICosmeticsAdaptor, IDrawable
    {
        public RainWorld rainWorld => graphics.owner.room.game.rainWorld;
        public GraphicsModule graphics { get; protected set; }

        public float BodyAndTailLength { get => this.bodyLength + this.tailLength; }
        // public Color effectColor { get; protected set; }
        public float bodyLength { get; protected set; }
        public float tailLength { get; protected set; }
        public PaletteAdaptor palette { get; protected set; }
        public List<GenericCosmeticTemplate> cosmetics { get; protected set; }

        public float depthRotation { get; set; }
        public float headDepthRotation { get; set; }
        public float lastHeadDepthRotation { get; set; }
        public float lastDepthRotation { get; set; }

        public float showDominance { get; protected set; }

        public int firstSprite { get; protected set; }
        public int extraSprites { get; protected set; }

        public abstract Vector2 headPos { get; }
        public abstract Vector2 baseOfTailPos { get; }
        public abstract Vector2 mainBodyChunkPos { get; }
        public abstract Vector2 headLastPos { get; }
        public abstract Vector2 baseOfTailLastPos { get; }
        public abstract Vector2 mainBodyChunkLastPos { get; }
        public abstract Vector2 mainBodyChunkVel { get; }
        public abstract BodyChunk mainBodyChunckSecret { get; }

        public abstract SpineData SpinePosition(float spineFactor, bool inFront, float timeStacker);

        public abstract Color BodyColorFallback(float y);

        // public abstract Color HeadColor(float timeStacker);

        public bool PointSubmerged(Vector2 pos)
        {
            return graphics.owner.room.PointSubmerged(pos);
        }


        public GraphicsModuleCosmeticsAdaptor(GraphicsModule graphicsModule) : base(graphicsModule)
        {
            this.graphics = graphicsModule;
            this.cosmetics = new List<GenericCosmeticTemplate>();
        }

        public virtual void AddCosmetic(GenericCosmeticTemplate cosmetic)
        {
            this.cosmetics.Add(cosmetic);
            cosmetic.startSprite = this.extraSprites;
            this.extraSprites += cosmetic.numberOfSprites;
        }

        public override void Update()
        {
            for (int l = 0; l < this.cosmetics.Count; l++)
            {
                this.cosmetics[l].Update();
            }
        }

        internal LeaserAdaptor _leaserAdaptor;
        internal LeaserAdaptor GetLeaserAdaptor(RoomCamera.SpriteLeaser sLeaser)
        {
            if (_leaserAdaptor == null || !_leaserAdaptor.IsAdaptorForLeaser(sLeaser))
                _leaserAdaptor = new LeaserAdaptor(sLeaser);

            return _leaserAdaptor;
        }

        internal CameraAdaptor _cameraAdaptor;
        internal CameraAdaptor GetCameraAdaptor(RoomCamera rCam)
        {
            if (_cameraAdaptor == null || !_cameraAdaptor.IsAdaptorForCamera(rCam))
                _cameraAdaptor = new CameraAdaptor(rCam);

            return _cameraAdaptor;
        }

        //internal PaletteAdaptor _paletteAdaptor;
        internal PaletteAdaptor GetPaletteAdaptor(RoomPalette palette)
        {
            // focking structs man
            //if (_paletteAdaptor == null || palette.Equals(_paletteAdaptor.palette.Value))
            //    _paletteAdaptor = new PaletteAdaptor(palette);

            //return _paletteAdaptor;
            return new PaletteAdaptor(palette);
        }

        /// TODO
        /// allow switching containers during ondraw
        /// store weakrefs to containers
        /// expose SwitchContainers(sprite, newoverlap)
        public virtual void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            this.firstSprite = sLeaser.sprites.Length;

            System.Array.Resize(ref sLeaser.sprites, this.firstSprite + this.extraSprites);

            LeaserAdaptor leaserAdaptor = GetLeaserAdaptor(sLeaser);
            CameraAdaptor cameraAdaptor = GetCameraAdaptor(rCam);

            for (int l = 0; l < this.cosmetics.Count; l++)
            {
                this.cosmetics[l].InitiateSprites(leaserAdaptor, cameraAdaptor);
            }
            this.AddToContainer(sLeaser, rCam, null);
        }

        public virtual void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            if (newContatiner == null)
            {
                newContatiner = rCam.ReturnFContainer("Midground");
            }
            FContainer behind = new FContainer();
            FContainer behindHead = new FContainer();
            FContainer onTop = new FContainer();
            newContatiner.AddChild(behind);
            behind.MoveBehindOtherNode(getBehindNode(sLeaser));
            newContatiner.AddChild(behindHead);
            behindHead.MoveBehindOtherNode(getBehindHeadNode(sLeaser));
            newContatiner.AddChild(onTop);
            onTop.MoveInFrontOfOtherNode(getOnTopNode(sLeaser));

            LeaserAdaptor leaserAdaptor = GetLeaserAdaptor(sLeaser);
            CameraAdaptor cameraAdaptor = GetCameraAdaptor(rCam);

            for (int j = 0; j < this.cosmetics.Count; j++)
            {
                if (this.cosmetics[j].spritesOverlap == GenericCosmeticTemplate.SpritesOverlap.Behind)
                {
                    this.cosmetics[j].AddToContainer(leaserAdaptor, cameraAdaptor, behind);
                }
                if (this.cosmetics[j].spritesOverlap == GenericCosmeticTemplate.SpritesOverlap.BehindHead)
                {
                    this.cosmetics[j].AddToContainer(leaserAdaptor, cameraAdaptor, behindHead);
                }
                if (this.cosmetics[j].spritesOverlap == GenericCosmeticTemplate.SpritesOverlap.InFront)
                {
                    this.cosmetics[j].AddToContainer(leaserAdaptor, cameraAdaptor, onTop);
                }
            }
        }

        protected abstract FNode getOnTopNode(RoomCamera.SpriteLeaser sLeaser);

        protected abstract FNode getBehindHeadNode(RoomCamera.SpriteLeaser sLeaser);

        protected abstract FNode getBehindNode(RoomCamera.SpriteLeaser sLeaser);

        public virtual void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            LeaserAdaptor leaserAdaptor = GetLeaserAdaptor(sLeaser);
            CameraAdaptor cameraAdaptor = GetCameraAdaptor(rCam);
            for (int j = 0; j < this.cosmetics.Count; j++)
            {
                this.cosmetics[j].DrawSprites(leaserAdaptor, cameraAdaptor, timeStacker, camPos);
            }
        }

        public virtual void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            LeaserAdaptor leaserAdaptor = GetLeaserAdaptor(sLeaser);
            CameraAdaptor cameraAdaptor = GetCameraAdaptor(rCam);
            PaletteAdaptor paletteAdaptor = GetPaletteAdaptor(palette);
            this.palette = paletteAdaptor;
            for (int i = 0; i < this.cosmetics.Count; i++)
            {
                this.cosmetics[i].ApplyPalette(leaserAdaptor, cameraAdaptor, paletteAdaptor);
            }
        }

        // Bodypart reset
        public override void Reset(Vector2 resetPoint)
        {
            base.Reset(resetPoint);
            Reset();
        }

        // graphics hookpoint reset
        public virtual void Reset()
        {
            for (int l = 0; l < this.cosmetics.Count; l++)
            {
                this.cosmetics[l].Reset();
            }
        }
    }
}