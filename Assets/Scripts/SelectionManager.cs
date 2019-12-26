//Takes mouse or xr controlls, finds what they're pointing to, return object being selected
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SelectionManager : MonoBehaviour
{
    EnvironmentManager em;
    GameObject selectedGo;
    Camera camera;

    //move indicators
    public GameObject move_ring;
    public GameObject move_line;
    public GameObject ui_height;
    public GameObject label_distance;

    //TODO move into MoveUnit later
    public Color stem_color;
    float base_height = 0.05f;
    public float offset = 0.15f;
    public float line_width = 0.05f;
    private MaterialPropertyBlock _propBlock;

    class MoveUnit
    {
        public GameObject move_ring;
        public GameObject label_distance;
        public TextMeshPro ld_tmp;
        GameObject move_line;
        public LineRenderer lr;
        public bool done_moving;
        public Vector3 destination;
        public Vector3 h_point;         //co-planar of destination
        public float y_offset;          //y offset from h-point, to be added onto h_point.y
        public GameObject go;           //unit that is moving
        public GameObject base_height;         //ui_height for move unit

        public MoveUnit(float line_width, GameObject ld, GameObject mr, GameObject ml, GameObject unit, GameObject bh)
        {
            go = unit;
            label_distance = ld;
            ld_tmp = label_distance.GetComponent<TextMeshPro>();
            base_height = bh;
            move_ring = mr;
            move_line = ml;
            y_offset = 0f;
            lr = move_line.GetComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = 2;
            lr.SetWidth(line_width, line_width);
            lr.enabled = false;
            done_moving = true;
            //lr.GetPropertyBlock(_propBlock);
            //_propBlock.SetColor("_Color", stem_color);
            //lr.SetPropertyBlock(_propBlock);
        }

        public void StartMoving()
        {
            done_moving = false;
        }

        public bool HasArrived()
        {
            float distance = (go.transform.position - destination).magnitude;
            return (distance < .5f);
        }

        public void SetActive(bool active)
        {
            move_ring.SetActive(active);
            move_line.SetActive(active);
            base_height.SetActive(active);
            label_distance.SetActive(active);
            lr.enabled = active;
            y_offset = 0;
            if (!active)
            {
                done_moving = true;
            }
        }

    }
    MoveUnit ui_move_unit;
    Dictionary<GameObject, MoveUnit> move_units;

    GameObject NewBase(GameObject target)
    {
        var go = Instantiate(ui_height);
        var comp = go.GetComponent<updateHeight>();
        comp.target = target;
        return go;
    }

    void BeginPlanning(MoveUnit mu, GameObject g)
    {
        //TODO assert g is not null
        mu.go = g;
        mu.SetActive(true);
        mu.done_moving = false;
    }


    //on disable, remember to disable base as well
    MoveUnit NewMoveUnit(GameObject unit=null)
    {
        var mr = Instantiate(move_ring);
        var ml = Instantiate(move_line);
        var ld = Instantiate(label_distance);
        GameObject b = NewBase(mr);
        return new MoveUnit(line_width, ld, mr, ml, unit, b);
    }

    //list commands and key combos
    Dictionary<string,string> commands = new Dictionary<string,string>{
        {"move", "rmb;ctrl,lmb"},
        {"focus", "alt,lmb"},
        {"select", "lmb"},
        {"attack", "ctrl,rmb"}
    };

    // Start is called before the first frame update
    void Start()
    {
        camera = GetComponent<Camera>();
        em = GameObject.FindObjectOfType<EnvironmentManager>();
        move_line.SetActive(false);
        move_ring.SetActive(false);
        move_units = new Dictionary<GameObject, MoveUnit>();
        ui_move_unit = NewMoveUnit();
    }

    // Update is called once per frame
    void Update()
    {
        StateMachine();
        //Select();
        MoveIndicator();
        MoveUnits();
    }

    enum MoveState {
        idle,
        planning,
        vertical_planning,
        confirmed
    }

    //TODO make into a UI class
    MoveState move_ui_state = MoveState.idle;
    bool prev_trigger = false;

    //when user starts move command, a ui_move_unit is created to represent the UI
    //when move command is confirmed, the MoveUnit is... 
    //moved over to move_units to have an effect
    void StateMachine()
    {
        //Philosophy: only accept 1 legal command per frame
        //TODO specify key combos for commands; auto resolve based on priority list
        bool ctrl_key = Input.GetKey(KeyCode.LeftControl);
        bool alt_key = Input.GetKey(KeyCode.LeftAlt);
        bool shift_key_down = Input.GetKeyDown(KeyCode.LeftShift);
        bool shift_key_up = Input.GetKeyUp(KeyCode.LeftShift);
        bool shift_key = shift_key_down | shift_key_up;
        bool lmb = Input.GetMouseButtonDown(0);
        bool rmb = Input.GetMouseButtonDown(1);
        bool move_command = ctrl_key & lmb | rmb;
        bool focus_command = !move_command & lmb & alt_key;
        bool attack_command = !move_command & lmb & ctrl_key;
        bool select_command = !move_command & !focus_command & lmb;

        bool has_commands = move_command | focus_command | select_command | shift_key;
        if (!has_commands)
        {
            return;
        }

        if (select_command)
        {
            //if move in porgress
            //  do nothing if moused over a unit
            //  cancel move
            //else select unit
            if (move_ui_state == MoveState.planning)
            {
                if (!em.newFocusTarget)
                {
                    move_ui_state = MoveState.idle;
                    ui_move_unit.SetActive(false);
                }
            } else if (em.newFocusTarget) {
                //take new focus and set to old focus, stop move unit first
                if (selectedGo)
                {
                    selectedGo.transform.Find("selection_ring").gameObject.SetActive(false);
                }
                selectedGo = em.newFocusTarget;
                selectedGo.transform.Find("selection_ring").gameObject.SetActive(true);
            }
            em.newFocusTarget = null;
        } else if (focus_command)
        {
            //switch focus
            em.cameraFocusTarget = em.newFocusTarget;
        } else if (move_command && selectedGo != null)
        {
            switch(move_ui_state) {
                case MoveState.idle:
                    move_ui_state = MoveState.planning;
                    break;
                case MoveState.planning:
                    move_ui_state = MoveState.confirmed;
                    break;
                case MoveState.vertical_planning:
                    move_ui_state = MoveState.confirmed;
                    break;
                case MoveState.confirmed:
                    move_ui_state = MoveState.idle;
                    break;
                default:
                    break;
            }
            ProcessMoveState();
        } else if (shift_key_down && move_ui_state == MoveState.planning)
        {
            Debug.Log("in vertical planning, maybe?");
            move_ui_state = MoveState.vertical_planning;
        } else if (shift_key_up && move_ui_state == MoveState.vertical_planning)
        {
            Debug.Log("in horizontal planning, maybe?");
            move_ui_state = MoveState.planning;
        }
    }

    void ProcessMoveState()
    {
        if (move_ui_state == MoveState.planning)
        {
            Debug.Log("start move planning!");
            BeginPlanning(ui_move_unit, selectedGo);
        } else if (move_ui_state == MoveState.confirmed)
        {
            //create new move_unit
            //swap existing if possible
            MoveUnit nmu;
            if (move_units.ContainsKey(selectedGo)) {
                nmu = move_units[selectedGo];
            } else {
                nmu = NewMoveUnit(selectedGo);
            }
            nmu.SetActive(false);
            //swap
            move_units[selectedGo] = ui_move_unit;
            ui_move_unit = nmu;

            move_units[selectedGo].StartMoving();
            Debug.Log("move confirmed, moving! y offset: " + ui_move_unit.y_offset.ToString());
            move_ui_state = MoveState.idle;
        }
    }

    void MoveIndicator()
    {
        //right click to start move
        UpdateMoveIndicator(ui_move_unit.go, ui_move_unit, true);

        foreach (var entry in move_units)
        {
            UpdateMoveIndicator(entry.Key, entry.Value, false);
        }
    }

    //TODO add height marker to destination relative to unit when planning
    //TODO add height marker to destination relative to map base when moving/confirmed
    void UpdateMoveIndicator(GameObject go, MoveUnit mu, bool ui)
    {
        if (mu == null || mu.done_moving)
        {
            return;
        }
        float distance;
        if (ui) //go == selectedGo && move_ui_state == MoveState.start)
        {
            //get mouse x/y
            Ray ray = camera.ScreenPointToRay(Input.mousePosition);//, Camera.MonoOrStereoscopicEye.Right);
            if (move_ui_state == MoveState.planning)
            {
                //intersect with a given plane
                //handle vertical->horizontal switch; mouse at y+offset , so plane must be up there too
                var y = go.transform.position.y + mu.y_offset;
                var hplane = new Plane(Vector3.up, -y);
                hplane.Raycast(ray, out distance);
                mu.h_point = ray.GetPoint(distance);
                mu.h_point.y -= mu.y_offset;
            } else if (move_ui_state == MoveState.vertical_planning)
            {
                //need vertical plane
                //need 3 points
                var pt0 = mu.h_point;
                var pt1 = pt0 - camera.transform.right;
                var pt2 = pt0 + Vector3.up;
                var vplane = new Plane(pt0, pt1, pt2);
                //find intercept w/ vplane
                vplane.Raycast(ray, out distance);
                Vector3 v_point = ray.GetPoint(distance);
                mu.y_offset = v_point.y - mu.h_point.y;
            }
            if (move_ui_state == MoveState.planning
                    || move_ui_state == MoveState.vertical_planning)
            {
                //remember delta y
                mu.destination = mu.h_point;
                mu.destination.y += mu.y_offset;
                mu.move_ring.transform.position = mu.destination;
                mu.lr.SetPosition(1, mu.destination);
                mu.label_distance.transform.position = mu.destination;
            }
        }
        //else maybe moving to another unit?
        //  that's only guarding or attacking, or supporting
        //need to update destination every frame then
        //connect line from selected object to hitPoint
        mu.lr.SetPosition(0, go.transform.position);
        distance = (mu.destination - go.transform.position).magnitude;
        mu.ld_tmp.SetText(distance.ToString());
    }

    void MoveUnits()
    {
        if (move_units.Count == 0)
        {
            return;
        }
        //for all units in list, move them to their destination;
        //when close enough to their destination
        //disable move indicators
        foreach (var entry in move_units)
        {
            var go = entry.Key;
            var mu = entry.Value;
            if (mu == null || mu.done_moving || mu.HasArrived())
            {
                if (mu != null && !mu.done_moving) {
                    mu.SetActive(false);
                }
                continue;
            }
            var dir = mu.destination - go.transform.position;
            go.transform.position += dir.normalized * .0002f;
        }
    }

    void Select()
    {
        //if selection has changed, disable old selection ring, enable new selection ring  
        if (em.newFocusTarget)
        {
            //TODO add or subtract from selection; multiple selected objects
            if (Input.GetKey(KeyCode.LeftAlt))
            {
                em.cameraFocusTarget = em.newFocusTarget;
            } else {
                if (selectedGo)
                {
                    selectedGo.transform.Find("selection_ring").gameObject.SetActive(false);
                }
                em.newFocusTarget.transform.Find("selection_ring").gameObject.SetActive(true);
                selectedGo = em.newFocusTarget;
            }
            em.newFocusTarget = null;
        }
    }
}
