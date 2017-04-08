namespace math
{
    public static class MathEx
    {     
        public static double lerp (double a, double b, double t)
        {
            return a * (1.0 - t) + b * t;
        }        
    }
}