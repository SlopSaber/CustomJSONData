using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using CustomJSONData.CustomBeatmap;
using HarmonyLib;

namespace CustomJSONData.HarmonyPatches
{
#if BEATMAP_CALLBACK_TYPE_IDS
    [HarmonyPatch(typeof(BeatmapDataItem))]
#elif LATEST
    [HarmonyPatch(
        typeof(BeatmapDataItem),
        MethodType.Constructor,
        typeof(float),
        typeof(int),
        typeof(int),
        typeof(BeatmapDataItem.BeatmapDataItemType))]
#else
    [HarmonyPatch(typeof(CallbacksInTime), nameof(CallbacksInTime.CallCallbacks), typeof(BeatmapDataItem))]
#endif
    internal static class CallbackCustomTypeGetter
    {
        private static readonly MethodInfo _getType = AccessTools.Method(typeof(object), nameof(GetType));
        private static readonly MethodInfo _getCustomType = AccessTools.Method(typeof(CustomBeatmapData), nameof(CustomBeatmapData.GetCustomType));

        [HarmonyTranspiler]
#if BEATMAP_CALLBACK_TYPE_IDS
        // The game caches both callback type IDs in the item constructor.
        // Normalize custom subclasses before those IDs are computed so both
        // concrete and base-type callbacks behave like their vanilla counterparts.
        [HarmonyPatch(MethodType.Constructor, new[] { typeof(float), typeof(int), typeof(int), typeof(BeatmapDataItem.BeatmapDataItemType) })]
#endif
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
                .MatchForward(false, new CodeMatch(instruction => instruction.Calls(_getType)))
                .Set(OpCodes.Call, _getCustomType)
                .InstructionEnumeration();
        }
    }
}
