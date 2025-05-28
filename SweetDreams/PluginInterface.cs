using Menu.Remix.MixedUI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static SweetDreams.PluginInterface;

namespace SweetDreams
{
  public class PluginInterface : OptionInterface
  {
    // To specify preference of creature in dream selection
    public enum PreferenceMode
    {
      Any,
      Pups,
      Lizards,
    }

    public readonly Configurable<PreferenceMode> preferenceMode;

    private UIelement[] options;

    public PluginInterface()
    {
      preferenceMode = config.Bind("preferenceMode", PreferenceMode.Any);
    }

    public override void Initialize()
    {
      OpTab optionsTab = new OpTab(this, "Options");
      Tabs = new OpTab[] { optionsTab };

      options = new UIelement[] {
        new OpLabel(10f, 550f, "Options", bigText: true),

        new OpLabel(10f, 510f, "Creature preference in dreams", bigText: false),
        new OpComboBox(preferenceMode, new Vector2(10f, 480f), 130f, OpResourceSelector.GetEnumNames(null, typeof(PreferenceMode)).ToList()),
      };

      optionsTab.AddItems(options);
    }
  }
}
