using Menu;
using RainMeadow;
using System;
using UnityEngine;

namespace TagMod
{
    internal class TagPauseMenu : PauseMenu
    {
        private readonly TagGameMode tgm;
        private Creature avatarCreature;
        private int targetHub;
        private int suco4;
        private SimplerButton joinHuntersBtn;
        private SimplerButton resetHuntersBtn;
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
            this.joinHuntersBtn = AddButton(this.Translate("BE HUNTER"), this.Translate("Become the hunter for this round"), this.JoinHunters);

            if (OnlineManager.lobby.isOwner)
            {
                this.resetHuntersBtn = AddButton(this.Translate("RESET HUNTERS"), this.Translate("Reset hunters"), this.ResetHunters);
                this.hunterCount = AddButton("", this.Translate("And they're hungry"), (_) => { });
            }
        }

        private void ResetHunters(SimplerButton button)
        {
            tgm.tagData.hunters.Clear();
        }

        private void JoinHunters(SimplerButton button)
        {
            OnlineManager.lobby.owner.InvokeRPC(TagMod.NowHunter, OnlineManager.mePlayer);
        }

        public override void Update()
        {
            joinHuntersBtn.buttonBehav.greyedOut = tgm.tagData.hunters.Count > 0;
            if(hunterCount != null)
            {
                hunterCount.menuLabel.text = "Hunters: " + tgm.tagData.hunters.Count;
            }
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