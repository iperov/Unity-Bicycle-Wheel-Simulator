using UnityEngine;
using UnityEngine.Events;

namespace unity.ui
{
    public class TouchRollerEventData
    {
        public float value;
        public bool is_cancel_roll;
    }

    public class TouchRoller: MonoBehaviour
    {
        RectTransform m_RectTransform;
        int m_touch_finger_id = -1;

        Vector2 m_touch_initial_position;
        float m_last_correct_value;

        public float m_value_per_width = 1.0f;

        [SerializeField]
        public UnityEventTouchRollerEventData m_onRollStart = new UnityEventTouchRollerEventData();
        [SerializeField]
        public UnityEventTouchRollerEventData m_onRoll = new UnityEventTouchRollerEventData();
        [SerializeField]
        public UnityEventTouchRollerEventData m_onRollEnd = new UnityEventTouchRollerEventData();

        TouchRollerEventData m_ev = new TouchRollerEventData();

        void Start()
        {
            m_RectTransform = GetComponent<RectTransform>();

        }

        void Update()
        {
            if ( m_touch_finger_id == -1 )
            {
                Touch touch = PlatformInput.g.getTouchInRectTransform (m_RectTransform);
                if ( touch.fingerId >= 0 )
                {
                    if ( touch.phase == TouchPhase.Began )
                    {
                        m_touch_finger_id = touch.fingerId;
                        m_touch_initial_position = touch.position;

                        m_ev.is_cancel_roll = false;
                        m_ev.value = 0;
                        UnityEventTouchRollerEventData.invoke (m_onRollStart, m_ev);   
                        if ( m_ev.is_cancel_roll )
                        {
                            m_touch_finger_id = -1;
                        }
                    }
                }
            }
            else
            {
                Touch touch = PlatformInput.g.getTouchByFingerID (m_touch_finger_id);
                if (touch.fingerId == m_touch_finger_id)
                {
                    Vector2 pos = touch.position - m_touch_initial_position;
                    float value = pos.x * (m_value_per_width / m_RectTransform.rect.width);
                    value = Mathf.Clamp( value, -m_value_per_width, m_value_per_width );

                    if ( touch.phase == TouchPhase.Moved )
                    {      
                        if ( RectTransformUtility.RectangleContainsScreenPoint( m_RectTransform, touch.position) )
                        {
                            m_last_correct_value = value;

                            m_ev.is_cancel_roll = false;
                            m_ev.value = value;

                            UnityEventTouchRollerEventData.invoke( m_onRoll, m_ev );
                            if ( m_ev.is_cancel_roll )
                            {
                                m_touch_finger_id = -1;
                            }
                        }
                    } else

                    if ( touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    {
                        m_ev.is_cancel_roll = false;
                        m_ev.value = value;

                        if ( !RectTransformUtility.RectangleContainsScreenPoint( m_RectTransform, touch.position ) )
                            m_ev.value = m_last_correct_value;

                        UnityEventTouchRollerEventData.invoke (m_onRollEnd, m_ev);
                        m_touch_finger_id = -1;
                    }
                } else
                {//error
                    m_touch_finger_id = -1;   
                }
            } 
        }
    }

    [System.Serializable]
    public class UnityEventTouchRollerEventData : UnityEvent<TouchRollerEventData>
    {

        public static void invoke (UnityEventTouchRollerEventData ev, TouchRollerEventData roller_ev)
        {
            for(int i = 0 ; i < ev.GetPersistentEventCount();i++ )    
             ((MonoBehaviour)ev.GetPersistentTarget(i)).SendMessage(ev.GetPersistentMethodName(i),roller_ev);
            
        }
    }
}
