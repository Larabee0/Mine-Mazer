using MazeGame.Input;
using System;
using System.Collections;
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
        [SerializeField] private float zoomLevelFadeSpeed = 1;
        [SerializeField] private float zoomLevelHoldTime = 3;
        [SerializeField, Min(0.1f)] private float miniMapMinZoom = 0.5f;
        [SerializeField, Min(1f)] private float miniMapMaxZoom = 3.2f;
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
        private Label zoomLevel;
        private float pixelsPerUnit;
        private float angle;

        private void Awake()
        {
            miniMapMinZoom = miniMapMinZoom > miniMapMaxZoom ? miniMapMaxZoom : miniMapMinZoom;
            if (mapGenerator == null|| !mapGenerator.isActiveAndEnabled)
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

            zoomLevel = DocRoot.Q<Label>("ZoomLevel");

            miniMapAssets = mapGenerator.GenerateMiniMapTextures();
            

            pixelsPerUnit = (textureResolution*0.5f) / viewPortSize;

            player = FindObjectOfType<Improved_Movement>().transform;
            if (player == null)
            {
                Debug.LogError("No Player, UI cannot start");
                enabled = false;
                return;
            }

            player.GetComponent<AutoLadder>().autoLadderTransform += UpdateMiniMapForce;
            mapGenerator.OnMapUpdate += MapUpdateEvent;
            minimapZoomOffset = textureResolution * (miniMapScale - 1);
            minimapZoomOffset -= minimapCentreOffset;


        }

        private void Start()
        {
            ScaleMap(0.3f);
            //DebugMap();
        }

        private void OnEnable()
        {

            if (InputManager.Instance != null)
            {
                InputManager.Instance.lookAxis.OnAxis += OnLook;
                InputManager.Instance.moveAxis.OnAxis += OnMove;

                InputManager.Instance.PlayerActions.MinimapZoomOut.canceled += ZoomOut;
                InputManager.Instance.PlayerActions.MinimapZoomIn.canceled += ZoomIn;
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

                InputManager.Instance.PlayerActions.MinimapZoomOut.canceled -= ZoomOut;
                InputManager.Instance.PlayerActions.MinimapZoomIn.canceled -= ZoomIn;
            }
        }

        public void UpdateMiniMapForce()
        {
            Vector2 axis = Vector2.zero;
            OnLook(axis);
            OnMove(axis);
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

        private void ZoomOut(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            ScaleMap(-0.1f);
        }

        private void ZoomIn(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            ScaleMap(0.1f);
        }

        private void ScaleMap(float newScale)
        {
            miniMapScale += newScale;
            miniMapScale = Mathf.Clamp(miniMapScale,miniMapMinZoom,miniMapMaxZoom);
            minimapZoomOffset = textureResolution * (miniMapScale - 1);
            minimapZoomOffset -= minimapCentreOffset;
            miniMap.root.style.scale = new Scale(new Vector2(miniMapScale, miniMapScale));
            TranslateMap();

            StopAllCoroutines();
            StartCoroutine(ZoomLevelIndicator());
        }

        private IEnumerator ZoomLevelIndicator()
        {
            zoomLevel.text = string.Format("x{0}", miniMapScale.ToString("0.0"));
            zoomLevel.style.opacity = 100;


            yield return new WaitForSeconds(zoomLevelHoldTime);

            for (float i = 1; i >= 0; i-=Time.deltaTime * zoomLevelFadeSpeed)
            {
                zoomLevel.style.opacity = i;
                yield return null;
            }
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
            AddElement(stagnationBeacon, 0, Color.white, trans);
            miniMap.root.Add(miniMap.mapAssembly[^1].asset);
            miniMap.waypoints.Add(miniMap.mapAssembly[^1]);
            miniMap.mapAssembly[^1].asset.style.translate = new Translate((trans.pos.x * pixelsPerUnit) + (textureResolution / 2), (-(trans.pos.z * pixelsPerUnit)) + (textureResolution * 0.5f));
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
            miniMap.waypoints.Clear();
            List<List<MapTreeElement>> mapTree = mapGenerator.MapTree;
            float curHeight = mapGenerator.CurPlayerSection.sectionInstance.Position.y;
            for (int i = 0; i < mapTree.Count; i++)
            {
                List<MapTreeElement> ring = mapTree[i];
                for (int j = 0; j < ring.Count; j++)
                {
                    MapTreeElement section = ring[j];
                    int instanceid = section.OriginalInstanceId;
                    if (miniMapAssets.ContainsKey(instanceid))
                    {
                        Vector3 position = section.LocalToWorld.Translation();
                        float sectioHieght = position.y;

                        Color above = section.Explored ? aboveExplored : aboveUnExplored;
                        Color below = section.Explored ? belowExplored : belowUnexplored;
                        Color same = section.Explored ? explored : unexplored;

                        Color tint = sectioHieght > curHeight ? above : same;
                        tint = sectioHieght < curHeight ? below : tint;

                        tint = section == mapGenerator.CurPlayerSection ? playerCurrent : tint;
                        var trans = new BoxTransform
                        {
                            pos = position,
                            rot = section.LocalToWorld.Rotation()
                        };
                        AddElement(instanceid, tint, trans);
                        if (section.Keep)
                        {
                            trans = new BoxTransform
                            {
                                pos = section.WaypointPosition,
                                rot = section.LocalToWorld.Rotation()
                            };
                            AddElement(stagnationBeacon, instanceid, Color.white, trans);
                            miniMap.waypoints.Add(miniMap.mapAssembly[^1]);
                            AddText(miniMap.mapAssembly[^1], section.WaypointName);
                        }
                    }
                    else if(instanceid != mapGenerator.DeadEndPlugInstanceId)
                    {
                        Debug.LogErrorFormat("Missing minimap asset for original instance id {0} {1}", instanceid,section.GameObjectName);
                    }
                }
            }
            miniMap.mapAssembly.Sort();

            miniMap.mapAssembly.ForEach(element => miniMap.root.Add(element.asset));

            for (int i = 0; i < miniMap.mapAssembly.Count; i++)
            {
                MiniMapElement element = miniMap.mapAssembly[i];
                BoxTransform transform = element.transform;
                element.asset.style.translate = new Translate((transform.pos.x * pixelsPerUnit) + (textureResolution * 0.5f), (-(transform.pos.z * pixelsPerUnit)) + (textureResolution / 2));
                element.asset.transform.rotation = Quaternion.Euler(0, 0, ((Quaternion)transform.rot).eulerAngles.y);
            }
            int startIndex = miniMap.mapAssembly.Count;
            List<MapTreeElement> mothballedSections = mapGenerator.GetMothballedSections();
            for (int i = 0; i < mothballedSections.Count; i++)
            {
                MapTreeElement section = mothballedSections[i];

                if (section.Keep)
                {
                    BoxTransform trans = new BoxTransform
                    {
                        pos = section.WaypointPosition,
                        rot = section.LocalToWorld.Rotation()
                    };
                    AddElement(stagnationBeacon, section.OriginalInstanceId, Color.white, trans);
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
                (trans.pos.x * pixelsPerUnit) + (textureResolution *0.5f),
                (-(trans.pos.z * pixelsPerUnit)) + (textureResolution * 0.5f));
            Vector3 pos = player.position;
            Vector2 playerPos = new((pos.x * pixelsPerUnit) + (textureResolution * 0.5f), ((-pos.z * pixelsPerUnit)) + (textureResolution * 0.5f));
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

        private void AddElement(Texture2D texture, int id, Color tint, BoxTransform transform)
        {
            var element = new MiniMapElement { asset = new VisualElement() { name = texture.name, usageHints = UsageHints.DynamicTransform }, originalInstanceId = id, transform = transform };
            element.asset.style.backgroundImage = texture;
            element.asset.style.height = textureResolution;
            element.asset.style.width = textureResolution;
            element.asset.style.position = Position.Absolute;
            element.asset.style.unityBackgroundImageTintColor = tint;
            element.asset.pickingMode = PickingMode.Ignore;
            miniMap.mapAssembly.Add(element);
        }

        private void AddText(MiniMapElement element, string text)
        {
            Label label = new()
            {
                text = text,
                usageHints = UsageHints.DynamicTransform
            };
            // label.style.position = Position.Absolute;
            label.AddToClassList("WayPointText");
            label.style.top = 50;
            element.asset.style.alignItems = Align.Center;
            element.asset.style.justifyContent = Justify.Center;
            element.asset.Add(label);
            element.asset.pickingMode = PickingMode.Ignore;
        }

        private void AddElement(int id,Color tint, BoxTransform transform)
        {
            AddElement(miniMapAssets[id], id, tint, transform);
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