using System;
using UnityEngine;

public static class NumberFormatter
{
    private static readonly string[] Suffixes = { "", "k", "M", "B", "T", "Q", "Qi", "Sx", "Sp", "Oc", "No", "Dc" };

    public static string Format(double value)
    {
        if (value < 0) return "-" + Format(-value);
        if (value < 1000)
        {
            // For small numbers (like production rates 0.1/s), we want to see decimals.
            // For larger numbers (like 100, 999), we stick to integers.
            if (value < 10) return value.ToString("0.##");
            return Math.Floor(value).ToString("F0");
        }

        int suffixIndex = 0;
        double displayValue = value;

        while (displayValue >= 1000 && suffixIndex < Suffixes.Length - 1)
        {
            displayValue /= 1000;
            suffixIndex++;
        }

        // Logic: 
        // if mantissa < 100: one decimal place (e.g. 1.2k, 12.5k)
        // if mantissa >= 100: no decimal place (e.g. 125k)
        if (displayValue < 100)
        {
            return $"{displayValue:F1}{Suffixes[suffixIndex]}";
        }
        else
        {
            return $"{displayValue:F0}{Suffixes[suffixIndex]}";
        }
    }

    public static string Format(long value)
    {
        return Format((double)value);
    }

    /// <summary>
    /// Formats numbers to always be integers for small values (checking user request for HP).
    /// </summary>
    public static string FormatNoDecimals(double value)
    {
        if (value < 1000)
        {
            return Math.Floor(value).ToString("F0");
        }
        return Format(value);
    }

    public static string FormatNoDecimals(long value)
    {
        return FormatNoDecimals((double)value);
    }
}
