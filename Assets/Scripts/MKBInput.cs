//Takes mouse or xr controlls, finds what they're pointing to, return object being selected
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine;
using System.Linq;
using System;
using TMPro;


public class MoveUnit
{
    bool valid; //don't touch this if not valid
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
    float distance = 0;

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
        Assert.IsTrue(valid == true);
        done_moving = false;
    }

    public bool HasArrived()
    {
        done_moving = (GetDistance()< .2f);
        return done_moving;
    }

    public float GetDistance()
    {
        distance = (go.transform.position - destination).magnitude;
        return distance;
    }

    public bool IsValid()
    {
        return valid;
    }
    public void SetActive(bool active)
    {
        valid = active;
        move_ring.SetActive(active);
        move_line.SetActive(active);
        base_height.SetActive(active);
        label_distance.SetActive(active);
        lr.enabled = active;
        y_offset = 0;
        done_moving = !active;
    }

    public void BeginPlanning(GameObject g)
    {
        Assert.IsTrue(g != null);
        go = g;
        SetActive(true);
        done_moving = false;
    }

}

public class MKBInput : MonoBehaviour
{
    EnvironmentManager em;
    List<GameObject> selected_gos = new List<GameObject>();
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


    List<MoveUnit> mu_pool = new List<MoveUnit>();
    List<MoveUnit> ui_move_units = new List<MoveUnit>();
    //move_units need to be dictionary b/c fast search on cancel
    List<MoveUnit> move_units = new List<MoveUnit>();

    List<MouseButtonState> mb;

