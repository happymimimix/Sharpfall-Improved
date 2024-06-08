using System;

public static class InputManager
{
    public static bool loaded = false;

    static IntPtr hMidiIn;

    private static void MidiInCallback(IntPtr hMidiIn, uint wMsg, IntPtr dwInstance, uint dwParam1, uint dwParam2)
    {
        // Handle MIDI input messages here
        if (wMsg == 0x3C3) // MIM_DATA
        {
            byte status = (byte)(dwParam1 & 0xFF);
            byte data1 = (byte)((dwParam1 >> 8) & 0xFF);
            byte data2 = (byte)((dwParam1 >> 16) & 0xFF);

            //Debug.Log($"MIDI Message: Status={status:X2} Data1={data1:X2} Data2={data2:X2}");

            Sound.Submit((status | (data1 << 8) | (data2 << 16)));

            int type = (status & 0b11110000);

            switch (type)
            {
                case 0x90:
                    NoteManager.Submit(data1, 0, 0);
                    break;
                case 0xB0:
                    ControlPointer.SubmitControl(data1, data2);
                    ControlHandler.instance.SubmitControl(data1, data2);
                    break;
            }
        }
    }

    private const int MMSYSERR_NOERROR = 0;
    private const int CALLBACK_FUNCTION = 0x00030000;

    public static (bool,string) LoadDeviceID(int id)
    {
        if(loaded)
        {
            return (false, "Cannot load multiple devices.");
        }
        loaded = false;
        WinMM.MidiInProc callback = new WinMM.MidiInProc(MidiInCallback);
        int result = WinMM.midiInOpen(out hMidiIn, id, callback, IntPtr.Zero, CALLBACK_FUNCTION);
        if(result != MMSYSERR_NOERROR)
        {
            return (false, "Failed to open MIDI input device.");
        }
        result = WinMM.midiInStart(hMidiIn);
        if (result != MMSYSERR_NOERROR)
        {
            return (false,"Failed to start MIDI input.");
        }
        loaded = true;
        return (true,"");
    }

    public static void Disconnect()
    {
        if(loaded)
        {
            WinMM.midiInStop(hMidiIn);
            WinMM.midiInClose(hMidiIn);
            hMidiIn = IntPtr.Zero;
            loaded = false;
        }
    }
}
