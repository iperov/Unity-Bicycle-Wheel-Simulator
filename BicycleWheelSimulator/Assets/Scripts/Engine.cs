using math;
using model.wheel;
using sys.debug;
using System;
using unity;
using unity.objectcontrollers;
using unity.ui;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Engine : MonoBehaviour
{
    static public Engine g;

    public enum EEngineState
    {
        none,
        view1,
        view2,
        options,
        help_quality,
        QTY
    }

    static public int layer_wheel;
    static public int layer_mask_wheel;

    static public int layer_wheel_box;
    static public int layer_mask_wheel_box;

    // assigned by inspector

    public Material m_rim_mat;
    public Material m_hub_mat;
    public Material m_eyelet_mat;
    public Material m_eyelet_circle_mat;
    public Material m_nipple_mat;
    public Material m_spoke_mat;

    public GameObject m_spoke_wrench_prefab;
    public GameObject m_HDialIndicator_prefab;
    public GameObject m_VDialIndicator_prefab;
    public GameObject m_nipple_prefab;

    public AudioClip[] m_spokes_AudioClips;
    public int m_spokes_AudioClips_reference_frequency = 288;

    // ui assigns

    public RectTransform m_play_spoke_sound_Button_RectTransform;
    public Text m_wheel_quality_Text;
    public GameObject m_view1_Panel;
    public GameObject m_view2_Panel;
    public GameObject m_options_Panel;
    public GameObject m_help_Panel;
    public GameObject m_helpQuality_Panel;
    ////////////

    // some defines
    const float m_rim_profile_width = 0.020f; //must equal rim mesh section
    const float m_rim_profile_height = 0.020f;
    const double m_bent_on_every_100kgf = 0.00005;  // visual "deformation" multipliers
    const double m_egg_on_every_100kgf  = 0.001;    //

    const float m_WheelModel_force_modifier = 2f; // visual 2x rotation equal 1x wheelmodel rotation

    const int m_spokes_count = 32;
    const int m_cross_count = 3;
    const double m_rim_d = 0.519;
    const double m_hub_d = 0.050;
    const double m_axle_length = 0.100;
    const double m_l_dish = 0.015;
    const double m_r_dish = 0.015;

    const double m_rim_rough_size = 0.00007;
    const int m_wheel_mesh_additional_sections_count = 10;

    //

    EEngineState m_state; public EEngineState state { get { return m_state; } }

    WheelModel m_WheelModel;
    int[] m_WheelModel_spokes_rev90_offset = new int[48];
    double m_WheelModel_left_side_disbalance;
    double m_WheelModel_right_side_disbalance;
    int m_wheel_mesh_sections_count;
    Points3D[] m_rim_base_sections;
    int m_current_spoke = -1;
    double m_current_spoke_rotate;
    bool m_is_spoke_rolling = false;
    bool m_is_wheel_rotating = false;
    float m_wheel_angle_prev;
    float m_wheel_angle;
    float m_wheel_angular_velocity = 0.0f;
    float m_wheel_angular_damping = 0.0f;
    Quaternion m_spoke_rolling_initial_nipple_rotation;

    // UI
    GUIStyle m_log_GUIStyle = new GUIStyle();


    // Unity objects
    Camera m_Camera;
    AudioSource m_AudioSource;
    GameObject m_wheel_GO;
    GameObject m_wheel_box_GO;
    GameObject[] m_wheel_nipples_GO;

    GameObject m_wheel_spoke_wrench_GO;

    GameObject m_HDialIndicator_GO;
    HDialIndicator m_HDialIndicator;
    GameObject m_VDialIndicator_GO;
    VDialIndicator m_VDialIndicator;

    void Start ()
    {
        g = this;
        new Log();

        layer_wheel = LayerMask.NameToLayer( "wheel" );
        layer_mask_wheel = 1 << layer_wheel;
        layer_wheel_box = LayerMask.NameToLayer( "wheel_box" );
        layer_mask_wheel_box = 1 << layer_wheel_box;

        Physics.queriesHitBackfaces = true;
        m_Camera = Camera.main;

        gameObject.AddComponent<PlatformInput>();

        m_AudioSource = gameObject.AddComponent<AudioSource>();

        m_log_GUIStyle.fontSize = 12;
        m_log_GUIStyle.normal.textColor = Color.white;

        m_WheelModel_reinitialize();

        m_HDialIndicator_GO = Instantiate( m_HDialIndicator_prefab );
        m_HDialIndicator_GO.name = "HDialIndicator";
        m_HDialIndicator = m_HDialIndicator_GO.GetComponent<HDialIndicator>();
        m_HDialIndicator.initialize( layer_mask_wheel );
        m_HDialIndicator_GO.transform.rotation = Quaternion.LookRotation( Vector3.right, Vector3.up );
        Vector3 HDialIndicator_pos = new Vector3(0, -(float)m_WheelModel.rim_d / 2 - m_rim_profile_height + 0.005f, m_rim_profile_width*1.5f )
                                    - m_HDialIndicator.getStickDefaultWorldOffset();
        m_HDialIndicator_GO.transform.position = HDialIndicator_pos;

        m_VDialIndicator_GO = Instantiate( m_VDialIndicator_prefab );
        m_VDialIndicator_GO.name = "VDialIndicator";
        m_VDialIndicator = m_VDialIndicator_GO.GetComponent<VDialIndicator>();
        m_VDialIndicator.initialize( layer_mask_wheel );
        m_VDialIndicator_GO.transform.rotation = Quaternion.LookRotation( Vector3.right, Vector3.up );
        Vector3 VDialIndicator_pos = new Vector3(0, (float) -m_WheelModel.rim_d / 2 - m_rim_profile_height - m_VDialIndicator.getStickTravelMax()/2 , 0 )
                                    - m_VDialIndicator.getStickWorldOffset();
        m_VDialIndicator_GO.transform.position = VDialIndicator_pos;

        m_view1_Panel.SetActive( false );
        m_view2_Panel.SetActive( false );
        m_options_Panel.SetActive( false );
        m_help_Panel.SetActive( false );
        m_helpQuality_Panel.SetActive( false );
        setEngineState( EEngineState.view1 );
    }

    enum CameraView
    {
        view1,
        view2
    }
    void m_Camera_setView ( CameraView cv )
    {
        switch ( cv )
        {
            case CameraView.view1:
            {
                float V_fov = m_Camera.fieldOfView;
                float target_camera_bottom_Y = -(float)m_WheelModel.rim_d;// - 0.20f;
                float V_dist = Mathf.Tan ( V_fov*Mathf.Deg2Rad / 2.0f ) * target_camera_bottom_Y;
                float cam_angle = -45 * Mathf.Deg2Rad;

                m_Camera.transform.position =
                m_WheelModel.get_hub_pos().toVector3() - new Vector3( Mathf.Cos( cam_angle ) * (float) m_WheelModel.rim_d / 2, -Mathf.Sin( cam_angle ) * (float) m_WheelModel.rim_d / 2, 0 )
                    + new Vector3( 0, 0, 0.05f );

                m_Camera.transform.rotation = Quaternion.Euler( 45 / 2, 90, 0 );
                break;
            }

            case CameraView.view2:
            {
                m_Camera.transform.position =
               m_WheelModel.get_hub_pos().toVector3() + new Vector3( -0.040f, -(float) m_WheelModel.rim_d / 2 - m_rim_profile_height * 1.5f, m_rim_profile_width );

                m_Camera.transform.rotation = Quaternion.Euler( 0, 90, 0 );
                break;
            }
        }

    }

    void m_WheelModel_reinitialize ()
    {
        m_wheel_mesh_sections_count = m_spokes_count * (m_wheel_mesh_additional_sections_count + 1);

        m_rim_base_sections = new Points3D[m_wheel_mesh_sections_count + 1];
        for ( int i = 0; i < m_wheel_mesh_sections_count; ++i )
        {
            m_rim_base_sections[i] = new Points3D();
            m_rim_base_sections[i].addPoint( new Vector3D( 0, -0.015, 0 ) );
            m_rim_base_sections[i].addPoint( new Vector3D( -0.008, -0.015, 0 ) );
            m_rim_base_sections[i].addPoint( new Vector3D( -0.008, -0.020, 0 ) );
            m_rim_base_sections[i].addPoint( new Vector3D( -0.010, -0.020, 0 ) );
            m_rim_base_sections[i].addPoint( new Vector3D( -0.010, -0.010, 0 ) );
            m_rim_base_sections[i].addPoint( new Vector3D( -0.005, 0, 0 ) );
            m_rim_base_sections[i].addPoint( new Vector3D( 0.005, 0, 0 ) );
            m_rim_base_sections[i].addPoint( new Vector3D( 0.010, -0.010, 0 ) );
            m_rim_base_sections[i].addPoint( new Vector3D( 0.010, -0.020, 0 ) );
            m_rim_base_sections[i].addPoint( new Vector3D( 0.008, -0.020, 0 ) );
            m_rim_base_sections[i].addPoint( new Vector3D( 0.008, -0.015, 0 ) );
            m_rim_base_sections[i].addPoint( new Vector3D( 0, -0.015, 0 ) );
        }

        int base_section_point_count = m_rim_base_sections[0].points_count;
        double[][] rough_wave_x  = new double [base_section_point_count][];
        double[][] rough_wave_y  = new double [base_section_point_count][];
        int rough_min_range = (m_wheel_mesh_sections_count / m_spokes_count) * 3;
        int rough_max_range = (m_wheel_mesh_sections_count / m_spokes_count) * 6;
        for ( int i = 0; i < base_section_point_count; ++i )
        {
            rough_wave_x[i] = SplineInterpolator.generateRoughLoopedCurve( m_wheel_mesh_sections_count, rough_min_range, rough_max_range, m_rim_rough_size );
            rough_wave_y[i] = SplineInterpolator.generateRoughLoopedCurve( m_wheel_mesh_sections_count, rough_min_range, rough_max_range, m_rim_rough_size );
        }

        for ( int i = 0; i < m_wheel_mesh_sections_count; ++i )
            for ( int j = 1; j < base_section_point_count - 1; ++j ) //exclude seam

                m_rim_base_sections[i][j] += new Vector3D( rough_wave_x[j][i], rough_wave_y[j][i], 0 );

        m_rim_base_sections[m_wheel_mesh_sections_count] = m_rim_base_sections[0].copy();

        m_WheelModel = new WheelModel( m_spokes_count, m_cross_count, m_rim_d, m_hub_d, m_axle_length, m_l_dish, m_r_dish );
        for ( int i = 0; i < m_WheelModel.spokes_count; ++i )
            m_WheelModel_spokes_rev90_offset[i] = UnityEngine.Random.Range( 0, 4 );

        m_WheelModel_random_unbalance();
    }

    void m_WheelModel_random_unbalance ()
    {
        for ( int i = 0; i < m_WheelModel.spokes_count; ++i )
        {
            //m_WheelModel.nippleSetRotate( i,  UnityEngine.Random.Range(0.7f,0.8f) );
            m_WheelModel.nippleSetRotate( i, 0.7f );
        }

        for ( int i = 0; i < 12; ++i )
        {
            int spoke_id = sys.Random.range ( 0, m_WheelModel.spokes_count );
            float rev = sys.Random.sign() * 0.1f;

            m_WheelModel.nippleRotate( (spoke_id + 0) % m_WheelModel.spokes_count, rev );
            // m_WheelModel.nippleRotate( (spoke_id + 2) % m_WheelModel.spokes_count, rev );
        }

        /*
        for ( int i = 0; i < 2; ++i )
        {
            int spoke_id = sys.Random.range ( 0, m_WheelModel.spokes_count );

            float rev = sys.Random.sign() * 0.2f;

            m_WheelModel.nippleRotate( (spoke_id + 0) % m_WheelModel.spokes_count, rev );
            m_WheelModel.nippleRotate( (spoke_id + 1) % m_WheelModel.spokes_count, rev );
            m_WheelModel.nippleRotate( (spoke_id + 2) % m_WheelModel.spokes_count, rev );
            m_WheelModel.nippleRotate( (spoke_id + 3) % m_WheelModel.spokes_count, rev );
        }
        */

        m_WheelModel_compute_and_rebuild_mesh();
    }

    void m_WheelModel_compute_and_rebuild_mesh ()
    {
        m_WheelModel.compute();

        m_WheelModel_left_side_disbalance = m_WheelModel.getSpokeForceKgfSideDisbalance( false );
        m_WheelModel_right_side_disbalance = m_WheelModel.getSpokeForceKgfSideDisbalance( true );

        double total_disbalance = m_WheelModel_left_side_disbalance + m_WheelModel_right_side_disbalance;

        if ( total_disbalance < m_WheelModel.spokes_count )
        {
            m_wheel_quality_Text.text = "Идеальное";
            m_wheel_quality_Text.color = new Color( 0, 0.8f, 0 );
        } else
        if ( total_disbalance < m_WheelModel.spokes_count * 2 )
        {
            m_wheel_quality_Text.text = "Хорошее";
            m_wheel_quality_Text.color = new Color( 0.5f, 0.8f, 0 );
        } else
        if ( total_disbalance < m_WheelModel.spokes_count * 4 )
        {
            m_wheel_quality_Text.text = "Сойдёт";
            m_wheel_quality_Text.color = new Color( 0.8f, 0.8f, 0 );
        } else
        if ( total_disbalance < m_WheelModel.spokes_count * 6 )
        {
            m_wheel_quality_Text.text = "Плохое";
            m_wheel_quality_Text.color = new Color( 0.8f, 0.5f, 0 );
        } else
        {
            m_wheel_quality_Text.text = "Ужасное";
            m_wheel_quality_Text.color = new Color( 0.8f, 0, 0 );
        };

        /*
        for (int i=0; i<m_WheelModel.spokes_count; ++i )
        {
            Log.g.add( string.Format("{0} {1:0.00}", i, m_WheelModel.getSpokeForceKgf( i )) );
        }*/

        double angle_per_section = 360.0 / m_wheel_mesh_sections_count;
        Points3D[] rim_sections = new Points3D[m_wheel_mesh_sections_count+1];
        for ( int i = 0; i < m_wheel_mesh_sections_count + 1; ++i )
        {
            Points3D rim_section = m_rim_base_sections[i].copy();

            Vector3D rim_center = m_WheelModel.get_rim_center();
            Vector3D rim_point = m_WheelModel.getRimPointByAngle( i*angle_per_section, 0, 0 );
            Vector3D rim_bent_egg_point = m_WheelModel.getRimPointByAngle( i*angle_per_section, m_bent_on_every_100kgf, m_egg_on_every_100kgf);
            Vector3D Y = ( rim_center - rim_point ).normalized();
            Vector3D X = -m_WheelModel.get_rim_cross_axis();

            rim_section.rotate( Matrix3x3.fromVerticalVectors( X, Y, X.cross( Y ).normalized() ), Vector3D.zero );

            double angle = Vector3D.angleBetween ( rim_bent_egg_point - rim_center, rim_point - rim_center );
            if ( angle > Math.PI / 10000 )
                rim_section.rotate( Y.cross( X ).normalized(), angle, Vector3D.zero );

            rim_section.move( rim_bent_egg_point );

            rim_sections[i] = rim_section;
        }

        Mesh rim_mesh = MeshCreator.createFromPoints3D (rim_sections);

        Quaternion wheel_rotation = Quaternion.identity;
        if ( m_wheel_GO != null )
        {
            wheel_rotation = m_wheel_GO.transform.rotation;
            Destroy( m_wheel_GO );
            m_wheel_GO = null;
        }

        if ( m_wheel_spoke_wrench_GO != null )
        {
            Destroy( m_wheel_spoke_wrench_GO );
            m_wheel_spoke_wrench_GO = null;
        }

        m_wheel_GO = new GameObject( "wheel" );
        m_wheel_GO.layer = layer_wheel;

        m_wheel_spoke_wrench_GO = Instantiate( m_spoke_wrench_prefab, m_wheel_GO.transform );

        MeshFilter mf = m_wheel_GO.AddComponent<MeshFilter>();
        mf.mesh = rim_mesh;
        MeshRenderer mr = m_wheel_GO.AddComponent<MeshRenderer>();
        mr.material = m_rim_mat;
        MeshCollider mc = m_wheel_GO.AddComponent <MeshCollider>();
        mc.sharedMesh = rim_mesh;

        if ( m_wheel_box_GO != null )
        {
            Destroy( m_wheel_box_GO );
            m_wheel_box_GO = null;
        }
        m_wheel_box_GO = new GameObject( "wheel_box" );
        m_wheel_box_GO.layer = layer_wheel_box;
        BoxCollider wheel_box_GO_BoxCollider = m_wheel_box_GO.AddComponent<BoxCollider>();
        wheel_box_GO_BoxCollider.center = mc.bounds.center;
        wheel_box_GO_BoxCollider.size = mc.bounds.size;

        //hub
        Vector3D hub_l = m_WheelModel.get_hub_pos() + new Vector3D(0,0,1)*m_WheelModel.axle_length/2;
        Vector3D hub_r = m_WheelModel.get_hub_pos() + new Vector3D(0,0,-1)*m_WheelModel.axle_length/2;
        Vector3D hub_lr = hub_r - hub_l;

        Mesh hub_mesh = MeshCreator.createHub ( hub_l, hub_lr.normalized(), 40, 0.005, 0.002 + m_WheelModel.hub_d / 2, m_WheelModel.axle_length, m_WheelModel.l_dish, m_WheelModel.r_dish, 0.005 );
        GameObject hub_GO = new GameObject ("hub");
        hub_GO.transform.parent = m_wheel_GO.transform;
        hub_GO.AddComponent<MeshFilter>().mesh = hub_mesh;
        hub_GO.AddComponent<MeshRenderer>().material = m_hub_mat;

        m_wheel_nipples_GO = new GameObject[m_WheelModel.spokes_count];

        for ( int i = 0; i < m_WheelModel.spokes_count; ++i )
        {
            Vector3D hub_point = m_WheelModel.get_hub_point (i);
            Vector3D nip_point = m_WheelModel.getRimPointBySpoke (i, m_bent_on_every_100kgf, m_egg_on_every_100kgf);
            Vector3D rim_center = m_WheelModel.get_rim_center();
            Vector3D nip_hub = hub_point - nip_point;

            // spokes    
            Mesh spoke_mesh = MeshCreator.createCylinder (0.001, nip_hub.length(), 8, nip_point, nip_hub.normalized(), 0.0 );

            GameObject spoke_GO = new GameObject("spoke");
            spoke_GO.transform.parent = m_wheel_GO.transform;
            spoke_GO.AddComponent<MeshFilter>().mesh = spoke_mesh;
            spoke_GO.AddComponent<MeshRenderer>().material = m_spoke_mat;

            //nipple
            float nipple_rev_deg = (float)m_WheelModel.getNippleRotate(i) * m_WheelModel_force_modifier * Mathf.PI * 2 * Mathf.Rad2Deg;

            GameObject nipple_GO = Instantiate (m_nipple_prefab);
            nipple_GO.transform.parent = spoke_GO.transform;
            nipple_GO.transform.localPosition = nip_point.toVector3();
            nipple_GO.transform.localRotation = Quaternion.LookRotation( (hub_point - nip_point).normalized().toVector3(),
                m_WheelModel.get_rim_cross_axis().toVector3() )
                                                *
                                                Quaternion.AngleAxis( nipple_rev_deg + m_WheelModel_spokes_rev90_offset[i] * 90, -Vector3.forward );
            m_wheel_nipples_GO[i] = nipple_GO;

            // eyelets
            Vector3D eyelet_center = nip_point;
            Vector3D eyelet_vector = m_WheelModel.get_rim_center() - eyelet_center;

            double eyelet_inner_dia = 0.0038;
            double eyelet_outter_dia = 0.0075;
            double eyelet_torus_circle_radius = (eyelet_outter_dia - eyelet_inner_dia) / 4;
            int eyelet_half_torus_points_count = 3;
            int eyelet_section_count = 12;

            Points3D[] eyelet_sections = new Points3D[eyelet_section_count];

            double torus_rad_per_section = 2*Math.PI / (eyelet_section_count);

            for ( int n_section = 0; n_section < eyelet_section_count; ++n_section )
            {
                Points3D eyelet_section = new Points3D();
                double torus_rad_per_point = Math.PI / (eyelet_half_torus_points_count-1);
                for ( int n_point = 0; n_point < eyelet_half_torus_points_count; ++n_point )
                {
                    eyelet_section.addPoint(
                    new Vector3D( Math.Cos( n_point * torus_rad_per_point ) * eyelet_torus_circle_radius, Math.Sin( n_point * torus_rad_per_point ) * eyelet_torus_circle_radius, 0 )
                    );
                }
                eyelet_section.move( new Vector3D( -(eyelet_inner_dia / 2 + eyelet_torus_circle_radius), 0, 0 ) );
                eyelet_section.rotate( new Vector3D( 0, 1, 0 ), n_section * torus_rad_per_section, Vector3D.zero );
                eyelet_section.rotate( Matrix3x3.getRotationMatrixLookAtYAxis( new Vector3D( 0, 0, 1 ), eyelet_vector.normalized() ), Vector3D.zero );
                eyelet_section.move( eyelet_center );

                eyelet_sections[n_section] = eyelet_section;
            }
            Mesh eyelet_mesh = MeshCreator.createFromLoopPoints3D (eyelet_sections);

            GameObject eyelet_GO = new GameObject("eyelet");
            eyelet_GO.transform.parent = spoke_GO.transform;
            eyelet_GO.AddComponent<MeshFilter>().mesh = eyelet_mesh;
            eyelet_GO.AddComponent<MeshRenderer>().material = m_eyelet_mat;

            Mesh eyelet_low_circle_mesh = MeshCreator.createCircle (eyelet_inner_dia/2 + eyelet_torus_circle_radius, eyelet_section_count, eyelet_center + eyelet_vector.normalized()*eyelet_torus_circle_radius/2, eyelet_vector.normalized(), false );
            GameObject eyelet_low_circle_GO = new GameObject("eyelet_low_circle");
            eyelet_low_circle_GO.transform.parent = eyelet_GO.transform;
            eyelet_low_circle_GO.AddComponent<MeshFilter>().mesh = eyelet_low_circle_mesh;
            eyelet_low_circle_GO.AddComponent<MeshRenderer>().material = m_eyelet_circle_mat;

        }

        m_wheel_GO.transform.rotation = wheel_rotation;
        m_wheel_spoke_wrench_GO_updateTransformByCurrentSpoke();

    }

    void m_wheel_spoke_wrench_GO_updateTransformByCurrentSpoke ()
    {
        if ( m_current_spoke != -1 )
        {
            m_wheel_spoke_wrench_GO.transform.localPosition = m_wheel_nipples_GO[m_current_spoke].transform.localPosition;
            m_wheel_spoke_wrench_GO.transform.localRotation = m_wheel_nipples_GO[m_current_spoke].transform.localRotation;
        }
    }

    void setEngineState ( EEngineState state )
    {
        if ( m_state == state )
            return;

        switch ( m_state )
        {
            case EEngineState.view1:
            {
                m_view1_Panel.SetActive( false );
                break;
            };

            case EEngineState.view2:
            {
                m_view2_Panel.SetActive( false );
                break;
            };

            case EEngineState.options:
            {
                m_options_Panel.SetActive( false );
                break;
            };

            case EEngineState.help_quality:
            {
                m_helpQuality_Panel.SetActive( false );
                break;
            };
        };

        switch ( state )
        {
            case EEngineState.view1:
            {
                m_view1_Panel.SetActive( true );
                m_Camera_setView( CameraView.view1 );
                break;
            };

            case EEngineState.view2:
            {
                m_view2_Panel.SetActive( true );
                m_Camera_setView( CameraView.view2 );
                break;
            };

            case EEngineState.options:
            {
                m_options_Panel.SetActive( true );
                m_Camera_setView( CameraView.view1 );
                break;
            };

            case EEngineState.help_quality:
            {
                m_helpQuality_Panel.SetActive( true );
                break;
            };

        };

        m_state = state;
    }

    struct fingerUpdateData
    {
        public enum EType
        {
            none,
            moving_in_YZ_plane,
            wheel_rotate
        };
        public EType m_GO_control_type;

        public GameObject  m_GO;

        public Plane       m_moving_GO_in_YZ_plane_Plane;
        public Vector3     m_moving_GO_in_YZ_plane_Plane_start_hit_point;
        public Vector3     m_moving_GO_in_YZ_plane_GO_start_point;
        public Bounds      m_moving_GO_in_YZ_plane_GO_bounds;

        public float m_wheel_rotate_Y_modifier;
    }

    fingerUpdateData[] m_fingerUpdateData = new fingerUpdateData[10];

    bool m_fingerUpdateData_isGOControlledByAnyFinger ( GameObject GO )
    {
        for ( int i_finger = 0; i_finger < m_fingerUpdateData.Length; ++i_finger )
            if ( m_fingerUpdateData[i_finger].m_GO == GO )
                return true;
        return false;
    }

    void Update ()
    {
        int touchCount = PlatformInput.g.touchCount;

        if ( EventSystem.current != null )
            if ( !EventSystem.current.IsPointerOverGameObject() )
                for ( int i = 0; i < touchCount; ++i )
                {
                    Touch       touch = PlatformInput.g.getTouch(i);
                    int         touch_fingerId = touch.fingerId;
                    Ray         touch_world_ray = m_Camera.ScreenPointToRay( touch.position );
                    GameObject  touch_world_ray_hit_obj = null;
                    Collider    touch_world_ray_hit_collider = null;
                    RaycastHit  touch_world_ray_hit_info;

                    if ( Physics.Raycast( touch_world_ray, out touch_world_ray_hit_info, float.MaxValue ) )
                    {
                        touch_world_ray_hit_collider = touch_world_ray_hit_info.collider;
                        touch_world_ray_hit_obj = touch_world_ray_hit_collider.gameObject;
                    }

                    if ( touch.phase == TouchPhase.Began )
                    {
                        m_fingerUpdateData[touch_fingerId].m_GO = null;
                        m_fingerUpdateData[touch_fingerId].m_GO_control_type = fingerUpdateData.EType.none;

                        if ( touch_world_ray_hit_obj != null &&
                            !m_fingerUpdateData_isGOControlledByAnyFinger( touch_world_ray_hit_obj ) )
                        {
                            if ( touch_world_ray_hit_obj.name == "HDialIndicator" ||
                                touch_world_ray_hit_obj.name == "VDialIndicator" )
                            {

                                m_fingerUpdateData[touch_fingerId].m_GO = touch_world_ray_hit_obj;
                                m_fingerUpdateData[touch_fingerId].m_GO_control_type = fingerUpdateData.EType.moving_in_YZ_plane;

                                m_fingerUpdateData[touch_fingerId].m_moving_GO_in_YZ_plane_Plane = new Plane( Vector3.left, touch_world_ray_hit_obj.transform.position );

                                float ray_dist;
                                m_fingerUpdateData[touch_fingerId].m_moving_GO_in_YZ_plane_Plane.Raycast( touch_world_ray, out ray_dist );
                                m_fingerUpdateData[touch_fingerId].m_moving_GO_in_YZ_plane_Plane_start_hit_point = touch_world_ray.origin + touch_world_ray.direction * ray_dist;
                                m_fingerUpdateData[touch_fingerId].m_moving_GO_in_YZ_plane_GO_start_point = touch_world_ray_hit_obj.transform.position;

                                if ( touch_world_ray_hit_obj.name == "HDialIndicator" )
                                {
                                    m_fingerUpdateData[touch_fingerId].m_moving_GO_in_YZ_plane_GO_bounds
                                        = new Bounds( new Vector3( 0, (float) -m_WheelModel.rim_d / 2 - m_rim_profile_height * 3f / 4f, m_rim_profile_width * 2 )
                                                                                - m_HDialIndicator.getStickDefaultWorldOffset(),
                                                                                       new Vector3( 0, m_rim_profile_height * 1.5f, m_rim_profile_width * 1.5f ) );
                                } else
                                if ( touch_world_ray_hit_obj.name == "VDialIndicator" )
                                {
                                    m_fingerUpdateData[touch_fingerId].m_moving_GO_in_YZ_plane_GO_bounds
                                       = new Bounds( new Vector3( 0, (float) -m_WheelModel.rim_d / 2 - m_rim_profile_height - m_VDialIndicator.getStickTravelMax(), 0 )
                                                                                    - m_VDialIndicator.getStickWorldOffset(),
                                                                                      new Vector3( 0, m_VDialIndicator.getStickTravelMax(), m_rim_profile_width / 2 ) );
                                }

                            } else
                            if ( touch_world_ray_hit_obj.layer == layer_wheel_box )
                            {
                                if ( !m_is_spoke_rolling & !m_is_wheel_rotating )
                                {
                                    m_fingerUpdateData[touch_fingerId].m_GO = m_wheel_GO;
                                    m_fingerUpdateData[touch_fingerId].m_GO_control_type = fingerUpdateData.EType.wheel_rotate;
                                    m_fingerUpdateData[touch_fingerId].m_wheel_rotate_Y_modifier = 0.1f;
                                    m_is_wheel_rotating = true;

                                    m_wheel_angular_velocity = 0.0f;
                                }
                            }
                        }

                    } else
                    if ( touch.phase == TouchPhase.Moved )
                    {
                        if ( m_fingerUpdateData[touch_fingerId].m_GO != null )
                        {
                            switch ( m_fingerUpdateData[touch_fingerId].m_GO_control_type )
                            {
                                case fingerUpdateData.EType.moving_in_YZ_plane:
                                {
                                    float ray_dist;
                                    if ( m_fingerUpdateData[touch_fingerId].m_moving_GO_in_YZ_plane_Plane.Raycast( touch_world_ray, out ray_dist ) )
                                    {
                                        Vector3 hit_point = touch_world_ray.origin + touch_world_ray.direction*ray_dist;

                                        Vector3 diff = (hit_point - m_fingerUpdateData[touch_fingerId].m_moving_GO_in_YZ_plane_Plane_start_hit_point );

                                        Vector3 new_pos = m_fingerUpdateData[touch_fingerId].m_moving_GO_in_YZ_plane_GO_start_point + diff;

                                        new_pos.x = Mathf.Clamp( new_pos.x, m_fingerUpdateData[touch_fingerId].m_moving_GO_in_YZ_plane_GO_bounds.min.x,
                                                                            m_fingerUpdateData[touch_fingerId].m_moving_GO_in_YZ_plane_GO_bounds.max.x );

                                        new_pos.y = Mathf.Clamp( new_pos.y, m_fingerUpdateData[touch_fingerId].m_moving_GO_in_YZ_plane_GO_bounds.min.y,
                                                                            m_fingerUpdateData[touch_fingerId].m_moving_GO_in_YZ_plane_GO_bounds.max.y );

                                        new_pos.z = Mathf.Clamp( new_pos.z, m_fingerUpdateData[touch_fingerId].m_moving_GO_in_YZ_plane_GO_bounds.min.z,
                                                                            m_fingerUpdateData[touch_fingerId].m_moving_GO_in_YZ_plane_GO_bounds.max.z );

                                        m_fingerUpdateData[touch_fingerId].m_GO.transform.position = new_pos;
                                    }
                                    break;
                                }

                                case fingerUpdateData.EType.wheel_rotate:
                                {

                                    m_fingerUpdateData[touch_fingerId].m_GO.transform.Rotate( Vector3.forward, touch.deltaPosition.y * m_fingerUpdateData[touch_fingerId].m_wheel_rotate_Y_modifier );
                                    break;
                                }
                            }
                        }
                    } else
                    if ( touch.phase == TouchPhase.Ended ||
                        touch.phase == TouchPhase.Canceled
                        )
                    {
                        switch ( m_fingerUpdateData[touch_fingerId].m_GO_control_type )
                        {
                            case fingerUpdateData.EType.wheel_rotate:
                            {
                                m_wheel_angular_velocity = (m_wheel_angle - m_wheel_angle_prev) / Time.deltaTime;
                                m_is_wheel_rotating = false;
                                break;
                            }
                        }


                        m_fingerUpdateData[touch_fingerId].m_GO = null;
                    }
                }

        m_wheel_angle_prev = m_wheel_angle;
        m_wheel_angle = m_wheel_GO.transform.rotation.eulerAngles.z;

        m_current_spoke = m_WheelModel.getNearestSpokeByAngle( 180 + m_wheel_GO.transform.rotation.eulerAngles.z );
        m_current_spoke_rotate = m_WheelModel.getNippleRotate( m_current_spoke );

        if ( m_wheel_angular_velocity == 0.0f )
        {
            m_wheel_spoke_wrench_GO.SetActive( true );
        } else
        {
            m_wheel_spoke_wrench_GO.SetActive( false );

            m_wheel_GO.transform.Rotate( Vector3.forward, m_wheel_angular_velocity * Time.deltaTime );

            m_wheel_angular_velocity = m_wheel_angular_velocity - m_wheel_angular_velocity * Time.deltaTime * m_wheel_angular_damping;

            if ( Mathf.Abs( m_wheel_angular_velocity ) < 0.005f )
                m_wheel_angular_velocity = 0.0f;
        }

        m_wheel_spoke_wrench_GO_updateTransformByCurrentSpoke();
    }

    void OnGUI ()
    {
        int line_height = m_log_GUIStyle.fontSize;
        int line_width = Screen.width / 2;
        int max_screen_lines = Screen.height / line_height;
        for ( int i = 0; i < max_screen_lines; ++i )
            GUI.Label( new Rect( 0, Screen.height - line_height * (i + 1), line_width, line_height ), Log.g.getLine( Log.g.getLineCount() - i - 1 ), m_log_GUIStyle );
    }

    #region UI

    public void EngineUI_view1_onSpokeSoundClick ()
    {
        if ( m_current_spoke != -1 )
        {
            double spoke_force = m_WheelModel.getSpokeForceN(m_current_spoke);
            if ( spoke_force > 15.0 )
            {
                float frequency = (float) ( Math.Sqrt (spoke_force/m_WheelModel.getSpokeUnitMass()) / (2*m_WheelModel.getSpokeTightenedLength(m_current_spoke) ) );

                m_AudioSource.clip = m_spokes_AudioClips[UnityEngine.Random.Range( 0, m_spokes_AudioClips.Length )];
                m_AudioSource.pitch = frequency / m_spokes_AudioClips_reference_frequency;
                m_AudioSource.Play();
            } else
                m_AudioSource.Stop();
        }
    }

    public void EngineUI_options_onBackButtonClick ()
    {
        setEngineState( EEngineState.view1 );
    }

    public void EngineUI_options_onNewWheelButtonClick ()
    {
        m_WheelModel_random_unbalance();
        setEngineState( EEngineState.view1 );
    }

    public void EngineUI_options_onExitButtonClick ()
    {
        Application.Quit();
    }

    public void EngineUI_view2_onView1ButtonClick ()
    {
        setEngineState( EEngineState.view1 );
    }

    public void EngineUI_view1_onOptionsButtonClick ()
    {
        setEngineState( EEngineState.options );
    }

    public void EngineUI_view1_onView2ButtonClick ()
    {
        setEngineState( EEngineState.view2 );
    }

    public void EngineUI_view1_onHelpQualityButtonClick ()
    {
        setEngineState( EEngineState.help_quality );
    }

    public void EngineUI_view1_spokeRoller_onRollStart ( TouchRollerEventData ev )
    {
        if ( m_wheel_angular_velocity != 0.0f )
        {
            ev.is_cancel_roll = true;
            return;
        }

        m_is_spoke_rolling = true;
        m_spoke_rolling_initial_nipple_rotation = m_wheel_nipples_GO[m_current_spoke].transform.localRotation; //m_WheelModel.get_nipple_rotate( m_selected_spoke );
    }

    public void EngineUI_view1_spokeRoller_onRoll ( TouchRollerEventData ev )
    {
        float deg = ev.value * m_WheelModel_force_modifier * Mathf.PI * 2 * Mathf.Rad2Deg;

        m_wheel_nipples_GO[m_current_spoke].transform.localRotation
            = m_spoke_rolling_initial_nipple_rotation * Quaternion.AngleAxis( deg, -Vector3.forward );
    }

    public void EngineUI_view1_spokeRoller_onRollEnd ( TouchRollerEventData ev )
    {
        m_is_spoke_rolling = false;

        m_wheel_nipples_GO[m_current_spoke].transform.localRotation = m_spoke_rolling_initial_nipple_rotation;
        if ( ev.value > 0 && m_WheelModel.getSpokeForceKgf( m_current_spoke ) > 200.0 )
        {
            //overtight
            return;
        }

        m_WheelModel.nippleRotate( m_current_spoke, ev.value );
        m_WheelModel_compute_and_rebuild_mesh();
    }

    public void EngineUI_helpQuality_onView1ButtonClick ()
    {
        setEngineState( EEngineState.view1 );
    }

    #endregion

}
