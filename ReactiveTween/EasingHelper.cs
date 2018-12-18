using System;
using System.Collections.Generic;
using System.Text;

namespace ReactiveTween
{
    public static class EasingHelper
    {
        public const float Pi     = (float)Math.PI;
        public const float HalfPi = (float)(Math.PI / 2);

        public static float Lerp(double from, double to, double step)
        {
            return (float)((to - from) * step + from);
        }

        public static float SLerp(double from, double to, double step)
        {
            return (float)((to - from) * Math.Sin(HalfPi*step) + from);
        }

    }
}
