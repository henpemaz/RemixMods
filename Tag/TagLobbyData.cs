using RainMeadow;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TagMod
{
    public class TagLobbyData : OnlineResource.ResourceData
    {
        public List<OnlinePlayer> hunters = new();
        public string startingRoom = "";
        public bool setupStarted;
        public bool huntStarted;
        public bool huntEnded;
        public ushort setupTime = 20;

        public TagLobbyData() { }

        public override ResourceDataState MakeState(OnlineResource resource)
        {
            return new TagState(this);
        }

        private class TagState : ResourceDataState
        {
            [OnlineField(nullable = true)]
            RainMeadow.Generics.DynamicUnorderedUshorts hunters;
            [OnlineField]
            string startingRoom;
            [OnlineField]
            public bool setupStarted;
            [OnlineField]
            public bool huntStarted;
            [OnlineField]
            public bool huntEnded;
            [OnlineField]
            public ushort setupTime;

            public TagState() { }
            public TagState(TagLobbyData tagLobbyData)
            {
                hunters = new(tagLobbyData.hunters.Select(p => p.inLobbyId).ToList());
                startingRoom = tagLobbyData.startingRoom;
                setupStarted = tagLobbyData.setupStarted;
                huntStarted = tagLobbyData.huntStarted;
                huntEnded = tagLobbyData.huntEnded;
                setupTime = tagLobbyData.setupTime;
            }

            public override Type GetDataType() => typeof(TagLobbyData);

            public override void ReadTo(OnlineResource.ResourceData data, OnlineResource resource)
            {
                TagLobbyData tagLobbyData = (TagLobbyData)data;
                tagLobbyData.hunters = hunters.list.Select(i => OnlineManager.lobby.PlayerFromId(i)).Where(p => p != null).ToList();
                tagLobbyData.startingRoom = startingRoom;
                tagLobbyData.setupStarted = setupStarted;
                tagLobbyData.huntStarted = huntStarted;
                tagLobbyData.huntEnded = huntEnded;
                tagLobbyData.setupTime = setupTime;
            }
        }
    }
}