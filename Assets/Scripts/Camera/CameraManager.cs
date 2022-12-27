using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraManager : MonoBehaviour
{
    public float translationSpeed = 60f;
    public float zoomSpeed = 60f;
    public float minOrthographicSize = 4f;
    public float maxOrthographicSize = 20f;
    public float altitude = 40f;

    private int mouseOnScreenBorder;
    private Coroutine mouseOnScreenCoroutine;

    private new Camera camera;
    private Transform parent;
    private RaycastHit hit;
    private Ray ray;

    public Material minimapIndicatorMaterial;
    private float minimapIndicatorStrokeWidth = 0.1f; // relative to indicator size
    private Transform minimapIndicator;
    private Mesh minimapIndicatorMesh;

    public Transform groundAudioListener;

    private float xMin = -331f;
    private float xMax = 331f;
    private float zMin = -309f;
    private float zMax = 370f;

    private float camParentOffsetZ;

    private void Awake()
    {
        camera = GetComponent<Camera>();
        parent = transform.parent;
        mouseOnScreenBorder = -1;
        PrepareMapIndicator();
        mouseOnScreenCoroutine = null;

        groundAudioListener.position = Utility.MiddleOfScreenPointToWorld();

        camParentOffsetZ = parent.transform.position.z - Utility.MiddleOfScreenPointToWorld().z;
    }

    void Update()
    {
        if (Game.Instance.gameIsPaused) return;

        if (mouseOnScreenBorder >= 0)
        {
            TranslateCamera(mouseOnScreenBorder);
        }
        else
        {
            if (Input.GetKey(KeyCode.W))
                TranslateCamera(0);
            if (Input.GetKey(KeyCode.D))
                TranslateCamera(1);
            if (Input.GetKey(KeyCode.S))
                TranslateCamera(2);
            if (Input.GetKey(KeyCode.A))
                TranslateCamera(3);
        }


        if (Input.GetAxis("Mouse ScrollWheel") > 0)
        {
            ZoomOrthographicSize(1);
        }
        else if (Input.GetAxis("Mouse ScrollWheel") < 0)
        {
            ZoomOrthographicSize(-1);
        }
    }

    private void LookAtPoint(Vector3 point)
    {
        Vector3 target = new Vector3(Mathf.Clamp(point.x, -350f, 350f), point.y, Mathf.Clamp(point.z, -350f, 350f));
        Vector3 moveToPos = new Vector3(target.x, parent.transform.position.y, target.z + camParentOffsetZ);

        parent.transform.position = moveToPos;
    }

    private void TranslateCamera(int dir)
    {
        if (dir == 0)       // top
            parent.Translate(parent.forward * Time.deltaTime * translationSpeed);
        else if (dir == 1)  // right
            parent.Translate(parent.right * Time.deltaTime * translationSpeed);
        else if (dir == 2)  // bottom
            parent.Translate(-parent.forward * Time.deltaTime * translationSpeed);
        else if (dir == 3)  // left
            parent.Translate(-parent.right * Time.deltaTime * translationSpeed);

        Vector3 pos = parent.transform.position;

        parent.transform.position = new Vector3(Mathf.Clamp(pos.x, xMin, xMax), pos.y, Mathf.Clamp(pos.z, zMin, zMax));

        ComputeMinimapIndicator(false);
    }

    private void ZoomOrthographicSize(int dir)
    {
        if (dir == 1)
        {
            camera.orthographicSize = Math.Min(camera.orthographicSize + (zoomSpeed * Time.deltaTime), maxOrthographicSize);
        }
        else if (dir == -1)
        {
            camera.orthographicSize = Math.Max(camera.orthographicSize - (zoomSpeed * Time.deltaTime), minOrthographicSize);
        }

        ComputeMinimapIndicator(true);
    }

    public void OnMouseEnterScreenBorder(int borderIndex)
    {
        mouseOnScreenCoroutine = StartCoroutine(SetMouseOnScreenBorder(borderIndex));
    }

    public void OnMouseExitScreenBorder()
    {
        StopCoroutine(mouseOnScreenCoroutine);
        mouseOnScreenBorder = -1;
    }

    private IEnumerator SetMouseOnScreenBorder(int borderIndex)
    {
        yield return new WaitForSeconds(0.2f);
        mouseOnScreenBorder = borderIndex;
    }

    private void PrepareMapIndicator()
    {
        GameObject g = new GameObject("MinimapIndicator");
        minimapIndicator = g.transform;
        g.layer = 10; // put on "Minimap" layer
        minimapIndicator.position = Vector3.zero;
        minimapIndicatorMesh = CreateMinimapIndicatorMesh();
        MeshFilter mf = g.AddComponent<MeshFilter>();
        mf.mesh = minimapIndicatorMesh;
        MeshRenderer mr = g.AddComponent<MeshRenderer>();
        mr.material = new Material(minimapIndicatorMaterial);
        mr.receiveShadows = false;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ComputeMinimapIndicator(true);
    }

    private Mesh CreateMinimapIndicatorMesh()
    {
        Mesh m = new Mesh();
        Vector3[] vertices = new Vector3[] {
            Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero,
            Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero
        };
        int[] triangles = new int[] {
            0, 4, 1, 4, 5, 1,
            0, 2, 6, 6, 4, 0,
            6, 2, 7, 2, 3, 7,
            5, 7, 3, 3, 1, 5
        };
        m.vertices = vertices;
        m.triangles = triangles;
        return m;
    }

    private void ComputeMinimapIndicator(bool zooming)
    {
        Vector3 middle = Utility.MiddleOfScreenPointToWorld();
        // if zooming: recompute the indicator mesh
        if (zooming)
        {
            Vector3[] viewCorners = Utility.ScreenCornersToWorldPoints();
            float w = viewCorners[1].x - viewCorners[0].x;
            float h = viewCorners[2].z - viewCorners[0].z;
            for (int i = 0; i < 4; i++)
            {
                viewCorners[i].x -= middle.x;
                viewCorners[i].z -= middle.z;
            }
            Vector3[] innerCorners = new Vector3[]
            {
                new Vector3(viewCorners[0].x + minimapIndicatorStrokeWidth * w, 0f, viewCorners[0].z + minimapIndicatorStrokeWidth * h),
                new Vector3(viewCorners[1].x - minimapIndicatorStrokeWidth * w, 0f, viewCorners[1].z + minimapIndicatorStrokeWidth * h),
                new Vector3(viewCorners[2].x + minimapIndicatorStrokeWidth * w, 0f, viewCorners[2].z - minimapIndicatorStrokeWidth * h),
                new Vector3(viewCorners[3].x - minimapIndicatorStrokeWidth * w, 0f, viewCorners[3].z - minimapIndicatorStrokeWidth * h)
            };
            Vector3[] allCorners = new Vector3[]
            {
                viewCorners[0], viewCorners[1], viewCorners[2], viewCorners[3],
                innerCorners[0], innerCorners[1], innerCorners[2], innerCorners[3]
            };

            for (int i = 0; i < 8; i++)
                allCorners[i].y = 100f;
            minimapIndicatorMesh.vertices = allCorners;
            minimapIndicatorMesh.RecalculateNormals();
            minimapIndicatorMesh.RecalculateBounds();
        }
        // move the game object at the center of the main camera screen
        minimapIndicator.position = middle;
    }

    private void OnMoveCamera(object data)
    {
        Vector3 pos = (Vector3)data;
        LookAtPoint(pos);
        ComputeMinimapIndicator(false);
    }

    private void OnEnable()
    {
        EventManager.AddListener("MoveCamera", OnMoveCamera);
    }

    private void OnDisable()
    {
        EventManager.RemoveListener("MoveCamera", OnMoveCamera);
    }
}
