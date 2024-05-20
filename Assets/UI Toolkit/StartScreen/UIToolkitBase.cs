using MazeGame.Input;
using System;
using UnityEngine.UIElements;

public abstract class UIToolkitBase
{
    public VisualElement RootVisualElement;

    public bool Open => RootVisualElement.style.display == DisplayStyle.Flex;

    public UIToolkitBase(VisualElement rootVisualElement)
    {
        RootVisualElement = rootVisualElement;
    }

    public abstract void Bind();
    public abstract void Query();

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

