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
    class BetterComboBox : OpComboBox
    {
        public BetterComboBox(ConfigurableBase configBase, Vector2 pos, float width, List<ListItem> list) : base(configBase, pos, width, list) { }
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            if(this._rectList != null && !_rectList.isHidden)
            {
                for (int j = 0; j < 9; j++)
                {
                    this._rectList.sprites[j].alpha = 1;
                }
            }
        }
    }
    public SplitScreenCoopOptions()
    {
        PreferredSplitMode = this.config.Bind("PreferredSplitMode", SplitScreenCoop.SplitMode.SplitVertical);
        AlwaysSplit = this.config.Bind("AlwaysSplit", false);
        DualDisplays = this.config.Bind("DualDisplays", false);
    }

    public readonly Configurable<SplitScreenCoop.SplitMode> PreferredSplitMode;
    public readonly Configurable<bool> AlwaysSplit;
    public readonly Configurable<bool> DualDisplays;
    private UIelement[] UIArrOptions;

    public override void Initialize()
    {
        var opTab = new OpTab(this, "Options");
        this.Tabs = new[] { opTab };
        OpCheckBox e;
        UIArrOptions = new UIelement[]
        {
            new OpLabel(10f, 550f, "General", true),

            new OpCheckBox(AlwaysSplit, 10f, 450),
            new OpLabel(40f, 450, "Permanent split mode") { verticalAlignment = OpLabel.LabelVAlignment.Center },

            e = new OpCheckBox(DualDisplays, 10f, 380) { description = "Requires two physical displays" },
            new OpLabel(40f, 380, "Dual Display (experimental)") { verticalAlignment = OpLabel.LabelVAlignment.Center },
            
            // added last due to overlap
            new OpLabel(10f, 520, "Split Mode") { verticalAlignment = OpLabel.LabelVAlignment.Center },
            new BetterComboBox(PreferredSplitMode, new Vector2(10f, 490), 200f, OpResourceSelector.GetEnumNames(null, typeof(SplitScreenCoop.SplitMode)).ToList()),
        };

        e.greyedOut = !SplitScreenCoop.DualDisplaySupported();
        
        // Add items to the tab
        opTab.AddItems(UIArrOptions);
    }
}