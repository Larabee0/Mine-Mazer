using System;
using UnityEngine.UIElements;

public abstract class UIToolkitBase
{
    public VisualElement RootVisualElement;
    public Focusable focusOnOpen;
    public bool IsOpen
    {
        get
        {
            return RootVisualElement.style.display == DisplayStyle.Flex;
        }
        set
        {
            if (value == IsOpen) return;
            SetActive(value);
        }
    }

    /// <summary>
    /// Sets the root visual element.
    /// </summary>
    /// <param name="rootVisualElement"></param>
    public UIToolkitBase(VisualElement rootVisualElement)
    {
        RootVisualElement = rootVisualElement;
    }

    /// <summary>
    /// Where you should query <see cref="RootVisualElement"/> for visual elements needed to run the UI.
    /// You should usally call it in the constructor.
    /// Must be called before <see cref="Bind"/>
    /// </summary>
    public abstract void Query();

    /// <summary>
    /// Where you should register permant callbacks.
    /// You should usally call it in the constructor.
    /// Must be called after <see cref="Query"/>
    /// </summary>
    public abstract void Bind();

    /// <summary>
    /// By default sets display style of the root to Flex & subscribes <see cref="FocusOnOpen(GeometryChangedEvent)"/> event
    /// </summary>
    public virtual void Open()
    {
        RootVisualElement.RegisterCallback<GeometryChangedEvent>(FocusOnOpen);
        RootVisualElement.style.display = DisplayStyle.Flex;
    }

    /// <summary>
    /// By default sets display style of the root to none
    /// </summary>
    public virtual void Close()
    {
        RootVisualElement.style.display = DisplayStyle.None;
    }

    /// <summary>
    /// By default allows calling of Open/Close like <see cref="UnityEngine.GameObject.SetActive"/>
    /// By default this has no state check and will always call Open or Close. <see cref="IsOpen"/> has a state check.
    /// </summary>
    /// <param name="active"></param>
    public virtual void SetActive(bool active)
    {
        if (active)
        {
            Open();
        }
        else
        {
            Close();
        }
    }

    /// <summary>
    /// Invoked by the layout engine after all repaints completed useful for focusing a specific element.
    /// To use for that, set focusOnOpen to any <see cref="Focusable"/> element that will be <see cref="DisplayStyle.Flex">
    /// Override to extend functionality.
    /// </summary>
    /// <param name="evt"></param>
    protected virtual void FocusOnOpen(GeometryChangedEvent evt)
    {
        RootVisualElement.UnregisterCallback<GeometryChangedEvent>(FocusOnOpen);
        focusOnOpen?.Focus();
    }

    /// <summary>
    /// Binds ClickEvent & NavigationSubmitEvent of the given button to the given action
    /// </summary>
    /// <param name="button"></param>
    /// <param name="action"></param>
    public void DoubleBindButton(Button button, Action action)
    {
        button.RegisterCallback<ClickEvent>(ev=>action?.Invoke());
        button.RegisterCallback<NavigationSubmitEvent>(ev=> action?.Invoke());
    }

    /// <summary>
    /// Query <see cref="RootVisualElement"/>
    /// Same as VisualElement.Q<T>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="name"></param>
    /// <param name="className"></param>
    /// <returns></returns>
    public T RootQ<T>(string name = null, string className = null) where T : VisualElement
    {
        return RootVisualElement.Q<T>(name, className);
    }

    /// <summary>
    /// Query <see cref="RootVisualElement"/>
    /// Same as VisualElement.Q<T>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="name"></param>
    /// <param name="classes"></param>
    /// <returns></returns>
    public T RootQ<T>(string name = null, params string[] classes) where T : VisualElement
    {
        return RootVisualElement.Q<T>(name, classes);
    }

    /// <summary>
    /// Query <see cref="RootVisualElement"/>
    /// Same as VisualElement.Q
    /// </summary>
    /// <param name="name"></param>
    /// <param name="className"></param>
    /// <returns></returns>
    public VisualElement RootQ(string name = null, string className = null)
    {
        return RootVisualElement.Q(name, className);
    }

    /// <summary>
    /// Query <see cref="RootVisualElement"/>
    /// Same as VisualElement.Q
    /// </summary>
    /// <param name="name"></param>
    /// <param name="classes"></param>
    /// <returns></returns>
    public VisualElement RootQ(string name = null, params string[] classes)
    {
        return RootVisualElement.Q(name, classes);
    }
}
