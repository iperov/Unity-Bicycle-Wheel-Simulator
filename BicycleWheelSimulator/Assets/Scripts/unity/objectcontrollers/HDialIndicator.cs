using UnityEngine;

namespace unity.objectcontrollers
{
    public class HDialIndicator : MonoBehaviour
    {
        public GameObject m_stick_GO;
        public GameObject m_big_arrow_GO;
        public GameObject m_small_arrow_GO;

        public float m_max_hit_distance = 0.045f;

         Vector3 m_stick_initial_local_pos;
        int m_collide_layer_mask;
  
        public void initialize(int collide_layer_mask)
        {
            m_collide_layer_mask = collide_layer_mask;
             m_stick_initial_local_pos = m_stick_GO.transform.localPosition;
        }

        void Update ()
        {
            RaycastHit hit_info;

            float hit_distance = m_max_hit_distance;

            if ( Physics.Raycast( gameObject.transform.TransformPoint(m_stick_initial_local_pos), gameObject.transform.TransformDirection( Vector3.right ), out hit_info, m_max_hit_distance, m_collide_layer_mask ) )
                hit_distance = hit_info.distance;

            m_stick_GO.transform.localPosition = m_stick_initial_local_pos + Vector3.right*hit_distance;

            float big_dial_int =  (m_max_hit_distance - hit_distance) * 1000.0f; 
            big_dial_int = big_dial_int - (int)big_dial_int ;

            m_big_arrow_GO.transform.localRotation = Quaternion.AngleAxis( -big_dial_int * 360.0f, Vector3.forward );

            float small_dial_int =  (m_max_hit_distance - hit_distance) * 1000.0f; 
  
            m_small_arrow_GO.transform.localRotation = Quaternion.AngleAxis( -( small_dial_int / 10.0f ) * 360.0f, Vector3.forward );

        }

        public Vector3 getStickDefaultWorldOffset()
        {
            return gameObject.transform.TransformPoint (m_stick_initial_local_pos) - gameObject.transform.position;
        }

        public float getStickTravelMax()
        {
            return m_max_hit_distance;
        }

    }

}