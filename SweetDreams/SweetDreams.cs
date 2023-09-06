using BepInEx;
using Menu;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using UnityEngine;

[assembly: AssemblyTrademark("Intikus, Tealppup & Henpemaz")]
[module: UnverifiableCode]
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace SweetDreams
{
    [BepInPlugin("com.henpemaz.sweetdreams", "Sweet Dreams", "0.1.0")]
    public class SweetDreams : BaseUnityPlugin
    {
        public void OnEnable()
        {
            Logger.LogInfo("OnEnable");
            On.RainWorld.OnModsInit += OnModsInit;
            On.RainWorld.PostModsInit += RainWorld_PostModsInit;
        }

        private void RainWorld_PostModsInit(On.RainWorld.orig_PostModsInit orig, RainWorld self)
        {
            orig(self);
            try
            {
                Logger.LogInfo("PostModsInit");
                LoadSlugsAndDreams(self);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                throw;
            }
        }

        //void Update() // debug thinghies
        //{
        //    if (Input.GetKeyDown("1"))
        //    {
        //        if (GameObject.FindObjectOfType<RainWorld>().processManager.currentMainLoop is RainWorldGame game)
        //            game.Win(false);
        //    }
        //}

        public bool init;
        public void OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            try
            {
                if (init) return;
                init = true;

                Logger.LogInfo("OnModsInit");
                On.Menu.SleepAndDeathScreen.GetDataFromGame += SleepAndDeathScreen_GetDataFromGame;
                On.RainWorldGame.RestartGame += RainWorldGame_RestartGame;
                Logger.LogInfo("OnModsInit done");
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                throw;
            }
            finally
            {
                orig(self);
            }
        }

        private void RainWorldGame_RestartGame(On.RainWorldGame.orig_RestartGame orig, RainWorldGame self)
        {
            LoadSlugsAndDreams(self.rainWorld);
            orig(self);
        }

        public class Vector2Converter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(Vector2?);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var t = serializer.Deserialize<double[]>(reader);
                if (t == null) return new Vector2?();
                return new Vector2?(new Vector2((float)t[0], (float)t[1]));
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                Vector2? v = (Vector2?)value;
                if (!v.HasValue) return;
                writer.WriteStartArray();
                writer.WriteValue(v.Value.x);
                writer.WriteValue(v.Value.y);
                writer.WriteEndArray();
            }
        }

        public class ExtEnumConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return typeof(ExtEnumBase).IsAssignableFrom(objectType);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var value = serializer.Deserialize<string>(reader);
                return value != null && ExtEnumBase.TryParse(objectType, value, true, out var val) ? val : null;
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                if (value == null) writer.WriteNull();
                else writer.WriteValue(value.ToString());
            }
        }

        public static Dictionary<string, SlugDreams> dreamtionary;
        public void LoadSlugsAndDreams(RainWorld rw)
        {
            dreamtionary = new();
            var jsonSerializer = new JsonSerializer();
            jsonSerializer.Converters.Add(new Vector2Converter());
            jsonSerializer.Converters.Add(new ExtEnumConverter());
            foreach (var s in AssetManager.ListDirectory("sweetdreams"))
            {
                Logger.LogInfo("reading file " + s);
                try
                {
                    var read = (Dictionary<string, SlugDreams>)jsonSerializer.Deserialize(File.OpenText(AssetManager.ResolveFilePath(s)), typeof(Dictionary<string, SlugDreams>));
                    UpdateFromOther(dreamtionary, read);
                }
                catch (Exception e)
                {
                    Logger.LogError("Error reading file " + s);
                    Logger.LogError(e);
                    //throw;
                }
            }

            try
            {
                Logger.LogInfo(JsonConvert.SerializeObject(dreamtionary, Formatting.Indented, new Vector2Converter(), new ExtEnumConverter()));
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                throw;
            }
        }

        public interface IUpdatebleFromOther
        {
            void UpdateFromOther(IUpdatebleFromOther other);
        }
        public static void UpdateFromOther(IDictionary self, IDictionary other)
        {
            foreach (var key in other.Keys)
            {
                if (self.Contains(key))
                {
                    var obj = self[key];
                    if (obj is IUpdatebleFromOther iufo) iufo.UpdateFromOther((IUpdatebleFromOther)other[key]);
                    else if (obj is IDictionary idi) UpdateFromOther(idi, (IDictionary)other[key]);
                    else self[key] = other[key];
                }
                else
                {
                    self[key] = other[key];
                }
            }
        }

        public class SlugDreams : IUpdatebleFromOther
        {
            public Dictionary<string, Dream> dreams = new();
            public SlugcatStats.Name fallback;
            public string sleepingSprite;

            internal bool TryGetValue(CreatureTemplate.Type type, out Dream dream)
            {
                return this.dreams.TryGetValue(type.value, out dream) || (fallback != null && dreamtionary.TryGetValue(fallback.value, out var other) && other.TryGetValue(type, out dream));
            }

            public void UpdateFromOther(IUpdatebleFromOther other)
            {
                if (other is SlugDreams sd)
                {
                    SweetDreams.UpdateFromOther(dreams, sd.dreams);
                    fallback = sd.fallback ?? fallback;
                    sleepingSprite = sd.sleepingSprite ?? sd.sleepingSprite;
                }
            }
        }

        public class Dream : IUpdatebleFromOther
        {
            public string song;
            public Dictionary<string, DreamLayer> layers = new();

            public void UpdateFromOther(IUpdatebleFromOther other)
            {
                if (other is Dream sd)
                {
                    song = sd.song ?? song;
                    SweetDreams.UpdateFromOther(layers, sd.layers);
                }
            }
        }

        public class DreamLayer : IUpdatebleFromOther
        {
            public string sprite;
            public bool? onTop;
            public Vector2? pos;
            public float? slugDepthOffset;
            public MenuDepthIllustration.MenuShader shader;

            public void UpdateFromOther(IUpdatebleFromOther other)
            {
                if (other is DreamLayer sd)
                {
                    sprite = sd.sprite ?? sprite;
                    onTop = sd.onTop ?? onTop;
                    pos = sd.pos ?? pos;
                    slugDepthOffset = sd.slugDepthOffset ?? slugDepthOffset;
                    shader = sd.shader ?? shader;
                }
            }
        }

        private void SleepAndDeathScreen_GetDataFromGame(On.Menu.SleepAndDeathScreen.orig_GetDataFromGame orig, SleepAndDeathScreen self, KarmaLadderScreen.SleepDeathScreenDataPackage package)
        {
            orig(self, package);
            // sleep and death, but we only care about sleep
            if (!self.IsSleepScreen)
            {
                return;
            }
            var slugcat = GetSlugInDen(self, package);
            if (slugcat == null)
            {
                Logger.LogError("couldn't find slugcat!");
                return;
            }
            var slugstration = GetIllustrationOfSlug(self, slugcat);
            if (slugstration == null)
            {
                Logger.LogError("couldn't find slugcat's illustration in the scene!");
                return;
            }
            var friendInDen = GetFriendInDen(self, package);
            if (friendInDen == null)
            {
                Logger.LogInfo("couldn't find friend in den!");
                return;
            }
            var dream = GetDreamOfFriend(package, slugcat, friendInDen);
            if (dream == null)
            {
                Logger.LogInfo($"no dream for {slugcat} and {friendInDen}");
                return;
            }

            if (!string.IsNullOrEmpty(dream.song) && self.manager.musicPlayer != null)
            {
                self.manager.musicPlayer.MenuRequestsSong(dream.song, 0.5f, 10f);
            }

            foreach (var layer in dream.layers.Values)
            {
                var stration = new MenuDepthIllustration(self, self.scene, "illustrations" + Path.DirectorySeparatorChar + "sweetdreams", layer.sprite, layer.pos ?? Vector2.zero, slugstration.depth + layer.slugDepthOffset ?? 0, layer.shader ?? MenuDepthIllustration.MenuShader.Basic);
                self.scene.AddIllustration(stration);
                if (layer.onTop ?? true) stration.sprite.MoveInFrontOfOtherNode(slugstration.sprite);
                else stration.sprite.MoveBehindOtherNode(slugstration.sprite);
            }
        }

        public SlugcatStats.Name GetSlugInDen(SleepAndDeathScreen sads, KarmaLadderScreen.SleepDeathScreenDataPackage package)
        {
            return package.characterStats.name;
        }

        public AbstractCreature GetFriendInDen(SleepAndDeathScreen sads, KarmaLadderScreen.SleepDeathScreenDataPackage package)
        {
            return package.sessionRecord.friendInDen;
        }

        public Dream GetDreamOfFriend(KarmaLadderScreen.SleepDeathScreenDataPackage package, SlugcatStats.Name slugcat, AbstractCreature friendInDen)
        {
            if (dreamtionary.TryGetValue(slugcat.value, out var listOfDreams))
            {
                if (listOfDreams.TryGetValue(friendInDen.creatureTemplate.type, out Dream dream))
                {
                    return dream;
                }
            }
            return null;
        }

        public MenuDepthIllustration GetIllustrationOfSlug(SleepAndDeathScreen sads, SlugcatStats.Name slug)
        {
            if (dreamtionary.TryGetValue(slug.value, out var slugDreams) && !string.IsNullOrEmpty(slugDreams.sleepingSprite))
            {
                return sads.scene.depthIllustrations.FirstOrDefault(v => v.fileName.ToLowerInvariant() == slugDreams.sleepingSprite.ToLowerInvariant());
            }
            return null;
        }
    }
}
