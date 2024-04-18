using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class RadialInventoryComp : MonoBehaviour
{
    RadialMenuItem m_radialItem;

    private void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        m_radialItem = new RadialMenuItem()
        {
            style =
            {
                position = Position.Absolute,
                left = 20, top = 20, width = 20, height = 200
            }
        };

        root.Add(m_radialItem);
    }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.O))
        {
            m_radialItem.Segments -= 1;
        }
        if (Input.GetKeyUp(KeyCode.P))
        {
            m_radialItem.Segments += 1;
        }
    }
}
