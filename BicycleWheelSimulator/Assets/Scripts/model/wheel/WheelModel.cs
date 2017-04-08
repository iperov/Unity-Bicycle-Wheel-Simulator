using math;
using sys.debug;
using System;
using UnityEngine;

namespace model.wheel
{
    public class WheelModel
    {
        public struct SpokeData
        {
            public double default_len;
            public double tightened_len;
            public double rev;

            public Vector3D hub_local_point;
            public Vector3D rim_local_point;

            public double F;
            public double F_prev;
            public double TightFactor;
            public double RimBentF;
            public double RimBentFAbsolute;
            public double RimEggF;
            public double RimEggFAbsolute;

            public Vector3D rim_move_vec;
            public Vector3D rim_torque_axis;
            public double rim_torque_angle;


        };

        SpokeData[] m_spokes_data;

        static double m_total_model_modifier = 1;
        static double N_to_kgf = (1 / 9.8) / m_total_model_modifier;

        int m_spokes_count; public int spokes_count { get { return m_spokes_count; } }
        int m_cross_count; public int cross_count { get { return m_cross_count; } }
        double m_rim_d; public double rim_d { get { return m_rim_d; } }
        double m_hub_d; public double hub_d { get { return m_hub_d; } }
        double m_axle_length; public double axle_length { get { return m_axle_length; } }
        double m_l_dish; public double l_dish { get { return m_l_dish; } }
        double m_r_dish; public double r_dish { get { return m_r_dish; } }
        double m_hub_fl_thickness; public double fl_thickness { get { return m_hub_fl_thickness; } }
        int m_spoke_tpi; public int spoke_tpi { get { return m_spoke_tpi; } }
        double m_spoke_diameter; public double spoke_diameter { get { return m_spoke_diameter; } }
        double m_spoke_young_modulus; public double spoke_young_modulus { get { return m_spoke_young_modulus; } }
        double m_spoke_density;
        double m_spoke_space;

        Vector3D m_rim_pos;
        Matrix3x3 m_rim_rot;

        public double m_max_f_diff;


        public Vector3D get_hub_pos () { return Vector3D.zero; }
        public Vector3D get_hub_point ( int n_spoke ) { return m_spokes_data[n_spoke].hub_local_point; }
        public Vector3D get_nipple_point ( int n_spoke ) { return m_rim_pos + m_rim_rot * m_spokes_data[n_spoke].rim_local_point; }

        public Vector3D get_rim_center () { return m_rim_pos; }
        public Matrix3x3 get_rim_rot () { return m_rim_rot; }
        public Vector3D get_rim_cross_axis ()
        {
            return (get_nipple_point( 0 ) - get_nipple_point( 1 )).cross(
                   (get_nipple_point( 2 ) - get_nipple_point( 1 )) ).normalized();
        }


        public double getNippleRotate ( int n_spoke ) { return m_spokes_data[n_spoke].rev; }
        public double getSpokeForceN ( int n_spoke ) { return m_spokes_data[n_spoke].F; }
        public double getSpokeForceKgf ( int n_spoke ) { return getSpokeForceN( n_spoke ) * N_to_kgf; }
        public double getSpokeTightenedLength ( int n_spoke ) { return m_spokes_data[n_spoke].tightened_len; }
        public double getSpokeUnitMass ()
        {
            return m_spoke_density * m_spoke_space;
        }

        public double getSpokeForceKgfSideDisbalance ( bool is_right_side )
        {
            double lowest_kgf = double.MaxValue;
            double highest_kgf = double.MinValue;
            for ( int i = 0; i < m_spokes_count / 2; ++i )
            {
                double kgf = getSpokeForceKgf(i*2 + (is_right_side ? 1:0) );
                lowest_kgf = Math.Min( lowest_kgf, kgf );
                highest_kgf = Math.Max( highest_kgf, kgf );
            }
            double mid_kgf = (lowest_kgf + highest_kgf) / 2;
            double result = 0;
            for ( int i = 0; i < m_spokes_count / 2; ++i )
            {
                double kgf = getSpokeForceKgf(i*2 + (is_right_side ? 1:0) );
                result += Math.Abs( mid_kgf - kgf );
            }
            return result;
        }

