using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using RainMeadow;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace TagMod
{
    internal class TagMenu : CustomLobbyMenu
    {
        private Lobby lobby;
        private TagGameMode gamemode;
        private SlugcatCustomization customization;
        private SlugcatCustomizationSelector customizationHolder;
        private SimplerButton playButton;
        private OpComboBox2 shelterSelect;
        private ProperlyAlignedMenuLabel namesLabel;
        private ProperlyAlignedMenuLabel hidingLabel;
        private ProperlyAlignedMenuLabel huntingLabel;
        private OpUpdown setupTime;

        public TagMenu(ProcessManager manager) : base(manager, TagMod.TagMenu)
        {
            // customization
            this.lobby = OnlineManager.lobby;
            this.gamemode = (TagGameMode)lobby.gameMode;
            this.customization = gamemode.avatarSettings;

            this.customizationHolder = new SlugcatCustomizationSelector(this, this.mainPage, new Vector2(540, 460), customization);
            mainPage.subObjects.Add(this.customizationHolder);

            playButton = new SimplerButton(this, mainPage, Translate("PLAY!"), new Vector2(1056f, 50f), new Vector2(110f, 30f));
            playButton.OnClick += Play;
            mainPage.subObjects.Add(playButton);

            if (lobby.isOwner)
            {
                var shelterPattern = new Regex(@"^.._[sS]\d\d");
                System.Collections.Generic.List<Menu.Remix.MixedUI.ListItem> shelters = RainWorld.roomNameToIndex.Keys.Where(k => shelterPattern.IsMatch(k)).Select(e => new Menu.Remix.MixedUI.ListItem(e)).ToList();
                
                mainPage.subObjects.Add(new MenuLabel(this, mainPage, "Starting shelter", new Vector2(550, 400), new Vector2(120, 20), false));
                this.shelterSelect = new OpComboBox2(new Configurable<string>(gamemode.tagData.startingRoom), new Vector2(700, 400), 120, shelters);
                new UIelementWrapper(tabWrapper, this.shelterSelect);
                shelterSelect.OnValueChanged += ShelterSelect_OnValueChanged;

                mainPage.subObjects.Add(new MenuLabel(this, mainPage, "Setup time (seconds)", new Vector2(550, 360), new Vector2(120, 20), false));
                this.setupTime = new OpUpdown(new Configurable<int>(gamemode.tagData.setupTime, accept: new ConfigAcceptableRange<int>(0, 600)), new Vector2(700, 360), 120);
                new UIelementWrapper(tabWrapper, this.setupTime);
                setupTime._lastArrX = setupTime._arrX; // crazy how these ui elements all have a quirk or three
                setupTime.OnValueChanged += SetupTime_OnValueChanged;
            }

            mainPage.subObjects.Add(namesLabel = new RainMeadow.ProperlyAlignedMenuLabel(this, mainPage, "", new Vector2(870, 560), new Vector2(150, 20f), false));
            mainPage.subObjects.Add(hidingLabel = new RainMeadow.ProperlyAlignedMenuLabel(this, mainPage, "", new Vector2(1020, 560), new Vector2(60, 20f), false));
            mainPage.subObjects.Add(huntingLabel = new RainMeadow.ProperlyAlignedMenuLabel(this, mainPage, "", new Vector2(1100, 560), new Vector2(60, 20f), false));
        }

        private void SetupTime_OnValueChanged(UIconfig config, string value, string oldValue)
        {
            gamemode.tagData.setupTime = (ushort)Mathf.Clamp(setupTime.valueInt, 0, 600);
        }

        private void ShelterSelect_OnValueChanged(Menu.Remix.MixedUI.UIconfig config, string value, string oldValue)
        {
            if (string.IsNullOrEmpty(value)) { TagMod.Error("null value"); }
            else
            {
                TagMod.Debug("starting room is " + value);
                gamemode.tagData.startingRoom = value;
            }
        }

        public override void Update()
        {
            base.Update();
            if (!lobby.isOwner) playButton.buttonBehav.greyedOut = !gamemode.isInGame;

            namesLabel.text = string.Join("\n", new string[] { "Players" }.Concat(lobby.clientSettings.Select(c => c.Value.TryGetData<HunterData>(out var data) ?
            c.Key.id.name
            : null).Where(t => t != null)));
            hidingLabel.text = string.Join("\n", new string[] { "Hiding" }.Concat(lobby.clientSettings.Select(c => c.Value.TryGetData<HunterData>(out var data) ?
            HideTimer.FormatTime(data.TotalTimeHiding)
            : null).Where(t => t != null)));
            huntingLabel.text = string.Join("\n", new string[] { "Hunting" }.Concat(lobby.clientSettings.Select(c => c.Value.TryGetData<HunterData>(out var data) ?
            HideTimer.FormatTime(data.TotalTimeHunting)
            : null).Where(t => t != null)));
        }

        private void Play(SimplerButton button)
        {
            TagMod.Debug("my name is " + customization.nickname);
            TagMod.Debug("starting room is " + gamemode.tagData.startingRoom);
            manager.arenaSitting = null;
            manager.rainWorld.progression.ClearOutSaveStateFromMemory();
            manager.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat = SlugcatStats.Name.White;
            manager.menuSetup.startGameCondition = ProcessManager.MenuSetup.StoryGameInitCondition.RegionSelect;
            manager.menuSetup.regionSelectRoom = gamemode.tagData.startingRoom;
            manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Game);
        }
    }
}