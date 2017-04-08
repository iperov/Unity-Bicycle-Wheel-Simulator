namespace sys
{
    class Random
    {
        static System.Random m_Random = new System.Random();

        static public int range ( int min, int max )
        {
             return m_Random.Next( min, max );
        }

        static public double range (double min, double max)
        { 
            return m_Random.NextDouble() * (max - min) + min;
        }

        static public float range (float min, float max)
        {
            return (float) (m_Random.NextDouble() * (max - min) + min);
        }

        static public int sign()
        {          
            return (range( 0, 2 ) == 0 ? -1 : 1);
        }
    }
}