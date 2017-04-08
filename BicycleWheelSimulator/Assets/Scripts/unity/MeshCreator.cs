using math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace unity
{
    public class MeshCreator
    {
        static public Points3D createCircle_XZ_Points3D (double radius, int circle_sections_count)
        {
            Points3D section = new Points3D();

            double rad_per_point = 2 * Math.PI / circle_sections_count;

            for ( int n_section = 0; n_section < circle_sections_count; ++n_section )
                section.addPoint( new Vector3D( -Math.Cos(n_section*rad_per_point)*radius, 0, Math.Sin(n_section*rad_per_point)*radius ) );

            return section;
        }

        static public Mesh createCircle ( double radius, int circle_sections_count, Vector3D center, Vector3D Y_axis, bool is_back_face = false)
        {
            Mesh mesh = new Mesh();

            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>();
            List<int> indices = new List<int>();

            double rad_per_point = 2 * Math.PI / circle_sections_count;

            vertices.Add( center.toVector3() );
            uv.Add( new Vector2( 0.5f, 0.5f ) );

            for ( int i = 0; i < circle_sections_count; ++i )
            {
                Points3D section = new Points3D();

                uv.Add( new Vector2( (float) (0.5 + Math.Cos( i * rad_per_point )/2), (float) ( 0.5-Math.Sin( i * rad_per_point )/2)  ) );


                section.addPoint( new Vector3D( -Math.Cos( i * rad_per_point ) * radius, 0, Math.Sin( i * rad_per_point ) * radius ) );

                section.rotate( Matrix3x3.getRotationMatrixLookAtYAxis( new Vector3D( 0, 1, 0 ), Y_axis ), Vector3D.zero );
                section.move( center );

                vertices.Add( section[0].toVector3() );
            }

            for ( int i = 0; i < circle_sections_count; ++i )
            {
                indices.AddRange(
                new int[]
                   {
                            0,
                            i +1,
                            (i +1) % (circle_sections_count) + 1
                        }
                );
            }
            mesh.vertices = vertices.ToArray();
            mesh.uv = uv.ToArray();
            mesh.triangles = indices.ToArray();
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            return mesh;
        }

        static public Mesh createCylinder ( double radius, double height, int circle_sections_count, Vector3D center, Vector3D Y_axis, double rev_rotate, bool is_back_face = false)
        {
            Points3D[] sections = new Points3D[2];

            double rad_per_point = 2 * Math.PI / circle_sections_count;


            for ( int n_section = 0; n_section < sections.Length; ++n_section )
            {

                Points3D section = new Points3D();

                for (int i=0; i<circle_sections_count; ++i )
                    section.addPoint( new Vector3D( -Math.Cos(i*rad_per_point)*radius, 0, Math.Sin(i*rad_per_point)*radius ) );

                section.rotate( new Vector3D(0,1,0), rev_rotate, Vector3D.zero );
                section.move( new Vector3D(0, n_section*height, 0) );
                section.rotate( Matrix3x3.getRotationMatrixLookAtYAxis( new Vector3D(0,1,0),  Y_axis ), Vector3D.zero );
                section.move( center );
                sections[n_section] = section;

            }


            return createFromLoopPoints3D( sections, true, false, is_back_face);
        }

         static public Mesh createFromPoints3D ( Points3D[] sections, bool is_back_face = false )
        {
            Mesh mesh = new Mesh();

            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>();
            List<int> indices = new List<int>();

            int sections_count = sections.Length;
            int section_points_count = sections[0].points_count;


            float uv_x_per_section = 1.0f / sections_count;

            double sectionLength = sections[0].getWireFullLoopLength();

            float uv_y_per_point_length = 1.0f / ((float)sectionLength);

            float[] uv_y_ar = new float[section_points_count];
            float total_wire_dist  = 0;
            for ( int i = 0; i < section_points_count; ++i )
            {
                
                if ( i != 0 )
                {
                    total_wire_dist += (float) sections[0][i].distance( sections[0][(i - 1) % section_points_count] );
                }

                uv_y_ar[i] = total_wire_dist*uv_y_per_point_length;
            }

            int vertices_count = sections_count * section_points_count;
            for ( int i = 0; i < sections_count; ++i )
            {
                for ( int n = 0; n < section_points_count; ++n )
                    vertices.Add( sections[i][n].toVector3() );

                int section_indice_0 = i*section_points_count;

                for ( int j = 0; j < section_points_count; ++j )
                {
                    int cur_indice = section_indice_0 + j;

                    if ( i < sections_count - 1 && j < section_points_count -1   )

                        if ( !is_back_face )
                            indices.AddRange(
                                new int[]
                                {
                                    cur_indice+0,
                                    section_indice_0 + (cur_indice+1) % section_points_count,

                                    ( section_indice_0 + ( (cur_indice+1) % section_points_count ) + section_points_count ) % vertices_count,

                                    ( section_indice_0 + ( (cur_indice+1) % section_points_count ) + section_points_count ) % vertices_count,
                                    ( section_indice_0 + ( (cur_indice+0) % section_points_count ) + section_points_count ) % vertices_count,
                                    cur_indice
                                    }
                                );
                        else
                            indices.AddRange(
                            new int[]
                            {
                                cur_indice+0,

                                
                                ( section_indice_0 + ( (cur_indice+1) % section_points_count ) + section_points_count ) % vertices_count,
                                section_indice_0 + (cur_indice+1) % section_points_count,

                                ( section_indice_0 + ( (cur_indice+1) % section_points_count ) + section_points_count ) % vertices_count,

                                 cur_indice,
                                ( section_indice_0 + ( (cur_indice+0) % section_points_count ) + section_points_count ) % vertices_count
                               
                                }
                            );

                    uv.Add( new Vector2(uv_x_per_section*i, uv_y_ar[j]) );
                }
            }
            mesh.vertices = vertices.ToArray();
            mesh.uv = uv.ToArray();
            mesh.triangles = indices.ToArray();
            mesh.RecalculateNormals();


            return mesh;
        }

        static public Mesh createFromLoopPoints3D ( Points3D[] sections, bool loop_section = true, bool loop_last_to_first = true, bool is_back_face = false )
        {
            Mesh mesh = new Mesh();

            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>();
            List<int> indices = new List<int>();

            int sections_count = sections.Length;
            int section_points_count = sections[0].points_count;


            float uv_x_per_section = 1.0f / sections_count;

            double sectionLength = sections[0].getWireFullLoopLength();

            float uv_y_per_point_length = 1.0f / ((float)sectionLength);

            float[] uv_y_ar = new float[section_points_count];
            float total_wire_dist  = 0;
            for ( int i = 0; i < section_points_count; ++i )
            {                
                if ( i != 0 )
                    total_wire_dist += (float) sections[0][i].distance( sections[0][(i - 1) % section_points_count] );

                uv_y_ar[i] = total_wire_dist*uv_y_per_point_length;
            }

            int vertices_count = sections_count * section_points_count;
            for ( int i = 0; i < sections_count; ++i )
            {
                for ( int n = 0; n < section_points_count; ++n )
                    vertices.Add( sections[i][n].toVector3() );

                int section_indice_0 = i*section_points_count;

                for ( int j = 0; j < section_points_count; ++j )
                {
                    int cur_indice = section_indice_0 + j;

                    if ( loop_last_to_first || i < sections_count - 1 )

                        if ( !is_back_face )
                            indices.AddRange(
                                new int[]
                                {
                                    cur_indice+0,
                                    section_indice_0 + (cur_indice+1) % section_points_count,

                                    ( section_indice_0 + ( (cur_indice+1) % section_points_count ) + section_points_count ) % vertices_count,

                                    ( section_indice_0 + ( (cur_indice+1) % section_points_count ) + section_points_count ) % vertices_count,
                                    ( section_indice_0 + ( (cur_indice+0) % section_points_count ) + section_points_count ) % vertices_count,
                                    cur_indice
                                    }
                                );
                        else
                            indices.AddRange(
                            new int[]
                            {
                                cur_indice+0,

                                
                                ( section_indice_0 + ( (cur_indice+1) % section_points_count ) + section_points_count ) % vertices_count,
                                section_indice_0 + (cur_indice+1) % section_points_count,

                                ( section_indice_0 + ( (cur_indice+1) % section_points_count ) + section_points_count ) % vertices_count,

                                 cur_indice,
                                ( section_indice_0 + ( (cur_indice+0) % section_points_count ) + section_points_count ) % vertices_count
                               
                                }
                            );

                    uv.Add( new Vector2(uv_x_per_section*i, uv_y_ar[j]) );
                }
            }
            mesh.vertices = vertices.ToArray();
            mesh.uv = uv.ToArray();
            mesh.triangles = indices.ToArray();
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            return mesh;
        }

        static public Mesh createFromCenterToEveryPoints ( Vector3D center, Vector3D[] points, bool is_back_face = false)
        {
            Mesh mesh = new Mesh();

            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>();
            List<int> indices = new List<int>();

            vertices.Add( center.toVector3() );
            uv.Add( new Vector2( 0.5f, 0.5f ) );

            int vertices_count = points.Length + 1;
            for ( int i = 0; i < points.Length; ++i )
            {
                vertices.Add( points[i].toVector3() );


                if ( is_back_face )
                    indices.AddRange(
                        new int[]
                        {
                            0,
                            (i +1) % (points.Length) + 1 ,
                            i +1
                        }
                    );
                else
                    indices.AddRange(
                    new int[]
                        {
                            0,
                            i +1,
                            (i +1) % (points.Length) + 1
                        }
                    );

                uv.Add( new Vector2( 0, 0 ) );
            }
            mesh.vertices = vertices.ToArray();
            mesh.uv = uv.ToArray();
            mesh.triangles = indices.ToArray();
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            return mesh;
        }


        static public Mesh createHub ( Vector3D l_axle_center, Vector3D dir, int circle_sections_count, double axle_radius, double hub_radius, double axle_length , double l_dish, double r_dish, double fl_thickness )
        {
            Mesh mesh = new Mesh();

            Points3D l_axle_circle_center = new Points3D(); l_axle_circle_center.addPoint( Vector3D.zero );

            Points3D l_axle_circle = createCircle_XZ_Points3D (axle_radius, circle_sections_count);
            //
            Points3D l_axle_l_dish_ll_circle = createCircle_XZ_Points3D (axle_radius, circle_sections_count);
            l_axle_l_dish_ll_circle.move ( new Vector3D(0, l_dish - fl_thickness/2, 0) );

            Points3D l_axle_l_dish_lh_circle = createCircle_XZ_Points3D (hub_radius, circle_sections_count);
            l_axle_l_dish_lh_circle.move ( new Vector3D(0, l_dish - fl_thickness/2, 0) );

            Points3D l_axle_l_dish_rh_circle = createCircle_XZ_Points3D (hub_radius, circle_sections_count);
            l_axle_l_dish_rh_circle.move ( new Vector3D(0, l_dish + fl_thickness/2, 0) );

            Points3D l_axle_l_dish_r_hl_circle = createCircle_XZ_Points3D (axle_radius+(hub_radius-axle_radius)*0.1, circle_sections_count);
            l_axle_l_dish_r_hl_circle.move ( new Vector3D(0, l_dish + fl_thickness/2, 0) );

            Points3D l_axle_l_dish_rl_circle = createCircle_XZ_Points3D (axle_radius, circle_sections_count);
            l_axle_l_dish_rl_circle.move ( new Vector3D(0, l_dish + fl_thickness/2, 0) );
            //
            Points3D l_axle_r_dish_ll_circle = createCircle_XZ_Points3D (axle_radius, circle_sections_count);
            l_axle_r_dish_ll_circle.move ( new Vector3D(0, axle_length - r_dish - fl_thickness/2, 0) );

            Points3D l_axle_r_dish_l_lh_circle = createCircle_XZ_Points3D (axle_radius+(hub_radius-axle_radius)*0.1, circle_sections_count);
            l_axle_r_dish_l_lh_circle.move ( new Vector3D(0, axle_length - r_dish - fl_thickness/2, 0) );

            Points3D l_axle_r_dish_lh_circle = createCircle_XZ_Points3D (hub_radius, circle_sections_count);
            l_axle_r_dish_lh_circle.move ( new Vector3D(0, axle_length - r_dish - fl_thickness/2, 0) );

            Points3D l_axle_r_dish_rh_circle = createCircle_XZ_Points3D (hub_radius, circle_sections_count);
            l_axle_r_dish_rh_circle.move ( new Vector3D(0, axle_length - r_dish + fl_thickness/2, 0) );

            Points3D l_axle_r_dish_rl_circle = createCircle_XZ_Points3D (axle_radius, circle_sections_count);
            l_axle_r_dish_rl_circle.move ( new Vector3D(0, axle_length - r_dish + fl_thickness/2, 0) );
            //
            Points3D r_axle_circle = createCircle_XZ_Points3D (axle_radius, circle_sections_count);
            r_axle_circle.move( new Vector3D(0,axle_length,0) );

            Points3D r_axle_circle_center = new Points3D(); r_axle_circle_center.addPoint( new Vector3D(0,axle_length,0) );

            Points3D[] sections = new Points3D[14] 
            {
                l_axle_circle_center,
                l_axle_circle,
                l_axle_l_dish_ll_circle,
                l_axle_l_dish_lh_circle,
                l_axle_l_dish_rh_circle,
                l_axle_l_dish_r_hl_circle,
                l_axle_l_dish_rl_circle,
                l_axle_r_dish_ll_circle,
                l_axle_r_dish_l_lh_circle,
                l_axle_r_dish_lh_circle,
                l_axle_r_dish_rh_circle,
                l_axle_r_dish_rl_circle,
                r_axle_circle,
                r_axle_circle_center
            };

            for (int i=0; i< sections.Length; ++i )
            {
                sections[i].rotate( Matrix3x3.getRotationMatrixLookAtYAxis( new math.Vector3D( 0, 1, 0 ), dir ), Vector3D.zero );
                sections[i].move( l_axle_center );
            }

            return createLatheFromPoints3D (sections);
        }

        
        static public Mesh createLatheFromPoints3D ( Points3D[] sections )
        {
            Mesh mesh = new Mesh();

            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>();
            List<int> indices = new List<int>();

            int sections_count = sections.Length;
            int section_points_count = sections[0].points_count;

            int[] section_vertices_start = new int[sections.Length];
            int[] section_vertices_end = new int[sections.Length];

            for ( int i = 0; i < sections_count; ++i )
            {
                section_vertices_start[i] = vertices.Count;

                vertices.AddRange( sections[i].toVector3Array() );

                section_vertices_end[i] = vertices.Count;
            }

            for (int i = 0; i< vertices.Count;  ++i)
                uv.Add( new Vector2( 0, 0 ) );
            

            for ( int n_section = 0; n_section < sections_count-1; ++n_section )
            {
                int cur_section_vertices_start = section_vertices_start[n_section];
                int cur_section_vertices_count = section_vertices_end[n_section] - cur_section_vertices_start;
                int next_section_vertices_start = section_vertices_start[(n_section+1)%sections_count];
                int next_section_vertices_count = section_vertices_end[(n_section+1)%sections_count] - next_section_vertices_start;


                if ( cur_section_vertices_count == 1 && next_section_vertices_count >= 3 )
                {
                    for ( int i = 0; i < next_section_vertices_count; ++i )
                        indices.AddRange(
                        new int[]
                            {
                                cur_section_vertices_start,
                                next_section_vertices_start+(i+1) % next_section_vertices_count,
                                next_section_vertices_start+i                                
                            }
                        );

                } else
                if ( cur_section_vertices_count >= 3 && next_section_vertices_count == 1 )
                {
                    for ( int i = 0; i < cur_section_vertices_count; ++i )
                        indices.AddRange(
                        new int[]
                            {
                                next_section_vertices_start,
                                cur_section_vertices_start+i,
                                cur_section_vertices_start+(i+1) % cur_section_vertices_count
                            }
                        );

                } else
                if ( cur_section_vertices_count == next_section_vertices_count )
                {
                    for ( int i = 0; i < cur_section_vertices_count; ++i )
                        indices.AddRange(
                        new int[]
                            {
                                cur_section_vertices_start+i,
                                cur_section_vertices_start+ (i+1) % cur_section_vertices_count,
                                next_section_vertices_start+(i+1) % next_section_vertices_count,

                                cur_section_vertices_start+i,
                                next_section_vertices_start+(i+1) % next_section_vertices_count,
                                next_section_vertices_start+i
                            }
                        );
                }else
                {
                    throw new Exception( "incorrect sections vertices" );
                }
            }
            mesh.vertices = vertices.ToArray();
            mesh.uv = uv.ToArray();
            mesh.triangles = indices.ToArray();
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            return mesh;
        }
    }
}
