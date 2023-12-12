using MazeGame.Input;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

namespace MazeGame.Navigation
{
    public class MiniMapController : MonoBehaviour
    {
        [SerializeField] private UIDocument miniMapDoc;
        [SerializeField] private RenderTexture miniMapOutput;

        [SerializeField] private SpatialParadoxGenerator mapGenerator;

        [SerializeField] private Dictionary<int,Texture2D> miniMapAssets;
        [SerializeField] private int textureResolution = 512;
        [SerializeField] private float viewPortSize = 22f;
        [SerializeField] private float miniMapScale = 1f;
        [SerializeField] private float minimapCentreOffset = 0f;
        [SerializeField] private float minimapZoomOffset = 0f;
        [SerializeField] private float offscreenThreshold = 510;
        [SerializeField] private Color playerCurrent = new(1f, 1f, 1f, 0.5f);
        [SerializeField] private Color explored = new(0.5f, 0.25f, 0.25f, 0.5f);
        [SerializeField] private Color unexplored = new(0.5f, 0.25f, 0.25f, 0.5f);
        [SerializeField] private Color aboveExplored = new(0.25f, 0.5f, 0.25f, 0.5f);
        [SerializeField] private Color aboveUnExplored = new(0.25f, 0.5f, 0.25f, 0.5f);
        [SerializeField] private Color belowExplored = new(0.5f, 0.25f, 0.25f, 0.5f);
        [SerializeField] private Color belowUnexplored = new(0.5f, 0.25f, 0.25f, 0.5f);
        [SerializeField] private Texture2D stagnationBeacon;


        private VisualElement DocRoot => miniMapDoc.rootVisualElement;
        private Transform player;
        private MiniMap miniMap;
        private Compass compass;
        private float pixelsPerUnit;
        private float angle;

        private void Awake()
        {
            if(mapGenerator == null|| !mapGenerator.isActiveAndEnabled)
            {
                Debug.LogError("No Map Generator or Map Generator Disabled");
                enabled = false;
                return;
            }
            miniMap = new() { root = DocRoot.Q("MiniMap"), mapAssembly = new(), waypoints = new() };
            compass = new()
            {
                rotator = DocRoot.Q("Rotator"),
                North = DocRoot.Q("North"),
                East = DocRoot.Q("East"),
                South = DocRoot.Q("South"),
                West = DocRoot.Q("West")
            };

            miniMapAssets = mapGenerator.GenerateMiniMapTextures();
            

            pixelsPerUnit = (textureResolution/2) / viewPortSize;

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
            minimapZoomOffset = textureResolution * (miniMapScale - 1);
            minimapZoomOffset -= minimapCentreOffset;
        }

        private void Start()
        {
            //DebugMap();
            Debug.Log(Application.targetFrameRate);
            Debug.Log(QualitySettings.vSyncCount);
        }

        private void OnLook(Vector2 axis)
        {
            angle = player.rotation.eulerAngles.y;
            compass.RotateTo(-angle);
            miniMap.waypoints.ForEach(element => TranslateWayPoint(element));
        }

        private void OnMove(Vector2 axis)
        {
            TranslateMap();
        }

        private void ScaleMap()
        {
            minimapZoomOffset = textureResolution * (miniMapScale - 1);
            minimapZoomOffset -= minimapCentreOffset;
            miniMap.root.style.scale = new Scale(new Vector2(miniMapScale, miniMapScale));
            TranslateMap();
        }

        private void TranslateMap()
        {
            Vector3 pos = player.position;
            miniMap.root.style.translate = new Translate(
                -(pos.x * pixelsPerUnit * miniMapScale) - minimapZoomOffset,
                (pos.z * pixelsPerUnit * miniMapScale) - minimapZoomOffset);

            miniMap.waypoints.ForEach(element => TranslateWayPoint(element));
        }

        private void OnDestroy()
        {
            mapGenerator.OnMapUpdate -= MapUpdateEvent;
        }

        private void MapUpdateEvent()
        {
            MapUpdateProcess();
        }