        public WheelModel ( int spokes_count, int cross_count, double rim_d, double hub_d, double axle_length, double l_dish, double r_dish, double hub_fl_thickness = 0.0,
                                int spoke_tpi = 56, double spoke_diameter = 0.002, double spoke_young_modulus = 200e9, double spoke_density = 7800
                                )
        {
            m_rim_pos = Vector3D.zero;
            m_rim_rot = Matrix3x3.identity;

            m_spokes_count = spokes_count;
            m_cross_count = cross_count;
            m_rim_d = rim_d;
            m_hub_d = hub_d;
            m_axle_length = axle_length;
            m_l_dish = l_dish;
            m_r_dish = r_dish;
            m_hub_fl_thickness = hub_fl_thickness;
            m_spoke_diameter = spoke_diameter;
            m_spoke_tpi = spoke_tpi;
            m_spoke_space = (m_spoke_diameter / 2.0) * (m_spoke_diameter / 2.0) * Math.PI;
            m_spoke_young_modulus = spoke_young_modulus;
            m_spoke_density = spoke_density;

            m_spokes_data = new SpokeData[m_spokes_count];

            double angle = 2*Math.PI / m_spokes_count;

            for ( int n = 0; n < m_spokes_count; ++n )
            {
                int rim_a = n;

                int hub_a;
                if ( (n % 4) >= 2 )
                    hub_a = (n - 2 * m_cross_count) % m_spokes_count;
                else
                    hub_a = (n + 2 * m_cross_count) % m_spokes_count;

                double xr = m_rim_d/2*Math.Sin(rim_a*angle+angle*0.5);// 
                double yr = m_rim_d/2*Math.Cos(rim_a*angle+angle*0.5);// 

                double xh = m_hub_d/2*Math.Sin(hub_a*angle+angle*0.5);// 
                double yh = m_hub_d/2*Math.Cos(hub_a*angle+angle*0.5);// 

                double zr = 0;

                double zh;
                if ( n % 2 == 0 ) //l_spoke	
                    zh = m_axle_length / 2 - m_l_dish;
                else
                    zh = -m_axle_length / 2 + m_r_dish;

                if ( n % 4 <= 1 )
                {
                    if ( n % 2 == 0 )
                        zh -= m_hub_fl_thickness / 2.0;
                    else
                        zh += m_hub_fl_thickness / 2.0;
                } else
                {
                    if ( n % 2 == 0 )
                        zh += m_hub_fl_thickness / 2.0;
                    else
                        zh -= m_hub_fl_thickness / 2.0;
                }

                m_spokes_data[n].rim_local_point = new Vector3D( xr, yr, zr );
                m_spokes_data[n].hub_local_point = new Vector3D( xh, yh, zh );

                double spoke_len = m_spokes_data[n].rim_local_point.distance (m_spokes_data[n].hub_local_point);

                m_spokes_data[n].default_len = spoke_len;
                m_spokes_data[n].tightened_len = spoke_len;
            }


        }


