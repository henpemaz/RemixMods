using Menu.Remix.MixedUI;
using UnityEngine;

namespace LapMod
{
    public class LapModRemix : OptionInterface
    {
        
        public static LapModRemix instance = new LapModRemix();

        public static Configurable<KeyCode> roomPassthroughKey = instance.config.Bind("roomPassthroughKey", KeyCode.U, new ConfigurableInfo(
        "Keybind to toggle room passthrough", null, "", "Keyboard"));
        public static Configurable<KeyCode> resetKey = instance.config.Bind("resetKey", KeyCode.Y, new ConfigurableInfo(
        "Keybind to reset as though you exited the room", null, "", "Keyboard"));

        public override void Initialize()
        {
            base.Initialize();
            Tabs = new OpTab[] { new OpTab(this, "Settings") };

            Tabs[0].AddItems(new UIelement[]
            {
                // First column
                new OpLabel(10f, 570f, "Toggle Room Passthrough")
                    {description = roomPassthroughKey.info.description},
                new OpKeyBinder(roomPassthroughKey, new Vector2(165f, 565f),
                        new Vector2(100f, 25f)) {description = roomPassthroughKey.info.description},
                new OpLabel(10f, 525f, "Reset keybind")
                    {description = resetKey.info.description},
                new OpKeyBinder(resetKey, new Vector2(100f, 520f),
                        new Vector2(100f, 25f)) {description = resetKey.info.description}
            });
        }

    }
}
