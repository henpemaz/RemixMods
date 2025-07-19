using HarmonyLib;
using Menu;
using Menu.Remix.MixedUI;
using Menu.Remix.MixedUI.ValueTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;

namespace LizardSkin
{
    public class LizKinConfiguration
    {
        private const int version = 1;
        private const int maxProfileCount = 10;
        public List<LizKinProfileData> profiles;

        public LizKinConfiguration()
        {
            profiles = new List<LizKinProfileData>();
        }

        public bool AddDefaultProfile()
        {
            if (profiles.Count >= maxProfileCount) return false;
            profiles.Add(GetDefaultProfile());
            return true;
        }

        private LizKinProfileData GetDefaultProfile()
        {
            LizKinProfileData myProfile = new LizKinProfileData();
            myProfile.profileName = "New Profile";
            return myProfile;
        }

        internal bool MoveProfileUp(LizKinProfileData profileData)
        {
            int index = profiles.IndexOf(profileData);
            if (index > 0)
            {
                profiles.RemoveAt(index);
                profiles.Insert(index - 1, profileData);
                return true;
            }
            return false;
        }

        internal bool MoveProfileDown(LizKinProfileData profileData)
        {
            int index = profiles.IndexOf(profileData);
            if (index < profiles.Count - 1)
            {
                profiles.RemoveAt(index);
                profiles.Insert(index + 1, profileData);
                return true;
            }
            return false;
        }

        internal bool DuplicateProfile(LizKinProfileData profileData)
        {
            if (profiles.Count >= maxProfileCount) return false;
            profiles.Add(LizKinProfileData.Clone(profileData));
            return true;
        }

        internal bool DeleteProfile(LizKinProfileData profileData)
        {
            if (profiles.Count == 1) return false;
            profiles.Remove(profileData);
            return true;
        }

        internal List<LizKinCosmeticData> GetCosmeticsForSlugcat(int difficulty, int character, int player)
        {
            List<LizKinCosmeticData> cosmetics = new List<LizKinCosmeticData>();
            foreach (LizKinProfileData profile in profiles)
            {
                if (profile.MatchesSlugcat(difficulty, character, player))
                {
                    cosmetics.AddRange(profile.cosmetics);
                }
            }
            return cosmetics;
        }

        internal static LizKinConfiguration Clone(LizKinConfiguration instance)
        {
            return JsonConvert.DeserializeObject<LizKinConfiguration>(JsonConvert.SerializeObject(instance, LizardSkin.jsonSerializerSettings), LizardSkin.jsonSerializerSettings);
        }
    }

    public static class Exts
    {
        public static void SetRange(this OpUpdown instance, float min, float max)
        {
            instance._fMin = min; instance._fMax = max;
            if (instance._fMin < 0f)
            {
                instance.allowSpace = true;
            }
            instance._bumpDeciMax = Mathf.FloorToInt(Mathf.Log10(instance.IsInt ? Mathf.Max(Mathf.Abs(instance._iMax), Mathf.Abs(instance._iMin)) : Mathf.Max(Mathf.Abs(instance._fMax), Mathf.Abs(instance._fMin))));
        }

        public static void SetRange(this OpUpdown instance, int min, int max)
        {
            instance._iMin = min; instance._iMax = max;
            if (instance._iMin < 0)
            {
                instance.allowSpace = true;
            }
            instance._bumpDeciMax = Mathf.FloorToInt(Mathf.Log10(instance.IsInt ? Mathf.Max(Mathf.Abs(instance._iMax), Mathf.Abs(instance._iMin)) : Mathf.Max(Mathf.Abs(instance._fMax), Mathf.Abs(instance._fMin))));
        }
    }

    public class LizKinProfileData
    {
        private const int version = 1;

        public string profileName;

        public enum ProfileAppliesToMode
        {
            Basic = 1,
            Advanced,
            VeryAdvanced
        }

        public ProfileAppliesToMode appliesToMode;

        public enum ProfileAppliesToSelector
        {
            Character = 1,
            Difficulty,
            Player
        }

        public ProfileAppliesToSelector appliesToSelector;

        public List<int> appliesToList;

        public Color effectColor;

        public bool overrideBaseColor;

        public Color baseColorOverride;

        public List<LizKinCosmeticData> cosmetics;

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            cosmetics.Do(c => c.profile = this);
        }

        internal LizKinProfileData()
        {
            profileName = "New Profile";
            appliesToMode = ProfileAppliesToMode.Basic;
            appliesToSelector = ProfileAppliesToSelector.Character;
            appliesToList = new List<int>() { -1 };
            effectColor = Color.magenta;
            overrideBaseColor = false;
            baseColorOverride = Color.white;
            cosmetics = new List<LizKinCosmeticData>();
        }

        internal static LizKinProfileData Clone(LizKinProfileData instance)
        {
            return JsonConvert.DeserializeObject<LizKinProfileData>(JsonConvert.SerializeObject(instance, LizardSkin.jsonSerializerSettings), LizardSkin.jsonSerializerSettings);
        }

        public Color GetBaseColor(ICosmeticsAdaptor iGraphics, float y) => overrideBaseColor ? baseColorOverride : iGraphics.BodyColorFallback(y);

        internal bool MatchesSlugcat(int difficulty, int character, int player)
        {
            if (appliesToList.Contains(-1)) return true;

            switch (appliesToSelector)
            {
                case ProfileAppliesToSelector.Character:
                    return appliesToList.Contains(character);
                case ProfileAppliesToSelector.Difficulty:
                    return appliesToList.Contains(difficulty);
                case ProfileAppliesToSelector.Player:
                    return appliesToList.Contains(player);
            }
            return false;
        }

