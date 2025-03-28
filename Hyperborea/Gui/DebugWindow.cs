﻿using Dalamud.Memory;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using ECommons.Hooks;
using ECommons.SimpleGui;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Graphics.Environment;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using Lumina.Excel.Sheets;
using System.Windows.Forms;

namespace Hyperborea.Gui;
public unsafe class DebugWindow: Window
{
    public DebugWindow() : base("Hyperborea 调试")
    {
        EzConfigGui.WindowSystem.AddWindow(this);
    }

    int i1, i2, i3, i4;
    uint i5, i6, i7;

    public override void Draw()
    {
        ImGuiEx.EzTabBar("Tabs", [("Debug", DrawDebug, null, true)]);
    }

    void DrawZoneEditor()
    {
        var TerrID = Svc.ClientState.TerritoryType;
    }

    public override void OnClose()
    {
        P.SaveZoneData();
    }

    int[] ints = new int[100];
    uint[] d = new uint[100];
    long[] longs = new long[100];
    void DrawDebug()
    {

        {
            var l = LayoutWorld.Instance()->ActiveLayout;
            if(l != null)
            {
                ImGuiEx.Text($"{l->FestivalStatus:X8}");
                ImGuiEx.Text($"{l->ActiveFestivals[0]}");
                ImGuiEx.Text($"{l->ActiveFestivals[1]}");
                ImGuiEx.Text($"{l->ActiveFestivals[2]}");
                ImGuiEx.Text($"{l->ActiveFestivals[3]}");
                ImGui.InputInt("节日 0", ref ints[0]);
                ImGui.InputInt("节日 1", ref ints[1]);
                ImGui.InputInt("节日 2", ref ints[2]);
                ImGui.InputInt("节日 3", ref ints[3]);
                if (ImGui.Button("设置节日"))
                {
                    var s = stackalloc uint[] { (uint)ints[0], (uint)ints[1], (uint)ints[2], (uint)ints[3] };
                    l->SetActiveFestivals((FFXIVClientStructs.FFXIV.Client.Game.GameMain.Festival*)s);
                }
            }
        }

        ImGui.Checkbox($"无视所有限制", ref P.Bypass);
        if(ImGui.Button("Fill phases based on supported weather"))
        {
            if (P.Weathers.TryGetValue(Svc.ClientState.TerritoryType, out var weathers))
            {
                if (!(P.ZoneData.Data.TryGetValue(Utils.GetLayout(), out var level)))
                {
                    level = new()
                    {
                        Name = ExcelTerritoryHelper.GetName(Svc.ClientState.TerritoryType)
                    };
                    P.ZoneData.Data[Utils.GetLayout()] = level;
                }
                var i = 0u;
                level.Phases = [];
                foreach (var x in weathers)
                {
                    level.Phases.Add(new() { Weather = x, Name = $"阶段 {++i}" });
                }
                P.SaveZoneData();
                Notify.Info($"成功");
            }
            else
            {
                Notify.Error($"失败");
            }
        }
        if(ImGui.CollapsingHeader("Map effect"))
        {
            ImGuiEx.TextCopy($"Module: {Utils.GetMapEffectModule()}");
            ImGuiEx.TextCopy($"Address: {(((nint)EventFramework.Instance()) + 344):X16}");
            ImGui.InputInt("1", ref i1);
            ImGui.InputInt("2", ref i2);
            ImGui.InputInt("3", ref i3);
            if (ImGui.Button("Do"))
            {
                MapEffect.Delegate(Utils.GetMapEffectModule(), (uint)i1, (ushort)i2, (ushort)i3);
            }
            if (ImGui.Button("Do 1-i1"))
            {
                for (int i = 1; i <= i1; i++)
                {
                    MapEffect.Delegate(Utils.GetMapEffectModule(), (uint)i, (ushort)i2, (ushort)i3);
                }
            }
        }

        if (ImGui.CollapsingHeader("Weather"))
        {
            ImGui.InputInt("weather", ref i4);
            if(ImGui.Button("Set weather"))
            {
                var e = EnvManager.Instance();
                e->ActiveWeather = (byte)i4;
                e->TransitionTime = 0.5f;
            }
            var s = (int)*P.Memory.ActiveScene;
            if(ImGui.InputInt("scene", ref s))
            {
                *P.Memory.ActiveScene = (byte)s;
            }
        }

        if (ImGui.CollapsingHeader("Story"))
        {/*
            foreach (var x in Svc.Data.GetExcelSheet<Story>())
            {
                ImGuiEx.Text($"{x.RowId} {ExcelTerritoryHelper.GetName(x.LayerSetTerritoryType0?.Value?.RowId ?? 0, true)}:");
                for (int i = 0; i < x.LayerSet0.Length; i++)
                {
                    ImGuiEx.Text($"  LayerSet0: {i} = {x.LayerSet0[i]}");
                }
            }*/
        }

        if (ImGui.CollapsingHeader("Story1"))
        {/*
            foreach (var x in Svc.Data.GetExcelSheet<Story>())
            {
                ImGuiEx.Text($"{x.RowId} {ExcelTerritoryHelper.GetName(x.LayerSetTerritoryType1?.Value?.RowId ?? 0, true)}:");
                for (int i = 0; i < x.LayerSet1.Length; i++)
                {
                    ImGuiEx.Text($"  LayerSet1: {i} = {x.LayerSet1[i]}");
                }
            }*/
        }

    }
}
