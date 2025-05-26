// #define _DEBUG

using BepInEx;
using Menu;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static SweetDreams.Plugin;
using Debug = UnityEngine.Debug;

namespace SweetDreams
{
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
      orig(self);

      On.Menu.SleepAndDeathScreen.GetDataFromGame += SleepAndDeathScreen_GetDataFromGame;

      LoadData(self);

#if _DEBUG
      On.RainWorldGame.Update += RainWorldGame_Update;
#endif
    }

#if _DEBUG
    public void RainWorldGame_Update(On.RainWorldGame.orig_Update orig, RainWorldGame self)
    {
      orig(self);
      if (Input.GetKey("b"))
        self.Win(false, false);
    }
#endif

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

      if (!dreams.TryGetValue(slugcatName, out SlugcatDreams slugcatDreams))
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

      if (!dreams[slugcatName].dreams.TryGetValue(friend.creatureTemplate.type.value, out Dream dream))
      {
        Logger.LogError($"No dream was found for {slugcatName} and {friend.creatureTemplate.type.value}");
        return;
      }

      if (!dream.song.IsNullOrWhiteSpace())
      {
        self.manager.musicPlayer?.MenuRequestsSong(dream.song, 0.5f, 10f);
      }

      foreach (DreamLayer layer in dream.layers)
      {
        MenuDepthIllustration customIllustration =
          new(self, self.scene, "illustrations\\sweetdreams", layer.sprite, layer.pos,
          menuDepthIllustration.depth + layer.slugDepthOffset, layer.shader);
        self.scene.AddIllustration(customIllustration);
        if (layer.onTop)
          customIllustration.sprite.MoveInFrontOfOtherNode(menuDepthIllustration.sprite);
        else
          customIllustration.sprite.MoveBehindOtherNode(menuDepthIllustration.sprite);
      }
    }

    public class DreamLayer
    {
      public string sprite;
      public float slugDepthOffset;
      public bool onTop = false;
      public Vector2 pos = Vector2.zero;
      public MenuDepthIllustration.MenuShader shader = MenuDepthIllustration.MenuShader.Basic;

      public DreamLayer(Dictionary<string, object> layer)
      {
        sprite = (string)layer["sprite"];
        shader ??= typeof(MenuDepthIllustration.MenuShader)
          .GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
          .FirstOrDefault(v => v.Name.ToLowerInvariant() == (layer["shader"] as string).ToLowerInvariant())
          .GetValue(null) as MenuDepthIllustration.MenuShader;
        slugDepthOffset = (float)(double)layer["slugDepthOffset"];
        onTop = (bool)layer["onTop"];
        if (layer.TryGetValue("pos", out object obj) && obj is List<object> coordinates)
          pos = new(Convert.ToInt32(coordinates[0]), Convert.ToInt32(coordinates[1]));
      }
    }

    public class Dream
    {
      public string song;
      public List<DreamLayer> layers = new();

      public Dream(KeyValuePair<string, object> creatureDream)
      {
        song = (string)(creatureDream.Value as Dictionary<string, object>)["song"];
        Dictionary<string, object> parsedLayers = (creatureDream.Value as Dictionary<string, object>)["layers"] as Dictionary<string, object>;
        layers.Add(new DreamLayer(parsedLayers["bottom"] as Dictionary<string, object>));
        if (parsedLayers.TryGetValue("top", out object layer))
          layers.Add(new DreamLayer(layer as Dictionary<string, object>));
      }
    }

    public class SlugcatDreams
    {
      public string sleepingSprite;
      public Dictionary<string, Dream> dreams = new();

      public SlugcatDreams(KeyValuePair<string, object> dream)
      {
        sleepingSprite = (string)(dream.Value as Dictionary<string, object>)["sleepingSprite"];
        foreach (KeyValuePair<string, object> creatureDream in (dream.Value as Dictionary<string, object>)["dreams"] as Dictionary<string, object>)
          dreams[creatureDream.Key] = new(creatureDream);
      }
    }

    public static Dictionary<string, SlugcatDreams> dreams = new();

    public void LoadData(RainWorld self)
    {
      foreach (string path in AssetManager.ListDirectory("sweetdreams"))
      {
        try
        {
          Logger.LogInfo($"Reading at: {path}");
          foreach (KeyValuePair<string, object> slugcatDream in Json.Parser.Parse(File.ReadAllText(path)) as Dictionary<string, object>)
            dreams[slugcatDream.Key] = new(slugcatDream);
        }
        catch (Exception e)
        {
          Logger.LogError(e);
        }
      }
      Logger.LogInfo($"Finished reading");
    }
  }
}
