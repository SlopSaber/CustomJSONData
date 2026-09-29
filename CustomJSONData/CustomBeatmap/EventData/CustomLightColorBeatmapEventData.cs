using System;

namespace CustomJSONData.CustomBeatmap
{
    public class CustomLightColorBeatmapEventData : LightColorBeatmapEventData, ICustomData, IVersionable
    {
        public CustomLightColorBeatmapEventData(
            float time,
            int groupId,
            int elementId,
#if PRE_V1_37_1
            BeatmapEventTransitionType transitionType,
#else
            bool usePreviousValue,
            EaseType easeType,
#endif
            EnvironmentColorType colorType,
            float brightness,
            int strobeBeatFrequency,
#if !V1_29_1
            float strobeBrightness,
            bool strobeFade,
#endif
            CustomData customData)
            : this(
                time,
                groupId,
                elementId,
#if PRE_V1_37_1
                transitionType,
#else
                usePreviousValue,
                easeType,
#endif
                colorType,
                brightness,
                strobeBeatFrequency,
#if !V1_29_1
                strobeBrightness,
                strobeFade,
#endif
                customData,
                VersionExtensions.version3)
        {
        }

        public CustomLightColorBeatmapEventData(
            float time,
            int groupId,
            int elementId,
#if PRE_V1_37_1
            BeatmapEventTransitionType transitionType,
#else
            bool usePreviousValue,
            EaseType easeType,
#endif
            EnvironmentColorType colorType,
            float brightness,
            int strobeBeatFrequency,
#if !V1_29_1
            float strobeBrightness,
            bool strobeFade,
#endif
            CustomData customData,
            Version version)
            : base(
                time,
                groupId,
                elementId,
#if PRE_V1_37_1
                transitionType,
#else
                usePreviousValue,
                easeType,
#endif
                colorType,
                brightness,
#if !V1_29_1
                strobeBeatFrequency,
                strobeBrightness,
                strobeFade)
#else
                strobeBeatFrequency)
#endif
        {
            this.customData = customData;
            this.version = version;
        }

        public CustomData customData { get; }

        public Version version { get; }

        public override BeatmapDataItem GetCopy()
        {
            return new CustomLightColorBeatmapEventData(
                time,
                groupId,
                elementId,
#if PRE_V1_37_1
                transitionType,
#else
                usePreviousValue,
                easeType,
#endif
                colorType,
                brightness,
                strobeBeatFrequency,
#if !V1_29_1
                strobeBrightness,
                strobeFade,
#endif
                customData.Copy(),
                version);
        }
    }
}
