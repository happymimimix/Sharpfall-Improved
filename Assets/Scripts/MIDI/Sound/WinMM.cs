using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

struct MidiOutCaps
{
    public ushort wMid;
    public ushort wPid;
    public uint vDriverVersion;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
    public string szPname;

    public ushort wTechnology;
    public ushort wVoices;
    public ushort wNotes;
    public ushort wChannelMask;
    public uint dwSupport;
}

struct MidiInCaps
{
    public ushort wMid;
    public ushort wPid;
    public uint vDriverVersion;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
    public string szPname;
    public uint dwSupport;
}

class WinMM
{
    public delegate void MidiInProc(IntPtr hMidiIn, uint wMsg, IntPtr dwInstance, uint dwParam1, uint dwParam2);

    [DllImport("winmm.dll")]
    public static extern int midiInGetNumDevs();
    [DllImport("winmm.dll")]
    public static extern int midiInGetDevCaps(int uDeviceID, ref MidiInCaps lpMidiInCaps, uint cbMidiInCaps);
    [DllImport("winmm.dll")]
    public static extern int midiInOpen(out IntPtr lphMidiIn, int uDeviceID, MidiInProc dwCallback, IntPtr dwInstance, int dwFlags);
    [DllImport("winmm.dll")]
    public static extern int midiInStart(IntPtr hMidiIn);
    [DllImport("winmm.dll")]
    public static extern int midiInStop(IntPtr hMidiIn);
    [DllImport("winmm.dll")]
    public static extern int midiInClose(IntPtr hMidiIn);

    [DllImport("winmm.dll")]
    private static extern int midiOutGetNumDevs();
    [DllImport("winmm.dll")]
    private static extern int midiOutGetDevCaps(int uDeviceID, ref MidiOutCaps lpMidiOutCaps, uint cbMidiOutCaps);
    [DllImport("winmm.dll")]
    static extern uint midiOutOpen(out IntPtr lphMidiOut, uint uDeviceID, IntPtr dwCallback, IntPtr dwInstance, uint dwFlags);
    [DllImport("winmm.dll")]
    public static extern uint midiOutClose(IntPtr hMidiOut);
    [DllImport("winmm.dll")]
    public static extern uint midiOutShortMsg(IntPtr hMidiOut, uint dwMsg);

    public static List<string> GetDevices()
    {
        List<string> list = new List<string>();
        int devices = midiOutGetNumDevs();
        for (uint i = 0; i < devices; i++)
        {
            MidiOutCaps caps = new MidiOutCaps();
            midiOutGetDevCaps((int)i, ref caps, (uint)Marshal.SizeOf(caps));
            list.Add(caps.szPname);
        }
        return list;
    }

    public static List<string> GetInputs()
    {
        List<string> list = new List<string>();
        int devices = midiInGetNumDevs();
        for (uint i = 0; i < devices; i++)
        {
            MidiInCaps caps = new MidiInCaps();
            midiInGetDevCaps((int)i, ref caps, (uint)Marshal.SizeOf(caps));
            list.Add(caps.szPname);
        }
        return list;
    }

    public static string GetInputNameByID(int id)
    {
        MidiInCaps caps = new MidiInCaps();
        midiInGetDevCaps(id, ref caps, (uint)Marshal.SizeOf(caps));
        return caps.szPname;
    }

    public static (bool, string, string, IntPtr?, MidiOutCaps?) Setup(string device)
    {
        int devices = midiOutGetNumDevs();
        if (devices == 0)
        {
            return (false, "None", "No WinMM devices were found!", null, null);
        }
        else
        {
            MidiOutCaps myCaps = new MidiOutCaps();
            midiOutGetDevCaps(0, ref myCaps, (uint)Marshal.SizeOf(myCaps));
            IntPtr handle;
            for (uint i = 0; i < devices; i++)
            {
                MidiOutCaps caps = new MidiOutCaps();
                midiOutGetDevCaps((int)i, ref caps, (uint)Marshal.SizeOf(caps));
                if (device == caps.szPname)
                {
                    midiOutOpen(out handle, i, (IntPtr)0, (IntPtr)0, (uint)0);
                    return (true, myCaps.szPname, "WinMM initialized!", handle, myCaps);
                }
            }
            return (false, "None", "Could not find the specified WinMM device.", null, null);
        }
    }
}