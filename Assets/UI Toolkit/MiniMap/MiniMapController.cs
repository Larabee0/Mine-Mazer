using MazeGame.Input;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace MazeGame.MiniMap
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

        private void Awake()
        {
            if(mapGenerator == null|| !mapGenerator.isActiveAndEnabled)
            {
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
            mapGenerator.OnMapUpdate += MapUpdateEvent;
            

            pixelsPerUnit = (textureResolution/2) / viewPortSize;

            if(InputManager.Instance != null)
            {
                InputManager.Instance.OnLookDelta += OnLook;
                InputManager.Instance.OnMoveAxis += OnMove;

            }
            player = FindObjectOfType<Improved_Movement>().transform;
        }
        private void Start()
        {
            Debug.Log(Application.targetFrameRate);
            Debug.Log(QualitySettings.vSyncCount);
        }
        private void OnLook(Vector2 axis)
        {
            float angle = player.rotation.eulerAngles.y;
            compass.RotateTo(-angle);
        }

        private void OnMove(Vector2 axis)
        {
            Vector3 pos = player.position;
            miniMap.root.style.translate = new Translate(-(pos.x * pixelsPerUnit) , ((pos.z * pixelsPerUnit)) );
            miniMap.waypoints.ForEach(element =>TranslateWayPoint(element));
        }

        //private void OnValidate()
        //{
        //    if (Application.isPlaying)
        //    {
        //        pixelsPerUnit = textureResolution / viewPortSize;
        //        miniMap.root.style.scale = new Scale(new Vector2(miniMapScale, miniMapScale));
        //        MapUpdateEvent();
        //    }
        //}

        private void OnDestroy()
        {
            mapGenerator.OnMapUpdate -= MapUpdateEvent;
        }

        private void MapUpdateEvent()
        {
            MapUpdateProcess();
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
                                    pos = ring[j].transform.TransformPoint(ring[j].strongKeepPosition),
                                    rot = ring[j].Rotation
                                }
                                : new BoxTransform
                                {
                                    pos = ring[j].stagnationBeacon.transform.position,
                                    rot = ring[j].stagnationBeacon.transform.rotation
                                };
                            AddElement(stagnationBeacon, instanceid, ring[j].name, Color.white, trans);
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
                            pos = section.transform.TransformPoint(section.strongKeepPosition),
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
                    TranslateWayPoint(element);
                }
            }

            OnMove(Vector2.zero);
        }

        private void TranslateWayPoint(MiniMapElement element)
        {
            BoxTransform trans = element.transform;
            Vector2 pixelPos = new((trans.pos.x * pixelsPerUnit) + (textureResolution / 2), (-(trans.pos.z * pixelsPerUnit)) + (textureResolution / 2));
            pixelPos = (pixelPos.normalized * 500);

            element.asset.style.translate = new Translate(pixelPos.x, pixelPos.y);
            element.asset.transform.rotation = Quaternion.Euler(0, 0, ((Quaternion)trans.rot).eulerAngles.y);
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