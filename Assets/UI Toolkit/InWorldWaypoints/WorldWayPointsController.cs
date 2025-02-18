using MazeGame.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace MazeGame.Navigation
{
    public class WorldWayPointsController : MonoBehaviour
    {
        private static WorldWayPointsController instance;
        public static WorldWayPointsController Instance
        {
            get
            {
                if (instance == null)
                {
                    Debug.LogWarning("Expected WorldWay Points Controller instance not found. Order of operations issue? Or WorldWay Points Controller is disabled/missing.");
                }
                return instance;
            }
            private set
            {
                if (value != null && instance == null)
                {
                    instance = value;
                }
            }
        }

        [SerializeField] private UIDocument uiController;
        [SerializeField] private SpatialParadoxGenerator mapGenerator;
        [SerializeField] private VisualTreeAsset waypointTemplate;
        [SerializeField] private Texture2D[] waypointAssets;
        [SerializeField] private int wayPointRes = 512;
        private VisualElement DocRoot => uiController.rootVisualElement;
        private Transform player;

        private VisualElement root;
        private readonly List<WorldWayPoint> waypoints = new();

        private Coroutine wayPointTransformProcess = null;
        
        private void Awake()
        {
            if (instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(this);
            }
        }

        public void StartWWPC()
        {
            mapGenerator = FindAnyObjectByType<SpatialParadoxGenerator>();
            if (mapGenerator == null || !mapGenerator.isActiveAndEnabled)
            {
                Debug.LogError("No Map Generator or Map Generator Disabled");
                enabled = false;
                return;
            }

            player = FindObjectOfType<Improved_Movement>().transform;
            if (player == null)
            {
                Debug.LogError("No Player, UI cannot start");
                enabled = false;
                return;
            }

            mapGenerator.OnMapUpdate += MapUpdateEvent;

            

            root = DocRoot.Q("WayPoints");
            
        }
        
        private void OnEnable()
        {
            if (InputManager.Instance != null)
            {
                InputManager.Instance.lookAxis.OnAxis += OnLook;
                InputManager.Instance.moveAxis.OnAxis += OnMove;

            }
            else
            {
                Debug.LogError("No Input, UI cannot start");
                enabled = false;
            }
        }

        private void OnDisable()
        {
            if (InputManager.Instance != null)
            {
                InputManager.Instance.lookAxis.OnAxis -= OnLook;
                InputManager.Instance.moveAxis.OnAxis -= OnMove;

            }
        }

        // private void OnDrawGizmos()
        // {
        //     Gizmos.color = Color.magenta;
        //     waypoints.ForEach(waypoint => Gizmos.DrawSphere(waypoint.CurPositon, 0.25f));
        // }

        private void OnMove(Vector2 axis)
        {
            wayPointTransformProcess ??= StartCoroutine(UpdateWayPoints());
        }

        private void OnLook(Vector2 axis)
        {
            wayPointTransformProcess ??= StartCoroutine(UpdateWayPoints());
        }

        private IEnumerator UpdateWayPoints()
        {
            yield return new WaitForEndOfFrame();

            waypoints.ForEach(waypoint => TransformWayPoint(waypoint));

            wayPointTransformProcess = null;
        }

        private void MapUpdateEvent()
        {
            List<List<MapTreeElement>> mapTree = mapGenerator.MapTree;
            HashSet<MapTreeElement> waypointable = new(new MapTreeElementComparer());
            for (int i = 0; i < mapTree.Count; i++)
            {
                List<MapTreeElement> ring = mapTree[i];
                for (int j = 0; j < ring.Count; j++)
                {
                    if (ring[j].Instantiated && ring[j].Keep)
                    {
                        waypointable.Add(ring[j]);
                    }
                }
            }

            waypointable.UnionWith(mapGenerator.GetMothballedSections());

            HashSet<MapTreeElement> existingPoints = new(waypoints.Count, new MapTreeElementComparer());

            for (int i = waypoints.Count - 1; i >= 0; i--)
            {
                if (waypoints[i].GetType() == typeof(TunnelWayPoint))
                {
                    TunnelWayPoint tunnelWayPoint = waypoints[i] as TunnelWayPoint;
                    if (!waypointable.Contains(tunnelWayPoint.target))
                    {
                        root.Remove(tunnelWayPoint.wayPointRoot);
                        waypoints.RemoveAt(i);
                    }
                    else
                    {
                        existingPoints.Add(tunnelWayPoint.target);
                    }
                }
            }

            waypointable.ExceptWith(existingPoints);

            foreach (var item in waypointable)
            {
                AddwayPoint(item, Color.white);
            }
        }

        public void RemoveWaypoint(WorldWayPoint waypoint)
        {
            waypoints.Remove(waypoint);
            root.Remove(waypoint.wayPointRoot);
        }

        public void ClearWaypoints()
        {
            waypoints.Clear();
            root.Clear();
        }

        private void AddwayPoint(MapTreeElement section, Color tint)
        {
            waypoints.Add(new TunnelWayPoint(section, waypointTemplate.Instantiate()));
            AddWayPoint(section.WaypointName,0, tint);
        }

        public WorldWayPoint AddwayPoint(string text, Vector3 position, Color tint, int iconIndex = 1)
        {
            if(root == null)
            {
                StartWWPC();
            }
            waypoints.Add(new WorldWayPoint(position, waypointTemplate.Instantiate().Q("WorldWaypoint")));

            AddWayPoint(text, iconIndex, tint);
            return waypoints[^1];
        }

        public WorldWayPoint AddwayPoint(string text, Transform transform, Color tint, int iconIndex = 1)
        {
            if (root == null)
            {
                StartWWPC();
            }
            waypoints.Add(new WorldWayPoint(transform, waypointTemplate.Instantiate().Q("WorldWaypoint")));

            AddWayPoint(text, iconIndex, tint);
            return waypoints[^1];
        }

        private void AddWayPoint(string text, int iconIndex,Color tint)
        {
            VisualElement wayPoint = waypoints[^1].texture;
            wayPoint.style.backgroundImage = waypointAssets[iconIndex];
            wayPoint.style.height = wayPointRes;
            wayPoint.style.width = wayPointRes;
            wayPoint.style.unityBackgroundImageTintColor = tint;
            waypoints[^1].Name = text;
            waypoints[^1].wayPointRoot.style.position = Position.Absolute;
            root.Add(waypoints[^1].wayPointRoot);
            wayPointTransformProcess ??= StartCoroutine(UpdateWayPoints());
        }

        private void TransformWayPoint(WorldWayPoint waypoint)
        {
            Vector3 screenPosition = Camera.main.WorldToScreenPoint(waypoint.CurPositon);
            Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(uiController.rootVisualElement.panel, new(screenPosition.x, screenPosition.y));
            //panelPos = root.WorldToLocal(panelPos);
            waypoint.wayPointRoot.style.visibility = screenPosition.z > 0 ? Visibility.Visible : Visibility.Hidden;
            float x = waypoint.wayPointRoot.resolvedStyle.width*0.5f ;
            waypoint.wayPointRoot.style.translate = new Translate(panelPos.x - x, Screen.height - panelPos.y);
        }
    }

    public class WorldWayPoint
    {
        public VisualElement wayPointRoot;
        public VisualElement texture;
        public Label text;
        public Vector3 position;
        public Transform transform;
        public virtual Vector3 CurPositon
        {
            get
            {
                return transform != null ? transform.position : position;
            }
        }

        public string Name
        {
            set => text.text = value;
        }
        public WorldWayPoint() { }
        public WorldWayPoint(Vector3 position, VisualElement wayPointRoot)
        {
            this.position = position;
            this.wayPointRoot = wayPointRoot;
            this.wayPointRoot.style.visibility = Visibility.Hidden;
            texture = wayPointRoot.Q("WaypointImage");
            text = wayPointRoot.Q<Label>("WaypointName");
        }
        public WorldWayPoint(Transform transform, VisualElement wayPointRoot)
        {
            this.transform = transform;
            this.wayPointRoot = wayPointRoot;
            this.wayPointRoot.style.visibility = Visibility.Hidden;
            texture = wayPointRoot.Q("WaypointImage");
            text = wayPointRoot.Q<Label>("WaypointName");
        }
    }

    internal class TunnelWayPoint : WorldWayPoint
    {
        public MapTreeElement target;

        public override Vector3 CurPositon => target.WaypointPosition;

        public TunnelWayPoint(MapTreeElement target, VisualElement wayPointRoot)
        {
            this.target = target;
            this.wayPointRoot = wayPointRoot;
            texture = wayPointRoot.Q("WaypointImage");
            text = wayPointRoot.Q<Label>("WaypointName");
        }
    }
}