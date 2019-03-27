﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Harmony;
using Overload;

namespace GameMod.Core
{
    public class GameMod
    {
        internal static void Initialize()
        {
            //HarmonyInstance.DEBUG = true;
            var harmony = HarmonyInstance.Create("olmod.olmod");
            try {
                harmony.PatchAll(Assembly.GetExecutingAssembly());
            } catch (Exception ex) {
                Debug.Log(ex.ToString());
            }
        }

        // enable monsterball mode, allow max players up to 16
        [HarmonyPatch(typeof(Overload.MenuManager), "MpMatchSetup")]
        class MBModeSelPatch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                int n = 0;
                var codes = new List<CodeInstruction>(instructions);
                for (var i = 0; i < codes.Count; i++)
                {
                    // increase max mode to allow monsterball mode
                    if (codes[i].opcode == OpCodes.Ldsfld && (codes[i].operand as FieldInfo).Name == "mms_mode")
                    {
                        i++;
                        if (codes[i].opcode == OpCodes.Ldc_I4_2)
                            codes[i].opcode = OpCodes.Ldc_I4_3;
                        i++;
                        while (codes[i].opcode == OpCodes.Add || codes[i].opcode == OpCodes.Ldsfld)
                            i++;
                        if (codes[i].opcode == OpCodes.Ldc_I4_2)
                            codes[i].opcode = OpCodes.Ldc_I4_3;
                        n++;
                    }
                    if (codes[i].opcode == OpCodes.Ldsfld && (codes[i].operand as FieldInfo).Name == "mms_max_players" &&
                        i > 0 && codes[i - 1].opcode == OpCodes.Br) // take !online branch
                    {
                        while (codes[i].opcode == OpCodes.Add || codes[i].opcode == OpCodes.Ldsfld)
                            i++;
                        if (codes[i].opcode == OpCodes.Ldc_I4_1 && codes[i + 1].opcode == OpCodes.Ldc_I4_8) {
                            codes[i + 1].opcode = OpCodes.Ldc_I4;
                            codes[i + 1].operand = 16;
                        }
                        n++;
                    }
                }
                Debug.Log("Patched MpMatchSetup n=" + n);
                return codes;
            }
        }

        // add modified indicator to main menu
        [HarmonyPatch(typeof(Overload.UIElement), "DrawMainMenu")]
        class VersionPatch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);
                for (var i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldstr && codes[i].operand as string == "VERSION {0}.{1} BUILD {2}")
                    {
                        codes[i].operand = "VERSION {0}.{1} BUILD {2} MOD";
                    }
                }
                return codes;
            }

            static void Postfix(UIElement __instance)
            {
                Vector2 pos = new Vector2(UIManager.UI_RIGHT - 10f, -155f - 60f + 50f + 40f);
                __instance.DrawStringSmall("UNOFFICIAL MODIFIED VERSION!", pos,
                    0.35f, StringOffset.RIGHT, UIManager.m_col_ui1, 0.5f, -1f);
            }
        }

        // add monsterball mb_arena1 level to multiplayer levels
        [HarmonyPatch(typeof(Overload.GameManager), "ScanForLevels")]
        class MBLevelPatch
        {
            static bool SLInit = false;
            static void Prefix()
            {
                if (SLInit)
                    return;
                SLInit = true;
                Overload.GameManager.MultiplayerMission.AddLevel("mb_arena1", "ARENA", "TITAN_06", new int[]
                            {
                                1,
                                4,
                                2,
                                8
                            });
            }
        }
    }
}
