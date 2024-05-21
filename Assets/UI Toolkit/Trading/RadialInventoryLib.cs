using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class RadialMenuItem : VisualElement
{
    public new class UxmlTraits : VisualElement.UxmlTraits
    {
        UxmlBoolAttributeDescription m_ForceLabels = new()
        {
            name = "forceLabels"
        };
        UxmlColorAttributeDescription m_BackgroundColor = new()
        {
            name = "menuBackgroundColor"
        };
        UxmlColorAttributeDescription m_DivisionColor = new()
        {
            name = "menuDivisionColor"
        };
        UxmlIntAttributeDescription m_SegmentsPerPageAtribute = new()
        {
            name = "segmentsPerPage"
        };
        UxmlIntAttributeDescription m_SegementAtribute = new()
        {
            name = "segments"
        };
        UxmlFloatAttributeDescription m_LineThickness = new()
        {
            name = "lineThickness"
        };
        UxmlFloatAttributeDescription m_radius = new()
        {
            name = "radius"
        };
        UxmlFloatAttributeDescription m_divThickness = new()
        {
            name = "divisionThickness"
        };

        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
        {
            base.Init(ve, bag, cc);
            (ve as RadialMenuItem).ForceLabels = m_ForceLabels.GetValueFromBag(bag, cc);
            (ve as RadialMenuItem).Segments = m_SegementAtribute.GetValueFromBag(bag, cc);
            (ve as RadialMenuItem).SegmentsPerPage = m_SegmentsPerPageAtribute.GetValueFromBag(bag, cc);
            (ve as RadialMenuItem).LineThickness = m_SegmentsPerPageAtribute.GetValueFromBag(bag, cc);
            (ve as RadialMenuItem).LineThickness = m_LineThickness.GetValueFromBag(bag, cc);
            (ve as RadialMenuItem).Radius = m_radius.GetValueFromBag(bag, cc);
            (ve as RadialMenuItem).BackgroundColor = m_BackgroundColor.GetValueFromBag(bag, cc);
            (ve as RadialMenuItem).DivisionColor = m_DivisionColor.GetValueFromBag(bag, cc);
            (ve as RadialMenuItem).DivisionThickness = m_divThickness.GetValueFromBag(bag, cc);
        }
    }

    public new class UxmlFactory : UxmlFactory<RadialMenuItem, UxmlTraits> { }

    public static readonly string ussClassName = "radial-item";
    public static readonly string ussLabelClassName = "radial-item__label";
    public static readonly string ussButtonLeftClassName = "radial__button-left";
    public static readonly string ussButtonRightClassName = "radial__button-right";

    static CustomStyleProperty<Color> s_TrackColor = new("--track-color");
    static CustomStyleProperty<Color> s_ItemColor = new("--item-color");

    Color m_TrackedColor = Color.gray;
    Color m_ItemColor = Color.red;

    Label m_label;
    Button m_left;
    Button m_right;

    private List<InventoryItem> internalItems = new();
    private List<Label> internalLabels = new();
    private List<string> inventoryDisplay = new();
    public List<Action> inventoryActions = new();

    int m_segmentHover = -1;
    bool mouseCapture = false;
    bool active = false;

    int m_pages = 1;
    int m_curpage = 0;
    int m_segmentsPerPage = 8;
    int m_segments = 1;
    float m_lineThickness;
    float m_divThickness = 4;
    float m_radius = 1;

    Color m_backgroundColor = Color.white;
    Color m_divisionColor = Color.black;

    public float DegreesPerSegment => 360f / CurPageSegments;

    public int CurPageSegments
    {
        get
        {
            if (m_pages > m_curpage + 1)
            {
                return m_segmentsPerPage;
            }
            else
            {
                int piorPages = m_curpage * m_segmentsPerPage;
                return piorPages + m_segmentsPerPage <= m_segments
                    ? m_segmentsPerPage
                    : Mathf.Clamp(m_segments - piorPages, 1, m_segmentsPerPage);
            }
        }
    }

    public int CurPageStartIndex => m_curpage * m_segmentsPerPage;

    public int SelectedItem => m_segmentHover;

    public Color BackgroundColor
    {
        get => m_backgroundColor;
        set
        {
            m_backgroundColor = value;
            MarkDirtyRepaint();
        }
    }
    public Color DivisionColor
    {
        get => m_divisionColor;
        set
        {
            m_divisionColor = value;
            MarkDirtyRepaint();
        }
    }

    public float DivisionThickness
    {
        get => m_divThickness;
        set
        {
            value = Mathf.Clamp(value, 1, m_lineThickness);
            value = Mathf.Max(1, value);
            m_divThickness = value;
            MarkDirtyRepaint();
        }
    }

    public float Radius
    {
        get => m_radius;
        set
        {
            value = Mathf.Clamp01(value);
            m_radius = value;
            MarkDirtyRepaint();
        }
    }

    public float LineThickness
    {
        get => m_lineThickness;
        set
        {
            value = Mathf.Max(10, value);
            m_lineThickness = value;
            MarkDirtyRepaint();
        }
    }

    public int Curpage
    {
        get => m_curpage;
        set
        {
            m_curpage = value;
            m_label.text = string.Format("{0}/{1}", m_curpage + 1, m_pages);            
            MarkDirtyRepaint();
        }
    }

    public int SegmentsPerPage
    {
        get
        {
            if(m_segmentsPerPage == 0)
            {
                m_segmentsPerPage = 1;
                RecalucateItemsPerPage();
            }
            return m_segmentsPerPage;
        }

        set
        {
            m_segmentsPerPage = value;
            RecalucateItemsPerPage();
            MarkDirtyRepaint();
        }
    }

    public int Segments
    {
        get => m_segments;
        set
        {
            value = Mathf.Max(value, 1);
            m_segments = value;
            RecalucateItemsPerPage();
            MarkDirtyRepaint();
        }
    }

    public bool ForceLabels
    {
        get => m_forceLabels;
        set
        {
            m_forceLabels = value;

            if (value)
            {
                UpdateLabels();
                UpdateIventoryItems();
            }

            MarkDirtyRepaint();
        }
    }

    bool m_forceLabels;

    public RadialMenuItem()
    {
        m_label = new Label();
        m_label.AddToClassList(ussLabelClassName);
        m_label.style.alignContent = Align.Center;
        m_label.pickingMode = PickingMode.Ignore;


        m_left = new Button();
        m_right = new Button();
        m_left.AddToClassList(ussButtonLeftClassName);
        m_right.AddToClassList(ussButtonRightClassName);
        m_left.style.alignContent = Align.Center;
        m_right.style.alignContent = Align.Center;
        Add(m_left);
        Add(m_label);
        Add(m_right);


        AddToClassList(ussClassName);

        RegisterCallback<CustomStyleResolvedEvent>(evt => CustomStyleResolved(evt));
        RegisterCallback<MouseOverEvent>(evt => MouseOver(evt));
        RegisterCallback<MouseOutEvent>(evt => MouseOut(evt));
        RegisterCallback<MouseMoveEvent>(evt => MouseMove(evt));

        RegisterCallback<PointerDownEvent>(evt => OnClickIntercept(evt));
        RegisterCallback<PointerUpEvent>(evt => OnClickRelease(evt));
        RegisterCallback<NavigationSubmitEvent>(evt => OnNavIntercept(evt));


        m_left.RegisterCallback<PointerUpEvent>(evt => OnPageAdvance(evt, -1));
        m_left.RegisterCallback<NavigationSubmitEvent>(evt => OnPageAdvance(evt, -1));
        m_right.RegisterCallback<PointerUpEvent>(evt => OnPageAdvance(evt, 1));
        m_right.RegisterCallback<NavigationSubmitEvent>(evt => OnPageAdvance(evt, 1));

        generateVisualContent += GenerateVisualContent;
    }

    private static void OnNavIntercept(NavigationSubmitEvent evt)
    {
        RadialMenuItem element = (RadialMenuItem)evt.currentTarget;
        OnSetActive(element,false);
    }

    private static void OnClickIntercept(PointerDownEvent evt)
    {
        RadialMenuItem element = (RadialMenuItem)evt.currentTarget;
        OnSetActive(element,true);
    }

    private static void OnSetActive(RadialMenuItem element, bool active)
    {
        element.active = active;
        element.MarkDirtyRepaint();
        if (!active)
        {
            element.InvokeEvent();
        }
    }

    private static void PageAdvance(RadialMenuItem element, int dir)
    {
        int newPage =   (element.m_curpage + dir) % element.m_pages;
        element.Curpage = newPage < 0 ? element.m_pages - 1 : newPage;
        element.UpdateLabels();
        element.UpdateIventoryItems();
        element.SetLabelVisibility(true);
    }

    private static void OnPageAdvance(PointerUpEvent evt, int dir)
    {
        PageAdvance((RadialMenuItem)((Button)evt.currentTarget).parent, dir);
    }

    private static void OnPageAdvance(NavigationSubmitEvent evt, int dir)
    {
        PageAdvance((RadialMenuItem)((Button)evt.currentTarget).parent, dir);
    }

    private static void OnClickRelease(PointerUpEvent evt)
    {
        
        RadialMenuItem element = (RadialMenuItem)evt.currentTarget;
        OnSetActive(element,false);
    }

    private static void MouseOut(MouseOutEvent evt)
    {
        RadialMenuItem element = (RadialMenuItem)evt.currentTarget;
        //element.internalLabels.ForEach(label => label.style.color = Color.white);
        element.internalItems.ForEach(label => label.Color = Color.white);
        element.m_segmentHover = -1;
        element.mouseCapture = false;
        element.MarkDirtyRepaint();
    }

    private static void MouseOver(MouseOverEvent evt)
    {
        RadialMenuItem element = (RadialMenuItem)evt.currentTarget;
        element.mouseCapture = true;
    }

    private static void MouseMove(MouseMoveEvent evt)
    {
        RadialMenuItem element = (RadialMenuItem)evt.currentTarget;
        if (element.mouseCapture)
        {
            element.UpdateSegmentHover(evt.localMousePosition);
            element.MarkDirtyRepaint();
        }
    }

    private void UpdateSegmentHover(Vector2 localMousePosition)
    {
        Vector2 center = new Vector2(contentRect.width,contentRect.height) * 0.5f;
        float dstFromCenter = Unity.Mathematics.math.distance(center, localMousePosition);

        float radius = center.x * 0.6f * m_radius;
        float dst = Unity.Mathematics.math.distancesq(center, localMousePosition);
        float lineThickness = Mathf.Min(LineThickness, radius + ( Mathf.Min(center.x, center.y) * 0.5f) );

        if ( dst < radius * radius || dst > (radius + lineThickness)* (radius + lineThickness))
        {
            internalItems.ForEach(label => label.Color = Color.white);
            m_segmentHover = -1;
            return;
        }

        float deg = Mathf.Rad2Deg * Mathf.Atan2(localMousePosition.y - contentRect.height * 0.5f, localMousePosition.x - contentRect.width * 0.5f);

        if (deg < 0)
        {
            deg += 360f;
        }

        ProcessAxisAngle(deg);
    }

    public void InputAxisAngle(float angle)
    {
        if (angle < 0)
        {
            angle += 360f;
        }
        ProcessAxisAngle(angle);
        Focus();
        MarkDirtyRepaint();
    }

    public void ProcessAxisAngle(float angle)
    {
        m_segmentHover = (int)(angle / DegreesPerSegment);
        internalItems.ForEach(label => label.Color = Color.white);
    }


    public void ChangePage(int dir)
    {
        PageAdvance(this, dir);
    }

    static void CustomStyleResolved(CustomStyleResolvedEvent evt)
    {
        RadialMenuItem element = (RadialMenuItem)evt.currentTarget;
        element.UpdateCustomStyles();
    }

    private void UpdateCustomStyles()
    {
        bool repaint = false;
        if (customStyle.TryGetValue(s_ItemColor, out m_ItemColor))
        {
            repaint = true;
        }
        if (customStyle.TryGetValue(s_TrackColor, out m_TrackedColor))
        {
            repaint = true;
        }
        if (repaint)
        {
            //UpdateLabels();
            MarkDirtyRepaint();
        }
    }

    private void RecalucateItemsPerPage()
    {
        int segmentsPerPage = SegmentsPerPage;
        int pages = Mathf.CeilToInt((float)Segments / (float)segmentsPerPage);
        m_pages = Mathf.Max(pages, 1);
        if (m_pages <= 1)
        {
            m_label.style.display = DisplayStyle.None;
            m_left.style.display = DisplayStyle.None;
            m_right.style.display = DisplayStyle.None;
        }
        else
        {
            m_label.style.display = DisplayStyle.Flex;
            m_left.style.display = DisplayStyle.Flex;
            m_right.style.display = DisplayStyle.Flex;
        }
        while (m_curpage >= m_pages)
        {
            m_curpage--;
        }

        Curpage = Mathf.Max(m_curpage, 0);

    }

    void GenerateVisualContent(MeshGenerationContext context)
    {
        float width = contentRect.width;
        float height = contentRect.height;

        float radius = width * (0.5f * m_radius);
        Vector2 center = new(width * 0.5f, height * 0.5f);

        var painter = context.painter2D;
        float backgroundLineWidth = painter.lineWidth = Mathf.Min(LineThickness, (radius + Mathf.Min(width, height)) * 0.5f);
        painter.lineCap = LineCap.Butt;

        int drawCount = Mathf.Max(1, Mathf.Min(CurPageSegments, SegmentsPerPage));

        float startAngle = 0f;
        for (int i = 0; i < drawCount; i++)
        {
            if (m_segmentHover == i && active)
            {
                painter.strokeColor = new Color32(35,168,197,255);
            }
            else if (m_segmentHover == i)
            {
                painter.strokeColor = new Color32(35, 168, 197, 255);
            }
            else
            {
                painter.strokeColor = BackgroundColor;
            }
            painter.BeginPath();
            painter.Arc(center, radius, startAngle, startAngle + DegreesPerSegment);
            painter.Stroke();
            startAngle += DegreesPerSegment;
        }
        startAngle = 0f;
        painter.strokeColor = DivisionColor;
        float ThetaScale = (1f / (drawCount)) + 1;
        float theta = 0;
        painter.lineWidth = m_divThickness;
        painter.lineCap = LineCap.Butt;
        for (int i = 0; i < drawCount; i++)
        {
            painter.BeginPath();
            theta += 2.0f * Mathf.PI * ThetaScale;
            float x = Mathf.Cos(theta);
            float y = Mathf.Sin(theta);

            painter.LineTo(center + (new Vector2(x, y) * (radius - 0.2f - (backgroundLineWidth * 0.5f))));
            painter.LineTo(center + (new Vector2(x, y) * (radius + 0.2f + (backgroundLineWidth * 0.5f))));

            painter.Stroke();
            startAngle += 360f / (drawCount);
        }


    }

    public void UpdateLabels()
    {
        return;
        for (int i = internalLabels.Count; i < Segments; i++)
        {
            internalLabels.Add(new Label() { text = string.Format("Test {0}", i) });
            internalLabels[^1].style.position = Position.Absolute;
            internalLabels[^1].style.alignContent = Align.Center;
            internalLabels[^1].style.marginBottom = 0;
            internalLabels[^1].style.marginTop = 0;
            internalLabels[^1].style.marginRight = 0;
            internalLabels[^1].style.marginLeft = 0;
            internalLabels[^1].style.paddingBottom = 0;
            internalLabels[^1].style.paddingTop = 0;
            internalLabels[^1].style.paddingRight = 0;
            internalLabels[^1].style.paddingLeft = 0;
            internalLabels[^1].style.unityTextAlign = TextAnchor.MiddleCenter;
            internalLabels[^1].style.color = Color.white;
            internalLabels[^1].pickingMode = PickingMode.Ignore;
            internalLabels[^1].style.visibility = Visibility.Hidden;
            Add(internalLabels[^1]);
        }

        internalLabels.ForEach(label =>
        {
            label.style.display = DisplayStyle.None;
            label.style.visibility = Visibility.Hidden;
        });


        int drawCount = Mathf.Max(1, Mathf.Min(CurPageSegments, SegmentsPerPage));
        //drawCount = Segments;

        float height = contentRect.height;
        float radius = contentRect.width * (0.5f * m_radius);
        float ThetaScale = (1f / (drawCount * 2)) + 1;
        float theta = 0;
        int j = CurPageStartIndex;
        for (int i = 0; i < drawCount * 2; i++)
        {
            theta += 2.0f * Mathf.PI * ThetaScale;
            if (i % 2 != 0)
            {
                continue;
            }
            float x = Mathf.Cos(theta);
            float y = Mathf.Sin(theta);
            Vector2 pos = new(x * radius, y * radius);
            Label cur = internalLabels[j];
            float w = cur.resolvedStyle.width * 0.5f;
            float h = cur.resolvedStyle.height * 0.5f;
            cur.style.transformOrigin = new TransformOrigin(new Length(50, LengthUnit.Percent), new Length(50, LengthUnit.Percent));
            // cur.style.translate = new Translate(pos.x, pos.y - h + (height * 0.5f));
            cur.style.translate = new Translate(pos.x, pos.y);
            cur.style.display = DisplayStyle.Flex;
            j++;
        }
    }


    public void UpdateIventoryItems()
    {
        for (int i = internalItems.Count; i < Segments; i++)
        {
            internalItems.Add(new InventoryItem(this) { Text = string.Format("Test {0}", i) });
        }

        internalItems.ForEach(item =>
        {
            item.Display = DisplayStyle.None;
            item.Visibility = Visibility.Hidden;
        });

        int drawCount = Mathf.Max(1, Mathf.Min(CurPageSegments, SegmentsPerPage));

        float height = contentRect.height;
        float radius = contentRect.width * (0.5f * m_radius);
        float ThetaScale = (1f / (drawCount * 2)) + 1;
        float theta = 0;
        int j = CurPageStartIndex;
        for (int i = 0; i < drawCount * 2; i++)
        {
            theta += 2.0f * Mathf.PI * ThetaScale;
            if (i % 2 != 0)
            {
                continue;
            }
            float x = Mathf.Cos(theta);
            float y = Mathf.Sin(theta);
            Vector2 pos = new(x * radius, y * radius);
            VisualElement cur = internalItems[j].parent;
            float w = cur.resolvedStyle.width * 0.5f;
            float h = cur.resolvedStyle.height * 0.5f;
            cur.style.transformOrigin = new TransformOrigin(new Length(50, LengthUnit.Percent), new Length(50, LengthUnit.Percent));
            cur.style.translate = new Translate(pos.x, pos.y);
            cur.style.display = DisplayStyle.Flex;
            j++;
        }
    }

    public void SetLabelVisibility(bool visible)
    {
        int j = CurPageStartIndex;
        for (int i = 0; i < CurPageSegments ; i++)
        {
            //internalLabels[j].style.visibility = visible ? Visibility.Visible : Visibility.Hidden;
            internalItems[j].Visibility = visible ? Visibility.Visible : Visibility.Hidden;
            j++;
        }
    }

    public void PushInventory(List<string> inventory, List<Texture2D> icons = null)
    {
        inventoryDisplay = inventory;
        Segments = inventory.Count;
        UpdateLabels();
        UpdateIventoryItems();

        for (int i = 0; i < inventory.Count; i++)
        {
            //internalLabels[i].text = inventory[i];
            internalItems[i].Text = inventory[i];
            internalItems[i].Icon = icons[i];
        }
    }

    public void PushInventory(List<string> inventory,List<Action> actions, List<Texture2D> icons = null)
    {
        PushInventory(inventory, icons);
        inventoryActions = actions;
    }

    public void InvokeEvent()
    {
        int actionindex = m_segmentHover + CurPageStartIndex;

        if(m_segmentHover >= 0 && actionindex < inventoryActions.Count)
        {
            inventoryActions[actionindex].Invoke();
        }
    }

}

