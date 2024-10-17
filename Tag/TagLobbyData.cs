using RainMeadow;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TagMod
{
    public class TagLobbyData : OnlineResource.ResourceData
    {
        public List<OnlinePlayer> hunters = new();
        internal string startingRoom = "";

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
            public TagState() { }
            public TagState(TagLobbyData tagLobbyData)
            {
                hunters = new(tagLobbyData.hunters.Select(p => p.inLobbyId).ToList());
                startingRoom = tagLobbyData.startingRoom;
            }

            public override Type GetDataType() => typeof(TagLobbyData);

            public override void ReadTo(OnlineResource.ResourceData data, OnlineResource resource)
            {
                TagLobbyData tagLobbyData = (TagLobbyData)data;
                tagLobbyData.hunters = hunters.list.Select(i => OnlineManager.lobby.PlayerFromId(i)).ToList();
                tagLobbyData.startingRoom = startingRoom;
            }
        }
    }
}