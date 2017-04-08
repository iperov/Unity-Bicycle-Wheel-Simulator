using UnityEngine;

namespace unity
{
    public class PlatformInput : MonoBehaviour
    {
        static public PlatformInput g;


        int m_last_framecount = -1;

        Touch[] m_touches_prev = null;
        Touch[] m_touches = null;

        public PlatformInput()
        {
            g = this;
        }

        void updateOncePerFrame()
        {
            if ( m_last_framecount != Time.frameCount )
            {
                m_last_framecount = Time.frameCount;

                m_touches_prev = m_touches;
                m_touches = null;
                
                if ( Input.touchSupported )
                {
                    m_touches = Input.touches;                   
                } else
                {
                    Touch mouse_touch = new Touch();
                    mouse_touch.fingerId = -1;
                    if ( Input.GetMouseButtonDown( 0 ) )
                    {
                        mouse_touch.position = new Vector2( Input.mousePosition.x, Input.mousePosition.y );
                        mouse_touch.phase = TouchPhase.Began;
                        mouse_touch.fingerId = 0;
                    } else if ( Input.GetMouseButtonUp( 0 ) )
                    {
                        mouse_touch.position = new Vector2( Input.mousePosition.x, Input.mousePosition.y );
                        mouse_touch.phase = TouchPhase.Ended;
                        mouse_touch.fingerId = 0;
                    } else if ( Input.GetMouseButton( 0 ) )
                    {
                        mouse_touch.position = new Vector2( Input.mousePosition.x, Input.mousePosition.y );
                        mouse_touch.phase = TouchPhase.Moved;
                        mouse_touch.fingerId = 0;
                    }
                    if ( mouse_touch.fingerId != -1 )
                        m_touches = new Touch[1] { mouse_touch };
                }

                if ( m_touches != null && m_touches_prev != null )
                    for ( int i = 0; i < m_touches.Length; ++i )
                        for ( int j = 0; j < m_touches_prev.Length; ++j )
                            if ( m_touches[i].fingerId == m_touches_prev[j].fingerId )
                            {
                                m_touches[i].deltaPosition = m_touches[i].position - m_touches_prev[j].position;

                                if ( m_touches[i].phase == TouchPhase.Ended || m_touches[i].phase == TouchPhase.Canceled )
                                {
                                    m_touches[i].position = m_touches_prev[j].position;
                                    m_touches[i].deltaPosition = Vector2.zero;
                                }

                                if ( m_touches[i].phase == TouchPhase.Moved && m_touches[i].deltaPosition.magnitude == 0.0f )
                                    m_touches[i].phase = TouchPhase.Stationary;
                                break;
                            }
            }
        }

        public Touch[] getTouches()
        {        
            updateOncePerFrame();

            return m_touches;
        }

        public int touchCount
        {
            get {
                updateOncePerFrame();

                if ( m_touches == null )
                    return 0;
                return m_touches.Length;
            }
        }

        public Touch getTouch (int index)
        {
            updateOncePerFrame();

            return m_touches[index];
        }

        public Touch getTouchByFingerID (int fingerId)
        {
            int touch_count = touchCount;
            for ( int i = 0; i < touch_count; ++i )
            {
                Touch touch = getTouch(i);
                if ( touch.fingerId == fingerId )
                    return touch;
            }
            return new Touch() { fingerId = -1 };
        }

        public Touch getTouchInRectTransform (RectTransform rt)
        {
            int touch_count = touchCount;
            for (int i=0; i<touch_count; ++i)
            {
                Touch touch = getTouch(i);

                if ( RectTransformUtility.RectangleContainsScreenPoint( rt, touch.position ) )
                    return touch;
            }
            return new Touch() { fingerId = -1 };
        }

        public bool isTouchInRectTransform (ref Touch touch, RectTransform rt)
        {
            return RectTransformUtility.RectangleContainsScreenPoint( rt, touch.position );
        }
    }
}