        double step ()
        {
            Vector3D RimC = get_rim_center();

            double TightFactor_max = 0.0;
            double max_F_diff = 0;

            double highest_F = 0;
            double lowest_F = double.MaxValue;

            for ( int i = 0; i < m_spokes_count; ++i )
            {
                Vector3D Nip = get_nipple_point(i);

                Vector3D Hub = get_hub_point(i);
                Vector3D NipHub = Hub - Nip;

                double NipHub_len = NipHub.length();
                double tight_len = m_spokes_data[i].tightened_len;
                double amp = NipHub_len - tight_len;

                if ( amp > 0 )
                { //spoke work only on stretch
                    double k = m_total_model_modifier* m_spoke_young_modulus * m_spoke_space / tight_len;
                    double F = k * amp;

                    highest_F = Math.Max( highest_F, F );
                    lowest_F = Math.Min( lowest_F, F );

                    m_spokes_data[i].F_prev = m_spokes_data[i].F;
                    m_spokes_data[i].F = F;
                    m_spokes_data[i].TightFactor = k;

                    max_F_diff = Math.Max( max_F_diff, Math.Abs( m_spokes_data[i].F - m_spokes_data[i].F_prev ) );
                    TightFactor_max = Math.Max( m_spokes_data[i].TightFactor, TightFactor_max );

                    Vector3D NipTight = NipHub.normalized() * amp;
                    Vector3D NipTightPoint = Nip + NipTight;
                    Vector3D RimCNip = Nip - RimC;
                    Vector3D RimCNipTightPoint = NipTightPoint - RimC;

                    m_spokes_data[i].rim_move_vec = NipTight;
                    m_spokes_data[i].rim_torque_axis = RimCNip.cross( RimCNipTightPoint ).normalized();
                    m_spokes_data[i].rim_torque_angle = Math.Acos( RimCNipTightPoint.dot( RimCNip ) / (RimCNipTightPoint.length() * RimCNip.length()) );

                    continue;

                } else
                {
                    m_spokes_data[i].F = 0;
                }

            }

            Vector3D move_vector = Vector3D.zero;
            Matrix3x3 rotation_matrix = Matrix3x3.identity;

            if ( TightFactor_max > 0.0 )
                for ( int i = 0; i < m_spokes_count; ++i )
                {
                    if ( m_spokes_data[i].F == 0.0 )
                        continue;

                    double TightF = 0.01 * ( m_spokes_data[i].TightFactor / TightFactor_max );

                    move_vector += m_spokes_data[i].rim_move_vec * TightF;
                    rotation_matrix = rotation_matrix * Matrix3x3.getRotationMatrix( m_spokes_data[i].rim_torque_axis, m_spokes_data[i].rim_torque_angle * TightF );
                }
            m_rim_pos += move_vector;
            m_rim_rot = m_rim_rot * rotation_matrix;

            return max_F_diff;
        }


        public bool compute ()
        {
            float time = Time.realtimeSinceStartup;

            double f_diff = 0;
            for ( ;;)
            {
                f_diff = step();
                if ( f_diff < 0.001 )
                    break;
                if ( Time.realtimeSinceStartup - time >= 2.0 )
                    return false;
            }

            // Update spokes data after compute

            for ( int i = 0; i < m_spokes_count; ++i )
            {
                Vector3D NipHub = get_hub_point(i) - get_nipple_point(i);
                Vector3D NipHubF = NipHub.normalized() * m_spokes_data[i].F;

                Vector3D rim_cross_axis = get_rim_cross_axis();
                m_spokes_data[i].RimBentF = NipHubF.dot( rim_cross_axis ) / rim_cross_axis.length2();

                Vector3D NipRimCAxis = ( get_rim_center() - get_nipple_point(i) ).normalized();
                m_spokes_data[i].RimEggF = NipHubF.dot( NipRimCAxis ) / NipRimCAxis.length2();

            }

            double m_lowest_RimEggF = double.MaxValue;
            double m_highest_RimEggF = double.MinValue;
            for ( uint i = 0; i < m_spokes_count; ++i )
            {
                m_lowest_RimEggF = Math.Min( m_lowest_RimEggF, m_spokes_data[i].RimEggF );
                m_highest_RimEggF = Math.Max( m_highest_RimEggF, m_spokes_data[i].RimEggF );

                double RimBentF_m1 = m_spokes_data[ (i-1) % (uint) m_spokes_count ].RimBentF;
                double RimBentF_p1 = m_spokes_data[ (i+1) % (uint) m_spokes_count ].RimBentF;

                m_spokes_data[i].RimBentFAbsolute = m_spokes_data[i].RimBentF + (RimBentF_m1 + RimBentF_p1) / 2;
            }

            for ( uint i = 0; i < m_spokes_count; ++i )
            {
                double RimEggF_p1 = m_spokes_data[ (i+1) % (uint) m_spokes_count ].RimEggF;
                m_spokes_data[i].RimEggFAbsolute = (m_spokes_data[i].RimEggF + RimEggF_p1) / 2 - m_lowest_RimEggF;
            }

            return true;
        }

        public void nippleSetRotate ( int n_spoke, double rev )
        {
            m_spokes_data[n_spoke].rev = rev;
            //m_spokes_data[n_spoke].rev = MathEx.clamp( m_spokes_data[n_spoke].rev, -8.0, 8.0 );
            m_spokes_data[n_spoke].tightened_len = m_spokes_data[n_spoke].default_len - (0.0254 / m_spoke_tpi) * m_spokes_data[n_spoke].rev;
        }

