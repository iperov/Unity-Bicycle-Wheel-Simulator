using com.cont;
using UnityEngine;

namespace math
{
    public class Points3D
    {
        TArray<Vector3D> m_points = new TArray<Vector3D>();

        public int points_count { get { return m_points.count; } }

        public void addPoint ( Vector3D p )
        {
            m_points.add( p );
        }

        public Vector3D this [ int index ]
        {
            get { return m_points[index]; }
            set { m_points[index] = value; }
        }

        public void move (Vector3D offset)
        {
            for ( int i = 0; i < m_points.count; ++i )
            {
                m_points[i] = m_points[i] + offset;
            }
        }

        public void rotate ( Matrix3x3 rot, Vector3D center )
        {
            for (int i=0; i < m_points.count; ++i)
            {
                 m_points[i] = center + rot * (m_points[i] - center);
            }
        }

        public void rotate (Vector3D axis, double angle_rad, Vector3D center)
        {
            Matrix3x3 rot = Matrix3x3.getRotationMatrix( axis, angle_rad );

            rotate( rot, center );
        }

        public double getWireFullLoopLength()
        {
            double length = 0;

            for ( int i = 0; i < m_points.count; ++i )
            {
                length += m_points[i].distance( m_points[(i + 1) % m_points.count] );
            }

            return length;
        }

        public Vector3[] toVector3Array()
        {
            Vector3D[] ar = m_points.getArray();

            Vector3[] result = new Vector3[m_points.count];
            for ( int i = 0; i < m_points.count; ++i )
                result[i] = ar[i].toVector3();

            return result;
        }

        public Points3D copy()
        {
            return new Points3D() { m_points = m_points.copy_shallow() };
        }
    }
}
