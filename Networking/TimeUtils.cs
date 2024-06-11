using System;

public static class TimeUtils
{
    public static double GetTimeDeltaS(long laterTickstamp, long earlierTickstamp)
    {
        return ((double)(laterTickstamp - earlierTickstamp)) / TimeSpan.TicksPerSecond;
    }
}