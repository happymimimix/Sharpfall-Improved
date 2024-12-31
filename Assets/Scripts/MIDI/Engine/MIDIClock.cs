using System.Diagnostics;
using UnityEngine;

class MIDIClock
{
    public static Stopwatch test = new Stopwatch();
    public static double timee = -3d;
    public static ushort cppq = 0;
    public static double bpm = 120d;
    public static double ticklen = 0;
    public static double last = 0;
    public static bool render = false;
    public static bool throttle = true;
    public static double timeLost = 0;
    public static double startTime = 0;
    public static double lastReset = 0;
    public static double renLast = 0;
    public static void Start(float offset = 0f)
    {
        test.Start();
        timeLost = 0d;
        last = 0d;
        renLast = 0;
        startTime = test.ElapsedMilliseconds;
        ticklen = ((double)1 / (double)cppq) * ((double)60 / bpm);
        timee = -3d + ((double)offset);
        timee /= ticklen;
        lastReset = Time.realtimeSinceStartupAsDouble;
    }
    public static void Reset()
    {
        startTime = test.ElapsedMilliseconds;
        last = 0;
        timeLost = 0;
        bpm = 120d;
        renLast = 0;
        ticklen = ((double)1 / (double)cppq) * ((double)60 / bpm);
        timee = 0d;
        lastReset = Time.realtimeSinceStartupAsDouble;
    }
    public static double GetPassedTime()
    {
        return (double)(test.ElapsedMilliseconds - startTime) / 1000d;
    }
    public static double GetElapsed(bool upd)
    {
        double temp = ((double)GetPassedTime());
        if (render)
        {
            if (upd) {
                renLast += Time.fixedDeltaTime;
            }
            return renLast;
        } else
        {
            if (upd)
            {
                renLast += Time.deltaTime;
            }
        }
        if (throttle)
        {
            if (temp - last > (double)1d / 15d)
            {
                timeLost += (temp - last) - (double)1d / 15d;
                last = temp;
                return temp - timeLost;
            }
        }
        last = temp;
        return temp - timeLost;
    }
    public static void SubmitBPM(double pos, int b)
    {
        double remainder = (GetTick(false) - pos);
        timee = pos;
        double lastBPM = bpm;
        bpm = 60000000 / b;
        timeLost = 0;
        ticklen = ((double)1 / (double)cppq) * ((double)60 / bpm);
        timee += remainder * (lastBPM / bpm);
        startTime = test.ElapsedMilliseconds;
        renLast = 0d;
    }
    public static double GetTick(bool upd = true)
    {
        return timee + (GetElapsed(upd) / ticklen);
    }
}