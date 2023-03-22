using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using Menu.Remix.MixedUI;
using Menu.Remix.MixedUI.ValueTypes;
using UnityEngine;

namespace SplitScreenCoop;

public class SplitScreenCoopOptions : OptionInterface
{
    public readonly ManualLogSource Logger;
    public SplitScreenCoopOptions(SplitScreenCoop modInstance, ManualLogSource loggerSource)
    {
        Logger = loggerSource;
        PreferredSplitMode = this.config.Bind<SplitScreenCoop.SplitMode>("PreferredSplitMode", SplitScreenCoop.SplitMode.SplitVertical);
        AlwaysSplit = this.config.Bind<bool>("AlwaysSplit", false);
    }
    
    public readonly Configurable<SplitScreenCoop.SplitMode> PreferredSplitMode;
    public readonly Configurable<bool> AlwaysSplit;
    private UIelement[] UIArrOptions;

    public override void Initialize()
    {
        var opTab = new OpTab(this, "Options");
        this.Tabs = new[]
        {
            opTab
        };

        UIArrOptions = new UIelement[]
        {
            new OpLabel(10f, 550f, "General", true),
            
            new OpLabel(10f, 520, "Split Mode") { verticalAlignment = OpLabel.LabelVAlignment.Center },
            new OpComboBox(PreferredSplitMode, new Vector2(10f, 490), 200f, OpResourceSelector.GetEnumNames(null, typeof(SplitScreenCoop.SplitMode)).ToList()),

            new OpCheckBox(AlwaysSplit, 10f, 450),
            new OpLabel(40f, 450, "Permanent split mode") { verticalAlignment = OpLabel.LabelVAlignment.Center },
        };
        
        // Permanent split option pokes through the ComboBox and looks ugly, hide it
        ((OpComboBox)UIArrOptions[2]).OnListOpen += delegate(UIfocusable trigger)
        {
            UIArrOptions[3].Hide();
            UIArrOptions[4].Hide();
        };
        ((OpComboBox)UIArrOptions[2]).OnListClose += delegate(UIfocusable trigger)
        {
            UIArrOptions[3].Show();
            UIArrOptions[4].Show();
        };
        
        // Add items to the tab
        opTab.AddItems(UIArrOptions);
    }

    public override void Update()
    {
    }
}