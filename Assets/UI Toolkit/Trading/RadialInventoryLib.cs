using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class RadialInventoryLib
{
    
}


public class RadialMenuItem : VisualElement
{
    public new class UxmlTraits : VisualElement.UxmlTraits
    {
        UxmlColorAttributeDescription m_BackgroundColor = new()
        {
            name = "menuBackgroundColor"
        };
        UxmlColorAttributeDescription m_DivisionColor = new()
        {
            name = "menuDivisionColor"
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
            (ve as RadialMenuItem).Segments = m_SegementAtribute.GetValueFromBag(bag, cc);
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

    static CustomStyleProperty<Color> s_TrackColor = new("--track-color");
    static CustomStyleProperty<Color> s_ItemColor = new("--item-color");

    Color m_TrackedColor = Color.gray;
    Color m_ItemColor = Color.red;

    Label m_label;

    List<Label> internalLabels = new();
    List<string> inventoryDisplay = new();

    int m_segmentHover = -1;
    bool mouseCapture = false;
    bool active = false;

    int m_segments = 1;
    float m_lineThickness;
    float m_divThickness = 4;
    float m_radius = 1;

    Color m_backgroundColor = Color.white;
    Color m_divisionColor = Color.black;

    public float DegreesPerSegment => 360f / m_segments;

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

    public int Segments
    {
        get => m_segments;
        set
        {
            value = Mathf.Max(value, 1);
            m_segments = value;
            m_label.text = value.ToString();
            UpdateLabels();
            MarkDirtyRepaint();
        }
    }

    public RadialMenuItem()
    {
        m_label = new Label();
        m_label.AddToClassList(ussLabelClassName);
        m_label.style.alignContent = Align.Center;
        m_label.pickingMode = PickingMode.Ignore;
        Add(m_label);

        AddToClassList(ussClassName);

        RegisterCallback<CustomStyleResolvedEvent>(evt => CustomStyleResolved(evt));

        RegisterCallback<MouseOverEvent>(evt => MouseOver());
        RegisterCallback<MouseOutEvent>(evt => MouseOut());
        RegisterCallback<MouseMoveEvent>(evt => MouseMove(evt));

        RegisterCallback<PointerDownEvent>(evt => OnClickIntercept(evt));
        RegisterCallback<PointerUpEvent>(evt => OnClickRelease(evt));
        RegisterCallback<NavigationSubmitEvent>(evt => OnNavIntercept(evt));

        generateVisualContent += GenerateVisualContent;
    }

    private void OnNavIntercept(NavigationSubmitEvent evt)
    {
        //UpdateSegmentHover(evt.originalMousePosition);
        active = true;
        MarkDirtyRepaint();
    }

    private void OnClickIntercept(PointerDownEvent evt)
    {
        //UpdateSegmentHover(evt.originalMousePosition);
        active = true;
        MarkDirtyRepaint();
    }

    private void OnClickRelease(PointerUpEvent evt)
    {
        active = false;
        MarkDirtyRepaint();
    }

    private void MouseOut()
    {
        internalLabels.ForEach(label => label.style.color = Color.white);
        m_segmentHover = -1;
        mouseCapture = false;
        MarkDirtyRepaint();
    }

    private void MouseOver()
    {
        mouseCapture = true;
    }

    private void MouseMove(MouseMoveEvent evt)
    {
        if (mouseCapture)
        {
            UpdateSegmentHover(evt.localMousePosition);
            MarkDirtyRepaint();
        }
    }

    private void UpdateSegmentHover(Vector2 localMousePosition)
    {
        float deg = Mathf.Rad2Deg * Mathf.Atan2(localMousePosition.y - contentRect.height * 0.5f, localMousePosition.x - contentRect.width * 0.5f);

        if (deg < 0)
        {
            deg += 360f;
        }

        m_segmentHover = (int)(deg / DegreesPerSegment);
        internalLabels.ForEach(label => label.style.color = Color.white);
        //if (m_segmentHover >= 0)
        //{
        //    internalLabels[m_segmentHover].style.color = Color.red;
        //}

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
            UpdateLabels();
            MarkDirtyRepaint();
        }
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


        float startAngle = 0f;
        for (int i = 0; i < Segments; i++)
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
        float ThetaScale = (1f / (Segments)) + 1;
        float theta = 0;
        painter.lineWidth = m_divThickness;
        painter.lineCap = LineCap.Butt;
        for (int i = 0; i < Segments; i++)
        {
            painter.BeginPath();
            theta += 2.0f * Mathf.PI * ThetaScale;
            float x = Mathf.Cos(theta);
            float y = Mathf.Sin(theta);

            painter.LineTo(center + (new Vector2(x, y) * (radius - 0.2f - (backgroundLineWidth * 0.5f))));
            painter.LineTo(center + (new Vector2(x, y) * (radius + 0.2f + (backgroundLineWidth * 0.5f))));

            painter.Stroke();
            startAngle += 360f / (Segments);
        }


    }

    public void UpdateLabels()
    {

        for (int i = internalLabels.Count; i < Segments; i++)
        {
            internalLabels.Add(new Label() { text = "Test" });
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
            Add(internalLabels[^1]);
        }

        internalLabels.ForEach(label => label.style.display = DisplayStyle.None);

        float height = contentRect.height;
        float radius = contentRect.width * (0.5f * m_radius);
        float ThetaScale = (1f / (Segments * 2)) + 1;
        float theta = 0;
        int j = 0;
        for (int i = 0; i < Segments * 2; i++)
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
            cur.style.translate = new Translate(pos.x, pos.y - h + (height * 0.5f));
            cur.style.display = DisplayStyle.Flex;
            j++;
        }
    }

    public void PushInventory(List<string> inventory)
    {
        inventoryDisplay = inventory;
        Segments = inventory.Count;

        for (int i = 0; i < inventory.Count; i++)
        {
            internalLabels[i].text = inventory[i];
        }

        //UpdateLabels();
    }
}