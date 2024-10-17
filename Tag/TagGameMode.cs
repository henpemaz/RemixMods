using Menu;
using RainMeadow;
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
            hunterData.lastHunter = hunterData.hunter;
            hunterData.hunter = tagData.hunters.Contains(OnlineManager.mePlayer);
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
            tagData.hunters.Clear();
            avatarSettings.bodyColor = origBodyColor;
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