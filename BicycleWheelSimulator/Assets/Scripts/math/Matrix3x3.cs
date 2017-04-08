using System;

namespace math
{

    public struct Matrix3x3
    {
        Vector3D x;
        Vector3D y;
        Vector3D z;

        public Matrix3x3 ( Vector3D x, Vector3D y, Vector3D z )
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }



        static public Matrix3x3 zero { get { return new Matrix3x3( new Vector3D( 0, 0, 0 ), new Vector3D( 0, 0, 0 ), new Vector3D( 0, 0, 0 ) ); } }
        static public Matrix3x3 identity { get { return new Matrix3x3( new Vector3D( 1, 0, 0 ), new Vector3D( 0, 1, 0 ), new Vector3D( 0, 0, 1 ) ); } }

        public static Matrix3x3 operator * ( Matrix3x3 a, Matrix3x3 b )
        {
            Matrix3x3 result = new Matrix3x3();

            result.x.x = a.x.x * b.x.x + a.x.y * b.y.x + a.x.z * b.z.x;
            result.x.y = a.x.x * b.x.y + a.x.y * b.y.y + a.x.z * b.z.y;
            result.x.z = a.x.x * b.x.z + a.x.y * b.y.z + a.x.z * b.z.z;

            result.y.x = a.y.x * b.x.x + a.y.y * b.y.x + a.y.z * b.z.x;
            result.y.y = a.y.x * b.x.y + a.y.y * b.y.y + a.y.z * b.z.y;
            result.y.z = a.y.x * b.x.z + a.y.y * b.y.z + a.y.z * b.z.z;

            result.z.x = a.z.x * b.x.x + a.z.y * b.y.x + a.z.z * b.z.x;
            result.z.y = a.z.x * b.x.y + a.z.y * b.y.y + a.z.z * b.z.y;
            result.z.z = a.z.x * b.x.z + a.z.y * b.y.z + a.z.z * b.z.z;

            return result;
        }

        public static Vector3D operator * ( Matrix3x3 m, Vector3D v )
        {
            return new Vector3D( v.x * m.x.x + v.y * m.x.y + v.z * m.x.z,
                        v.x * m.y.x + v.y * m.y.y + v.z * m.y.z,
                        v.x * m.z.x + v.y * m.z.y + v.z * m.z.z );
        }

        static public Matrix3x3 fromVerticalVectors( Vector3D x, Vector3D y, Vector3D z )
        {
            Matrix3x3 result = new Matrix3x3();
            result.x = new Vector3D( x.x, y.x, z.x );
            result.y = new Vector3D( x.y, y.y, z.y );
            result.z = new Vector3D( x.z, y.z, z.z );
            return result;
        }

        public static Matrix3x3 getRotationMatrixLookAtYAxis ( Vector3D Y_axis_from, Vector3D Y_axis_to ) 
        {
            Vector3D X_axis = Y_axis_from.cross(Y_axis_to).normalized();
            Vector3D Z_axis = X_axis.cross(Y_axis_to).normalized();
            

            return fromVerticalVectors(X_axis, Y_axis_to, Z_axis);

        }

        public static Matrix3x3 getRotationMatrix ( Vector3D a1, double phi )
        {
            double c = Math.Cos(phi);
            double s = Math.Sin(phi);
            double t = 1.0 - c;

            Matrix3x3 r = new Matrix3x3();

            r.x.x = c + a1.x * a1.x * t;
            r.y.y = c + a1.y * a1.y * t;
            r.z.z = c + a1.z * a1.z * t;

            double tmp1 = a1.x*a1.y*t;
            double tmp2 = a1.z*s;

            r.y.x = tmp1 + tmp2;
            r.x.y = tmp1 - tmp2;

            tmp1 = a1.x * a1.z * t;
            tmp2 = a1.y * s;
            r.z.x = tmp1 - tmp2;
            r.x.z = tmp1 + tmp2;

            tmp1 = a1.y * a1.z * t;
            tmp2 = a1.x * s;
            r.z.y = tmp1 + tmp2;
            r.y.z = tmp1 - tmp2;

            return r;
        }
    }

}