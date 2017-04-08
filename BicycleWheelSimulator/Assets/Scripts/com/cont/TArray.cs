using System;

namespace com.cont
{
    public class TArray<T>
    {
        T[] m_array;
        int m_count;
        int m_max;

        public TArray()
        {

        }

        public int count { get { return m_count; } }

        public void add( T value )
        {
            if (m_array == null || m_count >= m_max)
            {
                if ( m_max == 0 )
                    m_max = 1;
                m_max *= 2;

                T[] new_array = new T[m_max];
                if ( m_count > 0 )
                    Array.Copy( m_array, 0, new_array, 0, m_count );

                m_array = new_array;
            }

            m_array[m_count++] = value;
        }

        public T[] getArray() { return m_array; }

        public T this[int idx]
        {
            get { return m_array[idx]; }
            set { m_array[idx] = value; }
        }
        
        public TArray<T> copy_shallow()
        {
            TArray<T> result = new TArray<T>();
            result.m_array = new T[m_max];
            Array.Copy( m_array, 0, result.m_array, 0, m_count );
            result.m_count = m_count;
            result.m_max = m_max;

            return result;
        }
    }
}
