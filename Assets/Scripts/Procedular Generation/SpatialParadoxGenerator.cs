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

    [SerializeField, Min(1000)] private int maxInterations = 1000000000;

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
        curPlayerSection.transform.position = new Vector3(0, 0, 0);

        nextPlayerSection = PickSection(curPlayerSection, out StartEnd priPref, out StartEnd secPref);
        TransformSection(curPlayerSection, nextPlayerSection, priPref, secPref);

        nextNextPlayerSection = PickSection(nextPlayerSection, out priPref, out secPref);
        TransformSection(nextPlayerSection, nextNextPlayerSection, priPref, secPref);
        
        prevPlayerSection = PickSection(curPlayerSection, out priPref, out secPref);
        TransformSection(curPlayerSection, prevPlayerSection, priPref, secPref);
        
        prevPrevPlayerSection = PickSection(prevPlayerSection, out priPref, out secPref);
        TransformSection(prevPlayerSection, prevPrevPlayerSection, priPref, secPref);
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
                nextNextPlayerSection = PickSection(nextPlayerSection,out StartEnd priPref, out StartEnd secPref);
                TransformSection(nextPlayerSection, nextNextPlayerSection, priPref, secPref);
            }
            else if (lastEnter == prevPlayerSection)
            {
                Destroy(nextNextPlayerSection.gameObject);
                nextNextPlayerSection = null;

                nextNextPlayerSection = nextPlayerSection;
                nextPlayerSection = curPlayerSection;
                curPlayerSection = lastEnter;
                prevPlayerSection = prevPrevPlayerSection;
                prevPrevPlayerSection = PickSection(prevPlayerSection, out StartEnd priPref, out StartEnd secPref);
                TransformSection(prevPlayerSection, prevPrevPlayerSection, priPref, secPref);
            }
        }
        lastEnter = null;
        lastExit = null;
    }

    private void TransformSection(TunnelSection primary, TunnelSection secondary, StartEnd primaryPreference, StartEnd secondaryPreference)
    {
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

    private TunnelSection PickSection(TunnelSection primary, out StartEnd primaryPreference, out StartEnd secondaryPreference)
    {
        primaryPreference = StartEnd.Unused;
        secondaryPreference = StartEnd.Unused;
        List<TunnelSection> nextSections = new(tunnelSections);

        if (primary.ExcludePrefabConnections.Count > 0)
        {
            primary.ExcludePrefabConnections.ForEach(item => nextSections.RemoveAll(element => element == item));
        }
        int iterations = maxInterations;
        TunnelSection targetSection = null;
        while (targetSection == null)
        {
            targetSection = nextSections.ElementAt(Random.Range(0, nextSections.Count));
            if (IntersectionTest(primary,targetSection, out primaryPreference, out secondaryPreference))
            {
                break;
            }
            iterations--;
            if (iterations <= 0)
            {
                throw new System.StackOverflowException("Failed to find section that passed Intersection Test");
            }
        }

        return InstinateSection(nextSections.ElementAt(Random.Range(0, nextSections.Count)));
    }

    private bool IntersectionTest(TunnelSection primary, TunnelSection target, out StartEnd primaryPreference, out StartEnd secondaryPreference)
    {
        GetPreference(primary, target,out primaryPreference,out secondaryPreference);


        Vector3 pos = Vector3.zero;
        Quaternion rot = Quaternion.identity;

        switch (primaryPreference)
        {
            case StartEnd.Start when secondaryPreference == StartEnd.Start:
                CalculateSectionStartToStart(primary, target, out pos, out rot);
                break;
            case StartEnd.Start when secondaryPreference == StartEnd.End:
                CalculateSectionStartToEnd(primary, target, out pos, out rot);
                break;
            case StartEnd.End when secondaryPreference == StartEnd.Start:
                CalculateSectionEndToStart(primary, target, out pos, out rot);
                break;
            case StartEnd.End when secondaryPreference == StartEnd.End:
                CalculateSectionEndToEnd(primary, target, out pos, out rot);
                break;
        }

        for (int i = 0; i < target.BoundingBoxes.Length; i++)
        {
            BoxBounds boxBounds = target.BoundingBoxes[i];
            if (Physics.CheckBox(pos + boxBounds.center, boxBounds.size, rot))
            {
                return false;
            }
        }

        for (int i = 0; i < target.BoundingCaps.Length; i++)
        {
            CapsuleBounds capBounds = target.BoundingCaps[i];
            if (Physics.CheckCapsule(pos + capBounds.center, pos, capBounds.radius))
            {
                return false;
            }
        }

        return true;
    }

    private void GetPreference(TunnelSection primary, TunnelSection secondary, out StartEnd primaryPreference, out StartEnd secondaryPreference)
    {
        primaryPreference = primary.LastUsed switch
        {
            StartEnd.Start => StartEnd.End,
            StartEnd.End => StartEnd.Start,
            _ => Random.value < 0.5f ? StartEnd.End : StartEnd.Start,
        };
        secondaryPreference = secondary.LastUsed switch
        {
            StartEnd.Start => StartEnd.End,
            StartEnd.End => StartEnd.Start,
            _ => Random.value > 0.5f ? StartEnd.End : StartEnd.Start,
        };


    }

    private TunnelSection InstinateSection(int index)
    {
        return InstinateSection(tunnelSections.ElementAt(index));
    }

    private TunnelSection InstinateSection(TunnelSection tunnelSection)
    {
        TunnelSection section = Instantiate(tunnelSection);
        section.gameObject.SetActive(true);
        section.transform.parent = transform;
        return section;
    }

    private static void TransformSectionStartToEnd(TunnelSection primary, TunnelSection secondary)
    {
        CalculateSectionStartToEnd(primary, secondary,out Vector3 pos, out Quaternion rot);
        secondary.transform.SetPositionAndRotation(pos, rot);
        primary.LastUsed = StartEnd.Start;
        secondary.LastUsed = StartEnd.End;
    }

    private static void CalculateSectionStartToEnd(TunnelSection primary, TunnelSection secondary, out  Vector3 position, out Quaternion rotation)
    {
        Connector priStart = primary.startConnector;
        Connector secEnd = secondary.endConnector;
        Vector3 priStartConnPos = primary.GetConnectorWorldPos(priStart, out Quaternion priStartConnRot);
        Vector3 secEndConnPos = secondary.GetConnectorWorldPos(secEnd, out Quaternion secEndConnRot);

        Vector3 offset = secEnd.localPosition;
        rotation = ExtraUtilities.BetweenDirections(secEndConnRot * Vector3.back, priStartConnRot * Vector3.forward);
        offset = rotation.RotatePosition(offset);

        position = priStartConnPos - offset;
        position.y = primary.transform.position.y + (priStartConnPos.y - secEndConnPos.y);
    }


    private static void TransformSectionStartToStart(TunnelSection primary, TunnelSection secondary)
    {
        CalculateSectionStartToStart(primary, secondary, out Vector3 pos, out Quaternion rot);
        secondary.transform.SetPositionAndRotation(pos, rot);
        primary.LastUsed = StartEnd.Start;
        secondary.LastUsed = StartEnd.Start;
    }
    
    private static void CalculateSectionStartToStart(TunnelSection primary, TunnelSection secondary, out Vector3 position, out Quaternion rotation)
    {
        Connector priStart = primary.startConnector;
        Connector secStart = secondary.startConnector;
        Vector3 priStartConnPos = primary.GetConnectorWorldPos(priStart, out Quaternion priStartConnRot);
        Vector3 secStartConnPos = secondary.GetConnectorWorldPos(secStart, out Quaternion secStartConnRot);

        Vector3 offset = secStart.localPosition;
        rotation = ExtraUtilities.BetweenDirections(secStartConnRot * Vector3.back, priStartConnRot * Vector3.forward);
        offset = rotation.RotatePosition(offset);


        position = priStartConnPos - offset;
        position.y = primary.transform.position.y + (priStartConnPos.y - secStartConnPos.y);
    }

    private static void TransformSectionEndToEnd(TunnelSection primary, TunnelSection secondary)
    {
        CalculateSectionEndToEnd(primary, secondary, out Vector3 pos,out Quaternion rot);
        secondary.transform.SetPositionAndRotation(pos, rot);
        primary.LastUsed = StartEnd.End;
        secondary.LastUsed = StartEnd.End;
    }

    private static void CalculateSectionEndToEnd(TunnelSection primary, TunnelSection secondary, out Vector3 position, out Quaternion rotation)
    {
        Connector priEnd = primary.endConnector;
        Connector secEnd = secondary.endConnector;
        Vector3 priEndConnPos = primary.GetConnectorWorldPos(priEnd, out Quaternion priEndConnRot);
        Vector3 secEndConnPos = secondary.GetConnectorWorldPos(secEnd, out Quaternion secEndConnRot);

        Vector3 offset = secEnd.localPosition;
        rotation = ExtraUtilities.BetweenDirections(secEndConnRot * Vector3.back, priEndConnRot * Vector3.forward);
        offset = rotation.RotatePosition(offset);

        position = priEndConnPos - offset;
        position.y = primary.transform.position.y + (priEndConnPos.y - secEndConnPos.y);
    }

    private static void TransformSectionEndToStart(TunnelSection primary, TunnelSection secondary)
    {
        CalculateSectionEndToStart(primary, secondary, out Vector3 pos,out Quaternion rot);
        secondary.transform.SetPositionAndRotation(pos, rot);
        primary.LastUsed = StartEnd.End;
        secondary.LastUsed = StartEnd.Start;
    }
    
    private static void CalculateSectionEndToStart(TunnelSection primary, TunnelSection secondary, out Vector3 position, out Quaternion rotation)
    {
        Connector priEnd = primary.endConnector;
        Connector secStart = secondary.startConnector;
        Vector3 priEndConnPos = primary.GetConnectorWorldPos(priEnd, out Quaternion priEndConnRot);
        Vector3 secStartConnPos = secondary.GetConnectorWorldPos(secStart, out Quaternion secStartConnRot);

        Vector3 offset = secStart.localPosition;
        rotation = ExtraUtilities.BetweenDirections(secStartConnRot * Vector3.back, priEndConnRot * Vector3.forward);
        offset = rotation.RotatePosition(offset);

        position = priEndConnPos - offset;
        position.y = primary.transform.position.y + (priEndConnPos.y - secStartConnPos.y);
    }
}
