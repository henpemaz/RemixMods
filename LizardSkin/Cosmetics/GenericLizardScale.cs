using UnityEngine;

namespace LizardSkin
{
    // This used to be a genericbodyobject but it really only used a subset of its features so it was easily detachable
    public class GenericLizardScale
    {
        public GenericLizardScale(GenericCosmeticTemplate gCosmetics)
        {
            this.iCosmetics = gCosmetics;
        }

        public void Update()
        {
            if (this.iCosmetics.iGraphics.PointSubmerged(this.pos))
            {
                this.vel *= 0.5f;
            }
            else
            {
                this.vel *= 0.9f;
            }
            this.lastPos = this.pos;
            this.pos += this.vel;
        }

        public GenericCosmeticTemplate iCosmetics;

        public float length;

        public float width;

        public Vector2 lastPos;

        public Vector2 pos;

        public Vector2 vel;

        internal void ConnectToPoint(Vector2 pnt, float connectionRad, bool push, float elasticMovement, Vector2 hostVel, float adaptVel, float exaggerateVel)
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
}