using System;

// 합산 전에 64비트로 계산하고 표현 한계에서도 부호가 뒤집히지 않게 한다.
public static class MoneyMath
{
    public static long Add(long a, long b)
    {
        if (b > 0 && a > long.MaxValue - b) return long.MaxValue;
        if (b < 0 && a < long.MinValue - b) return long.MinValue;
        return a + b;
    }

    public static long Round(double value)
    {
        if (double.IsNaN(value)) return 0;
        if (value >= long.MaxValue) return long.MaxValue;
        if (value <= long.MinValue) return long.MinValue;
        return (long)Math.Round(value, MidpointRounding.ToEven);
    }

    public static long Ceiling(double value) => Round(Math.Ceiling(value));
    public static long Floor(double value) => Round(Math.Floor(value));
    public static int Int(double value) => (int)Math.Clamp(Round(value), int.MinValue, int.MaxValue);
}
