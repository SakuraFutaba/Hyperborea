using ECommons.ExcelServices;
using ECommons.EzHookManager;
using FFXIVClientStructs.FFXIV.Client.Graphics.Environment;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using Lumina.Excel.Sheets;

namespace Hyperborea;
public unsafe class Memory
{
    internal delegate nint LoadZone(nint a1, uint a2, int a3, byte a4, byte a5, byte a6);
    [EzHook("40 55 56 57 41 56 41 57 48 83 EC 50 48 8B F9", false)]
    internal EzHook<LoadZone> LoadZoneHook;

    internal delegate byte PacketDispatcher_OnSendPacket(nint a1, nint a2, nint a3, byte a4);
    [EzHook("48 89 5C 24 ?? 48 89 74 24 ?? 4C 89 64 24 ?? 55 41 56 41 57 48 8B EC 48 83 EC 70", false)]
    internal EzHook<PacketDispatcher_OnSendPacket> PacketDispatcher_OnSendPacketHook;

    internal delegate nint TargetSystem_InteractWithObject(nint a1, nint a2, byte a3);
    [EzHook("48 89 5C 24 ?? 48 89 6C 24 ?? 56 48 83 EC 20 48 8B E9 41 0F B6 F0", false)]
    internal EzHook<TargetSystem_InteractWithObject> TargetSystem_InteractWithObjectHook;

    internal delegate nint SetupTerritoryTypeDelegate(void* EventFramework, ushort territoryType);
    internal SetupTerritoryTypeDelegate SetupTerritoryType = EzDelegate.Get<SetupTerritoryTypeDelegate>("48 89 5C 24 ?? 48 89 7C 24 ?? 41 56 48 83 EC ?? 48 8B D9 48 89 6C 24");

    internal delegate nint SetupInstanceContent(nint a1, uint a2, uint a3, uint a4);
    [EzHook("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? E8 ?? ?? ?? ?? 8B 54 24 70 48 8B C8 E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? E8 ?? ?? ?? ?? 0F B6 54 24", true)]
    internal EzHook<SetupInstanceContent> SetupInstanceContentHook;

    internal delegate byte FinalizeInstanceContent(nint a1, uint a2);
    [EzHook("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 70 48 8D B1", false)]
    internal EzHook<FinalizeInstanceContent> FinalizeInstanceContentHook;

    internal delegate nint IsFlightProhibited();
    [EzHook("40 53 48 83 EC 20 48 8B 1D ?? ?? ?? ?? 48 85 DB 0F 84 ?? ?? ?? ?? 80 3D", false)]
    internal EzHook<IsFlightProhibited> IsFlightProhibitedHook;

    internal byte* ActiveScene;

    private static int HeartbeatUpOpcode;
    private static int? HeartbeatDownOpcode;
    private static int? ActorControlSelfOpcode;

    public Memory()
    {
        HeartbeatUpOpcode = Marshal.ReadInt32(Svc.SigScanner.ScanText("C7 44 24 ?? ?? ?? ?? ?? 48 F7 F1") + 0x4);

        var opcodeMapAddr = Svc.SigScanner.ScanText("49 8B 40 10  4C 8B 50 38  41 0F B7 42 02  83 C0 ??  3D ?? ?? ?? ??  0F 87 ?? ?? ?? ??  4C 8D 1D ?? ?? ?? ??  48 98  45 8B 8C 83 ?? ?? ?? ??");
        var heartbeatDownAddr = Svc.SigScanner.ScanText("48 8B 01 4D 8D 4A 10 48 FF 60 20");
        var actorControlSelfAddr = Svc.SigScanner.ScanText("48 8B 01 4D 8D 4A 10 48 FF A0 18 07 00 00");
        var minCase = -(sbyte)Marshal.ReadByte(opcodeMapAddr + 15);
        var jumpTableSize = Marshal.ReadInt32(opcodeMapAddr + 17) + 1;
        var imagebaseAddr = opcodeMapAddr + 30 + 4 + Marshal.ReadInt32(opcodeMapAddr + 30);
        var jumpTableRVA = Marshal.ReadInt32(opcodeMapAddr + 40);
        var jumpTableAddr = imagebaseAddr + jumpTableRVA;
        for (var i = 0; i < jumpTableSize; i++)
        {
            var caseBodyAddrOffset = Marshal.ReadInt32(jumpTableAddr + 4 * i);
            if (caseBodyAddrOffset == heartbeatDownAddr - imagebaseAddr)
            {
                HeartbeatDownOpcode ??= i + minCase;
            }
            if (caseBodyAddrOffset == actorControlSelfAddr - imagebaseAddr)
            {
                ActorControlSelfOpcode ??= i + minCase;
            }
            if (HeartbeatDownOpcode.HasValue && ActorControlSelfOpcode.HasValue) 
                break;
        }

        var logMessageHeartbeatDown = HeartbeatDownOpcode.HasValue 
            ? $"[HyperFirewall] Found Opcode HeartbeatDown:{HeartbeatDownOpcode}" 
            : "[HyperFirewall] Error: Couldn't find Opcode HeartbeatDown";
        var logMessageActorControlSelf = ActorControlSelfOpcode.HasValue 
            ? $"[HyperFirewall] Found Opcode ActorControlSelf:{ActorControlSelfOpcode}" 
            : "[HyperFirewall] Error: Couldn't find Opcode ActorControlSelf";
        
        PluginLog.Debug(logMessageHeartbeatDown);
        PluginLog.Debug(logMessageActorControlSelf);
        
        EzSignatureHelper.Initialize(this);
        ActiveScene = (byte*)(((nint)EnvManager.Instance()) + 36);
    }

