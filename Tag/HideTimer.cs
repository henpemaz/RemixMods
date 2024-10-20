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
        private TimerMode showMode = TimerMode.Waiting;  // Track which timer is being displayed
        private TimerMode matchMode = TimerMode.Waiting; // mode at start of match
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
            matchMode = tgm.hunterData.hunter ? TimerMode.Hunter : TimerMode.Hiding;

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
            modeLabel.SetPosition(DrawPos(1f) + new Vector2(135f, 0f));

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
                    showMode = TimerMode.Setup;
                    SetupTimer = tgm.tagData.setupTime;
                    matchMode = tgm.hunterData.hunter ? TimerMode.Hunter : TimerMode.Hiding;
                    isRunning = false;
                    break;
                case { setupStarted: true, huntStarted: false }:
                    currentMode = TimerMode.Setup;
                    showMode = TimerMode.Setup;
                    matchMode = tgm.hunterData.hunter ? TimerMode.Hunter : TimerMode.Hiding;
                    isRunning = true;
                    break;
                case { huntStarted: true, huntEnded: false }:
                    var nextMode = tgm.hunterData.hunter ? TimerMode.Hunter : TimerMode.Hiding;
                    if(currentMode == TimerMode.Waiting || currentMode == TimerMode.Setup)
                    {
                        showMode = matchMode; // sticks to start-of-match mode
                    }

                    if (player.dead || player.playerState.permaDead)
                    {
                        if (isRunning) // cash in on death
                        {
                            tgm.hunterData.TotalTimeHiding += HiderTimer;
                            tgm.hunterData.TotalTimeHunting += HunterTimer;
                        }
                        currentMode = TimerMode.Waiting;
                        isRunning = false; // oops this makes so we don't cash in, fix me
                    }
                    else
                    {
                        currentMode = nextMode;
                        isRunning = true;
                    }
                    break;
                case { huntEnded: true }:
                    if (isRunning) // cash in on end
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
                    break;
                case TimerMode.Hiding:
                    if (isRunning) HiderTimer += Time.deltaTime;
                    break;
                case TimerMode.Hunter:
                    if (isRunning) HunterTimer += Time.deltaTime;
                    break;
            }

            switch (showMode)
            {
                case TimerMode.Setup:
                    Readtimer = SetupTimer;
                    break;
                case TimerMode.Hiding:
                    Readtimer = HiderTimer;
                    break;
                case TimerMode.Hunter:
                    Readtimer = HunterTimer;
                    break;
            }

            timerLabel.text = FormatTime(Readtimer);
            modeLabel.text = showMode.ToString();
        }

        // Format time to MM:SS:MMM
        public static string FormatTime(float time)
        {
            int minutes = Mathf.FloorToInt(time / 60);
            int seconds = Mathf.FloorToInt(time % 60);
            int milliseconds = Mathf.FloorToInt((time % 1) * 1000);

            return $"{minutes:D2}:{seconds:D2}:{milliseconds:D3}";
        }

        public override void ClearSprites()
        {
            base.ClearSprites();
            timerLabel.RemoveFromContainer();

            TagMod.hideTimer = null;
        }
    }
}