        public void nippleRotate ( int n_spoke, double rev )
        {
            nippleSetRotate( n_spoke, m_spokes_data[n_spoke].rev + rev );
        }





        public Vector3D getRimPointBySpoke ( int n_spoke, double bent_on_every_100_kgf, double egg_on_every_100_kgf )
        {
            double angle = (360.0 / m_spokes_count);
            return getRimPointByAngle( n_spoke * angle + angle / 2, bent_on_every_100_kgf, egg_on_every_100_kgf );
        }



        public double getEggAverage ( int spoke_id, double f_norm, int spokes_spread )
        {
            double[] n_egg_avg = new double[spokes_spread];
            for ( int i = 0; i < spokes_spread; ++i )
            {
                n_egg_avg[i] = m_spokes_data[(uint) (spoke_id - spokes_spread / 2 + 1 + i) % (uint) m_spokes_count].RimEggFAbsolute;

            }

            double[] lerp_avg = new double[spokes_spread-1];
            for ( int i = 0; i < spokes_spread - 1; ++i )
            {
                lerp_avg[i] = MathEx.lerp( n_egg_avg[i], n_egg_avg[i + 1], f_norm );
            }

            double result = 0.0;
            for ( int i = 0; i < spokes_spread - 1; ++i )
                result += lerp_avg[i];
            result /= spokes_spread - 1;
            result *= N_to_kgf;

            return result;
        }

        public Vector3D getRimPointByAngle ( double angle_deg_f, double bent_on_every_100_kgf, double egg_on_every_100_kgf )
        {
            while ( angle_deg_f < 0 )
                angle_deg_f = 360.0 + angle_deg_f;
            while ( angle_deg_f >= 360 )
                angle_deg_f -= 360;

            double angle_rad = angle_deg_f * Math.PI / 180.0;
            double angle_rad_per_spoke = (Math.PI*2) / m_spokes_count;
            double spokes_idf = angle_rad / angle_rad_per_spoke;
            int spokes_id = (int)spokes_idf;
            double spokes_idf_norm = spokes_idf - (double) spokes_id;

            double m1_FAbs = m_spokes_data[ (uint) (spokes_id-1) % (uint)m_spokes_count].RimBentFAbsolute;
            double cur_FAbs = m_spokes_data[spokes_id].RimBentFAbsolute;
            double p1_FAbs = m_spokes_data[(spokes_id+1) % (uint)m_spokes_count].RimBentFAbsolute;
            double p2_FAbs = m_spokes_data[(spokes_id+2) % (uint)m_spokes_count].RimBentFAbsolute;
            double AvgFAbs123 = new SplineInterpolator (new double[4] { 0.0, 1.0, 2.0, 3.0 }, new double[4] { m1_FAbs, cur_FAbs, p1_FAbs, p2_FAbs } ).GetValue( 1.0 + spokes_idf_norm);

            double AvgEggFAbs123 = getEggAverage (spokes_id, spokes_idf_norm, 8);

            double radius =  m_rim_d / 2;

            double xr = radius*Math.Sin(angle_rad); 
            double yr = radius*Math.Cos(angle_rad);

            Vector3D rim_point = m_rim_pos + m_rim_rot * new Vector3D( xr, yr, 0 );
            Vector3D rim_point_center = get_rim_center() - rim_point;

            Vector3D egg_offset = rim_point_center.normalized() * ((AvgEggFAbs123 / 100.0) * egg_on_every_100_kgf);
            Vector3D bent_offset = get_rim_cross_axis() * ((AvgFAbs123 / 100.0) * bent_on_every_100_kgf);

            return rim_point + egg_offset + bent_offset;
        }

        public int getNearestSpokeByAngle ( double angle_deg_f )
        {
            while ( angle_deg_f < 0 )
                angle_deg_f = 360.0 + angle_deg_f;
            while ( angle_deg_f >= 360 )
                angle_deg_f -= 360;

            double angle = 360.0 / m_spokes_count;

            return (int) ( angle_deg_f / angle );
        }






    }


}

