using UnityEngine;

namespace LapMod
{
    internal abstract class Panel
    {
        private static readonly Color textColor = Color.white;
        private const float textAlpha = 0.5f;

        private static FLabel passthroughBool;
        private static FLabel roomTime;
        private static FContainer container;
        private static string timeDiffString;

        private static Vector2 panelAnchor = new Vector2(70f, 700f);
        private static Vector2 roomTimeOffset = new Vector2(0f, 15f);

        public static void Initialize()
        {
            container = new FContainer();
            Futile.stage.AddChild(container);
            container.SetPosition(Vector2.zero);

            passthroughBool = new FLabel(RWCustom.Custom.GetFont(), $"Passthrough: {LapMod.wantsNextRoom.ToString()}")
            {
                isVisible = true,
                alpha = textAlpha,
                color = textColor,
                alignment = FLabelAlignment.Left
            };
            container.AddChild(passthroughBool);

            CheckTimeDiff();

            roomTime = new FLabel(RWCustom.Custom.GetFont(), $"Room split time: {timeDiffString}")
            {
                isVisible = true,
                alpha = textAlpha,
                color = textColor,
                alignment = FLabelAlignment.Left
            };
            container.AddChild(roomTime);

            Update();

        }

        public static void Update()
        {
            passthroughBool.text = $"Passthrough: {LapMod.wantsNextRoom.ToString()}";
            passthroughBool.SetPosition(panelAnchor);
            CheckTimeDiff();
            roomTime.text = $"Room split time: {timeDiffString}";
            roomTime.SetPosition(panelAnchor - roomTimeOffset);

        }

        public static void Remove()
        {
            container.RemoveFromContainer();
            container.RemoveAllChildren();
            container = null;
        }

        public static void CheckTimeDiff()
        {
            if (LapMod.timeDiff != null)
            {
                timeDiffString = LapMod.timeDiff.ToString("mm':'ss':'fff");
            }
            else
            {
                timeDiffString = "";
            }
        }

    }
}
