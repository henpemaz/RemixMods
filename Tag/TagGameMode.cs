using Menu;
using RainMeadow;
using System.Linq;
using UnityEngine;

namespace TagMod
{
    public class TagGameMode : StoryGameMode
    {
        public TagLobbyData tagData;
        public HunterData hunterData;
        private Color origBodyColor;

        public TagGameMode(Lobby lobby) : base(lobby)
        {
            hunterData = new HunterData();
        }

        public override void ResourceAvailable(OnlineResource onlineResource)
        {
            base.ResourceAvailable(onlineResource);
            if (onlineResource is Lobby lobby)
            {
                this.tagData = lobby.AddData(new TagLobbyData());
            }
        }

        public override void AddClientData()
        {
            base.AddClientData();
            clientSettings.AddData(hunterData);
        }

        public override void ConfigureAvatar(OnlineCreature onlineCreature)
        {
            base.ConfigureAvatar(onlineCreature);
            onlineCreature.AddData(hunterData);
        }

        public override void PlayerLeftLobby(OnlinePlayer player)
        {
            base.PlayerLeftLobby(player);
            tagData.hunters.Remove(player);
        }

        public override void LobbyTick(uint tick)
        {
            base.LobbyTick(tick);
            var game = RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame;
            hunterData.lastHunter = hunterData.hunter;
            hunterData.hunter = (game != null) ? tagData.hunters.Contains(OnlineManager.mePlayer) : false;
            if (hunterData.hunter && !hunterData.lastHunter)
            {
                TagMod.Debug("hunter! applying war paint");
                avatarSettings.bodyColor = Color.Lerp(origBodyColor, new Color(1f, 23f / 51f, 23f / 51f), 0.66f);
            }
            if (!hunterData.hunter && hunterData.lastHunter)
            {
                TagMod.Debug("not hunter! resetting color");
                avatarSettings.bodyColor = origBodyColor;
            }
            if (game != null)
            {
                if (hunterData.hunter != hunterData.lastHunter)
                {
                    if (game.cameras[0].room != null)
                    {
                        game.cameras[0].ApplyPalette(); // recolor self
                    }
                }
            }

            if (lobby.isOwner) // management
            {
                if (game != null)
                {
                    if (tagData.setupStarted && TagMod.hideTimer.SetupTimer <= 0)
                    {
                        tagData.huntStarted = true;
                    }

                    if (tagData.huntStarted)
                    {
                        var hunterCount = tagData.hunters.Count;
                        var deadHunters = lobby.clientSettings.Where(c => tagData.hunters.Contains(c.Key) && c.Value.TryGetData<StoryClientSettingsData>(out var scd) && scd.isDead).Count();
                        var hidersCount = lobby.clientSettings.Where(c => !tagData.hunters.Contains(c.Key) && c.Value.inGame).Count();
                        var deadHiders = lobby.clientSettings.Where(c => !tagData.hunters.Contains(c.Key) && c.Value.TryGetData<StoryClientSettingsData>(out var scd) && scd.isDead).Count();

                        if (hunterCount == deadHunters || hidersCount == deadHiders || hunterCount == 0 || hidersCount == 0)
                        {
                            tagData.huntEnded = true;
                        }

                        if (game.world.rainCycle.deathRainHasHit)
                        {
                            tagData.huntEnded = true;
                        }
                    }
                }
                else
                {
                    tagData.hunters.Clear();
                }
            }

            if (UnityEngine.Input.GetKey(KeyCode.L)) { TagMod.Debug("hunters are " + string.Join(", ", tagData.hunters)); }
        }

        public override void PreGameStart()
        {
            base.PreGameStart();
            origBodyColor = avatarSettings.bodyColor;
        }

        public override void PostGameStart(RainWorldGame self)
        {
            base.PostGameStart(self);
        }

        public override void GameShutDown(RainWorldGame game)
        {
            base.GameShutDown(game);
            avatarSettings.bodyColor = origBodyColor;
            if (lobby.isOwner)
            {
                tagData.hunters.Clear();
                tagData.setupStarted = false;
                tagData.huntStarted = false;
                tagData.huntEnded = false;
            }
        }

        public override ProcessManager.ProcessID MenuProcessId()
        {
            return TagMod.TagMenu;
        }

        public override PauseMenu CustomPauseMenu(ProcessManager manager, RainWorldGame game)
        {
            return new TagPauseMenu(manager, game, this);
        }
    }
}