        internal void AddEmptyCosmetic()
        {
            this.cosmetics.Add(new CosmeticTailTuftData() { profile = this });
        }
    }

    public abstract class LizKinCosmeticData
    {
        private const int version = 1;

        [Newtonsoft.Json.JsonIgnore]
        public LizKinProfileData profile;

        public enum CosmeticInstanceType
        {
            Antennae = 1,
            AxolotlGills,
            BumpHawk,
            JumpRings,
            LongHeadScales,
            LongShoulderScales,
            ShortBodyScales,
            SpineSpikes,
            TailFin,
            TailGeckoScales,
            TailTuft,
            Whiskers,
            WingScales,
        }

        public abstract CosmeticInstanceType instanceType { get; }
        [Newtonsoft.Json.JsonIgnore]
        public Color effectColor => overrideEffectColor ? effectColorOverride : profile.effectColor;
        public Color GetBaseColor(ICosmeticsAdaptor iGraphics, float y) => overrideBaseColor ? baseColorOverride : profile.GetBaseColor(iGraphics, y);

        public int seed;

        public bool overrideEffectColor;

        public Color effectColorOverride;

        public bool overrideBaseColor;

        public Color baseColorOverride;

        // new in v0.7
        // object version 2
        public enum SpritesOverlapConfig
        {
            Default = -1,
            Behind,
            BehindHead,
            InFront
        }
        public SpritesOverlapConfig spritesOverlap;

        public LizKinCosmeticData()
        {
            seed = (int)(10000 * UnityEngine.Random.value);
            effectColorOverride = Color.red;
            baseColorOverride = Color.red;
            spritesOverlap = SpritesOverlapConfig.Default;
        }

        internal class LizKinCosmeticDataConverter : AbstractJsonConverter<LizKinCosmeticData>
        {
            // todo this is somehow saving enums as ints not strings
            protected override LizKinCosmeticData Create(Type objectType, JObject jObject)
            {
                return MakeCosmeticOfType((CosmeticInstanceType)Enum.Parse(typeof(CosmeticInstanceType), jObject.GetValue("instanceType").Value<string>()));
            }
        }

        internal static LizKinCosmeticData MakeCosmeticOfType(CosmeticInstanceType newType)
        {
            switch (newType)
            {
                case CosmeticInstanceType.Antennae:
                    return new CosmeticAntennaeData();
                case CosmeticInstanceType.AxolotlGills:
                    return new CosmeticAxolotlGillsData();
                case CosmeticInstanceType.BumpHawk:
                    return new CosmeticBumpHawkData();
                case CosmeticInstanceType.JumpRings:
                    return new CosmeticJumpRingsData();
                case CosmeticInstanceType.LongHeadScales:
                    return new CosmeticLongHeadScalesData();
                case CosmeticInstanceType.LongShoulderScales:
                    return new CosmeticLongShoulderScalesData();
                case CosmeticInstanceType.ShortBodyScales:
                    return new CosmeticShortBodyScalesData();
                case CosmeticInstanceType.SpineSpikes:
                    return new CosmeticSpineSpikesData();
                case CosmeticInstanceType.TailFin:
                    return new CosmeticTailFinData();
                case CosmeticInstanceType.TailGeckoScales:
                    return new CosmeticTailGeckoScalesData();
                case CosmeticInstanceType.TailTuft:
                    return new CosmeticTailTuftData();
                case CosmeticInstanceType.Whiskers:
                    return new CosmeticWhiskersData();
                case CosmeticInstanceType.WingScales:
                    return new CosmeticWingScalesData();
                default:
                    throw new ArgumentException("Unsupported instance type");
            }
        }

        internal virtual void ReadFromOther(LizKinCosmeticData other)
        {
            JsonConvert.PopulateObject(JsonConvert.SerializeObject(other, LizardSkin.jsonSerializerSettings), this, LizardSkin.jsonSerializerSettings);
        }

        internal static LizKinCosmeticData Clone(LizKinCosmeticData instance)
        {
            return JsonConvert.DeserializeObject<LizKinCosmeticData>(JsonConvert.SerializeObject(instance, LizardSkin.jsonSerializerSettings), LizardSkin.jsonSerializerSettings);
        }

        virtual internal CosmeticPanel MakeEditPanel(LizardSkinOI.ProfileManager manager)
        {
            return new CosmeticPanel(this, manager);
        }

        virtual internal void ReadEditPanel(CosmeticPanel panel)
        {
            //throw new NotImplementedException("Data wasn't read");
            this.seed = panel.seedBox.valueInt;
            this.overrideEffectColor = panel.effectCkb.GetValueBool();
            this.effectColorOverride = panel.effectColorPicker.valuecolor;
            this.overrideBaseColor = panel.baseCkb.GetValueBool();
            this.baseColorOverride = panel.baseColorPicker.valuecolor;

            this.spritesOverlap = (SpritesOverlapConfig)Enum.Parse(typeof(SpritesOverlapConfig), panel.spritesOverlapControl.value);
        }

        //abstract
        internal class CosmeticPanel : LizardSkinOI.GroupPanel
        {
            private const float pannelWidth = 364f;
            private LizardSkinOI.EventfulComboBox typeBox;
            private LizardSkinOI.ProfileManager manager;
            internal LizKinCosmeticData data;

            internal LizardSkinOI.EventfulTextBox seedBox;
            internal LizardSkinOI.EventfulCheckBox effectCkb;
            internal LizardSkinOI.OpTinyColorPicker effectColorPicker;
            internal LizardSkinOI.EventfulCheckBox baseCkb;
            internal LizardSkinOI.OpTinyColorPicker baseColorPicker;

            internal LizardSkinOI.EventfulComboBox spritesOverlapControl;

            //protected virtual float pannelHeight => 360f;
            //protected
            private Vector2 currentPlacement = new Vector2(3, -1);
            private float heightOfCurrentRow;

            protected void NewRow(float height, float verticalMargin = 2)
            {
                heightOfCurrentRow = height;
                currentPlacement.y -= height + verticalMargin;
                currentPlacement.x = 3;
                Vector2 currsize = this.size;
                currsize.y = Mathf.Abs(currentPlacement.y) + 3;
                this.size = currsize;
            }

            protected Vector2 PlaceInRow(float widthOfElement, float heightOfElement, float marginLeft = 2)
            {
                Vector2 placement = currentPlacement + new Vector2(marginLeft, (heightOfCurrentRow / 2 - heightOfElement / 2));
                currentPlacement.x += marginLeft + widthOfElement;
                return placement;
            }

            internal CosmeticPanel(LizKinCosmeticData data, LizardSkinOI.ProfileManager manager) : base(Vector2.zero, new Vector2(pannelWidth, 3))
            {
                this.data = data;
                this.manager = manager;

                NewRow(24);
                // Group panel Y coordinates are top-to-bottom
                // add type selector
                this.typeBox = new LizardSkinOI.EventfulComboBox(PlaceInRow(140, 24), 140, Enum.GetNames(typeof(LizKinCosmeticData.CosmeticInstanceType)), data.instanceType.ToString());
                typeBox.OnValueChangedEvent += TypeBox_OnChangeEvent;
                children.Add(typeBox);
                // add basic buttons
                LizardSkinOI.EventfulImageButton btnClip = new LizardSkinOI.EventfulImageButton(PlaceInRow(24, 24), new Vector2(24, 24), "LizKinClipboard");
                btnClip.OnClick += (_) => { this.manager.SetClipboard(data); };
                children.Add(btnClip);
                LizardSkinOI.EventfulImageButton btnDuplicate = new LizardSkinOI.EventfulImageButton(PlaceInRow(24, 24), new Vector2(24, 24), "LizKinDuplicate");
                btnDuplicate.OnClick += (_) => { this.manager.DuplicateCosmetic(data); };
                children.Add(btnDuplicate);
                LizardSkinOI.EventfulImageButton btnDelete = new LizardSkinOI.EventfulImageButton(PlaceInRow(24, 24), new Vector2(24, 24), "LizKinDelete");
                btnDelete.OnClick += (_) => { this.manager.DeleteCosmetic(this); };
                children.Add(btnDelete);

                float theX = currentPlacement.x;
                children.Add(new OpLabel(PlaceInRow(75, 24, 4), new Vector2(75, 24), "Effect Overr.:", FLabelAlignment.Right));
                children.Add(effectCkb = new LizardSkinOI.EventfulCheckBox(PlaceInRow(24, 24), data.overrideEffectColor));
                effectCkb.OnValueChangedEvent += DataChanged;
                children.Add(effectColorPicker = new LizardSkinOI.OpTinyColorPicker(PlaceInRow(24, 24), MenuColorEffect.ColorToHex(data.effectColorOverride)));
                effectColorPicker.OnValueChangedEvent += DataChanged;
                effectColorPicker.OnFrozenUpdate += TriggerUpdateWhileFrozen;

                // Second row
                NewRow(24);

                children.Add(new OpLabel(PlaceInRow(35, 24), new Vector2(35, 24), "Seed:", FLabelAlignment.Right));
                children.Add(seedBox = new LizardSkinOI.EventfulTextBox(PlaceInRow(50, 24), 50, data.seed.ToString()));
                seedBox.OnValueChangedEvent += DataChangedRefreshNeeded;
                seedBox.OnFrozenUpdate += TriggerUpdateWhileFrozen;

                // new in v0.7
                // SpritesOverlapConfig spritesOverlap
                children.Add(new OpLabel(PlaceInRow(50, 24, 4), new Vector2(50, 24), "Overlap:", FLabelAlignment.Right));
                this.spritesOverlapControl = new LizardSkinOI.EventfulComboBox(PlaceInRow(60, 24), 80, Enum.GetNames(typeof(SpritesOverlapConfig)), data.spritesOverlap.ToString());
                spritesOverlapControl.OnValueChangedEvent += DataChangedRefreshNeeded;
                children.Add(spritesOverlapControl);

                // force align
                currentPlacement.x = theX;
                children.Add(new OpLabel(PlaceInRow(75, 24, 4), new Vector2(75, 24), "Base Overr.:", FLabelAlignment.Right));
                children.Add(baseCkb = new LizardSkinOI.EventfulCheckBox(PlaceInRow(24, 24), data.overrideBaseColor));
                baseCkb.OnValueChangedEvent += DataChanged;
                children.Add(baseColorPicker = new LizardSkinOI.OpTinyColorPicker(PlaceInRow(24, 24), MenuColorEffect.ColorToHex(data.baseColorOverride)));
                baseColorPicker.OnValueChangedEvent += DataChanged;
                baseColorPicker.OnFrozenUpdate += TriggerUpdateWhileFrozen;
            }

            protected void TriggerUpdateWhileFrozen()
            {
                manager.KeepPreviewUpdated();
            }

            protected virtual void DataChanged()
            {
                this.data.ReadEditPanel(this);
                manager.lizardSkinOI.DataChanged();
            }
            protected virtual void DataChangedRefreshNeeded()
            {
                DataChanged();
                manager.RefreshPreview(null);
            }

            private void TypeBox_OnChangeEvent()
            {
                if (data.instanceType.ToString() == typeBox.value) return;

                // Not sure what happens here
                // do we try and clone a common ancestor of data to keep some of the info ?
                if (data.profile == null) return; // Already removed, combox dying triggers onchange :/
                manager.ChangeCosmeticType(this, (CosmeticInstanceType)Enum.Parse(typeof(CosmeticInstanceType), typeBox.value));
            }
        }
    }


    internal class CosmeticAntennaeData : LizKinCosmeticData
    {
        private const int version = 1;
        internal float length;
        internal float alpha;
        internal Color tintColor;
        internal int segments;
        internal float spinepos;
        internal float angle;
        internal float distance;
        internal float width;
        internal float offset;

        public CosmeticAntennaeData()
        {
            length = 30f;
            alpha = 0.9f;
            tintColor = new Color(1f, 0f, 0f);
            segments = 4;
            spinepos = 0f;
            angle = 0f;
            distance = 3f;
            width = 3f;
            offset = 0f;
        }

        public override CosmeticInstanceType instanceType => CosmeticInstanceType.Antennae;

        internal override CosmeticPanel MakeEditPanel(LizardSkinOI.ProfileManager manager)
        {
            return new AntennaePanel(this, manager);
        }

        internal override void ReadEditPanel(CosmeticPanel panel)
        {
            base.ReadEditPanel(panel);
            this.length = (panel as AntennaePanel).lengthControl.valueFloat;
            this.alpha = (panel as AntennaePanel).alphaControl.valueFloat;
            this.tintColor = (panel as AntennaePanel).tintPicker.valuecolor;
            segments = (panel as AntennaePanel).segmentsControl.valueInt;
            spinepos = (panel as AntennaePanel).spineposControl.valueFloat;
            angle = (panel as AntennaePanel).angleControl.valueFloat;
            distance = (panel as AntennaePanel).distanceControl.valueFloat;
            width = (panel as AntennaePanel).widthControl.valueFloat;
            offset = (panel as AntennaePanel).offsetControl.valueFloat;
        }

        internal class AntennaePanel : CosmeticPanel
        {
            internal LizardSkinOI.EventfulUpdown lengthControl;
            internal LizardSkinOI.EventfulUpdown alphaControl;
            internal LizardSkinOI.OpTinyColorPicker tintPicker;
            internal LizardSkinOI.EventfulUpdown segmentsControl;
            internal LizardSkinOI.EventfulUpdown spineposControl;
            internal LizardSkinOI.EventfulUpdown angleControl;
            internal LizardSkinOI.EventfulUpdown distanceControl;
            internal LizardSkinOI.EventfulUpdown widthControl;
            internal LizardSkinOI.EventfulUpdown offsetControl;

            internal AntennaePanel(CosmeticAntennaeData data, LizardSkinOI.ProfileManager manager) : base(data, manager)
            {
                seedBox.greyedOut = true;
                // 1st row
                NewRow(30);
                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "Length:", FLabelAlignment.Right));
                children.Add(this.lengthControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.length, 0));
                lengthControl.SetRange(1f, 400f);
                lengthControl.OnValueChangedEvent += DataChanged;
                lengthControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;

                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "Alpha:", FLabelAlignment.Right));
                children.Add(this.alphaControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.alpha, 2));
                alphaControl.SetRange(0f, 1f);
                alphaControl.OnValueChangedEvent += DataChanged;
                alphaControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;

                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "Tint:", FLabelAlignment.Right));
                children.Add(tintPicker = new LizardSkinOI.OpTinyColorPicker(PlaceInRow(24, 24), MenuColorEffect.ColorToHex(data.tintColor)));
                tintPicker.OnValueChangedEvent += DataChanged;
                tintPicker.OnFrozenUpdate += TriggerUpdateWhileFrozen;
                //tintPicker.OnSignal += DataChangedRefreshNeeded;

                // 2nd row
                NewRow(30);
                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "Segments:", FLabelAlignment.Right));
                children.Add(this.segmentsControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.segments));
                segmentsControl.SetRange(2, 20);
                segmentsControl.OnValueChangedEvent += DataChangedRefreshNeeded;
                segmentsControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;

                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "Spinepos:", FLabelAlignment.Right));
                children.Add(this.spineposControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.spinepos, 2));
                spineposControl.SetRange(0f, 1f);
                spineposControl.OnValueChangedEvent += DataChanged;
                spineposControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;

                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "Angle:", FLabelAlignment.Right));
                children.Add(this.angleControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.angle, 2));
                angleControl.SetRange(-1f, 1f);
                angleControl.OnValueChangedEvent += DataChanged;
                angleControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;

                // 3rd row
                NewRow(30);
                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "Distance:", FLabelAlignment.Right));
                children.Add(this.distanceControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.distance, 1));
                distanceControl.SetRange(-10f, 20f);
                distanceControl.OnValueChangedEvent += DataChanged;
                distanceControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;

                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "Width:", FLabelAlignment.Right));
                children.Add(this.widthControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.width, 1));
                widthControl.SetRange(0f, 20f);
                widthControl.OnValueChangedEvent += DataChanged;
                widthControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;

                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "Offset:", FLabelAlignment.Right));
                children.Add(this.offsetControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.offset, 1));
                offsetControl.SetRange(-10f, 20f);
                offsetControl.OnValueChangedEvent += DataChanged;
                offsetControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;
            }
        }
    }

    internal abstract class BodyScalesData : LizKinCosmeticData
    {
        private const int version = 1;
        public enum GenerationMode
        {
            Patch,
            Lines,
            Segments
        }

        internal GenerationMode mode;
        internal int count;
        internal float start;
        internal float length;
        internal float roundness;

        protected BodyScalesData()
        {
            mode = GenerationMode.Segments;
            start = 0.1f;
            length = 0.6f;
            count = 16;
            roundness = 0.6f;
        }

        internal override void ReadEditPanel(CosmeticPanel panel)
        {
            base.ReadEditPanel(panel);
            BodyScalesPanel p = panel as BodyScalesPanel;
            if (!p.hasModeControls) return;
            mode = (GenerationMode)Enum.Parse(typeof(GenerationMode), p.modeControl.value);
            start = p.startControl.valueFloat;
            length = p.lengthControl.valueFloat;
            count = p.countControl.valueInt;
            roundness = p.roundnessControl.valueFloat;
        }

        internal abstract class BodyScalesPanel : CosmeticPanel
        {
            internal bool hasModeControls = false;
            internal LizardSkinOI.EventfulComboBox modeControl;
            internal LizardSkinOI.EventfulUpdown startControl;
            internal LizardSkinOI.EventfulUpdown lengthControl;
            internal LizardSkinOI.EventfulUpdown countControl;
            internal LizardSkinOI.EventfulUpdown roundnessControl;
            internal BodyScalesPanel(BodyScalesData data, LizardSkinOI.ProfileManager manager) : base(data, manager)
            {

            }

            internal void MakeBodyScalesModeControls()
            {
                BodyScalesData data = this.data as BodyScalesData;
                NewRow(30);
                PlaceInRow(34, 24); // padding
                this.modeControl = new LizardSkinOI.EventfulComboBox(PlaceInRow(80, 24), 80, Enum.GetNames(typeof(BodyScalesData.GenerationMode)), data.mode.ToString());
                modeControl.OnValueChangedEvent += DataChangedRefreshNeeded;
                children.Add(modeControl);

                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "Start:", FLabelAlignment.Right));
                children.Add(this.startControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.start, 2));
                startControl.SetRange(0.0f, 0.9f);
                startControl.OnValueChangedEvent += DataChangedRefreshNeeded;
                startControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;

                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "Length:", FLabelAlignment.Right));
                children.Add(this.lengthControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.length, 2));
                lengthControl.SetRange(0.1f, 1f);
                lengthControl.OnValueChangedEvent += DataChangedRefreshNeeded;
                lengthControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;

                NewRow(30);
                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "Count:", FLabelAlignment.Right));
                children.Add(this.countControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.count));
                countControl.SetRange(1, 200);
                countControl.OnValueChangedEvent += DataChangedRefreshNeeded;
                countControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;

                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "Roundness:", FLabelAlignment.Right));
                children.Add(this.roundnessControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.roundness, 2));
                roundnessControl.SetRange(0.0f, 1.0f);
                roundnessControl.OnValueChangedEvent += DataChangedRefreshNeeded;
                roundnessControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;
                hasModeControls = true;
            }
        }
    }

    internal abstract class LongBodyScalesData : BodyScalesData
    {
        private const int version = 1;
        internal float rigor;
        internal int graphic;
        internal bool colored;
        internal float scale;
        internal float thickness;

        protected LongBodyScalesData()
        {
            rigor = 0f;
            graphic = 0;
            colored = true;
            scale = 1f;
            thickness = 1f;
        }

        internal override void ReadEditPanel(CosmeticPanel panel)
        {
            base.ReadEditPanel(panel);
            LongBodyScalesPanel lbsp = panel as LongBodyScalesPanel;
            rigor = lbsp.rigorControl.valueFloat;
            graphic = lbsp.graphicControl.valueInt;
            colored = lbsp.coloredControl.GetValueBool();
            scale = lbsp.scaleControl.valueFloat;
            thickness = lbsp.thicknessControl.valueFloat;
        }

        internal abstract class LongBodyScalesPanel : BodyScalesPanel
        {
            internal LizardSkinOI.EventfulUpdown rigorControl;
            internal LizardSkinOI.EventfulUpdown graphicControl;
            internal LizardSkinOI.EventfulCheckBox coloredControl;
            internal LizardSkinOI.EventfulUpdown scaleControl;
            internal LizardSkinOI.EventfulUpdown thicknessControl;

            internal LongBodyScalesPanel(LongBodyScalesData data, LizardSkinOI.ProfileManager manager) : base(data, manager)
            {
                NewRow(30);
                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "Rigor:", FLabelAlignment.Right));
                children.Add(this.rigorControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.rigor, 2));
                rigorControl.SetRange(0f, 1f);
                rigorControl.OnValueChangedEvent += DataChanged;
                rigorControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;

                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "Graphic:", FLabelAlignment.Right));
                children.Add(this.graphicControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.graphic));
                graphicControl.SetRange(0, 6);
                graphicControl.OnValueChangedEvent += DataChangedRefreshNeeded;
                graphicControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;

                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "Colored:", FLabelAlignment.Right));
                children.Add(coloredControl = new LizardSkinOI.EventfulCheckBox(PlaceInRow(24, 24), data.colored));
                coloredControl.OnValueChangedEvent += DataChangedRefreshNeeded;

                NewRow(30);
                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "Scale:", FLabelAlignment.Right));
                children.Add(this.scaleControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.scale, 2));
                scaleControl.SetRange(0.01f, 10f);
                scaleControl.OnValueChangedEvent += DataChangedRefreshNeeded;
                scaleControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;

                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "Thickness:", FLabelAlignment.Right));
                children.Add(this.thicknessControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.thickness, 2));
                thicknessControl.SetRange(0.01f, 10f);
                thicknessControl.OnValueChangedEvent += DataChangedRefreshNeeded;
                thicknessControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;
            }
        }
    }

    internal class CosmeticAxolotlGillsData : LongBodyScalesData
    {
        //const int version = 1; // until 0.6
        private const int version = 2; // v0.7

        // internal int count; // this is in upper classes
        internal float spread;
        internal float angle; // new in v0.7

        public CosmeticAxolotlGillsData()
        {
            start = 0f;
            count = 3;
            spread = 0.2f;
            angle = 0f;
        }

        public override CosmeticInstanceType instanceType => CosmeticInstanceType.AxolotlGills;

        internal override void ReadEditPanel(CosmeticPanel panel)
        {
            base.ReadEditPanel(panel);
            AxolotlGillsPanel p = panel as AxolotlGillsPanel;
            start = p.startControl.valueFloat;
            count = p.countControl.valueInt;
            spread = p.spreadControl.valueFloat;
            angle = p.angleControl.valueFloat;
        }

        internal override CosmeticPanel MakeEditPanel(LizardSkinOI.ProfileManager manager)
        {
            return new AxolotlGillsPanel(this, manager);
        }

        internal class AxolotlGillsPanel : LongBodyScalesPanel
        {
            // internal LizardSkinOI.EventfulUpdown countControl;
            internal LizardSkinOI.EventfulUpdown spreadControl;
            internal LizardSkinOI.EventfulUpdown angleControl;

            public AxolotlGillsPanel(CosmeticAxolotlGillsData data, LizardSkinOI.ProfileManager manager) : base(data, manager)
            {
                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "Start:", FLabelAlignment.Right));
                children.Add(this.startControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.start, 2));
                startControl.SetRange(0.0f, 0.9f);
                startControl.OnValueChangedEvent += DataChangedRefreshNeeded;
                startControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;

                NewRow(30);
                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "Count:", FLabelAlignment.Right));
                children.Add(this.countControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.count));
                countControl.SetRange(1, 10);
                countControl.OnValueChangedEvent += DataChangedRefreshNeeded;
                countControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;

                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "Spread:", FLabelAlignment.Right));
                children.Add(this.spreadControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.spread, 2));
                spreadControl.SetRange(-1f, 1f);
                spreadControl.OnValueChangedEvent += DataChangedRefreshNeeded;
                spreadControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;

                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "Angle:", FLabelAlignment.Right));
                children.Add(this.angleControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.angle, 2));
                angleControl.SetRange(-1f, 1f);
                angleControl.OnValueChangedEvent += DataChangedRefreshNeeded;
                angleControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;
            }
        }
    }

    internal class CosmeticBumpHawkData : LizKinCosmeticData
    {
        private const int version = 1;

        internal int bumps;
        internal float spineLength;
        internal bool coloredHawk;
        internal float sizeRangeMin;
        internal float sizeRangeMax;
        internal float sizeSkewExponent;

        public CosmeticBumpHawkData()
        {
            bumps = 6;
            spineLength = 0.6f;
            coloredHawk = true;
            sizeRangeMin = 0.15f;
            sizeRangeMax = 0.35f;
            sizeSkewExponent = 0.4f;
        }

        public override CosmeticInstanceType instanceType => CosmeticInstanceType.BumpHawk;

        internal override void ReadEditPanel(CosmeticPanel panel)
        {
            base.ReadEditPanel(panel);
            CosmeticBumpHawkPanel p = panel as CosmeticBumpHawkPanel;
            bumps = p.bumpsControl.valueInt;
            spineLength = p.spineLengthControl.valueFloat;
            coloredHawk = p.coloredHawkControl.GetValueBool();
            sizeRangeMin = p.sizeRangeMinControl.valueFloat;
            sizeRangeMax = p.sizeRangeMaxControl.valueFloat;
            sizeSkewExponent = p.sizeSkewExponentControl.valueFloat;
        }

        internal override CosmeticPanel MakeEditPanel(LizardSkinOI.ProfileManager manager)
        {
            return new CosmeticBumpHawkPanel(this, manager);
        }

        internal class CosmeticBumpHawkPanel : CosmeticPanel
        {
            internal LizardSkinOI.EventfulUpdown bumpsControl;
            internal LizardSkinOI.EventfulUpdown spineLengthControl;
            internal LizardSkinOI.EventfulCheckBox coloredHawkControl;
            internal LizardSkinOI.EventfulUpdown sizeRangeMinControl;
            internal LizardSkinOI.EventfulUpdown sizeRangeMaxControl;
            internal LizardSkinOI.EventfulUpdown sizeSkewExponentControl;

            public CosmeticBumpHawkPanel(CosmeticBumpHawkData data, LizardSkinOI.ProfileManager manager) : base(data, manager)
            {
                seedBox.greyedOut = true;
                NewRow(30);
                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "Bumps:", FLabelAlignment.Right));
                children.Add(this.bumpsControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.bumps));
                bumpsControl.SetRange(1, 100);
                bumpsControl.OnValueChangedEvent += DataChangedRefreshNeeded;
                bumpsControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;

                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "Length:", FLabelAlignment.Right));
                children.Add(this.spineLengthControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.spineLength, 2));
                spineLengthControl.SetRange(0.01f, 1f);
                spineLengthControl.OnValueChangedEvent += DataChanged;
                spineLengthControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;

                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "Colored:", FLabelAlignment.Right));
                children.Add(coloredHawkControl = new LizardSkinOI.EventfulCheckBox(PlaceInRow(24, 24), data.coloredHawk));
                coloredHawkControl.OnValueChangedEvent += DataChanged;

                NewRow(30);
                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "SizeMin:", FLabelAlignment.Right));
                children.Add(this.sizeRangeMinControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.sizeRangeMin, 2));
                sizeRangeMinControl.SetRange(0.01f, 10f);
                sizeRangeMinControl.OnValueChangedEvent += DataChanged;
                sizeRangeMinControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;

                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "SizeMax:", FLabelAlignment.Right));
                children.Add(this.sizeRangeMaxControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.sizeRangeMax, 2));
                sizeRangeMaxControl.SetRange(0.1f, 10f);
                sizeRangeMaxControl.OnValueChangedEvent += DataChanged;
                sizeRangeMaxControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;

                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "SizeExp:", FLabelAlignment.Right));
                children.Add(this.sizeSkewExponentControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.sizeSkewExponent, 1));
                sizeSkewExponentControl.SetRange(-10f, 10f);
                sizeSkewExponentControl.OnValueChangedEvent += DataChanged;
                sizeSkewExponentControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;
            }
        }
    }

    internal class CosmeticJumpRingsData : LizKinCosmeticData
    {
        private const int version = 1;
        internal int count;
        internal float spread;
        internal float innerScale;
        internal float spineStart;
        internal float spineStop;
        internal float spineExponent;
        internal float scaleStart;
        internal float scaleStop;
        internal float scaleExponent;
        internal float thickness;
        internal bool invertOverlap;

        public CosmeticJumpRingsData()
        {
            count = 2;
            spread = 2f;
            innerScale = 1f;
            spineStart = 0.15f;
            spineStop = 0.3f;
            spineExponent = 1f;
            scaleStart = 1f;
            scaleStop = 0.8f;
            scaleExponent = 1f;
            thickness = 1f;
            invertOverlap = false;
        }

        public override CosmeticInstanceType instanceType => CosmeticInstanceType.JumpRings;

        internal override void ReadEditPanel(CosmeticPanel panel)
        {
            base.ReadEditPanel(panel);
            CosmeticJumpRingsPanel p = panel as CosmeticJumpRingsPanel;

            count = p.countControl.valueInt;
            spread = p.spreadControl.valueFloat;
            invertOverlap = p.invertOverlapControl.GetValueBool();
            spineStart = p.spineStartControl.valueFloat;
            spineStop = p.spineStopControl.valueFloat;
            spineExponent = p.spineExponentControl.valueFloat;
            scaleStart = p.scaleStartControl.valueFloat;
            scaleStop = p.scaleStopControl.valueFloat;
            scaleExponent = p.scaleExponentControl.valueFloat;
            thickness = p.thicknessControl.valueFloat;
            innerScale = p.innerScaleControl.valueFloat;
        }

        internal override CosmeticPanel MakeEditPanel(LizardSkinOI.ProfileManager manager)
        {
            return new CosmeticJumpRingsPanel(this, manager);
        }

        internal class CosmeticJumpRingsPanel : CosmeticPanel
        {
            internal LizardSkinOI.EventfulUpdown countControl;
            internal LizardSkinOI.EventfulUpdown spreadControl;
            internal LizardSkinOI.EventfulCheckBox invertOverlapControl;
            internal LizardSkinOI.EventfulUpdown spineStartControl;
            internal LizardSkinOI.EventfulUpdown spineStopControl;
            internal LizardSkinOI.EventfulUpdown spineExponentControl;
            internal LizardSkinOI.EventfulUpdown scaleStartControl;
            internal LizardSkinOI.EventfulUpdown scaleStopControl;
            internal LizardSkinOI.EventfulUpdown scaleExponentControl;
            internal LizardSkinOI.EventfulUpdown thicknessControl;
            internal LizardSkinOI.EventfulUpdown innerScaleControl;

            public CosmeticJumpRingsPanel(CosmeticJumpRingsData data, LizardSkinOI.ProfileManager manager) : base(data, manager)
            {
                seedBox.greyedOut = true;
                NewRow(30);
                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "Count:", FLabelAlignment.Right));
                children.Add(this.countControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.count));
                countControl.SetRange(1, 100);
                countControl.OnValueChangedEvent += DataChangedRefreshNeeded;
                countControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;

                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "Spread:", FLabelAlignment.Right));
                children.Add(this.spreadControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.spread, 1));
                spreadControl.SetRange(-10f, 10f);
                spreadControl.OnValueChangedEvent += DataChanged;
                spreadControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;

                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "InvOverlap:", FLabelAlignment.Right));
                children.Add(invertOverlapControl = new LizardSkinOI.EventfulCheckBox(PlaceInRow(24, 24), data.invertOverlap));
                invertOverlapControl.OnValueChangedEvent += DataChangedRefreshNeeded;

                NewRow(30);
                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "SpineStart:", FLabelAlignment.Right));
                children.Add(this.spineStartControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.spineStart, 2));
                spineStartControl.SetRange(0.01f, 1f);
                spineStartControl.OnValueChangedEvent += DataChanged;
                spineStartControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;

                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "SpineStop:", FLabelAlignment.Right));
                children.Add(this.spineStopControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.spineStop, 2));
                spineStopControl.SetRange(0.01f, 1f);
                spineStopControl.OnValueChangedEvent += DataChanged;
                spineStopControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;

                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "SpineExp:", FLabelAlignment.Right));
                children.Add(this.spineExponentControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.spineExponent, 1));
                spineExponentControl.SetRange(0.01f, 10f);
                spineExponentControl.OnValueChangedEvent += DataChanged;
                spineExponentControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;

                NewRow(30);
                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "ScaleStart:", FLabelAlignment.Right));
                children.Add(this.scaleStartControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.scaleStart, 2));
                scaleStartControl.SetRange(0.01f, 10f);
                scaleStartControl.OnValueChangedEvent += DataChanged;
                scaleStartControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;

                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "ScaleStop:", FLabelAlignment.Right));
                children.Add(this.scaleStopControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.scaleStop, 2));
                scaleStopControl.SetRange(0.01f, 10f);
                scaleStopControl.OnValueChangedEvent += DataChanged;
                scaleStopControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;

                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "ScaleExp:", FLabelAlignment.Right));
                children.Add(this.scaleExponentControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.scaleExponent, 1));
                scaleExponentControl.SetRange(0.01f, 10f);
                scaleExponentControl.OnValueChangedEvent += DataChanged;
                scaleExponentControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;

                NewRow(30);
                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "Thickness:", FLabelAlignment.Right));
                children.Add(this.thicknessControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.thickness, 2));
                thicknessControl.SetRange(0.01f, 10f);
                thicknessControl.OnValueChangedEvent += DataChanged;
                thicknessControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;

                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "InnerScale:", FLabelAlignment.Right));
                children.Add(this.innerScaleControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.innerScale, 2));
                innerScaleControl.SetRange(0.01f, 10f);
                innerScaleControl.OnValueChangedEvent += DataChanged;
                innerScaleControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;
            }
        }
    }
    internal class CosmeticLongHeadScalesData : LongBodyScalesData
    {
        private const int version = 1;
        internal float spinePos;
        internal float offset;
        internal float angle;

        public CosmeticLongHeadScalesData()
        {
            spinePos = 0.05f;
            offset = 1.5f;
            angle = 0.6f;
        }

        public override CosmeticInstanceType instanceType => CosmeticInstanceType.LongHeadScales;

        internal override void ReadEditPanel(CosmeticPanel panel)
        {
            base.ReadEditPanel(panel);
            CosmeticLongHeadScalesPanel p = panel as CosmeticLongHeadScalesPanel;

            spinePos = p.spinePosControl.valueFloat;
            offset = p.offsetControl.valueFloat;
            angle = p.angleControl.valueFloat;
        }

        internal override CosmeticPanel MakeEditPanel(LizardSkinOI.ProfileManager manager)
        {
            return new CosmeticLongHeadScalesPanel(this, manager);
        }

        internal class CosmeticLongHeadScalesPanel : LongBodyScalesPanel
        {
            internal LizardSkinOI.EventfulUpdown spinePosControl;
            internal LizardSkinOI.EventfulUpdown offsetControl;
            internal LizardSkinOI.EventfulUpdown angleControl;

            public CosmeticLongHeadScalesPanel(CosmeticLongHeadScalesData data, LizardSkinOI.ProfileManager manager) : base(data, manager)
            {
                seedBox.greyedOut = true;
                NewRow(30);
                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "SpinePos:", FLabelAlignment.Right));
                children.Add(this.spinePosControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.spinePos, 2));
                spinePosControl.SetRange(0.01f, 1f);
                spinePosControl.OnValueChangedEvent += DataChanged;
                spinePosControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;

                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "Offset:", FLabelAlignment.Right));
                children.Add(this.offsetControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.offset, 1));
                offsetControl.SetRange(0f, 10f);
                offsetControl.OnValueChangedEvent += DataChanged;
                offsetControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;

                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "Angle:", FLabelAlignment.Right));
                children.Add(this.angleControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.angle, 2));
                angleControl.SetRange(-1f, 1f);
                angleControl.OnValueChangedEvent += DataChanged;
                angleControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;
            }
        }
    }
    internal class CosmeticLongShoulderScalesData : LongBodyScalesData
    {
        private const int version = 1;
        internal float minSize;
        internal float sizeExponent;

        public CosmeticLongShoulderScalesData()
        {
            minSize = 0.3f;
            sizeExponent = 0.8f;
        }

        public override CosmeticInstanceType instanceType => CosmeticInstanceType.LongShoulderScales;

        internal override void ReadEditPanel(CosmeticPanel panel)
        {
            base.ReadEditPanel(panel);
            CosmeticLongShoulderScalesPanel p = panel as CosmeticLongShoulderScalesPanel;
            minSize = p.minSizeControl.valueFloat;
            sizeExponent = p.sizeExponentControl.valueFloat;
        }

        internal override CosmeticPanel MakeEditPanel(LizardSkinOI.ProfileManager manager)
        {
            return new CosmeticLongShoulderScalesPanel(this, manager);
        }

        internal class CosmeticLongShoulderScalesPanel : LongBodyScalesPanel
        {
            internal LizardSkinOI.EventfulUpdown minSizeControl;
            internal LizardSkinOI.EventfulUpdown sizeExponentControl;

            public CosmeticLongShoulderScalesPanel(CosmeticLongShoulderScalesData data, LizardSkinOI.ProfileManager manager) : base(data, manager)
            {
                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "MinSize:", FLabelAlignment.Right));
                children.Add(this.minSizeControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.minSize, 2));
                minSizeControl.SetRange(0.01f, 1f);
                minSizeControl.OnValueChangedEvent += DataChangedRefreshNeeded;
                minSizeControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;

                MakeBodyScalesModeControls();

                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "SizeExpo:", FLabelAlignment.Right));
                children.Add(this.sizeExponentControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.sizeExponent, 2));
                sizeExponentControl.SetRange(-10f, 10f);
                sizeExponentControl.OnValueChangedEvent += DataChangedRefreshNeeded;
                sizeExponentControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;
            }
        }
    }
    internal class CosmeticShortBodyScalesData : BodyScalesData
    {
        private const int version = 1;
        internal float scale;
        internal float thickness;

        public CosmeticShortBodyScalesData()
        {
            scale = 2f;
            thickness = 1.5f;
        }

        public override CosmeticInstanceType instanceType => CosmeticInstanceType.ShortBodyScales;
        
        internal override void ReadEditPanel(CosmeticPanel panel)
        {
            base.ReadEditPanel(panel);
            CosmeticShortBodyScalesPanel p = panel as CosmeticShortBodyScalesPanel;
            scale = p.scaleControl.valueFloat;
            thickness = p.thicknessControl.valueFloat;
        }

        internal override CosmeticPanel MakeEditPanel(LizardSkinOI.ProfileManager manager)
        {
            return new CosmeticShortBodyScalesPanel(this, manager);
        }

        internal class CosmeticShortBodyScalesPanel : BodyScalesPanel
        {
            internal LizardSkinOI.EventfulUpdown scaleControl;
            internal LizardSkinOI.EventfulUpdown thicknessControl;

            public CosmeticShortBodyScalesPanel(CosmeticShortBodyScalesData data, LizardSkinOI.ProfileManager manager) : base(data, manager)
            {
                //NewRow(30f);
                MakeBodyScalesModeControls();

                NewRow(30);
                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "Scale:", FLabelAlignment.Right));
                children.Add(this.scaleControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.scale, 2));
                scaleControl.SetRange(0.01f, 10f);
                scaleControl.OnValueChangedEvent += DataChangedRefreshNeeded;
                scaleControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;

                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "Thickness:", FLabelAlignment.Right));
                children.Add(this.thicknessControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.thickness, 2));
                thicknessControl.SetRange(0.01f, 10f);
                thicknessControl.OnValueChangedEvent += DataChangedRefreshNeeded;
                thicknessControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;
            }
        }
    }

    internal class CosmeticSpineSpikesData : LongBodyScalesData
    {
        private const int version = 1;
        internal float sizeMin;
        internal float sizeExponent;
        internal bool colorFade;

        public CosmeticSpineSpikesData()
        {
            mode = GenerationMode.Lines;
            sizeMin = 0.3f;
            sizeExponent = 0.6f;
            colorFade = true;
        }

        public override CosmeticInstanceType instanceType => CosmeticInstanceType.SpineSpikes;

        internal override void ReadEditPanel(CosmeticPanel panel)
        {
            base.ReadEditPanel(panel);
            CosmeticSpineSpikesPanel p = panel as CosmeticSpineSpikesPanel;
            sizeMin = p.sizeMinControl.valueFloat;
            sizeExponent = p.sizeExponentControl.valueFloat;
            colorFade = p.colorFadeControl.GetValueBool();
        }

        internal override CosmeticPanel MakeEditPanel(LizardSkinOI.ProfileManager manager)
        {
            return new CosmeticSpineSpikesPanel(this, manager);
        }

        internal class CosmeticSpineSpikesPanel : LongBodyScalesPanel
        {
            internal LizardSkinOI.EventfulUpdown sizeMinControl;
            internal LizardSkinOI.EventfulUpdown sizeExponentControl;
            internal LizardSkinOI.EventfulCheckBox colorFadeControl;

            public CosmeticSpineSpikesPanel(CosmeticSpineSpikesData data, LizardSkinOI.ProfileManager manager) : base(data, manager)
            {
                rigorControl.greyedOut = true;

                MakeBodyScalesModeControls();
                this.modeControl.greyedOut = true;
                this.roundnessControl.greyedOut = true;

                // NewRow(30);
                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "SizeMin:", FLabelAlignment.Right));
                children.Add(this.sizeMinControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.sizeMin, 2));
                sizeMinControl.SetRange(0.01f, 1f);
                sizeMinControl.OnValueChangedEvent += DataChanged;
                sizeMinControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;

                NewRow(30);
                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "SizeExpo:", FLabelAlignment.Right));
                children.Add(this.sizeExponentControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.sizeExponent, 2));
                sizeExponentControl.SetRange(-10f, 10f);
                sizeExponentControl.OnValueChangedEvent += DataChanged;
                sizeExponentControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;

                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "ColorFade:", FLabelAlignment.Right));
                children.Add(colorFadeControl = new LizardSkinOI.EventfulCheckBox(PlaceInRow(24, 24), data.colorFade));
                colorFadeControl.OnValueChangedEvent += DataChangedRefreshNeeded;
            }
        }
    }

    internal class CosmeticTailFinData : CosmeticSpineSpikesData // Reuse recycle re...
    {
        private const int version = 1;
        internal float undersideSize;

        public CosmeticTailFinData()
        {
            undersideSize = 0.6f;
            count = 8;
        }

        public override CosmeticInstanceType instanceType => CosmeticInstanceType.TailFin;

        internal override void ReadEditPanel(CosmeticPanel panel)
        {
            base.ReadEditPanel(panel);
            CosmeticTailFinDataPanel p = panel as CosmeticTailFinDataPanel;
            undersideSize = p.undersideSizeControl.valueFloat;
        }

        internal override CosmeticPanel MakeEditPanel(LizardSkinOI.ProfileManager manager)
        {
            return new CosmeticTailFinDataPanel(this, manager);
        }

        internal class CosmeticTailFinDataPanel : CosmeticSpineSpikesPanel
        {
            internal LizardSkinOI.EventfulUpdown undersideSizeControl;

            public CosmeticTailFinDataPanel(CosmeticTailFinData data, LizardSkinOI.ProfileManager manager) : base(data, manager)
            {
                // NewRow(30f);
                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "UndersideSize:", FLabelAlignment.Right));
                children.Add(this.undersideSizeControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.undersideSize, 2));
                undersideSizeControl.SetRange(0.01f, 2f);
                undersideSizeControl.OnValueChangedEvent += DataChanged;
                undersideSizeControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;
            }
        }
    }

    internal class CosmeticTailGeckoScalesData : LizKinCosmeticData
    {
        private const int version = 2;
        // v1
        internal int rows;
        internal int lines;
        internal bool bigScales;
        // v2
        internal float start;
        internal float stop;
        internal float exponent;
        internal float shine;

        public CosmeticTailGeckoScalesData()
        {
            rows = 11;
            lines = 4;
            bigScales = false;
            start = 0.5f;
            stop = 0.98f;
            exponent = 0.8f;
            shine = 0.5f;
        }

        public override CosmeticInstanceType instanceType => CosmeticInstanceType.TailGeckoScales;
        
        internal override void ReadEditPanel(CosmeticPanel panel)
        {
            base.ReadEditPanel(panel);
            CosmeticTailGeckoScalesPanel p = panel as CosmeticTailGeckoScalesPanel;
            rows = p.rowsControl.valueInt;
            lines = p.linesControl.valueInt;
            bigScales = p.bigScalesControl.GetValueBool();
            start = p.startControl.valueFloat;
            stop = p.stopControl.valueFloat;
            exponent = p.exponentControl.valueFloat;
            shine = p.shineControl.valueFloat;
        }

        internal override CosmeticPanel MakeEditPanel(LizardSkinOI.ProfileManager manager)
        {
            return new CosmeticTailGeckoScalesPanel(this, manager);
        }

        internal class CosmeticTailGeckoScalesPanel : CosmeticPanel
        {
            internal LizardSkinOI.EventfulUpdown rowsControl;
            internal LizardSkinOI.EventfulUpdown linesControl;
            internal LizardSkinOI.EventfulCheckBox bigScalesControl;
            internal LizardSkinOI.EventfulUpdown startControl;
            internal LizardSkinOI.EventfulUpdown stopControl;
            internal LizardSkinOI.EventfulUpdown exponentControl;
            internal LizardSkinOI.EventfulUpdown shineControl;

            public CosmeticTailGeckoScalesPanel(CosmeticTailGeckoScalesData data, LizardSkinOI.ProfileManager manager) : base(data, manager)
            {
                NewRow(30);
                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "Rows:", FLabelAlignment.Right));
                children.Add(this.rowsControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.rows));
                rowsControl.SetRange(2, 100);
                rowsControl.OnValueChangedEvent += DataChangedRefreshNeeded;
                rowsControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;

                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "Lines:", FLabelAlignment.Right));
                children.Add(this.linesControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.lines));
                linesControl.SetRange(2, 100);
                linesControl.OnValueChangedEvent += DataChangedRefreshNeeded;
                linesControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;

                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "BigScales:", FLabelAlignment.Right));
                children.Add(bigScalesControl = new LizardSkinOI.EventfulCheckBox(PlaceInRow(24, 24), data.bigScales));
                bigScalesControl.OnValueChangedEvent += DataChangedRefreshNeeded;

                NewRow(30);
                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "Start:", FLabelAlignment.Right));
                children.Add(this.startControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.start, 2));
                startControl.SetRange(0f, 1f);
                startControl.OnValueChangedEvent += DataChanged;
                startControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;

                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "Stop:", FLabelAlignment.Right));
                children.Add(this.stopControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.stop, 2));
                stopControl.SetRange(0f, 1f);
                stopControl.OnValueChangedEvent += DataChanged;
                stopControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;

                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "Exponent:", FLabelAlignment.Right));
                children.Add(this.exponentControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.exponent, 2));
                exponentControl.SetRange(0.01f, 10f);
                exponentControl.OnValueChangedEvent += DataChanged;
                exponentControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;

                NewRow(30);
                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "Shine:", FLabelAlignment.Right));
                children.Add(this.shineControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.shine, 2));
                shineControl.SetRange(0f, 1f);
                shineControl.OnValueChangedEvent += DataChanged;
                shineControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;
            }
        }
    }

    internal class CosmeticTailTuftData : LongBodyScalesData
    {
        private const int version = 1;
        internal float minSize;

        public CosmeticTailTuftData()
        {
            this.minSize = 0.2f;
            count = 8;
            length = 0.4f;
        }

        public override CosmeticInstanceType instanceType => CosmeticInstanceType.TailTuft;

        internal override void ReadEditPanel(CosmeticPanel panel)
        {
            base.ReadEditPanel(panel);
            CosmeticTailTuftPanel p = panel as CosmeticTailTuftPanel;
            minSize = p.minSizeControl.valueFloat;
        }

        internal override CosmeticPanel MakeEditPanel(LizardSkinOI.ProfileManager manager)
        {
            return new CosmeticTailTuftPanel(this, manager);
        }

        internal class CosmeticTailTuftPanel : LongBodyScalesPanel
        {
            internal LizardSkinOI.EventfulUpdown minSizeControl;

            public CosmeticTailTuftPanel(CosmeticTailTuftData data, LizardSkinOI.ProfileManager manager) : base(data, manager)
            {
                MakeBodyScalesModeControls();
                modeControl.ForceValue(BodyScalesData.GenerationMode.Lines.ToString());
                modeControl.greyedOut = true;
                startControl.greyedOut = true;

                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "MinSize:", FLabelAlignment.Right));
                children.Add(this.minSizeControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.minSize, 2));
                minSizeControl.SetRange(0.01f, 1f);
                minSizeControl.OnValueChangedEvent += DataChangedRefreshNeeded;
                minSizeControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;
            }
        }
    }

    internal class CosmeticWhiskersData : LizKinCosmeticData
    {
        private const int version = 1;
        internal int count;
        internal float length;
        internal float thickness;
        internal float spread;
        internal float spring;

        public CosmeticWhiskersData()
        {
            this.count = 4;
            this.length = 1f;
            this.thickness = 1f;
            this.spread = 1f;
            this.spring = 1f;
        }

        public override CosmeticInstanceType instanceType => CosmeticInstanceType.Whiskers;

        internal override void ReadEditPanel(CosmeticPanel panel)
        {
            base.ReadEditPanel(panel);
            CosmeticWhiskersPanel p = panel as CosmeticWhiskersPanel;
            count = p.countControl.valueInt;
            length = p.lengthControl.valueFloat;
            thickness = p.thicknessControl.valueFloat;
            spread = p.spreadControl.valueFloat;
            spring = p.springControl.valueFloat;
        }

        internal override CosmeticPanel MakeEditPanel(LizardSkinOI.ProfileManager manager)
        {
            return new CosmeticWhiskersPanel(this, manager);
        }

        internal class CosmeticWhiskersPanel : CosmeticPanel
        {
            internal LizardSkinOI.EventfulUpdown countControl;
            internal LizardSkinOI.EventfulUpdown lengthControl;
            internal LizardSkinOI.EventfulUpdown thicknessControl;
            internal LizardSkinOI.EventfulUpdown spreadControl;
            internal LizardSkinOI.EventfulUpdown springControl;

            public CosmeticWhiskersPanel(CosmeticWhiskersData data, LizardSkinOI.ProfileManager manager) : base(data, manager)
            {
                NewRow(30f);
                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "Count:", FLabelAlignment.Right));
                children.Add(this.countControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.count));
                countControl.SetRange(1, 20);
                countControl.OnValueChangedEvent += DataChangedRefreshNeeded;
                countControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;

                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "Length:", FLabelAlignment.Right));
                children.Add(this.lengthControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.length, 2));
                lengthControl.SetRange(0.01f, 10f);
                lengthControl.OnValueChangedEvent += DataChangedRefreshNeeded;
                lengthControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;

                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "Thickness:", FLabelAlignment.Right));
                children.Add(this.thicknessControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.thickness, 2));
                thicknessControl.SetRange(0.01f, 10f);
                thicknessControl.OnValueChangedEvent += DataChangedRefreshNeeded;
                thicknessControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;

                NewRow(30f);
                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "Spread:", FLabelAlignment.Right));
                children.Add(this.spreadControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.spread, 2));
                spreadControl.SetRange(0.01f, 10f);
                spreadControl.OnValueChangedEvent += DataChangedRefreshNeeded;
                spreadControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;

                children.Add(new OpLabel(PlaceInRow(60, 24), new Vector2(60, 24), "Spring:", FLabelAlignment.Right));
                children.Add(this.springControl = new LizardSkinOI.EventfulUpdown(PlaceInRow(55, 30), 55, data.spring, 2));
                springControl.SetRange(0.01f, 10f);
                springControl.OnValueChangedEvent += DataChangedRefreshNeeded;
                springControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;
            }
        }
    }

    internal class CosmeticWingScalesData : LongBodyScalesData//LizKinCosmeticData
    {
        private const int version = 1;

        public CosmeticWingScalesData()
        {
            count = 2;
            start = 0.15f;
            length = 0.15f;
            mode = GenerationMode.Lines;
        }

        public override CosmeticInstanceType instanceType => CosmeticInstanceType.WingScales;

        //internal override void ReadEditPanel(CosmeticPanel panel)
        //{
        //    base.ReadEditPanel(panel);
        //    CosmeticWingScalesPanel p = panel as CosmeticWingScalesPanel;
        //    spring = p.springControl.valueFloat;
        //}

        internal override CosmeticPanel MakeEditPanel(LizardSkinOI.ProfileManager manager)
        {
            return new CosmeticWingScalesPanel(this, manager);
        }

        internal class CosmeticWingScalesPanel : LongBodyScalesPanel
        {


            public CosmeticWingScalesPanel(CosmeticWingScalesData data, LizardSkinOI.ProfileManager manager) : base(data, manager)
            {
                MakeBodyScalesModeControls();
                modeControl.ForceValue(BodyScalesData.GenerationMode.Lines.ToString());
                modeControl.greyedOut = true;
            }
        }
    }
}