    GameObject NewBase(GameObject target)
    {
        var go = Instantiate(ui_height);
        var comp = go.GetComponent<updateHeight>();
        comp.target = target;
        return go;
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
        mb = new List<MouseButtonState>();
        mb.Add(new MouseButtonState());
        mb.Add(new MouseButtonState());
        camera = GetComponent<Camera>();
        em = GameObject.FindObjectOfType<EnvironmentManager>();
        move_line.SetActive(false);
        move_ring.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        MouseControl();
        StateMachine();
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

    class MouseButtonState {
        public bool key;
        public bool key_down;
        public bool key_up;
        public Vector2 pos0;
        public Vector2 pos1;
        float threshold = 3f;
        public MouseButtonState() { Debug.Log("MouseButtonState Constructor"); }
        public void Process() {
            if (key_down)
            {
                pos0 = Input.mousePosition;
            }
            if (key)
            {
                pos1 = Input.mousePosition;
            }
        }
        //if traveled while mousebutton is pressed
        public bool DidTravel() {
            return (pos0 - pos1).magnitude > threshold;
        }
    }

    void MouseControl()
    {
        for (int i = 0; i < 2; i++)
        {
            var mbs = mb[i];
            mbs.key = Input.GetMouseButton(i);
            mbs.key_down = Input.GetMouseButtonDown(i);
            mbs.key_up = Input.GetMouseButtonUp(i);
            mbs.Process();
        }
    }

    //select w/ lmb down if mouse over target else w/ lmb up
    //move if valid selection
    //when move command is confirmed, the MoveUnit is... 
    //moved over to move_units to have an effect
    int prev_command = 0;
    //Dictionary<int, string> command_map = new Dictionary<int, string>();
    void StateMachine()
    {
        //Philosophy: only accept 1 legal command per frame
        //TODO specify key combos for commands; auto resolve based on priority list
        bool ctrl_key = Input.GetKey(KeyCode.LeftControl);
        bool alt_key = Input.GetKey(KeyCode.LeftAlt);
        bool shift_key_down = Input.GetKeyDown(KeyCode.LeftShift);
        bool shift_key_up = Input.GetKeyUp(KeyCode.LeftShift);
        bool shift_key = shift_key_down | shift_key_up;
        bool lmb = Input.GetMouseButton(0);
        bool lmb_down = Input.GetMouseButtonDown(0);
        bool lmb_up = Input.GetMouseButtonUp(0);
        bool rmb_down = Input.GetMouseButtonDown(1);
        bool rmb_up = Input.GetMouseButtonUp(1);
        bool move_command = (!mb[0].DidTravel() & ctrl_key & lmb_up) 
                          | (!mb[1].DidTravel() & rmb_up);
        bool focus_command = !move_command & lmb_up & alt_key & (em.newFocusTargets.Any());
        bool attack_command = !move_command & lmb_up & ctrl_key;
        //FIXME lmb_up if drag select, otherwise on lmb_down
        bool select_command = !move_command & !focus_command & (lmb_up | lmb_down);
        bool cancel_command = Input.GetKey(KeyCode.Escape);
        bool stop_command = Input.GetKey("s");


        //priorities
        string[] ct = new string[32]; //command text
        ct[0x1] = "command: cancel";
        ct[0x2] = "command: select";
        ct[0x4] = "command: focus";
        ct[0x8] = "command: move";

        int command = 0;
        command += cancel_command ? 0x1 : 0;
        command += select_command ? 0x2 : 0;
        command += focus_command  ? 0x4 : 0;
        command += move_command   ? 0x8 : 0;
        //command += attack_command ? 0x10 : 0;

        if (prev_command != command && command != 0) {
            Debug.Log("command: " + command.ToString() + " " + ct[command]);
        }
        prev_command = command;

        if (cancel_command) { CancelCommand(); }
        else if (select_command) { SelectCommand(lmb_down, lmb, lmb_up); }
        else if (focus_command) { FocusCommand(); }
        else if (move_command) { MoveCommand(); }
        else if (shift_key_down && move_ui_state == MoveState.planning)
        {
            Debug.Log("in vertical planning, maybe?");
            move_ui_state = MoveState.vertical_planning;
        } else if (shift_key_up && move_ui_state == MoveState.vertical_planning)
        {
            Debug.Log("in horizontal planning, maybe?");
            move_ui_state = MoveState.planning;
        }
        else if (attack_command) { AttackCommand(); }
    }

    void CancelCommand()
    {
        move_ui_state = MoveState.idle;
        foreach (var mu in ui_move_units)
        {
            mu.SetActive(false);
        }
        em.newFocusTargets.Clear();
        //take all selected gos and cancel them in move_units
    }

    void SetSelection(bool active)
    {
        foreach (var sgo in selected_gos)
        {
            sgo.transform.Find("selection_ring").gameObject.SetActive(active);
        }
    }

    //TODO move this to a general help file
    void Swap<T>(ref T a, ref T b)
    {
        var tmp = a;
        a = b;
        b = tmp;
    }

    void SelectCommand(bool down, bool pressed, bool up)
    {
        //if move in porgress
        //  cancel move
        //else select unit
        //else drag select
        if (move_ui_state == MoveState.planning) //don't cancel if trying to select unit && !em.newFocusTarget)
        {
            CancelCommand();
        } else if (down)
        {
            //record start of box
        } else if (pressed)
        {
            //update selection box
        } else if (up)
        {
            //add all units to selection array
            //disable selection box
        }
        if (em.newFocusTargets.Any()) {
            //disable selection indicators for deselected units; swap w/ new focus list, show selection indicators
            SetSelection(false);
            selected_gos.Clear();
            Swap(ref selected_gos, ref em.newFocusTargets);
            SetSelection(true);
        }
    }

    void FocusCommand()
    {
        //distinction bewteen following focus and single location focus
        //switch focus
        em.cameraFocusTargets.Clear();
        Swap(ref em.cameraFocusTargets, ref em.newFocusTargets);
    }

    void MoveCommand()
    {
        if (!selected_gos.Any())
        {
            Debug.Log("Nothing selected to move");
            return;
        }
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

        //update ui_move_units to use all selected units
        if (move_ui_state == MoveState.planning)
        {
            Debug.Log("start move planning!");
            PopulateMoveUi();
        } else if (move_ui_state == MoveState.confirmed)
        {
            Debug.Log("move confirmed!");
            //start moving these units
            foreach (var mu in ui_move_units)
            {
                mu.StartMoving();

                //if mu already in move_units,
                //suspend and disable mu, move back to pool
                var index = move_units.FindIndex(x => x.go == mu.go);
                if (index != -1)
                {
                    //move it back to pool
                    var m = move_units[index];
                    m.SetActive(false);
                    mu_pool.Add(m);
                    move_units.RemoveAt(index);
                }
            }

            //move ui mu to become active mu
            move_units.AddRange(ui_move_units);
            ui_move_units.Clear();
            foreach (var mu in move_units) 
            {
                mu.SetActive(true);
            }

            Debug.Log("move_units.Count: " + move_units.Count.ToString());
            Assert.IsTrue(move_units.All(x => x.IsValid() == true));
            //Debug.Log("move confirmed, moving! y offset: " + ui_move_unit.y_offset.ToString());
            move_ui_state = MoveState.idle;
        }
    }

    //populate ui_move_units with selected units (game objects)
    void PopulateMoveUi()
    {
        //selected_gos and ui_move_units must be same size
        //if selected_gos > ui_move_units
        //  grab from pool
        //  allocate whatever is left
        //if ui_move_units > selected_gos
        //  throw extras back to pool
        Assert.IsTrue(selected_gos.Count > 0);
        var num_extra = selected_gos.Count - ui_move_units.Count;

        int selected_count = selected_gos.Count;
        int ui_count = ui_move_units.Count;
        int move_count = move_units.Count;

        //if selected_gos > ui_move_units
        if (num_extra > 0)
        {
            //  grab from pool
            Debug.Log("num extra; " + num_extra.ToString() + " pool size: " + mu_pool.Count.ToString());
            int take_from_pool = Math.Min(num_extra, mu_pool.Count);
            if (take_from_pool > 0)
            {
                ui_move_units.AddRange(mu_pool.Take(take_from_pool));
                mu_pool.RemoveRange(0, take_from_pool);
                num_extra -= take_from_pool;
                //num_extra = selected_gos.Count - ui_move_units.Count;
            }

            Debug.Log("num extra; " + num_extra.ToString() + " pool size: " + mu_pool.Count.ToString());
            //allocate the rest
            for (int i = 0; i < num_extra; i++)
            {
                ui_move_units.Add(NewMoveUnit());
            }
        } else if (num_extra < 0)
        {
            Assert.IsTrue(false); //this shouldn't happen, can't get here if ui_move_units must be cleared first?
            //if ui_move_units > selected_gos
            //  throw extras back to pool
            var num_return = -num_extra;
            //TODO consider using Skip instead of Take if better performance?
            mu_pool.AddRange(ui_move_units.Take(num_return));
        }

        string output = "ui_move_units: ";
        foreach (var mu in ui_move_units)
        {
            if (mu.IsValid() == true)
            {
                output += mu.go.name + "; ";
            }
        }
        int num_valid = 0;
        output += "\tmove_units: ";
        foreach (var mu in move_units)
        {
            if (mu.IsValid() == true)
            {
                num_valid++;
                output += mu.go.name + "; ";
            }
        }
        Debug.Log("before: " + selected_count.ToString()
                + " " + ui_count.ToString()
                + " " + move_count.ToString()
                + " after: " + selected_gos.Count
                + " " + ui_move_units.Count.ToString()
                + " " + move_units.Count.ToString()
                + " " + num_valid.ToString()
                + "\n" + output);
        if (selected_gos.Count != ui_move_units.Count)
        {
            Debug.Log("no good, " + selected_gos.Count.ToString() + ", " + ui_move_units.Count.ToString());
        }
        Assert.IsTrue(selected_gos.Count == ui_move_units.Count);
        //initialize mu
        int j = 0;
        foreach (var go in selected_gos)
        {
            ui_move_units[j].BeginPlanning(go);
            j++;
        }
    }

    void AttackCommand()
    {
    }
    void MoveIndicator()
    {
        //right click to start move
        foreach (var mu in ui_move_units)
        {
            UpdateMoveIndicator(mu.go, mu, true);
        }

        foreach (var mu in move_units)
        {
            UpdateMoveIndicator(mu.go, mu, false);
        }
    }

    //TODO add radius/sphere/ring indicator when moving
    void UpdateMoveIndicator(GameObject go, MoveUnit mu, bool ui)
    {
        if (mu == null || mu.done_moving)
        {
            return;
        }
        float distance;
        if (ui) //go == selected_gos && move_ui_state == MoveState.start)
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
        for (int i = 0; i < move_units.Count; i++)
        {
            var mu = move_units[i];
            if (mu == null || mu.HasArrived())
            {
                if (mu != null && mu.done_moving) {
                    Debug.Log("Done moving! " + mu.GetDistance().ToString());
                    mu.SetActive(false);
                    mu_pool.Add(mu);
                    move_units.RemoveAt(i);
                }
                continue;
            }
            var dir = mu.destination - mu.go.transform.position;
            mu.go.transform.position += dir.normalized * .0002f;
        }
    }
}