        private void DebugMap()
        {
            var trans = new BoxTransform
            {
                pos = float3.zero,
                rot = quaternion.identity
            };
            AddElement(stagnationBeacon, 0, "Debug waypoint", Color.white, trans);
            miniMap.root.Add(miniMap.mapAssembly[^1].asset);
            miniMap.waypoints.Add(miniMap.mapAssembly[^1]);
            miniMap.mapAssembly[^1].asset.style.translate = new Translate((trans.pos.x * pixelsPerUnit) + (textureResolution / 2), (-(trans.pos.z * pixelsPerUnit)) + (textureResolution / 2));
            miniMap.mapAssembly[^1].asset.transform.rotation = Quaternion.Euler(0, 0, ((Quaternion)trans.rot).eulerAngles.y);
            OnMove(Vector2.zero);
        }

        private void MapUpdateProcess()
        {
            if(miniMap == null)
            {
                return;
            }
            miniMap.root.Clear();
            miniMap.mapAssembly.Clear();
            List<List<TunnelSection>> mapTree = mapGenerator.MapTree;
            float curHeight = mapGenerator.CurPlayerSection.Position.y;
            for (int i = 0; i < mapTree.Count; i++)
            {
                List<TunnelSection> ring = mapTree[i];
                for (int j = 0; j < ring.Count; j++)
                {
                    int instanceid = ring[j].orignalInstanceId;
                    if (miniMapAssets.ContainsKey(instanceid))
                    {
                        float sectioHieght = ring[j].Position.y;
                        
                        Color above = ring[j].explored ? aboveExplored : aboveUnExplored;
                        Color below = ring[j].explored ? belowExplored : belowUnexplored;
                        Color same = ring[j].explored ? explored : unexplored;

                        Color tint = sectioHieght > curHeight ? above : same;
                        tint = sectioHieght < curHeight ? below : tint;

                        tint = ring[j] == mapGenerator.CurPlayerSection ? playerCurrent : tint;
                        var trans = new BoxTransform
                        {
                            pos = ring[j].Position,
                            rot = ring[j].Rotation
                        };
                        AddElement(instanceid, ring[j].name, tint, trans);
                        if (ring[j].Keep)
                        {
                            trans = ring[j].StrongKeep
                                ? new BoxTransform
                                {
                                    pos = ring[j].WaypointPosition,
                                    rot = ring[j].Rotation
                                }
                                : new BoxTransform
                                {
                                    pos = ring[j].stagnationBeacon.transform.position,
                                    rot = ring[j].stagnationBeacon.transform.rotation
                                };
                            AddElement(stagnationBeacon, instanceid, ring[j].name, Color.white, trans);
                            miniMap.waypoints.Add(miniMap.mapAssembly[^1]);
                            AddText(miniMap.mapAssembly[^1], ring[j].WaypointName);
                        }
                    }
                }
            }
            miniMap.mapAssembly.Sort();

            miniMap.mapAssembly.ForEach(element => miniMap.root.Add(element.asset));

            for (int i = 0; i < miniMap.mapAssembly.Count; i++)
            {
                MiniMapElement element = miniMap.mapAssembly[i];
                BoxTransform transform = element.transform;
                element.asset.style.translate = new Translate((transform.pos.x * pixelsPerUnit) + (textureResolution / 2), (-(transform.pos.z * pixelsPerUnit)) + (textureResolution / 2));
                element.asset.transform.rotation = Quaternion.Euler(0, 0, ((Quaternion)transform.rot).eulerAngles.y);
            }
            int startIndex = miniMap.mapAssembly.Count;
            List<TunnelSection> mothballedSections = mapGenerator.GetMothballedSections();
            for (int i = 0; i < mothballedSections.Count; i++)
            {
                TunnelSection section = mothballedSections[i];

                if (section.Keep)
                {
                    BoxTransform trans = section.StrongKeep
                        ? new BoxTransform
                        {
                            pos = section.WaypointPosition,
                            rot = section.Rotation
                        }
                        : new BoxTransform
                        {
                            pos = section.stagnationBeacon.transform.position,
                            rot = section.stagnationBeacon.transform.rotation
                        };
                    AddElement(stagnationBeacon, section.orignalInstanceId, section.name, Color.white, trans);
                    MiniMapElement element = miniMap.mapAssembly[^1];
                    miniMap.waypoints.Add(element);
                    miniMap.root.Add(element.asset);
                    AddText(element, section.WaypointName);
                    TranslateWayPoint(element);
                }
            }

            OnMove(Vector2.zero);
        }

