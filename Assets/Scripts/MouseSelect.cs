//12/26/2019 taken from https://hyunkell.com/blog/rts-style-unit-selection-in-unity-5/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public static class Utils
{
    static Texture2D _whiteTexture;
    public static Texture2D WhiteTexture
    {
        get
        {
            if( _whiteTexture == null )
            {
                _whiteTexture = new Texture2D( 1, 1 );
                _whiteTexture.SetPixel( 0, 0, Color.white );
                _whiteTexture.Apply();
            }
            return _whiteTexture;
        }
    }

    public static void DrawScreenRect( Rect rect, Color color )
    {
        GUI.color = color;
        GUI.DrawTexture( rect, WhiteTexture );
        GUI.color = Color.white;
    }

    public static void DrawScreenRectBorder( Rect rect, float thickness, Color color )
    {
        // Top
        Utils.DrawScreenRect( new Rect( rect.xMin, rect.yMin, rect.width, thickness ), color );
        // Left
        Utils.DrawScreenRect( new Rect( rect.xMin, rect.yMin, thickness, rect.height ), color );
        // Right
        Utils.DrawScreenRect( new Rect( rect.xMax - thickness, rect.yMin, thickness, rect.height ), color);
        // Bottom
        Utils.DrawScreenRect( new Rect( rect.xMin, rect.yMax - thickness, rect.width, thickness ), color );
    }

    public static Rect GetScreenRect( Vector3 screenPosition1, Vector3 screenPosition2 )
    {
        // Move origin from bottom left to top left
        screenPosition1.y = Screen.height - screenPosition1.y;
        screenPosition2.y = Screen.height - screenPosition2.y;
        // Calculate corners
        var topLeft = Vector3.Min( screenPosition1, screenPosition2 );
        var bottomRight = Vector3.Max( screenPosition1, screenPosition2 );
        // Create Rect
        return Rect.MinMaxRect( topLeft.x, topLeft.y, bottomRight.x, bottomRight.y );
    }

    public static Bounds GetViewportBounds( Camera camera, Vector3 screenPosition1, Vector3 screenPosition2 )
    {
        var v1 = Camera.main.ScreenToViewportPoint( screenPosition1 );
        var v2 = Camera.main.ScreenToViewportPoint( screenPosition2 );
        var min = Vector3.Min( v1, v2 );
        var max = Vector3.Max( v1, v2 );
        min.z = camera.nearClipPlane;
        max.z = camera.farClipPlane;

        var bounds = new Bounds();
        bounds.SetMinMax( min, max );
        return bounds;
    }
}


public class MouseSelect : MonoBehaviour
{
    EnvironmentManager em;
    bool isSelecting = false;
    Vector3 mouse_position1;
    int prev_num_selected = 0;
    Camera camera;

    void Start()
    {
        var go = GameObject.FindObjectOfType<EnvironmentManager>();
        em = go.GetComponent<EnvironmentManager>();
        camera = Camera.main;
    }

    void Swap<T>(ref T a, ref T b)
    {
        var tmp = a;
        a = b;
        b = tmp;
    }

    //triggers menu buttons or unit selection
    bool SingleSelect(Vector2 mouse_position)
    {
        RaycastHit hit;
        Ray ray = camera.ScreenPointToRay(mouse_position);
        if(Physics.Raycast(ray, out hit))
        {
            if(hit.collider.isTrigger)
            {
                //Do the thing
                Debug.Log("hit a thing! " + hit.collider.gameObject.name);
                //invoke event
                hit.collider.GetComponent<TestEvent>().m_MyEvent.Invoke();

                return true;
            }
        }
        return false;
    }

    void Update()
    {
        // If we press the left mouse button, save mouse location and begin selection
        if( Input.GetMouseButtonDown( 0 ) )
        {
            isSelecting = true;
            mouse_position1 = Input.mousePosition;
            if (SingleSelect(mouse_position1))
            {
                return;
            }
        }
        // If we let go of the left mouse button, end selection
        if( Input.GetMouseButtonUp( 0 ) )
            isSelecting = false;

        // select by drag box
        if (isSelecting)
        {
            var gos = GameObject.FindGameObjectsWithTag("unit");
            int num_selected = 0;
            var selection = new List<GameObject>();
            foreach (var go in gos)
            {
                num_selected += IsWithinSelectionBounds(go) ? 1 : 0;
                if (IsWithinSelectionBounds(go))
                {
                    selection.Add(go);
                }
            }
            if (num_selected != prev_num_selected)
            {
                Debug.Log("Num selected: " + num_selected.ToString());
            }
            prev_num_selected = num_selected;
            Swap(ref selection, ref em.newFocusTargets);
        }
    }

    public bool IsWithinSelectionBounds( GameObject gameObject )
    {
        if( !isSelecting )
            return false;

        var viewportBounds =
            Utils.GetViewportBounds( camera, mouse_position1, Input.mousePosition );

        return viewportBounds.Contains(
            camera.WorldToViewportPoint( gameObject.transform.position ) );
    }

    void OnGUI()
    {
        if( isSelecting )
        {
            // Create a rect from both mouse positions
            var rect = Utils.GetScreenRect( mouse_position1, Input.mousePosition );
            Utils.DrawScreenRect( rect, new Color( 0.8f, 0.8f, 0.95f, 0.25f ) );
            Utils.DrawScreenRectBorder( rect, 2, new Color( 0.8f, 0.8f, 0.95f ) );
        }
    }
}
