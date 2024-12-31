using UnityEngine;

namespace LapMod
{
    internal abstract class Panel
    {
        private static readonly Color textColor = Color.white;
        private const float baseTextAlpha = 0.3f;
        private const float highlightTextAlpha = 0.9f;
        private const float alphaInc = 0.0075f;

        private static FLabel passthroughBool;
        private static FLabel roomTime;
        private static FContainer container;
        private static string timeDiffString;

        private static Vector2 panelAnchor = new Vector2(50f + 0.01f, 690f + 0.01f); // Characters are cut off on a 1440p monitor for some reason, unless I add +0.01f :(
        private static Vector2 roomTimeOffset = new Vector2(0f, 20f);

        private static int highAlphaCounter = 0;
        private static int highAlphaCounterMax = 80; // 2 seconds

        public static void Initialize()
        {
            container = new FContainer();
            Futile.stage.AddChild(container);
            container.SetPosition(Vector2.zero);

            CheckTimeDiff();

            roomTime = new FLabel(RWCustom.Custom.GetDisplayFont(), timeDiffString)
            {
                isVisible = true,
                alpha = baseTextAlpha,
                color = textColor,
                alignment = FLabelAlignment.Left,
            };
            container.AddChild(roomTime);

            passthroughBool = new FLabel(RWCustom.Custom.GetFont(), $"Looping: {(!LapMod.wantsNextRoom).ToString()}")
            {
                isVisible = true,
                alpha = baseTextAlpha,
                color = textColor,
                alignment = FLabelAlignment.Left,
            };
            container.AddChild(passthroughBool);

            Update();

        }

        public static void Update()
        {
            passthroughBool.text = $"Looping: {(!LapMod.wantsNextRoom).ToString()}";
            passthroughBool.SetPosition(panelAnchor);
            CheckTimeDiff();
            if (highAlphaCounter > highAlphaCounterMax/2) 
            {
                roomTime.alpha = highlightTextAlpha;
                highAlphaCounter--;
            }
            else if (highAlphaCounter > 0 && highAlphaCounter <= highAlphaCounterMax/2)
            {
                roomTime.alpha = (highlightTextAlpha - alphaInc * (highAlphaCounterMax/2 - highAlphaCounter));
                highAlphaCounter--;
            }
            else 
            {
                roomTime.alpha = baseTextAlpha;
            }
            roomTime.text = timeDiffString;
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
            string newTime = LapMod.timeDiff.ToString("mm'm:'ss's:'fff'ms'");
            if (LapMod.timeDiff != null)
            {
                if (timeDiffString != newTime)
                {
                    highAlphaCounter = highAlphaCounterMax;
                }
                timeDiffString = newTime;
            }
            else
            {
                timeDiffString = "";
            }
        }

    }
}
