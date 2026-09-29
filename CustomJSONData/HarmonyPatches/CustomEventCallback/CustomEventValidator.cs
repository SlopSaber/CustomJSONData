using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;

namespace CustomJSONData.HarmonyPatches
{
    [HarmonyPatch(typeof(BeatmapCallbacksController))]
    internal static class CustomEventValidator
    {
        [HarmonyTranspiler]
        [HarmonyPatch(nameof(BeatmapCallbacksController.ManualUpdate))]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
#if BEATMAP_CALLBACK_TYPE_IDS
            // Event callbacks bypass the object start-time filter. Keep the existing
            // branch and include custom events without copying IL or exception labels.
            return new CodeMatcher(instructions)
                .MatchForward(
                    false,
                    new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(BeatmapDataItem), nameof(BeatmapDataItem.type))),
                    new CodeMatch(OpCodes.Ldc_I4_1),
                    new CodeMatch(instruction => instruction.opcode == OpCodes.Beq || instruction.opcode == OpCodes.Beq_S))
                .ThrowIfInvalid("Could not find the event callback filter")
                .Set(OpCodes.Call, AccessTools.Method(typeof(CustomEventValidator), nameof(IsEvent)))
                .InstructionEnumeration();
#else
            CodeMatcher matcher = new CodeMatcher(instructions)
                .MatchForward(
                    false,
                    new CodeMatch(OpCodes.Ldloc_S),
#if !PRE_V1_40_8
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

#if BEATMAP_CALLBACK_TYPE_IDS
        private static bool IsEvent(BeatmapDataItem item)
        {
            return (int)item.type == 1 || item is CustomBeatmap.CustomEventData;
        }
#endif
    }
}
