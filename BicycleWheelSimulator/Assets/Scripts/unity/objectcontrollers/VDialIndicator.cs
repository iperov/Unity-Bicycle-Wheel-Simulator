using UnityEngine;

namespace unity.objectcontrollers
{
    public class VDialIndicator : MonoBehaviour
    {

        public GameObject m_stick_GO;
        public GameObject m_big_arrow_GO;
        public GameObject m_small_arrow_GO;

        Vector3 m_stick_initial_local_pos;

        public Vector3 m_colliding_box_size = new Vector3(0.020f, 0.015f, 0.015f);

        public float m_max_hit_distance = 0.04f;
        int m_collide_layer_mask;

        public void initialize (int collide_layer_mask)
        {
            m_collide_layer_mask = collide_layer_mask;
            m_stick_initial_local_pos = m_stick_GO.transform.localPosition;
        }
        
        void Update ()
        {
            RaycastHit hit_info;

            float hit_distance = m_max_hit_distance;
            
            //
            float stick_half_width = 0.01f;
            float stick_width_step = stick_half_width*2 / 20;

            for ( float f = -stick_half_width; f <= stick_half_width; f += stick_width_step )
            {
                if ( Physics.Raycast( gameObject.transform.TransformPoint( m_stick_initial_local_pos + new Vector3( f, 0, 0 ) ), gameObject.transform.TransformDirection( Vector3.up ), out hit_info, m_max_hit_distance, m_collide_layer_mask ) )
                {
                    if ( hit_info.distance < hit_distance )
                        hit_distance = hit_info.distance;
                }
            }
          
            m_stick_GO.transform.localPosition = m_stick_initial_local_pos + new Vector3( 0, hit_distance, 0 );

            float big_dial_int =  (m_max_hit_distance - hit_distance) * 1000.0f; 
            big_dial_int = big_dial_int - (int)big_dial_int ;

            m_big_arrow_GO.transform.localRotation = Quaternion.AngleAxis( -big_dial_int * 360.0f, Vector3.forward );

            float small_dial_int =  (m_max_hit_distance - hit_distance) * 1000.0f; 
  
            m_small_arrow_GO.transform.localRotation = Quaternion.AngleAxis( -( small_dial_int / 10.0f ) * 360.0f, Vector3.forward );
        }


        public void moveByStickPosition (Vector3 world_pos)
        {
            Vector3 stick_initial_world_pos = gameObject.transform.TransformPoint (m_stick_initial_local_pos);
            Vector3 world_diff = stick_initial_world_pos - gameObject.transform.position;

            gameObject.transform.position = world_pos - world_diff;
        }

        public Vector3 getStickWorldOffset()
        {
            return gameObject.transform.TransformPoint (m_stick_initial_local_pos) - gameObject.transform.position;
        }

        public float getStickTravelMax()
        {
            return m_max_hit_distance;
        }

        
    }

}