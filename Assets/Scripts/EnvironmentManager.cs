//Handles camera rotation around a target position or around camera itself
//Accepts mouse input
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EnvironmentManager : MonoBehaviour
{
    Vector3 lastMousePos;
    Vector3 lastTargetPosition;
    Vector3 last_offset;
    public int target_frame_rate = 30;
    public List<GameObject> newFocusTargets = new List<GameObject>();
    public List<GameObject> cameraFocusTargets = new List<GameObject>();
    Vector3 prevFocusPos;
    float totalAngleY = 0, totalAngleX = 0;

    Vector3 GetAveragePosition(List<GameObject> list)
    {
        var avg = new Vector3(
                list.Average(x=>x.transform.position.x),
                list.Average(x=>x.transform.position.y),
                list.Average(x=>x.transform.position.z));
        return avg;
    }

    void Start()
    {
        lastTargetPosition = GetAveragePosition(cameraFocusTargets);
        last_offset = transform.position - lastTargetPosition;
        RenderSettings.skybox.SetFloat("_Rotation", 180.0f);
        Application.targetFrameRate = target_frame_rate;
    }

    void rotateWorldMouse()
    {
        float yangle = .5f * (Input.mousePosition.x - lastMousePos.x);
        float xangle = -.5f * (Input.mousePosition.y - lastMousePos.y);
        lastMousePos = Input.mousePosition;
        totalAngleY += yangle;
        transform.Rotate(0, yangle, 0, Space.World);

        if (totalAngleX + xangle > 80 || totalAngleX + xangle < -80)
            return;
        totalAngleX += xangle;
        transform.Rotate(xangle, 0, 0, Space.Self);
    }
    void rotateAroundTarget()
    {
        if (!cameraFocusTargets.Any())
        {
            return;
        }
        Vector3 target_position = GetAveragePosition(cameraFocusTargets);
        float hori_angle = .5f * (Input.mousePosition.x - lastMousePos.x);
        float elev_angle = -.5f * (Input.mousePosition.y - lastMousePos.y * Mathf.Sign(Input.mousePosition.y));
        lastMousePos = Input.mousePosition;

        transform.RotateAround(target_position, Vector3.up, hori_angle);

        //find actual pitch
        if (totalAngleX + elev_angle > 88 || totalAngleX + elev_angle < -88)
        {
            transform.LookAt(target_position);
            return;
        }
        transform.RotateAround(target_position, Vector3.right, elev_angle);
        transform.LookAt(target_position);
    }

    public float ScrollSensitvity = 2f;
    public float FollowSensitivity = .0001f;

    //readjust magnitude of vector relative to target
    void UpdateScroll(Vector3 target_position) {
        //scroll camera back and forth
        float scrollAmount = Input.GetAxis("Mouse ScrollWheel") * ScrollSensitvity;
        if (scrollAmount != 0f)
        {
            Vector3 toTarget = transform.position - target_position;
            float distance = toTarget.magnitude + scrollAmount;
            distance = Mathf.Clamp(distance, 1f, 30f);
            var orig_position = transform.position;
            transform.position = toTarget.normalized * distance + target_position;
            float actual_distance = (transform.position - orig_position).magnitude;
            Debug.Log("Non zero scroll amount! " + scrollAmount.ToString()
                + " distance: " + distance.ToString()
                + "actual distance: " + actual_distance);
        }
    }

    // Update is called once per frame
    void Update()
    {
        var target_position = lastTargetPosition;

        //FIXME first time switching to a new object, camera does not aim at object!
        if (cameraFocusTargets.Any())
        {
            //force camera to follow target each frame
            target_position = GetAveragePosition(cameraFocusTargets);
            lastTargetPosition = Vector3.Lerp(lastTargetPosition, target_position, Time.deltaTime * FollowSensitivity);
            transform.position = lastTargetPosition + last_offset;
        }

        UpdateScroll(target_position);

        //rotate camera around a position
        bool ctrl_key = Input.GetKey(KeyCode.LeftControl);
        bool lmb_down = Input.GetMouseButtonDown(0);
        bool rmb_down = Input.GetMouseButtonDown(1);
        if (ctrl_key && lmb_down || rmb_down)
        {
            lastMousePos = Input.mousePosition;
        }
        bool lmb = Input.GetMouseButton(0);
        bool rmb = Input.GetMouseButton(1);
        bool buttonEvent = ctrl_key && lmb || rmb;

        if (buttonEvent) rotateAroundTarget();
        last_offset = transform.position - lastTargetPosition;
    }

    protected Vector3 _LocalRotation;
    protected float _CameraDistance = 10f;

    public float MouseSensitivity = 4f;
    public float OrbitDampening = 10f;
    public float ScrollDampening = 6f;
}
