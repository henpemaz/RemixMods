using RainMeadow;
using System;
using UnityEngine;

namespace TagMod
{
    public class HunterData : OnlineEntity.EntityData
    {
        public bool lastHunter; // local
        public bool hunter;
        public float TotalTimeHiding;
        public float TotalTimeHunting;

        public HunterData() { }

        public override EntityDataState MakeState(OnlineEntity entity, OnlineResource inResource)
        {
            return new State(this);
        }

        public class State : OnlineEntity.EntityData.EntityDataState
        {
            [OnlineField]
            public bool hunter;
            [OnlineField]
            public float totalTimeHiding;
            [OnlineField]
            public float totalTimeHunting;

            public State() { }
            public State(HunterData hunterData)
            {
                hunter = hunterData.hunter;
                totalTimeHiding = hunterData.TotalTimeHiding;
                totalTimeHunting = hunterData.TotalTimeHunting;
            }

            public override Type GetDataType() => typeof(HunterData);

            public override void ReadTo(OnlineEntity.EntityData data, OnlineEntity onlineEntity)
            {
                var hunterData = (HunterData)data;
                hunterData.hunter = hunter;
                hunterData.TotalTimeHiding = totalTimeHiding;
                hunterData.TotalTimeHunting = totalTimeHunting;

                if (UnityEngine.Input.GetKey(KeyCode.L))
                {
                    TagMod.Debug("hunter? " + hunter);
                }
            }
        }
    }
}