    internal nint IsFlightProhibitedDetour()
    {
        try
        {
            if (P.Enabled && C.ForcedFlight) return 0;
        }
        catch(Exception e)
        {
            e.Log();
        }
        return IsFlightProhibitedHook.Original();
    }

    private byte FinalizeInstanceContentDetour(nint a1, uint a2)
    {
        PluginLog.Debug($"FinalizeInstanceContentDetour: {a2:X8}");
        return FinalizeInstanceContentHook.Original(a1, a2);
    }

    private nint SetupInstanceContentDetour(nint a1, uint a2, uint a3, uint a4)
    {
        try
        {
            PluginLog.Debug($"SetupInstanceContentDetour: {a2:X8}, {a3}, {a4}");
            var l = LayoutWorld.Instance()->ActiveLayout;
            if (l != null)
            {
                var obj = l->TerritoryTypeId;
                var level = Svc.Data.GetExcelSheet<TerritoryType>().GetRowOrDefault(obj)?.Bg.ExtractText();
                if (!level.IsNullOrEmpty())
                {
                    if (Utils.TryGetZoneInfo(level, out var info))
                    {
                        P.SaveZoneData();
                    }
                }
            }
        }
        catch(Exception e)
        {
            e.Log();
        }
        return SetupInstanceContentHook.Original(a1, a2, a3, a4);
    }

    private nint TargetSystem_InteractWithObjectDetour(nint a1, nint a2, byte a3)
    {
        return 0;
    }

    public void EnableFirewall()
    {
        PacketDispatcher_OnSendPacketHook.Enable();
        IsFlightProhibitedHook?.Enable();
    }

    public void DisableFirewall()
    {
        PacketDispatcher_OnSendPacketHook.Pause();
        IsFlightProhibitedHook?.Pause();
    }

    public bool IsFirewallEnabled => PacketDispatcher_OnSendPacketHook.IsEnabled;

    private byte PacketDispatcher_OnSendPacketDetour(nint a1, nint a2, nint a3, byte a4)
    {
        const byte DefaultReturnValue = 1;

        if (a2 == IntPtr.Zero)
        {
            PluginLog.Error("[HyperFirewall] Error: Opcode pointer is null.");
            return DefaultReturnValue;
        }

        try
        {
            var opcode = *(ushort*)a2;
            if ((ushort)HeartbeatUpOpcode == opcode)
            {
                PluginLog.Verbose($"[HyperFirewall] Passing outgoing packet with opcode {opcode} through.");
                return PacketDispatcher_OnSendPacketHook.Original(a1, a2, a3, a4);
            }
            else
            {
                PluginLog.Verbose($"[HyperFirewall] Suppressing outgoing packet with opcode {opcode}.");
            }
        }
        catch (Exception e)
        {
            PluginLog.Error($"[HyperFirewall] Exception caught while processing opcode: {e.Message}");
            e.Log();
            return DefaultReturnValue;
        }

        return DefaultReturnValue;
    }

    internal nint LoadZoneDetour(nint a1, uint a2, int a3, byte a4, byte a5, byte a6)
    {
        try
        {
            PluginLog.Debug($"Loading {ExcelTerritoryHelper.GetName(a2, true)}, {a3}, {a4}, {a5}, {a6}");
        }
        catch(Exception e)
        {
            e.Log();
        }
        return LoadZoneHook.Original(a1, a2, a3, a4, a5, a6);
    }
}
