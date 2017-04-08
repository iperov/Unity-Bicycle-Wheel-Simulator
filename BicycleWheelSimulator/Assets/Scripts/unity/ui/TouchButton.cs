using UnityEngine;
using UnityEngine.Events;

namespace unity.ui
{
    public class TouchButton : MonoBehaviour
    {
        RectTransform m_RectTransform;

        int m_touch_finger_id = -1;
        
        public UnityEvent m_onMultiTouchClick;

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
                    }
                }
            }
            else
            {
                Touch touch = PlatformInput.g.getTouchByFingerID (m_touch_finger_id);
                if (touch.fingerId == m_touch_finger_id)
                {
                    if ( touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    {
                        if ( PlatformInput.g.isTouchInRectTransform(ref touch, m_RectTransform) )
                            m_onMultiTouchClick.Invoke();
                        m_touch_finger_id = -1;
                    }
                } else
                {//error
                    m_touch_finger_id = -1;   
                }
            } 
        }
    }
}
