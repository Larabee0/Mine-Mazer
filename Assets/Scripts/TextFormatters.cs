using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.CompilerServices;
using UnityEngine.UIElements;

public static class TextFormatter
{
    public static Color32 Red = new Color32(255, 51, 51, 255);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ColourText(string text, Color colour)
    {
        return string.Format("<color=#{0}>{1}</color>", ColorUtility.ToHtmlStringRGBA(colour), StripSpaces(text));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ColourText(string text, string colour)
    {
        return string.Format("<color={0}>{1}</color>", colour, StripSpaces(text));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string SizeText(string text, int size)
    {
        return string.Format("<size={0}>{1}</size>", size, StripSpaces(text));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string BreakText(string text)
    {
        return string.Format("<br>{0}</br>", StripSpaces(text));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string AlignText(string text, TextAlignment alignment)
    {
        string alignTag = alignment switch
        {
            TextAlignment.Left => "left",
            TextAlignment.Center => "center",
            TextAlignment.Right => "right",
            _ => "left",
        };
        return string.Format("<align={0}>{1}</align>", alignTag, StripSpaces(text));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string BoldText(string text)
    {
        return string.Format("<b>{0}</b>", StripSpaces(text));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ItalicText(string text)
    {
        return string.Format("<i>{0}</i>", StripSpaces(text));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string StripSpaces(string text)
    {
        text.TrimStart(' ');
        text.TrimEnd(' ');
        return text;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsInt(TextField field, out int value)
    {
        if (int.TryParse(field.text, out value))
        {
            ColourTextFieldText(field, Color.white);
            return true;
        }
        else
        {
            ColourTextFieldText(field, Red);
            return false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsFloat(TextField field, out float value)
    {
        if (float.TryParse(field.text, out value))
        {
            ColourTextFieldText(field, Color.white);
            return true;
        }
        else
        {
            ColourTextFieldText(field, Red);
            return false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ColourTextFieldText(TextField field, Color colour)
    {
        VisualElement element = field.Q<VisualElement>("unity-text-input");
        StyleColor textColor = element.style.color;
        textColor.value = colour;
        element.style.color = textColor;
        field.style.borderBottomColor = colour == Color.white ? StyleKeyword.Null : colour;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string BoolToYesNo(bool value)
    {
        return value ? "Yes" : "No";
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Truncate(this string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value.Substring(0, maxLength);
    }
}
