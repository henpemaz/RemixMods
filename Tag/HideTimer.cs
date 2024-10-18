using UnityEngine;

namespace TagMod
{
    public class HideTimer : HUD.HudPart
    {
        public enum TimerMode
        {
            Setup,
            Hiding,
            Hunter,
            Waiting
        }

        private float Readtimer;
        private bool isRunning;
        private TimerMode currentMode = TimerMode.Waiting;  // Track which timer is active
        private FLabel timerLabel;
        private FLabel modeLabel;
        private Vector2 pos, lastPos;
        private float fade, lastFade;
        public float HunterTimer, HiderTimer, SetupTimer;
        public TagGameMode tgm;
        private Player? player;

        public HideTimer(HUD.HUD hud, FContainer fContainer, TagGameMode tgm) : base(hud)
        {
            SetupTimer = tgm.tagData.setupTime;
            HiderTimer = 0f;
            HunterTimer = 0f;
            isRunning = false;

            timerLabel = new FLabel("font", FormatTime(0))
            {
                scale = 2.4f,
                alignment = FLabelAlignment.Left
            };

            modeLabel = new FLabel("font", currentMode.ToString())
            {
                scale = 1.6f,
                alignment = FLabelAlignment.Left
            };

            pos = new Vector2(80f, hud.rainWorld.options.ScreenSize.y - 60f);
            lastPos = pos;
            timerLabel.SetPosition(DrawPos(1f));
            modeLabel.SetPosition(DrawPos(1f) + new Vector2(120f, 0f));

            fContainer.AddChild(timerLabel);
            fContainer.AddChild(modeLabel);
            this.tgm = tgm;
        }

        public Vector2 DrawPos(float timeStacker)
        {
            return Vector2.Lerp(lastPos, pos, timeStacker);
        }

        public override void Update()
        {
            base.Update();
            player = hud.owner as Player;
            if (player == null) return;

            switch (tgm.tagData)
            {
                case { setupStarted: false }:
                    currentMode = TimerMode.Setup;
                    SetupTimer = tgm.tagData.setupTime;
                    isRunning = false;
                    break;
                case { setupStarted: true, huntStarted: false }:
                    currentMode = TimerMode.Setup;
                    isRunning = true;
                    break;
                case { huntStarted: true, huntEnded: false }:
                    var nextMode = tgm.hunterData.hunter ? TimerMode.Hunter : TimerMode.Hiding;
                    if (nextMode != currentMode && currentMode != TimerMode.Waiting)
                    {
                        switch (currentMode)
                        {
                            case TimerMode.Setup:
                                SetupTimer = 0f;
                                hud.PlaySound(SoundID.SL_AI_Protest_3);
                                break;
                            case TimerMode.Hiding:
                                hud.PlaySound(SoundID.SL_AI_Pain_1);
                                break;
                            case TimerMode.Hunter:
                                break;
                        }
                    }
                    currentMode = nextMode;
                    isRunning = true;
                    if (player.dead || player.playerState.permaDead)
                    {
                        currentMode = TimerMode.Waiting;
                        isRunning = false;
                    }
                    break;
                case { huntEnded: true }:
                    //currentMode = tgm.hunterData.hunter ? TimerMode.Hunter : TimerMode.Hiding;
                    if (isRunning)
                    {
                        tgm.hunterData.TotalTimeHiding += HiderTimer;
                        tgm.hunterData.TotalTimeHunting += HunterTimer;
                    }
                    isRunning = false;
                    break;
            };
        }

        public override void Draw(float timeStacker)
        {
            base.Draw(timeStacker);

            // Increment and update the timer based on the current mode
            switch (currentMode)
            {
                case TimerMode.Setup:
                    if (isRunning) SetupTimer -= Time.deltaTime;
                    SetupTimer = Mathf.Max(SetupTimer, 0f);
                    //if (isRunning) TagMod.Stacktrace();
                    Readtimer = SetupTimer;
                    break;
                case TimerMode.Hiding:
                    if (isRunning) HiderTimer += Time.deltaTime;
                    Readtimer = HiderTimer;
                    break;
                case TimerMode.Hunter:
                    if (isRunning) HunterTimer += Time.deltaTime;
                    Readtimer = HunterTimer;
                    break;
            }

            timerLabel.text = FormatTime(Readtimer);
            modeLabel.text = currentMode.ToString();
        }

        // Format time to MM:SS
        public static string FormatTime(float time)
        {
            int minutes = Mathf.FloorToInt(time / 60);
            int seconds = Mathf.FloorToInt(time % 60);
            int milliseconds = Mathf.FloorToInt((time % 1) * 100);

            return $"{minutes:D2}:{seconds:D2}:{milliseconds:D2}";
        }

        public override void ClearSprites()
        {
            base.ClearSprites();
            timerLabel.RemoveFromContainer();

            TagMod.hideTimer = null;
        }
    }
}
