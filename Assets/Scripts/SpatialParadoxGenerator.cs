using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum StartEnd
{
    Unused,
    Start,
    End
}

public class SpatialParadoxGenerator : MonoBehaviour
{
    [SerializeField] private List<TunnelSection> tunnelSections;



    [SerializeField] private TunnelSection nextNextPlayerSection;
    [SerializeField] private TunnelSection nextPlayerSection;

    [SerializeField] private TunnelSection curPlayerSection;

    [SerializeField] private TunnelSection prevPlayerSection;
    [SerializeField] private TunnelSection prevPrevPlayerSection;


    private TunnelSection lastEnter;
    private TunnelSection lastExit;

    private void Start()
    {
        transform.position = Vector3.zero;
        GenerateInitialArea();
    }

    private void GenerateInitialArea()
    {
        int spawnIndex = Random.Range(0, tunnelSections.Count);
        curPlayerSection = InstinateSection(spawnIndex);
    }

    public void PlayerExitSection(TunnelSection section)
    {
        lastExit = section;

        if (lastEnter != null)
        {
            UpdateMap();
        }
    }

    public void PlayerEnterSection(TunnelSection section)
    {
        lastEnter = section;

        if (lastExit != null)
        {
            UpdateMap();
        }
    }

    private void UpdateMap()
    {
        if (lastExit == curPlayerSection && lastEnter != curPlayerSection)
        {
            if (lastEnter == nextPlayerSection)
            {
                Destroy(prevPrevPlayerSection.gameObject);
                
                prevPrevPlayerSection = null;

                prevPrevPlayerSection = prevPlayerSection;
                prevPlayerSection = curPlayerSection;
                curPlayerSection = lastEnter;
                nextPlayerSection = nextNextPlayerSection;
                nextNextPlayerSection = PickSection(nextPlayerSection);
                TransformSection(nextPlayerSection, nextNextPlayerSection);
            }
            else if (lastEnter == prevPlayerSection)
            {
                Destroy(nextNextPlayerSection.gameObject);
                nextNextPlayerSection = null;

                nextNextPlayerSection = nextPlayerSection;
                nextPlayerSection = curPlayerSection;
                curPlayerSection = lastEnter;
                prevPlayerSection = prevPrevPlayerSection;
                prevPrevPlayerSection = PickSection(prevPlayerSection);
                TransformSection(prevPlayerSection, prevPrevPlayerSection);
            }
        }
        lastEnter = null;
        lastExit = null;
    }

    private void TransformSection(TunnelSection primary, TunnelSection secondary)
    {
        StartEnd primaryPreference = primary.LastUsed switch
        {
            StartEnd.Start => StartEnd.End,
            StartEnd.End => StartEnd.Start,
            _ => Random.value < 0.5f ? StartEnd.End : StartEnd.Start,
        };
        StartEnd secondaryPreference = secondary.LastUsed switch
        {
            StartEnd.Start => StartEnd.End,
            StartEnd.End => StartEnd.Start,
            _ => Random.value > 0.5f ? StartEnd.End : StartEnd.Start,
        };

        switch (primaryPreference)
        {
            case StartEnd.Start when secondaryPreference == StartEnd.Start:
                TransformSectionStartToStart(primary, secondary);
                break;
            case StartEnd.Start when secondaryPreference == StartEnd.End:
                TransformSectionStartToEnd(primary, secondary);
                break;
            case StartEnd.End when secondaryPreference == StartEnd.Start:
                TransformSectionEndToStart(primary, secondary);
                break;
            case StartEnd.End when secondaryPreference == StartEnd.End:
                TransformSectionEndToEnd(primary, secondary);
                break;
        }
    }

    private TunnelSection PickSection(TunnelSection primary)
    {
        List<TunnelSection> nextSections = new(tunnelSections);

        if (primary.ExcludePrefabConnections.Count > 0)
        {
            primary.ExcludePrefabConnections.ForEach(item => nextSections.RemoveAll(element => element == item));
        }


        return InstinateSection(nextSections.ElementAt(Random.Range(0, nextSections.Count)));
    }

    private TunnelSection InstinateSection(int index)
    {
        return InstinateSection(tunnelSections.ElementAt(index));
    }

    private TunnelSection InstinateSection(TunnelSection tunnelSection)
    {
        TunnelSection section = Instantiate(tunnelSection);
        section.transform.parent = transform;
        return section;
    }

    private static void TransformSectionStartToEnd(TunnelSection primary, TunnelSection secondary)
    {
        Vector3 offset = secondary.EndPos.localPosition;
        Quaternion r = Quaternion.FromToRotation(secondary.EndPos.forward * -1, primary.StartPos.forward);
        offset = r * offset;

        Vector3 pos = primary.StartPos.position - offset;
        pos.y = primary.transform.position.y + (primary.StartPos.position.y - secondary.EndPos.position.y);

        secondary.transform.SetPositionAndRotation(pos, r);
        primary.LastUsed = StartEnd.Start;
        secondary.LastUsed = StartEnd.End;
    }

    private static void TransformSectionStartToStart(TunnelSection primary, TunnelSection secondary)
    {
        Vector3 offset = secondary.StartPos.localPosition;
        Quaternion r = Quaternion.FromToRotation(secondary.StartPos.forward *-1, primary.StartPos.forward);
        offset = r * offset;

        Vector3 pos = primary.StartPos.position - offset;
        pos.y = primary.transform.position.y + (primary.StartPos.position.y - secondary.StartPos.position.y);

        secondary.transform.SetPositionAndRotation(pos, r);
        primary.LastUsed = StartEnd.Start;
        secondary.LastUsed = StartEnd.Start;
    }

    private static void TransformSectionEndToEnd(TunnelSection primary, TunnelSection secondary)
    {
        Vector3 offset = secondary.EndPos.localPosition;
        Quaternion r = Quaternion.FromToRotation(secondary.EndPos.forward * -1, primary.EndPos.forward);
        offset = r*offset;

        Vector3 pos = primary.transform.position - offset;
        pos.y = primary.transform.position.y + (primary.EndPos.position.y - secondary.EndPos.position.y);

        secondary.transform.SetPositionAndRotation(pos, r);
        primary.LastUsed = StartEnd.End;
        secondary.LastUsed = StartEnd.End;
    }

    private static void TransformSectionEndToStart(TunnelSection primary, TunnelSection secondary)
    {
        Vector3 offset = secondary.StartPos.localPosition;
        Quaternion r = Quaternion.FromToRotation(secondary.StartPos.forward * -1, primary.EndPos.forward);
        offset =r*offset;

        Vector3 pos = primary.transform.position - offset;
        pos.y = primary.transform.position.y + (primary.EndPos.position.y - secondary.StartPos.position.y);

        secondary.transform.SetPositionAndRotation(pos, r);
        primary.LastUsed = StartEnd.End;
        secondary.LastUsed = StartEnd.Start;
    }
}
