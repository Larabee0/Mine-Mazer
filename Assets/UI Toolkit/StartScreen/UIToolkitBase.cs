using MazeGame.Input;
using UnityEngine.UIElements;

public abstract class UIToolkitBase
{
    public VisualElement RootVisualElement;

    public UIToolkitBase(VisualElement rootVisualElement)
    {
        RootVisualElement = rootVisualElement;
    }

    public abstract void Bind();
    public abstract void Query();

    public void DoubleBindButton(Button button, Pluse action)
    {
        button.RegisterCallback<ClickEvent>(ev=>action?.Invoke());
        button.RegisterCallback<NavigationSubmitEvent>(ev=> action?.Invoke());
    }
}