        private void TranslateWayPoint(MiniMapElement element)
        {
            BoxTransform trans = element.transform;
            Vector2 pixelPos = new(
                (trans.pos.x * pixelsPerUnit) + (textureResolution / 2),
                (-(trans.pos.z * pixelsPerUnit)) + (textureResolution / 2));
            Vector3 pos = player.position;
            Vector2 playerPos = new((pos.x * pixelsPerUnit) + (textureResolution / 2), ((-pos.z * pixelsPerUnit)) + (textureResolution / 2));
            Vector2 unnormalizedDir = playerPos - pixelPos;
            if (unnormalizedDir.magnitude >= offscreenThreshold)
            {
                Vector2 dir = (new Vector2(trans.pos.x, trans.pos.z) - new Vector2(-player.position.x, player.position.z)).normalized;

                Vector2 newTranslation = playerPos + (dir * -offscreenThreshold);

                element.asset.style.translate = new Translate(newTranslation.x, newTranslation.y);
            }
            else
            {
                element.asset.style.translate = new Translate(pixelPos.x, pixelPos.y);
            }

            float inverse = 1f-(miniMapScale * 0.15f);

            inverse = miniMapScale > 1f ? inverse : 1f;
            inverse = miniMapScale < 1f ? 1f + (1f-miniMapScale) : inverse;

            element.asset.style.scale = new StyleScale(new Vector2(inverse, inverse));

            element.asset.style.rotate = new Rotate(angle);
        }

        private void AddElement(Texture2D texture, int id, string name, Color tint, BoxTransform transform)
        {
            var element = new MiniMapElement { asset = new VisualElement() { name = name }, originalInstanceId = id, transform = transform };
            element.asset.style.backgroundImage = texture;
            element.asset.style.height = textureResolution;
            element.asset.style.width = textureResolution;
            element.asset.style.position = Position.Absolute;
            element.asset.style.unityBackgroundImageTintColor = tint;
            miniMap.mapAssembly.Add(element);
        }

        private void AddText(MiniMapElement element, string text)
        {
            Label label = new()
            {
                text = text
            };
            // label.style.position = Position.Absolute;
            label.AddToClassList("WayPointText");
            label.style.top = 50;
            element.asset.style.alignItems = Align.Center;
            element.asset.style.justifyContent = Justify.Center;
            element.asset.Add(label);
        }

        private void AddElement(int id,string name,Color tint, BoxTransform transform)
        {
            AddElement(miniMapAssets[id], id, name, tint, transform);
        }
    }

    internal class Compass
    {
        public VisualElement rotator;

        public VisualElement North;
        public VisualElement East;
        public VisualElement South;
        public VisualElement West;

        public void RotateTo(float heading)
        {
            rotator.style.rotate = new Rotate(heading);

            var inverse = new Rotate(-heading);
            North.style.rotate = inverse;
            East.style.rotate = inverse;
            South.style.rotate = inverse;
            West.style.rotate = inverse;
        }
    }

    internal class MiniMap
    {
        public VisualElement root;
        public List<MiniMapElement> mapAssembly;
        public List<MiniMapElement> waypoints;
    }

    internal class MiniMapElement : IComparable<MiniMapElement> 
    {
        public int originalInstanceId;
        public BoxTransform transform;
        public VisualElement asset;
        public int CompareTo(MiniMapElement other)
        {
            return transform.pos.y.CompareTo(other.transform.pos.y);
        }
    }
}