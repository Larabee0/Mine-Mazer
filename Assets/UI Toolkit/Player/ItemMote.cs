using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ItemMote : UIToolkitBase
{
    private VisualElement icon;
    private Label moteTitle;
    private Label moteDescription;

    public Texture2D Icon
    {
        get => icon.style.backgroundImage.value.texture;
        set => icon.style.backgroundImage = value;
    }

    public string Title
    {
        get => moteTitle.text;
        set => moteTitle.text = value;
    }

    public string Description
    {
        get => moteDescription.text;
        set => moteDescription.text = value;
    }

    public ItemMote(VisualElement rootVisualElement) : base(rootVisualElement)
    {
        Query();
    }

    public override void Query()
    {
        icon = RootQ("Icon");
        moteTitle = RootQ<Label>("MoteTitle");
        moteDescription = RootQ<Label>("MoteDescription");
    }

    public override void Bind()
    {
    }

}
