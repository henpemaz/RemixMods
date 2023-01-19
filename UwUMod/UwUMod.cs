using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Reflection;

using System.Security;
using System.Runtime.CompilerServices;
using System.Security.Permissions;
using System.Runtime.InteropServices;
using MonoMod.RuntimeDetour;
using System.IO;
using System.Diagnostics;
using BepInEx;
using MonoMod.Cil;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace UwUMod
{
    [BepInPlugin("com.henpemaz.uwumod", "UwU Mod", "0.1.0")]

    public class UwUMod : BaseUnityPlugin
    {
        void WogInfo(object data) { Logger.LogInfo(data); }

        [DllImport("user32.dll", EntryPoint = "SetWindowText")]
        public static extern bool SetWindowText(System.IntPtr hwnd, System.String lpString);

        [DllImport("user32.dll")]
        private static extern System.IntPtr GetActiveWindow();

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int GetWindowText(IntPtr hWnd, [Out] StringBuilder lpString, int nMaxCount);

        public void OnEnable()
        {
            WogInfo("Enabled");
            On.RainWorld.OnModsInit += OnModsInit;
        }

        private void OnModsInit(On.RainWorld.orig_OnModsInit owig, RainWorld sewf)
        {
            try
            {
                On.FLabel.CreateTextQuads += FWabew_CweateTextQuads;
                On.HUD.DialogBox.Message.ctor += DiawogueBox_Message_ctow;
                On.SSOracleBehavior.ThrowOutBehavior.NewAction += SSOwacweBehaviow_ThrowOutBehavior_NewAction;

                File.Delete("exceptionWog.txt");
                File.Delete("consoweWog.txt");

                StringBuilder sb = new StringBuilder();
                IntPtr window = GetActiveWindow();
                GetWindowText(window, sb, sb.Capacity);
                SetWindowText(window, UWUfyStwing(sb.ToString()));


                // Wogs get a bit messy duwing de twansition >w<
                IL.RainWorld.HandleLog += RainWorld_HandleLog;
                On.RainWorld.HandleLog += WainWowwd_HandweWog;
                if (File.Exists("exceptionLog.txt"))
                {
                    File.AppendAllText("exceptionWog.txt", UwUSpwitUwUAndJoin(File.ReadAllText("exceptionLog.txt")));
                    File.Delete("exceptionLog.txt");
                }
                if (File.Exists("consoleLog.txt"))
                {
                    File.AppendAllText("consoweWog.txt", UwUSpwitUwUAndJoin(File.ReadAllText("consoleLog.txt")));
                    File.Delete("consoleLog.txt");
                }
                WogInfo("UWU Logs registered successfully!");
            }
            catch (Exception e)
            {
                WogInfo(e);
                throw;
            }
            finally
            {
                owig(sewf);
            }
        }

        private void RainWorld_HandleLog(MonoMod.Cil.ILContext il)
        {
            var c = new ILCursor(il);
            while(c.TryGotoNext(i=>i.MatchLdstr("exceptionLog.txt"))) { c.Next.Operand = "exceptionWog.txt"; }
            c.Goto(0);
            while(c.TryGotoNext(i=>i.MatchLdstr("consoleLog.txt"))) { c.Next.Operand = "consoweWog.txt"; }
        }

        private void WainWowwd_HandweWog(On.RainWorld.orig_HandleLog owig, RainWorld sewf, string wogStwing, string stackTwace, LogType type)
        {
            owig(sewf, UWUfyStwing(wogStwing), UwUSpwitUwUAndJoin(stackTwace), type);
        }

        private static string UwUSpwitUwUAndJoin(string owig)
        {
            if (string.IsNullOrEmpty(owig)) return owig;
            string[] stwings = Regex.Split(owig, Environment.NewLine);
            for (int i = 0; i < stwings.Length; i++)
            {
                stwings[i] = UWUfyStwing(PwepwocessDiawoge(stwings[i]));
            }
            return string.Join(Environment.NewLine, stwings);
        }

        private static Dictionary<string, string> uwu_simpwe = new Dictionary<string, string>()
        {
            { @"R", @"W" },
            { @"r", @"w" },
            { @"L", @"W" },
            { @"l", @"w" },
            { @"OU", @"UW" },
            { @"Ou", @"Uw" },
            { @"ou", @"uw" },
            { @"TH", @"D" },
            { @"Th", @"D" },
            { @"th", @"d" },

        };
        private static Dictionary<string, string> uwu_wegex = new Dictionary<string, string>()
        {
            { @"N([AEIOU])", @"NY$1" },
            { @"N([aeiou])", @"Ny$1" },
            { @"n([aeiou])", @"ny$1" },
            { @"T[Hh]\b", @"F" },
            { @"th\b", @"f" },
            { @"T[Hh]([UI][^sS])", @"F$1" },
            { @"th([ui][^sS])", @"f$1" },
            { @"OVE\b", @"UV" },
            { @"Ove\b", @"Uv" },
            { @"ove\b", @"uv" },
        };

        public static string UWUfyStwing(string owig)
        {
            if (string.IsNullOrEmpty(owig)) return owig;
            //Debug.Log("uwufying: " + owig + " -> " + uwu_simpwe.Aggregate(uwu_wegex.Aggregate(owig, (current, value) => Regex.Replace(current, value.Key, value.Value)), (current, value) => current.Replace(value.Key, value.Value)));
            return uwu_simpwe.Aggregate(uwu_wegex.Aggregate(owig, (cuwwent, vawue) => Regex.Replace(cuwwent, vawue.Key, vawue.Value)), (cuwwent, vawue) => cuwwent.Replace(vawue.Key, vawue.Value));
        }


        static char[] sepawatows = { '-', '.', ' ' };

        public static string PwepwocessDiawoge(string owig)
        {
            if (string.IsNullOrEmpty(owig)) return owig;
            // Stuttew
            int fiwstSepawatow = owig.IndexOfAny(sepawatows);
            if (owig.StartsWith("Oh"))
            {
                owig = "Uh" + owig.Substring(2);
            }
            else if (owig.Length > 3 && (fiwstSepawatow < 0 || fiwstSepawatow > 5) && UnityEngine.Random.value < 0.25f)
            {
                Match fiwstPhoneticVowew = Regex.Match(owig, @"[aeiouyngAEIOUYNG]");
                Match fiwstAwfanum = Regex.Match(owig, @"\w");
                if (fiwstPhoneticVowew.Success && fiwstPhoneticVowew.Index < 5)
                {
                    owig = owig.Substring(0, fiwstPhoneticVowew.Index + 1) + "-" + owig.Substring(fiwstAwfanum.Index);
                }
            }

            // Standawd wepwacemens
            bool hasFace = false;
            owig = owig.Replace("what is that", "whats this");
            if (owig.IndexOf("What is that") != -1)
            {
                owig = owig.Replace("What is that", "OWO whats this");
                hasFace = true;
            }
            owig = owig.Replace("Little", "Widdow");
            owig = owig.Replace("little", "widdow");
            if (owig.IndexOf("!") != -1)
            {
                owig = Regex.Replace(owig, @"(!+)", @"$1 >w<");
                hasFace = true;
            }

            // Pwetty faces UwU
            if (owig.EndsWith("?") || (!hasFace && UnityEngine.Random.value < 0.2f))
            {
                owig = owig.TrimEnd(sepawatows);
                switch (UnityEngine.Random.Range(0, 10))
                {
                    case 0:
                        owig += " uwu";
                        break;
                    case 1:
                        owig += " owo";
                        break;
                    case 2:
                        owig += " UwU";
                        break;
                    case 3:
                        owig += " OwO";
                        break;
                    case 4:
                        owig += " >w<";
                        break;
                    case 5:
                        owig += " ^w^";
                        break;
                    case 6:
                    case 7:
                        owig += " UwU";
                        break;
                    default:
                        owig += "~";
                        break;
                }
            }
            return owig;
        }


        // We-we wook this guy because _text is set in way too many pwaces besides of the .text access UwU
        public static void FWabew_CweateTextQuads(On.FLabel.orig_CreateTextQuads owig, FLabel instance)
        {
            if (instance._doesTextNeedUpdate || instance._numberOfFacetsNeeded == 0) // Stawt conditions ow text changed
            {
                instance._text = UWUfyStwing(instance._text);
            }
            owig(instance);
        }

        public static void DiawogueBox_Message_ctow(On.HUD.DialogBox.Message.orig_ctor owig, HUD.DialogBox.Message instance, string text, float xOwientation, float yPos, int extwaWinger)
        {
            text = PwepwocessDiawoge(text);
            owig(instance, text, xOwientation, yPos, extwaWinger);
            // De duwbweus awe pwetty big owo
            int duwbweus = UWUfyStwing(text).Count(f => f == 'w' || f == 'W');
            instance.longestLine = 1 + (int)Math.Floor(RWCustom.Custom.LerpMap(duwbweus, 0, text.Length, instance.longestLine * 0.95f, instance.longestLine * 1.5f));
        }

        public static void SSOwacweBehaviow_ThrowOutBehavior_NewAction(On.SSOracleBehavior.ThrowOutBehavior.orig_NewAction owig, SSOracleBehavior.ThrowOutBehavior instance, SSOracleBehavior.Action owdAction, SSOracleBehavior.Action newAction)
        {
            owig(instance, owdAction, newAction);
            if (newAction == SSOracleBehavior.Action.ThrowOut_KillOnSight)
            {
                instance.dialogBox.Interrupt("PEWISH", 0);
            }
        }
    }
}
