

using System;
using System.Collections.Generic;
using UnityEngine;

namespace LizardSkin
{
    public partial class LizardSkin
    {
        public void ApplyHooksToPlayerGraphics()
        {
            On.PlayerGraphics.ctor += PlayerGraphics_ctor_hk;
            On.PlayerGraphics.Update += PlayerGraphics_Update_hk;
            On.PlayerGraphics.InitiateSprites += PlayerGraphics_InitiateSprites_hk;
            On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites_hk;
            On.PlayerGraphics.ApplyPalette += PlayerGraphics_ApplyPalette_hk;
            On.PlayerGraphics.Reset += PlayerGraphics_Reset_hk;
            On.PlayerGraphics.AddToContainer += PlayerGraphics_AddToContainer_hk;

            // Bugfix: Voidsea scene moves the player around and moves bodyparts too intead of calling a reset. Cosmetics wouldn't follow.
            On.VoidSea.VoidSeaScene.Move += VoidSeaScene_Move;
        }

        private void VoidSeaScene_Move(On.VoidSea.VoidSeaScene.orig_Move orig, VoidSea.VoidSeaScene self, Player player, Vector2 move, bool moveCamera)
        {
            orig(self, player, move, moveCamera);
            if (player.graphicsModule != null)
                GetAdaptor(player.graphicsModule as PlayerGraphics).Reset();
        }

        public void InitDebugLabels(PlayerGraphics pg, PhysicalObject ow)
        {
            pg.DEBUGLABELS = new DebugLabel[6];
            pg.DEBUGLABELS[0] = new DebugLabel(ow, new Vector2(0f, 50f));
            pg.DEBUGLABELS[1] = new DebugLabel(ow, new Vector2(0f, 40f));
            pg.DEBUGLABELS[2] = new DebugLabel(ow, new Vector2(0f, 30f));
            pg.DEBUGLABELS[3] = new DebugLabel(ow, new Vector2(0f, 20f));
            pg.DEBUGLABELS[4] = new DebugLabel(ow, new Vector2(0f, 10f));
            pg.DEBUGLABELS[5] = new DebugLabel(ow, new Vector2(0f, 0f));
        }

        protected void PlayerGraphics_ctor_hk(On.PlayerGraphics.orig_ctor orig, PlayerGraphics instance, PhysicalObject ow)
        {
            orig(instance, ow);
            //InitDebugLabels(instance, ow);

            PlayerGraphicsCosmeticsAdaptor adaptor = new PlayerGraphicsCosmeticsAdaptor(instance);
            System.Array.Resize(ref instance.bodyParts, instance.bodyParts.Length + 1);
            instance.bodyParts[instance.bodyParts.Length - 1] = adaptor;
            AddAdaptor(adaptor);
        }

        protected void PlayerGraphics_Reset_hk(On.PlayerGraphics.orig_Reset orig, PlayerGraphics instance)
        {
            orig(instance);
            GetAdaptor(instance).Reset();
        }

        protected void PlayerGraphics_ApplyPalette_hk(On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics instance, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            orig(instance, sLeaser, rCam, palette);
            GetAdaptor(instance).ApplyPalette(sLeaser, rCam, palette);
        }

        protected void PlayerGraphics_DrawSprites_hk(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics instance, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(instance, sLeaser, rCam, timeStacker, camPos);
            GetAdaptor(instance).DrawSprites(sLeaser, rCam, timeStacker, camPos);
        }

        protected bool orig_InitiateSprites_lock; // initialize calls palette lock
        protected void PlayerGraphics_InitiateSprites_hk(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics instance, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            orig_InitiateSprites_lock = true;
            orig(instance, sLeaser, rCam);
            orig_InitiateSprites_lock = false;

            GetAdaptor(instance).InitiateSprites(sLeaser, rCam);
        }

        protected void PlayerGraphics_AddToContainer_hk(On.PlayerGraphics.orig_AddToContainer orig, PlayerGraphics instance, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            orig(instance, sLeaser, rCam, newContatiner);
            if (orig_InitiateSprites_lock)
            {
                // LizardSkin.Debug("LizardSkin: Avoiding orig_InitiateSprites_lock");
            }
            else
            {
                GetAdaptor(instance).AddToContainer(sLeaser, rCam, newContatiner);
            }
        }

        protected void PlayerGraphics_Update_hk(On.PlayerGraphics.orig_Update orig, PlayerGraphics instance)
        {
            orig(instance);
            GetAdaptor(instance).Update();
        }

        //public PlayerGraphicsCosmeticsAdaptor[] playerAdaptors = new PlayerGraphicsCosmeticsAdaptor[4];
        //public List<PlayerGraphicsCosmeticsAdaptor> ghostAdaptors = new List<PlayerGraphicsCosmeticsAdaptor>();
        public WeakReference[] playerAdaptors = new WeakReference[4];
        public List<WeakReference> ghostAdaptors = new List<WeakReference>();
        protected PlayerGraphicsCosmeticsAdaptor GetAdaptor(PlayerGraphics instance)
        {
            PlayerState playerState = (instance.owner as Player).playerState;
            if (!playerState.isGhost)
            {
                // LizardSkin.DebugError("LizardSkin: Retreiving LS Adaptor for player " + playerState.playerNumber);
                return playerAdaptors[playerState.playerNumber].Target as PlayerGraphicsCosmeticsAdaptor;
            }
            PlayerGraphicsCosmeticsAdaptor toReturn = null;
            for (int i = ghostAdaptors.Count - 1; i >= 0; i--)
            {
                if (!ghostAdaptors[i].IsAlive)
                {
                    ghostAdaptors.RemoveAt(i);
                }
                else if (toReturn is null && (ghostAdaptors[i].Target as PlayerGraphicsCosmeticsAdaptor).pGraphics == instance)
                {
                    toReturn = ghostAdaptors[i].Target as PlayerGraphicsCosmeticsAdaptor;
                }
            }
            return toReturn;
        }

        protected void AddAdaptor(PlayerGraphicsCosmeticsAdaptor adaptor)
        {
            PlayerState playerState = (adaptor.graphics.owner as Player).playerState;
            if (!playerState.isGhost)
            {
                // LizardSkin.DebugError("LizardSkin: Adding LS Adaptor for player " + playerState.playerNumber);
                playerAdaptors[playerState.playerNumber] = new WeakReference(adaptor);
            }
            else
            {
                ghostAdaptors.Add(new WeakReference(adaptor));
            }
        }

    }
}
