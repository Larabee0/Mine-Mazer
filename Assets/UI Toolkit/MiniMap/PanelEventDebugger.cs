using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

public class PanelEventDebugger : MonoBehaviour
{

    void Update()
    {
        if (EventSystem.current == null) return;

        PointerEventData ped = new(EventSystem.current);
        List<RaycastResult> results = new();
        EventSystem.current.RaycastAll(ped, results);

        if (results.Count > 0)
        {
            Debug.Log(results[0].gameObject.name);
            if (results[0].gameObject.TryGetComponent(out PanelEventHandler eventHandler))
            {
                Vector2 mousePos = Input.mousePosition;
                mousePos.y = -mousePos.y;
                Debug.Log(eventHandler.panel.Pick(RuntimePanelUtils.ScreenToPanel(eventHandler.panel, mousePos)).name);
            }
        }
    }
}
