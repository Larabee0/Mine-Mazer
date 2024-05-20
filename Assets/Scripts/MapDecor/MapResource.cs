using MazeGame.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Flags]
public enum Item : int
{
    None = 0,
    LumenCrystal = 1,
    GoldenTrumpetMycelium =2,
    VelvetBud = 4,
    Versicolor = 8,
    Antarticite = 16,
    Cinnabite = 32,
    Torch = 64,
    Pickaxe = 128,
    Eudie = 256,
    StagnationBeacon = 512,
    BrokenHeart = 1024,
    Soup = 2048,
    FicusWood = 4096,
    ClockworkMechanism = 8192,
    GlanceiteResonator = 16384,
    HeartNode = 32768,
    SanctumMachine = 65536
}

public enum ItemCategory
{
    Crystal,
    Mushroom,
    Equippment,
    Lumenite,
    Wood,
    Quest
}

[Serializable]
public class ItemStats
{
    public string name;
    public Item type;
}

public class MapResource : MonoBehaviour, IInteractable, IHover
{
    [SerializeField] protected Collider itemCollider;
    [SerializeField] protected ItemStats itemStats;
    [SerializeField] protected bool Placeable;
    [SerializeField] protected bool Interactable = true;
    [SerializeField] protected bool requiresPickaxe = false;
    [SerializeField] protected float spawnRarity = 50;
    public bool useIdleA = true;
    [SerializeField] protected MeshRenderer[] meshRenderers;
    [SerializeField] protected Color onSelectColour = Color.yellow;
    public Texture2D icon;
    public Vector3 heldpositonOffset;
    public Vector3 heldOrenintationOffset;
    public Vector3 heldScaleOffset = Vector3.one;
    protected Vector3 originalScale;
    public Vector3 placementPositionOffset = Vector3.zero;
    public Action OnItemPickedUp;
    public Action OnInventoryItemInteract;
    [SerializeField,Tooltip("If left blank, falls back to ItemStats.name")] protected string toolTipNameOverride;
    protected bool pickedUp = false;
    public float Rarity => spawnRarity;
    public bool PickedUp => pickedUp;
    public bool PlaceableItem => Placeable;
    protected virtual string ToolTipName
    {
        get
        {
            if(string.IsNullOrEmpty( toolTipNameOverride)|| string.IsNullOrWhiteSpace(toolTipNameOverride))
            {
                return itemStats.name;
            }
            return toolTipNameOverride;
        }
    }

    public ItemStats ItemStats => itemStats;

    public void ForceInit()
    {
        Awake();
        Start();
    }

    protected virtual void Awake()
    {
        originalScale = transform.localScale;
        SetColliderActive(Interactable);
        meshRenderers = GetComponentsInChildren<MeshRenderer>(true);
#if UNITY_EDITOR
        if (Interactable && Application.isPlaying)
        {
            SetOutlineFader(true);
        }
#else
        if (Interactable)
        {
            SetOutlineFader(true);
        }
#endif
    }


    protected virtual void Start()
    {
    }

    public void SetRainbowOpacity(float opacity)
    {
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            MeshRenderer renderer = meshRenderers[i];
            List<Material> materials = new();
            renderer.GetMaterials(materials);
            materials.ForEach(mat => mat.SetFloat("_Overlay_Opacity", opacity));
        }
    }

    public void SetOutlineColour(Color colour)
    {
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            MeshRenderer renderer = meshRenderers[i];
            List<Material> materials = new();
            renderer.GetMaterials(materials);
            materials.ForEach(mat => mat.SetColor("_OutlineColour", colour));
        }
    }

    public void SetOutlineFader(bool fading)
    {
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            MeshRenderer renderer = meshRenderers[i];
            List<Material> materials = new();
            renderer.GetMaterials(materials);
            materials.ForEach(mat => mat.SetInt("_OutlineFading", fading ? 1 : 0));
        }
    }

    public virtual string GetToolTipText()
    {
        if(requiresPickaxe)
        {
            if (Inventory.Instance.CurHeldItem == Item.Pickaxe)
            {
                if (InputManager.GamePadPresent)
                {
                    return string.Format("RT to  Mine {0}", ToolTipName);
                }
                else
                {
                    return string.Format("Left Click to Mine {0}", ToolTipName);
                }
            }
            else
            {
                return string.Format("Select Pickaxe to Mine {0}", ToolTipName);
            }
        }
        else if (Placeable && pickedUp)
        {
            if(InputManager.GamePadPresent)
            {
                return string.Format("LT to place {0}", ToolTipName);
            }
            else
            {
                return string.Format("Right Click to place {0}", ToolTipName);
            }
        }
        else
        {
            if (InputManager.GamePadPresent)
            {
                return string.Format("LT to  Pick Up {0}", ToolTipName);
            }
            else
            {
                return string.Format("Right Click to Pick Up {0}", ToolTipName);
            }
        }
    }

    public virtual void InventoryInteract()
    {
        OnInventoryItemInteract?.Invoke();
    }

    public virtual void Interact()
    {
        if(Inventory.Instance== null)
        {
            return;
        }
        if (TryGetComponent(out Rigidbody body))
        {
            body.isKinematic = true;
        }
        OnItemPickedUp?.Invoke();
        pickedUp = true;
        Inventory.Instance.AddItem(itemStats.type, 1, this, !RequiresPickaxe());
    }

    public virtual void SetColliderActive(bool active)
    {
        if(itemCollider != null)
        {
            itemCollider.enabled = active;
        }
    }

    public virtual void SetMapResourceActive(bool active)
    {
        gameObject.SetActive(active);
    }

    public virtual bool PlaceItem()
    {
        if (Placeable)
        {
            Ray r = new(Camera.main.transform.position, Camera.main.transform.forward);
            if (Physics.Raycast(r, out RaycastHit hitInfo, NPC_Interact.Instance.InteractRange))
            {
                if (Inventory.Instance.TryRemoveItem(ItemStats.type, 1, out MapResource item))
                {
                    Vector3 playerPos = Inventory.Instance.transform.position;
                    Vector3 toPlayer = (playerPos- hitInfo.point).normalized;
                    toPlayer = Vector3.ProjectOnPlane(toPlayer, Vector3.up);
                    item.gameObject.transform.parent = FindObjectOfType<SpatialParadoxGenerator>().CurPlayerSection.sectionInstance.transform;
                    item.gameObject.transform.position = hitInfo.point+ placementPositionOffset;
                    item.gameObject.transform.forward = toPlayer;
                    item.gameObject.transform.localScale = originalScale;
                    item.SetMapResourceActive(true);
                    item.SetColliderActive(true);
                    OnItemPickedUp?.Invoke();
                    pickedUp = false;
                    //item.gameObject.transform.up = hitInfo.normal;
                    return true;
                }
            }
        }
        return false;
    }

    public virtual void SetRequiresPickaxe(bool requiresPickaxe)
    {
        this.requiresPickaxe = requiresPickaxe;
    }

    public virtual bool RequiresPickaxe()
    {
        return requiresPickaxe;
    }

    public virtual void HoverOn()
    {
        SetOutlineColour(onSelectColour);
        SetOutlineFader(false);
    }

    public virtual void HoverOff()
    {
        SetOutlineColour(Color.black);
        SetOutlineFader(true);
    }
}
