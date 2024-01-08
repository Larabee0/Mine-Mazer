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
        [SerializeField] private UIDocument uiController;
        [SerializeField] private SpatialParadoxGenerator mapGenerator;
        [SerializeField] private VisualTreeAsset waypointTemplate;
        [SerializeField] private Texture2D waypointAsset;
        [SerializeField] private int wayPointRes = 512;
        private VisualElement DocRoot => uiController.rootVisualElement;
        private Transform player;

        private VisualElement root;
        private List<WorldWayPoint> waypoints = new();

        private Coroutine wayPointTransformProcess = null;

        private void Awake()
        {
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

            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnLookDelta += OnLook;
                InputManager.Instance.OnMoveAxis += OnMove;

            }
            else
            {
                Debug.LogError("No Input, UI cannot start");
                enabled = false;
            }

            root = DocRoot.Q("WayPoints");
        }

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
            List<List<TunnelSection>> mapTree = mapGenerator.MapTree;
            HashSet<TunnelSection> waypointable = new();
            for (int i = 0; i < mapTree.Count; i++)
            {
                List<TunnelSection> ring = mapTree[i];
                for (int j = 0; j < ring.Count; j++)
                {
                    if (ring[j].Keep)
                    {
                        waypointable.Add(ring[j]);
                    }
                }
            }

            waypointable.UnionWith(mapGenerator.GetMothballedSections());


            HashSet<TunnelSection> existingPoints = new(waypoints.Count);

            for (int i = waypoints.Count - 1; i >= 0; i--)
            {
                if (!waypointable.Contains(waypoints[i].target))
                {
                    root.Remove(waypoints[i].wayPointRoot);
                    waypoints.RemoveAt(i);
                }
                else
                {
                    existingPoints.Add(waypoints[i].target);
                }
            }

            waypointable.ExceptWith(existingPoints);

            foreach (var item in waypointable)
            {
                AddwayPoint(item, Color.white);
            }
        }

        private void AddwayPoint(TunnelSection section, Color tint)
        {
            waypoints.Add(new(section, waypointTemplate.Instantiate()));

            

            VisualElement wayPoint = waypoints[^1].texture;
            wayPoint.style.backgroundImage = waypointAsset;
            wayPoint.style.height = wayPointRes;
            wayPoint.style.width = wayPointRes;
            wayPoint.style.unityBackgroundImageTintColor = tint;

            waypoints[^1].Name = section.WaypointName;
            waypoints[^1].wayPointRoot.style.position = Position.Absolute;
            root.Add(waypoints[^1].wayPointRoot);
            TransformWayPoint(waypoints[^1]);
        }


        private void TransformWayPoint(WorldWayPoint waypoint)
        {
            Vector3 screenPosition = Camera.main.WorldToScreenPoint(waypoint.CurPositon);
            waypoint.wayPointRoot.style.visibility = screenPosition.z > 0 ? Visibility.Visible : Visibility.Hidden;
            waypoint.wayPointRoot.style.translate = new Translate(screenPosition.x, Screen.height - screenPosition.y);
        }
    }

    internal class WorldWayPoint
    {
        public TunnelSection target;
        public VisualElement wayPointRoot;
        public VisualElement texture;
        public Label text;

        public string Name
        {
            set => text.text = value;
        }

        public WorldWayPoint(TunnelSection target, VisualElement wayPointRoot)
        {
            this.target = target;
            this.wayPointRoot = wayPointRoot;
            texture = wayPointRoot.Q("WaypointImage");
            text = wayPointRoot.Q<Label>("WaypointName");
        }

        public Vector3 CurPositon => target.WaypointPosition;
    }
}