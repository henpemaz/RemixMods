using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using UnityEngine;
using BepInEx;
using BepInEx.Configuration;
using MonoMod.RuntimeDetour;
using MonoMod.RuntimeDetour.HookGen;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System.IO;
using System.Security.Cryptography;
using BepInEx.Logging;

[module: UnverifiableCode]
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace Fartificer
{
    [BepInPlugin("com.henpemaz.fartificer", "Fartificer", "0.1.1")]
    public class Fartificer : BaseUnityPlugin
    {
        private bool init;
        private static SoundID fartificer_fart_with_reverb;

        public void OnEnable()
        {
            fartificer_fart_with_reverb = new SoundID("fartificer_fart_with_reverb", true);
            On.RainWorld.OnModsInit += RainWorld_OnModsInit;
        }

        private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            if (init) return;
            init = true;

            try
            {
                IL.Player.ClassMechanicsArtificer += Player_ClassMechanicsArtificer;
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                throw;
            }
        }

        private void Player_ClassMechanicsArtificer(ILContext il)
        {
            var c = new ILCursor(il);
            while (c.TryGotoNext(MoveType.Before,
                i => i.MatchLdsfld<SoundID>("Fire_Spear_Explode")
                ))
            {
                c.MoveAfterLabels();
                c.Remove();
                c.Emit<Fartificer>(OpCodes.Ldsfld, "fartificer_fart_with_reverb");
                Logger.LogInfo("farted");
            }
        }
    }
}
