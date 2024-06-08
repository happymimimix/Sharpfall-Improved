using System;
using UnityEngine;
using System.Runtime.CompilerServices;
using Unity.VisualScripting.FullSerializer;

class Sound
{
    public static int engine = 1;
    public static int threshold = 16;
    public static long totalEvents = 0;
    public static string lastWinMMDevice = "";
    private static IntPtr? handle;
    public static Func<uint, uint> sendTo = stKDMAPI;
    static uint stWinMM(uint ev)
    {
        return WinMM.midiOutShortMsg((IntPtr)handle, ev);
    }
    static uint stKDMAPI(uint ev)
    {
        return KDMAPI.SendDirectData(ev);
    }
    public static bool Init(int synth, string winMMdev)
    {
        Close();
        switch (synth)
        {
            case 1:
                bool KDMAPIAvailable = false;
                try { KDMAPIAvailable = KDMAPI.IsKDMAPIAvailable(); } catch (DllNotFoundException) { }
                if (KDMAPIAvailable)
                {
                    int loaded = KDMAPI.InitializeKDMAPIStream();
                    if (loaded == 1)
                    {
                        engine = 1;
                        sendTo = stKDMAPI;
                        return true;
                    }
                    else { UIHandler.AudioLoadError = "KDMAPI did not initialize."; return false; }
                }
                else { UIHandler.AudioLoadError = "KDMAPI is not available."; return false; }
            case 2:
                (bool, string, string, IntPtr?, MidiOutCaps?) result = WinMM.Setup(winMMdev);
                if (!result.Item1)
                {
                    UIHandler.AudioLoadError = result.Item3;
                    return false;
                }
                else
                {
                    engine = 2;
                    sendTo = stWinMM;
                    handle = result.Item4;
                    lastWinMMDevice = winMMdev;
                    return true;
                }
            default:
                return false;
        }
    }
    static ulong[] noteOffs = new ulong[16];
    public static void Reload()
    {
        for (int i = 0; i < noteOffs.Length; i++)
        {
            noteOffs[i] = 0;
        }
        Close(false);
        switch (engine)
        {
            case 1:
                KDMAPI.InitializeKDMAPIStream();
                return;
            case 2:
                (bool, string, string, IntPtr?, MidiOutCaps?) result = WinMM.Setup(lastWinMMDevice);
                handle = result.Item4;
                return;
        }
    }
    public static void Submit(int ev)
    {
        if (engine != 0)
        {
            sendTo((uint)ev);
            totalEvents++;
        }
    }
    public static void Close(bool clear = true)
    {
        switch (engine)
        {
            case 1:
                KDMAPI.TerminateKDMAPIStream();
                break;
            case 2:
                if (handle != null)
                {
                    WinMM.midiOutClose((IntPtr)handle);
                }
                break;
        }
        if (clear)
        {
            engine = 0;
        }
    }

    void OnApplicationQuit()
    {
        Close();
    }
}