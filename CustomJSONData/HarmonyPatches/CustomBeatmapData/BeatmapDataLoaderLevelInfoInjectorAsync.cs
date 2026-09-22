#if !PRE_V1_37_1
#if ASYNC_BEATMAP_LOADER_ONLY
using System.Threading.Tasks;
using CustomJSONData.CustomBeatmap;
using HarmonyLib;

namespace CustomJSONData.HarmonyPatches
{
    [HarmonyPatch(typeof(BeatmapDataLoader), nameof(BeatmapDataLoader.LoadBeatmapDataAsync))]
    internal static class BeatmapDataLoaderLevelInfoInjectorAsync
    {
        [HarmonyPostfix]
        private static void Postfix(ref Task<IReadonlyBeatmapData> __result, IBeatmapLevelData beatmapLevelData, BeatmapKey beatmapKey)
        {
            __result = InjectCustomDataAsync(__result, beatmapLevelData, beatmapKey);
        }

        private static async Task<IReadonlyBeatmapData> InjectCustomDataAsync(Task<IReadonlyBeatmapData> task, IBeatmapLevelData beatmapLevelData, BeatmapKey beatmapKey)
        {
            IReadonlyBeatmapData result = await task.ConfigureAwait(false);
            if (beatmapLevelData is CustomFileBeatmapLevelData fileBeatmapLevelData && result is CustomBeatmapData beatmapData)
            {
                beatmapData.levelCustomData = fileBeatmapLevelData.customData;
                if (fileBeatmapLevelData.GetDifficultyBeatmap(beatmapKey) is CustomFileDifficultyBeatmap customFileDifficultyBeatmap)
                {
                    beatmapData.beatmapCustomData = customFileDifficultyBeatmap.customData;
                }
            }

            return result;
        }
    }
}
#else
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using CustomJSONData.CustomBeatmap;
using HarmonyLib;
using JetBrains.Annotations;

namespace CustomJSONData.HarmonyPatches
{
    [HarmonyPatch]
    internal static class BeatmapDataLoaderLevelInfoInjectorAsync
    {
        private static readonly Type _stateMachineType = GetStateMachineType();

        private static readonly FieldInfo _beatmapLevelData = AccessTools.Field(_stateMachineType, "beatmapLevelData");
        private static readonly FieldInfo _beatmapKey = AccessTools.Field(_stateMachineType, "beatmapKey");

        private static readonly MethodInfo _injectCustomData =
            AccessTools.Method(typeof(BeatmapDataLoaderLevelInfoInjectorAsync), nameof(InjectCustomData));

        private static Type GetStateMachineType()
        {
            MethodInfo target = AccessTools.Method(typeof(BeatmapDataLoader), "LoadBeatmapDataAsync");
            AsyncStateMachineAttribute? stateMachineAttr = target.GetCustomAttribute<AsyncStateMachineAttribute>();
            if (stateMachineAttr is null)
            {
                throw new InvalidOperationException();
            }

            return stateMachineAttr.StateMachineType;
        }

        private static void InjectCustomData(IBeatmapLevelData beatmapLevelData, BeatmapKey beatmapKey, IReadonlyBeatmapData result)
        {
            if (beatmapLevelData is not CustomFileBeatmapLevelData fileBeatmapLevelData ||
                result is not CustomBeatmapData beatmapData)
            {
                return;
            }

            beatmapData.levelCustomData = fileBeatmapLevelData.customData;
            FileDifficultyBeatmap? fileDifficultyBeatmap = fileBeatmapLevelData.GetDifficultyBeatmap(beatmapKey);
            if (fileDifficultyBeatmap is CustomFileDifficultyBeatmap customFileDifficultyBeatmap)
            {
                beatmapData.beatmapCustomData = customFileDifficultyBeatmap.customData;
            }
        }

        [UsedImplicitly]
        [HarmonyTargetMethod]
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(_stateMachineType, "MoveNext");
        }

        [UsedImplicitly]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> InjectCustomDataAsyncTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
                .End()
                .MatchBack(
                    false,
                    new CodeMatch(OpCodes.Stloc_1))
                .InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld, _beatmapLevelData),
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld, _beatmapKey),
                    new CodeInstruction(OpCodes.Ldloc_1),
                    new CodeInstruction(OpCodes.Call, _injectCustomData))
                .InstructionEnumeration();
        }
    }
}
#endif
#endif
