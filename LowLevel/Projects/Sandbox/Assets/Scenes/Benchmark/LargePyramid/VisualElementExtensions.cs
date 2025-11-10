using UnityEngine.UIElements;

public static class VisualElementExtensions
{
    public static void SetVisibleInHierarchy(this VisualElement element, bool value)
    {
        element.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
