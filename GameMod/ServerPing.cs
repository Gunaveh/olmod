﻿using Harmony;
using Overload;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;

namespace GameMod
{
    [HarmonyPatch(typeof(BroadcastState), "Tick")]
    class ServerPingTick
    {
        private static void ProcessPing(byte[] packetData, IPEndPoint senderEndPoint, UdpClient client)
        {
            byte[] outBuf = new byte[packetData.Length];
            Array.Copy(packetData, outBuf, packetData.Length);

            // calculate incoming hash
            Array.Copy(BitConverter.GetBytes(0), 0, outBuf, 8, 4);
            uint srcHash = xxHashSharp.xxHash.CalculateHash(outBuf);

            if (srcHash != BitConverter.ToUInt32(packetData, 8)) // ignore packet with invalid hash
                return;

            Array.Copy(BitConverter.GetBytes((int)-2), 0, outBuf, 0, 4);
            if (outBuf.Length >= 19 + 4)
                Array.Copy(BitConverter.GetBytes(0), 0, outBuf, 19, 4); // version
            if (outBuf.Length >= 19 + 4 + 4)
                Array.Copy(BitConverter.GetBytes((int)NetworkMatch.NetSystemGetStatus()), 0, outBuf, 19 + 4, 4); // status

            // calculate outgoing hash
            Array.Copy(BitConverter.GetBytes(0), 0, outBuf, 8, 4);
            uint hash = xxHashSharp.xxHash.CalculateHash(outBuf);
            Array.Copy(BitConverter.GetBytes(hash), 0, outBuf, 8, 4);

            client.Send(outBuf, outBuf.Length, senderEndPoint);
        }

        public static int CheckPing(int packetType, byte[] packetData, IPEndPoint senderEndPoint, UdpClient client) // packetType is actually an enum but it's private so use int here
        {
            if (BitConverter.ToInt32(packetData, 0) == -1) // seqnum -1 = ping packet
            {
                ProcessPing(packetData, senderEndPoint, client);
                return 0;
            }
            return packetType;
        }

        private static bool Prepare()
        {
            return MPInternet.ServerEnabled;
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codes)
        {
            object lastLdloc = 0;
            foreach (var code in codes)
            {
                if (code.opcode == OpCodes.Ldloc_S)
                    lastLdloc = code.operand;
                else if (code.opcode == OpCodes.Call && ((MemberInfo)code.operand).Name == "ClassifyPacket")
                {
                    yield return code; // call ClassifyPacket
                    // returned packetType still on stack
                    yield return new CodeInstruction(OpCodes.Ldloc_S, lastLdloc); // this is byte[] packet, since also passed to ClassifyPacket
                    yield return new CodeInstruction(OpCodes.Ldloc_S, ((LocalBuilder)lastLdloc).LocalIndex - 1); // this should be IPEndPoint senderEndPoint
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, typeof(BroadcastState).GetField("m_receiveClient", BindingFlags.NonPublic | BindingFlags.Instance));
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ServerPingTick), "CheckPing"));
                    continue;
                }
                yield return code;
            }
        }

    }
}
