using RainMeadow;
using System;
using UnityEngine;

namespace TagMod
{
    public class HunterData : OnlineEntity.EntityData
    {
        public bool hunter;
        public bool lastHunter;

        public HunterData() { }

        public override EntityDataState MakeState(OnlineEntity entity, OnlineResource inResource)
        {
            return new State(this);
        }

        public class State : OnlineEntity.EntityData.EntityDataState
        {
            [OnlineField]
            public bool hunter;

            public State() { }
            public State(HunterData hunterData)
            {
                hunter = hunterData.hunter;
            }

            public override Type GetDataType() => typeof(HunterData);

            public override void ReadTo(OnlineEntity.EntityData data, OnlineEntity onlineEntity)
            {
                var hunterData = (HunterData)data;
                hunterData.hunter = hunter;
                if (UnityEngine.Input.GetKey(KeyCode.L))
                {
                    TagMod.Debug("hunter? " + hunter);
                }
            }
        }
    }
}