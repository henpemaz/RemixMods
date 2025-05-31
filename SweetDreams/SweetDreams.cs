// #define _DEBUG

using BepInEx;
using Menu;
using MoreSlugcats;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;
using PreferenceMode = SweetDreams.PluginInterface.PreferenceMode;

namespace SweetDreams
{
  public interface IUpdatableFromOther
  {
    public void Update(IUpdatableFromOther other);
  }

  public static class Extensions
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

    public static void TrySetExtEnum<T>(this Dictionary<string, object> self, string name, ref T output) where T : ExtEnum<T>
    {
      if (self.TryGetValueWithType(name, out string fieldName)
        && ExtEnumBase.TryParse(typeof(T), fieldName, true, out ExtEnumBase enumBase)
        && enumBase is T result)
        output = result;
    }

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

    public static List<AbstractCreature> GetCreaturesWithType(this List<AbstractCreature> creatures, CreatureTemplate.Type type)
    {
      List<AbstractCreature> result = new();
      foreach (AbstractCreature creature in creatures)
        if (creature.state.alive && creature.creatureTemplate.type == type)
          result.Add(creature);
      return result;
    }
  }

  [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
  public class Plugin : BaseUnityPlugin
  {
    public const string PLUGIN_GUID = "henpemaz_sweetdreams";
    public const string PLUGIN_NAME = "Sweet Dreams";
    public const string PLUGIN_VERSION = "0.2.0";

    public PluginInterface pluginInterface;

    public PreferenceMode preferenceMode => pluginInterface.preferenceMode.Value;

    public void OnEnable()
    {
      On.RainWorld.OnModsInit += RainWorld_OnModsInit;
    }

    public void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
      try
      {
        On.Menu.SleepAndDeathScreen.GetDataFromGame += SleepAndDeathScreen_GetDataFromGame;

#if _DEBUG
        On.RainWorldGame.Update += RainWorldGame_Update;
#endif

        LoadData();

        pluginInterface = new PluginInterface();
        MachineConnector.SetRegisteredOI(PLUGIN_GUID, pluginInterface);
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
      if (Input.GetKey("n"))
      {
        foreach (AbstractRoom room in self.world.abstractRooms)
          foreach (AbstractCreature creature in room.creatures)
            if (creature.realizedCreature is Player player)
              player.AddFood(16);
        self.Win(false, false);
      }
    }
#endif

    public void SleepAndDeathScreen_GetDataFromGame(On.Menu.SleepAndDeathScreen.orig_GetDataFromGame orig, SleepAndDeathScreen self, KarmaLadderScreen.SleepDeathScreenDataPackage package)
    {
      orig(self, package);

      if (!self.IsSleepScreen)
        return;

#if _DEBUG
      LoadData();
#endif

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

      List<AbstractCreature> denCreatures = package.mapData.world.abstractRooms
        .FirstOrDefault(v => v.name == package.saveState.denPosition).creatures;
      if (denCreatures.Count < 2)
      {
        Logger.LogWarning("Failed to find friend in room - too little creatures");
        return;
      }
      bool foundFriends = false;

      Dream dream = null;
      switch (preferenceMode)
      {
        case PreferenceMode.Any:
          foreach (AbstractCreature creature in denCreatures)
          {
            if (!creature.state.alive
              || creature.creatureTemplate.type == CreatureTemplate.Type.Slugcat && creature.abstractAI == null
              || !slugcatDreams.dreams.TryGetValue(creature.creatureTemplate.type.value, out dream))
              continue;
            denCreatures = denCreatures.GetCreaturesWithType(creature.creatureTemplate.type);
            foundFriends = true;
            break;
          }
          break;
        case PreferenceMode.Pups:
          foreach (AbstractCreature creature in denCreatures)
          {
            if (!creature.state.alive
              || creature.abstractAI.RealAI is not SlugNPCAI
              || !slugcatDreams.dreams.TryGetValue(creature.creatureTemplate.type.value, out dream))
              continue;
            denCreatures = denCreatures.GetCreaturesWithType(MoreSlugcatsEnums.CreatureTemplateType.SlugNPC);
            denCreatures.RemoveAll(v => v.abstractAI == null);
            foundFriends = true;
            break;
          }
          break;
        case PreferenceMode.Lizards:
          foreach (AbstractCreature creature in denCreatures)
          {
            if (!creature.state.alive
              || creature.abstractAI?.RealAI is not LizardAI
              || !slugcatDreams.dreams.TryGetValue(creature.creatureTemplate.type.value, out dream))
              continue;
            denCreatures = denCreatures.GetCreaturesWithType(creature.creatureTemplate.type);
            foundFriends = true;
          }
          break;
      }

      Dream fallbackDream = null;
      if (!foundFriends || slugcatDreams.fallback != null && !slugcatDreams.dreams.TryGetValue(slugcatDreams.fallback, out fallbackDream))
      {
        Logger.LogWarning("Failed to find friend in room - no friends were found");
        return;
      }
      dream ??= fallbackDream;

      self.scene.depthIllustrations[self.scene.depthIllustrations.Count - 1].pos.y -= 80f;
      List<KeyValuePair<DreamLayer, FSprite>> customSprites = new();
      int creatureAmount = Math.Min(denCreatures.Count, dream.layerSets.Count);
      for (int i = 1; i <= creatureAmount; ++i)
        foreach (KeyValuePair<string, DreamLayer> layerData in dream.layerSets[i.ToString()].layers)
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
          if (layer.order != null)
            customSprites.Add(new(layer, customIllustration.sprite));
        }
      customSprites.Sort((a, b) => (a.Key.order ?? 1).CompareTo(b.Key.order));
      for (int i = 1; i < customSprites.Count; ++i)
        if (customSprites[i - 1].Key.onTop == customSprites[i].Key.onTop)
          customSprites[i].Value.MoveInFrontOfOtherNode(customSprites[i - 1].Value);

      string song = dream.layerSets[creatureAmount.ToString()].song;
      if (song.IsNullOrWhiteSpace())
        song = dream.song;
      if (!song.IsNullOrWhiteSpace())
        self.manager.musicPlayer?.MenuRequestsSong(song, 1f, 10f);
    }

    public class DreamLayer : IUpdatableFromOther
    {
      public string sprite;
      public MenuDepthIllustration.MenuShader shader;
      public int? order;
      public float? slugDepthOffset;
      public bool? onTop;
      public Vector2? pos;

      public DreamLayer(Dictionary<string, object> layer)
      {
        layer.TrySetValueWithType("sprite", ref sprite);
        layer.TryGetValueWithType("order", out Int64 orderValue);
          order = (int)orderValue;
        layer.TrySetValueWithType("onTop", ref onTop);
        layer.TrySetExtEnum("shader", ref shader);
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
        order = layer.order ?? order;
        onTop = layer.onTop ?? onTop;
        shader = layer.shader ?? shader;
        slugDepthOffset = layer.slugDepthOffset ?? slugDepthOffset;
        pos = layer.pos ?? pos;
      }
    }

    public class LayerInfo : IUpdatableFromOther
    {
      public string song;
      public Dictionary<string, DreamLayer> layers = new();

      public LayerInfo(Dictionary<string, object> parsedLayers)
      {
        parsedLayers.TrySetValueWithType("song", ref song);
        if (parsedLayers.TryGetValueWithType("bottom", out Dictionary<string, object> bottom))
          layers["bottom"] = new(bottom);
        if (parsedLayers.TryGetValueWithType("top", out Dictionary<string, object> top))
          layers["top"] = new(top);
      }

      public void Update(IUpdatableFromOther other)
      {
        if (other is not LayerInfo layerInfo)
          return;
        song = layerInfo.song ?? song;
        layers.UpdateFromOther(layerInfo.layers);
      }
    }

    public class Dream : IUpdatableFromOther
    {
      public string song;
      public Dictionary<string, LayerInfo> layerSets = new();

      public Dream(Dictionary<string, object> creatureDream)
      {
        creatureDream.TrySetValueWithType("song", ref song);
        if (!creatureDream.TryGetValueWithType("layers", out Dictionary<string, object> indexedLayers))
          return;
        foreach (KeyValuePair<string, object> indexedLayer in indexedLayers)
          if (indexedLayer.Value is Dictionary<string, object> parsedLayers)
            layerSets[indexedLayer.Key] = new(parsedLayers);
      }

      public void Update(IUpdatableFromOther other)
      {
        if (other is not Dream dream)
          return;
        song = dream.song ?? song;
        layerSets.UpdateFromOther(dream.layerSets);
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
        foreach (string path in AssetManager.ListDirectory("sweetdreams")
          .OrderBy(v => !Path.GetFileName(v).Equals("main.json")))
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
