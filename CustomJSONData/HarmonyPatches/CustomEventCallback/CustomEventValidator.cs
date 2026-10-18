using System.Collections.Generic;
#if BEAT_SABER_1453
using System.Reflection;
#else
using System.Linq;
#endif
using System.Reflection.Emit;
using HarmonyLib;

namespace CustomJSONData.HarmonyPatches
{
    [HarmonyPatch(typeof(BeatmapCallbacksController))]
    internal static class CustomEventValidator
    {
#if BEAT_SABER_1453
        private static readonly MethodInfo _getItemType = AccessTools.PropertyGetter(typeof(BeatmapDataItem), nameof(BeatmapDataItem.type));
        private static readonly MethodInfo _isEvent = AccessTools.Method(typeof(CustomEventValidator), nameof(IsEvent));
#endif

        [HarmonyTranspiler]
        [HarmonyPatch(nameof(BeatmapCallbacksController.ManualUpdate))]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
#if BEAT_SABER_1453
            return new CodeMatcher(instructions)
                .MatchForward(
                    false,
                    new CodeMatch(OpCodes.Callvirt, _getItemType),
                    new CodeMatch(OpCodes.Ldc_I4_1),
                    new CodeMatch(OpCodes.Beq))
                .Advance(1)
                .SetAndAdvance(OpCodes.Call, _isEvent)
                .SetOpcodeAndAdvance(OpCodes.Brtrue)
                .InstructionEnumeration();
#else
            CodeMatcher matcher = new CodeMatcher(instructions)
                .MatchForward(
                    false,
                    new CodeMatch(OpCodes.Ldloc_S),
#if LATEST
                    new CodeMatch(OpCodes.Callvirt),
#else
                    new CodeMatch(OpCodes.Ldfld),
#endif
                    new CodeMatch(OpCodes.Ldc_I4_1),
                    new CodeMatch(OpCodes.Beq));
            CodeInstruction[] duped = matcher.InstructionsWithOffsets(0, 4).Select(n => new CodeInstruction(n)).ToArray();
            duped[2] = new CodeInstruction(OpCodes.Ldc_I4_2);
            return matcher.InsertAndAdvance(duped)

                // cursed transpiler bug
                // https://github.com/BepInEx/HarmonyX/issues/65
                .End()
                .MatchBack(false, new CodeMatch(OpCodes.Leave))
                .Advance(1)
                .Insert(new CodeInstruction(OpCodes.Nop))

                .InstructionEnumeration();
#endif
        }

#if BEAT_SABER_1453
        private static bool IsEvent(BeatmapDataItem.BeatmapDataItemType type)
        {
            return type == BeatmapDataItem.BeatmapDataItemType.BeatmapEvent || type == (BeatmapDataItem.BeatmapDataItemType)2;
        }
#endif
    }
}
