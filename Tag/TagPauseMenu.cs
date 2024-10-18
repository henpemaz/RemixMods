using Menu;
using RainMeadow;
using System;
using System.Linq;
using UnityEngine;

namespace TagMod
{
    internal class TagPauseMenu : PauseMenu
    {
        private readonly TagGameMode tgm;
        private Creature avatarCreature;
        private int targetHub;
        private int suco4;
        private SimplerButton teamBtn;
        private SimplerButton resetBtn;
        private SimplerButton startBtn;
        private SimplerButton hunterCount;

        public TagPauseMenu(ProcessManager manager, RainWorldGame game, TagGameMode tgm) : base(manager, game)
        {
            TagMod.DebugMe();
            this.tgm = tgm;
            this.avatarCreature = tgm.avatars[0].realizedCreature;

            this.pauseWarningActive = false;
            game.cameras[0].hud.textPrompt.pausedWarningText = false;
            game.cameras[0].hud.textPrompt.pausedMode = false;

            var world = game.world;
            var room = game.cameras[0].room;

            int buttonCount = 0;
            SimplerButton AddButton(string localizedText, string localizedDescription, Action<SimplerButton> onClick, bool active = true)
            {
                Vector2 pos = new Vector2(
                    this.ContinueAndExitButtonsXPos - 250.2f - this.moveLeft - this.manager.rainWorld.options.SafeScreenOffset.x,
                    Mathf.Max(manager.rainWorld.options.SafeScreenOffset.y, 15f) + 40.2f
                );
                pos.y += (buttonCount) * 40f;
                SimplerButton button = new SimplerButton(this, this.pages[0], localizedText, pos, new Vector2(110f, 30f), localizedDescription);
                button.OnClick += onClick;
                button.nextSelectable[0] = button;
                button.nextSelectable[2] = button;
                button.buttonBehav.greyedOut = !active;
                this.pages[0].subObjects.Add(button);
                buttonCount += 1;
                return button;
            }
            this.teamBtn = AddButton(this.Translate("TEAM"), this.Translate("Become a hunter for this round"), this.ToggleTeam);

            if (OnlineManager.lobby.isOwner)
            {
                this.resetBtn = AddButton(this.Translate("RESET"), this.Translate(""), this.Reset);
                this.startBtn = AddButton(this.Translate("START"), this.Translate(""), this.Start);
                this.hunterCount = AddButton("", this.Translate("And they're hungry"), (_) => { });
            }
        }

        private void Start(SimplerButton button)
        {
            TagMod.DebugMe();
            tgm.tagData.setupStarted = true;
            tgm.lobby.NewVersion();
        }

        private void Reset(SimplerButton button)
        {
            TagMod.DebugMe();
            tgm.tagData.setupStarted = false;
            tgm.tagData.huntStarted = false;
            tgm.tagData.hunters.Clear();
            tgm.lobby.NewVersion();
        }

        private void ToggleTeam(SimplerButton button)
        {
            if (!tgm.hunterData.hunter)
            {
                OnlineManager.lobby.owner.InvokeRPC(TagMod.NowHunter, OnlineManager.mePlayer);
            }
            else
            {
                OnlineManager.lobby.owner.InvokeRPC(TagMod.LeaveHunters);
            }
        }

        public override void Update()
        {
            teamBtn.buttonBehav.greyedOut = tgm.tagData.setupStarted;
            if (startBtn != null) startBtn.buttonBehav.greyedOut = tgm.tagData.setupStarted || tgm.tagData.hunters.Count == 0;
            if (resetBtn != null) resetBtn.buttonBehav.greyedOut = tgm.tagData.huntEnded || tgm.tagData.hunters.Count == 0;
            if (hunterCount != null) hunterCount.menuLabel.text = "Hunters: " + tgm.tagData.hunters.Count;
            base.Update();
        }

        public override string UpdateInfoText()
        {
            if (this.selectedObject is IHaveADescription ihad)
            {
                return ihad.Description;
            }
            return base.UpdateInfoText();
        }
    }
}