public class InventoryItem
{
    public VisualElement parent;
    public VisualElement icon;
    public Label label;

    public DisplayStyle Display
    {
        get => parent.style.display.value;
        set => parent.style.display = value;
    }

    public Visibility Visibility
    {
        get => parent.style.visibility.value;
        set => parent.style.visibility = value;
    }

    public string Text
    {
        get => label.text;
        set => label.text = value;
    }

    public Texture2D Icon
    {
        get => icon.style.backgroundImage.value.texture;
        set => icon.style.backgroundImage = value;
    }

    public Color Color
    {
        get => label.style.color.value;
        set => label.style.color = value;
    }

    public InventoryItem(RadialMenuItem radialMenu)
    {
        parent = new();
        icon = new();
        label = new();

        parent.Add(icon);
        parent.Add(label);
        radialMenu.Add(parent);
        SetStyle(parent);
        label.style.color = Color.white;
        label.style.unityTextAlign = TextAnchor.MiddleCenter;
        icon.style.width = 64;
        icon.style.height = 64;
        icon.style.backgroundColor = new Color(1,1,1,0.35f);
    }


    public void SetStyle(VisualElement element)
    {
        element.style.position = Position.Absolute;
        element.style.alignContent = Align.Center;
        element.style.alignItems = Align.Center;
        element.style.marginBottom = 0;
        element.style.marginTop = 0;
        element.style.marginRight = 0;
        element.style.marginLeft = 0;
        element.style.paddingBottom = 0;
        element.style.paddingTop = 0;
        element.style.paddingRight = 0;
        element.style.paddingLeft = 0;
        element.pickingMode = PickingMode.Ignore;
        element.style.visibility = Visibility.Hidden;
    }
}
