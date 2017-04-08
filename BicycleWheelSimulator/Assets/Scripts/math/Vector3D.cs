using System;
using UnityEngine;

namespace math
{
    public struct Vector3D
    {
        public double x;
        public double y;
        public double z;

        public Vector3D ( double x, double y, double z )
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Vector3D ( float x, float y, float z )
        {
            this.x = (double) x;
            this.y = (double) y;
            this.z = (double) z;
        }

        public Vector3D ( Vector3 v3 )
        {
            this.x = (double) v3.x;
            this.y = (double) v3.y;
            this.z = (double) v3.z;
        }

        public Vector3D ( double x, double y )
        {
            this.x = x;
            this.y = y;
            this.z = 0d;
        }

        public void set ( double new_x, double new_y, double new_z )
        {
            this.x = new_x;
            this.y = new_y;
            this.z = new_z;
        }

        public void normalize ()
        {
            double num = length();
            if ( num > 1.0E-16 )
                this = this / num;
            else
                this = Vector3D.zero;
        }

        public Vector3D normalized ()
        {
            double num = length();
            if ( num > 1.0E-16 )
                return new Vector3D( x / num, y / num, z / num );
            else
                return Vector3D.zero;
        }

        public double length () { return Math.Sqrt( this.x * this.x + this.y * this.y + this.z * this.z ); }
        public double length2 () { return this.x * this.x + this.y * this.y + this.z * this.z; }
        public double dot ( Vector3D b ) { return x * b.x + y * b.y + z * b.z; }
        public Vector3D cross ( Vector3D b ) { return new Vector3D( y * b.z - z * b.y, z * b.x - x * b.z, x * b.y - y * b.x ); }

        //1,0,0 -> 0, 1, -1

        public Vector3 toVector3() { return new Vector3( (float) x, (float) y, (float) z ); }
        override public string ToString() { return string.Format("{0},{1},{2}", x, y, z);  }
        public double distance ( Vector3D b )
        {
            double _x = x - b.x;
            double _y = y - b.y;
            double _z = z - b.z;

            return Math.Sqrt( _x * _x + _y * _y + _z * _z );
        }

        public static Vector3D operator + ( Vector3D a, Vector3D b ) { return new Vector3D( a.x + b.x, a.y + b.y, a.z + b.z ); }
        public static Vector3D operator - ( Vector3D a, Vector3D b ) { return new Vector3D( a.x - b.x, a.y - b.y, a.z - b.z ); }
        public static Vector3D operator - ( Vector3D a ) { return new Vector3D( -a.x, -a.y, -a.z ); }
        public static Vector3D operator * ( Vector3D a, double d ) { return new Vector3D( a.x * d, a.y * d, a.z * d ); }
        public static Vector3D operator * ( double d, Vector3D a ) { return new Vector3D( a.x * d, a.y * d, a.z * d ); }
        public static Vector3D operator / ( Vector3D a, double d ) { return new Vector3D( a.x / d, a.y / d, a.z / d ); }

        public static bool operator < ( Vector3D a, Vector3D b ) { return (a.x < b.x & a.y < b.y & a.z < b.z ); }
        public static bool operator > ( Vector3D a, Vector3D b ) { return (a.x > b.x & a.y > b.y & a.z > b.z ); }
        public static bool operator <= ( Vector3D a, Vector3D b ) { return (a.x <= b.x & a.y <= b.y & a.z <= b.z ); }
        public static bool operator >= ( Vector3D a, Vector3D b ) { return (a.x >= b.x & a.y >= b.y & a.z >= b.z ); }

        public static Vector3D zero { get { return new Vector3D( 0d, 0d, 0d ); } }
        public static Vector3D one { get { return new Vector3D( 1d, 1d, 1d ); } }

        public static double angleBetween ( Vector3D a, Vector3D b )
        {
            return Math.Acos( a.dot( b ) / (a.length() * b.length()) );
        }

    }

}