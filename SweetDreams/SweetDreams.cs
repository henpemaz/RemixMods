// #define _DEBUG

using BepInEx;
using Menu;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SweetDreams
{
  public interface IUpdatableFromOther
  {
    public void Update(IUpdatableFromOther other);
  }

  public static class ExtDictionary
  {
    // TryGetValue with type conversion
    public static bool TryGetValueWithType<T>(this Dictionary<string, object> self, string name, out T output)
    {
      if (self.TryGetValue(name, out object obj) && obj is T value)
      {
        output = value;
        return true;
      }
      output = default;
      return false;
    }

    public static void TrySetValueWithType<T>(this Dictionary<string, object> self, string name, ref T output)
    {
      if (self.TryGetValue(name, out object obj) && obj is T value)
        output = value;
    }

    // TryGetValueWithType, where retrieved element is searched in specified class' static fields
    public static bool TryGetStatic<T>(this Dictionary<string, object> self, string name, out T output)
    {
      if (self.TryGetValueWithType(name, out string fieldName) && Plugin.GetStaticElement<T>(fieldName) is T value)
      {
        output = value;
        return true;
      }
      output = default;
      return false;
    }

    public static void TrySetStatic<T>(this Dictionary<string, object> self, string name, ref T output)
    {
      if (self.TryGetValueWithType(name, out string fieldName) && Plugin.GetStaticElement<T>(fieldName) is T value)
        output = value;
    }
  }

  public static class ExtIDictionary
  {
    public static void UpdateFromOther(this IDictionary self, IDictionary other)
    {
      foreach (object key in other.Keys)
        if (self.Contains(key))
        {
          object obj = self[key];
          if (obj is IUpdatableFromOther iufo)
            iufo.Update((IUpdatableFromOther)other[key]);
          else if (obj is IDictionary idi)
            idi.UpdateFromOther((IDictionary)other[key]);
          else
            self[key] = other[key];
        }
        else
          self[key] = other[key];
    }
  }

  [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
  public class Plugin : BaseUnityPlugin
  {
    public const string PLUGIN_GUID = "henpemaz_sweetdreams";
    public const string PLUGIN_NAME = "Sweet Dreams";
    public const string PLUGIN_VERSION = "0.2.0";

    public void OnEnable()
    {
      On.RainWorld.OnModsInit += RainWorld_OnModsInit;
    }

    public void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
      try
      {
        On.Menu.SleepAndDeathScreen.GetDataFromGame += SleepAndDeathScreen_GetDataFromGame;
        On.RainWorldGame.RestartGame += RainWorldGame_RestartGame;

#if _DEBUG
        On.RainWorldGame.Update += RainWorldGame_Update;
#endif

        LoadData();
      }
      catch (Exception e)
      {
        Logger.LogError(e);
      }

      orig(self);
    }

#if _DEBUG
    public void RainWorldGame_Update(On.RainWorldGame.orig_Update orig, RainWorldGame self)
    {
      orig(self);
      if (Input.GetKey("b"))
        self.Win(false, false);
    }
#endif

    public void RainWorldGame_RestartGame(On.RainWorldGame.orig_RestartGame orig, RainWorldGame self)
    {
      LoadData();
      orig(self);
    }

    public void SleepAndDeathScreen_GetDataFromGame(On.Menu.SleepAndDeathScreen.orig_GetDataFromGame orig, SleepAndDeathScreen self, KarmaLadderScreen.SleepDeathScreenDataPackage package)
    {
      orig(self, package);

      if (!self.IsSleepScreen)
        return;

      string slugcatName = package.characterStats.name.value;
      if (slugcatName == null)
      {
        Logger.LogError("Failed to find slugcat in the shelter");
        return;
      }

      if (!allDreams.TryGetValue(slugcatName, out SlugcatDreams slugcatDreams))
      {
        Logger.LogError("Failed to find slugcat's illustration in loaded scenes");
        return;
      }

      MenuDepthIllustration menuDepthIllustration = self.scene.depthIllustrations
        .FirstOrDefault(v => v.fileName.ToLowerInvariant() == slugcatDreams.sleepingSprite.ToLowerInvariant());
      if (menuDepthIllustration == null)
      {
        Logger.LogError("Failed to find slugcat's illustration in current scene");
        return;
      }

      AbstractCreature friend = package.sessionRecord.friendInDen;
      if (friend == null)
      {
        Logger.LogWarning("Failed to find friend in the den");
        return;
      }

      if (!slugcatDreams.dreams.TryGetValue(friend.creatureTemplate.type.value, out Dream dream)
        && !slugcatDreams.dreams.TryGetValue(slugcatDreams.fallback, out dream))
      {
        Logger.LogError($"No dream was found for {slugcatName} and {friend.creatureTemplate.type.value}");
        return;
      }

      if (!dream.song.IsNullOrWhiteSpace())
      {
        self.manager.musicPlayer?.MenuRequestsSong(dream.song, 0.5f, 10f);
      }

      foreach (KeyValuePair<string, DreamLayer> layerData in dream.layers)
      {
        DreamLayer layer = layerData.Value;
        MenuDepthIllustration customIllustration =
          new(self, self.scene, "illustrations\\sweetdreams", layer.sprite, layer.pos ?? Vector2.zero,
          menuDepthIllustration.depth + (layer.slugDepthOffset ?? 0f), layer.shader);
        self.scene.AddIllustration(customIllustration);
        if (layer.onTop ?? true)
          customIllustration.sprite.MoveInFrontOfOtherNode(menuDepthIllustration.sprite);
        else
          customIllustration.sprite.MoveBehindOtherNode(menuDepthIllustration.sprite);
      }
    }

    // Returns static field with given name from specified class
    public static T GetStaticElement<T>(string name)
    {
      object value = typeof(T)
        .GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
        .FirstOrDefault(v => v.Name.ToLowerInvariant() == name.ToLowerInvariant())
        ?.GetValue(null);
      return value == null ? default : (T)value;
    }

    public class DreamLayer : IUpdatableFromOther
    {
      public string sprite;
      public MenuDepthIllustration.MenuShader shader;
      public float? slugDepthOffset;
      public bool? onTop;
      public Vector2? pos;

      public DreamLayer(Dictionary<string, object> layer)
      {
        layer.TrySetValueWithType("sprite", ref sprite);
        layer.TrySetStatic("shader", ref shader);
        layer.TrySetValueWithType("onTop", ref onTop);
        if (layer.TryGetValueWithType("slugDepthOffset", out double depth))
          slugDepthOffset = (float)depth;
        if (layer.TryGetValueWithType("pos", out List<object> coordinates))
          pos = new(Convert.ToInt32(coordinates[0]), Convert.ToInt32(coordinates[1]));
      }

      public void Update(IUpdatableFromOther other)
      {
        if (other is not DreamLayer layer)
          return;
        sprite = layer.sprite ?? sprite;
        shader = layer.shader ?? shader;
        onTop = layer.onTop ?? onTop;
        slugDepthOffset = layer.slugDepthOffset ?? slugDepthOffset;
        pos = layer.pos ?? pos;
      }
    }


    public class Dream : IUpdatableFromOther
    {
      public string song;
      public Dictionary<string, DreamLayer> layers = new();

      public Dream(Dictionary<string, object> creatureDream)
      {
        creatureDream.TrySetValueWithType("song", ref song);
        if (!creatureDream.TryGetValueWithType("layers", out Dictionary<string, object> parsedLayers))
          return;
        if (parsedLayers.TryGetValueWithType("bottom", out Dictionary<string, object> bottom))
          layers["bottom"] = new(bottom);
        if (parsedLayers.TryGetValueWithType("top", out Dictionary<string, object> top))
          layers["top"] = new(top);
      }

      public void Update(IUpdatableFromOther other)
      {
        if (other is not Dream dream)
          return;
        song = dream.song ?? song;
        layers.UpdateFromOther(dream.layers);
      }
    }

    public class SlugcatDreams : IUpdatableFromOther
    {
      public string sleepingSprite, fallback;
      public Dictionary<string, Dream> dreams = new();

      public SlugcatDreams(Dictionary<string, object> dream)
      {
        dream.TrySetValueWithType("sleepingSprite", ref sleepingSprite);
        dream.TrySetValueWithType("fallback", ref fallback);
        if (dream.TryGetValueWithType("dreams", out Dictionary<string, object> creatureDreams))
          foreach (KeyValuePair<string, object> creatureDream in creatureDreams)
            dreams[creatureDream.Key] = new(creatureDream.Value as Dictionary<string, object>);
      }

      public void Update(IUpdatableFromOther other)
      {
        if (other is not SlugcatDreams slugcatDreams)
          return;
        sleepingSprite = slugcatDreams.sleepingSprite ?? sleepingSprite;
        fallback = slugcatDreams.fallback ?? fallback;
        dreams.UpdateFromOther(slugcatDreams.dreams);
      }
    }

    public static Dictionary<string, SlugcatDreams> allDreams;

    public void LoadData()
    {
      allDreams = new();
      try
      {
        foreach (string path in AssetManager.ListDirectory("sweetdreams"))
        {
          Logger.LogInfo($"Reading at: {path}");
          foreach (KeyValuePair<string, object> slugcatDreamData in Json.Parser.Parse(File.ReadAllText(path)) as Dictionary<string, object>)
          {
            SlugcatDreams parsedSlugcatDreams = new(slugcatDreamData.Value as Dictionary<string, object>);
            if (allDreams.TryGetValue(slugcatDreamData.Key, out SlugcatDreams slugcatDreams))
              slugcatDreams.Update(parsedSlugcatDreams);
            else
              allDreams[slugcatDreamData.Key] = parsedSlugcatDreams;
          }
        }
        Logger.LogInfo($"Finished reading");
      }
      catch (Exception e)
      {
        Logger.LogError(e);
      }
    }
  }
}
