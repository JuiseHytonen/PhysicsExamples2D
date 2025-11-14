using UnityEngine.UIElements;

public static class VisualElementExtensions
{
    public static void SetVisibleInHierarchy(this VisualElement element, bool value)
    {
        element.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
    }

    public static void ShowForMilliSeconds(this VisualElement element, int milliSeconds)
    {
        element.SetVisibleInHierarchy(true);
        element.schedule.Execute(evt => element.SetVisibleInHierarchy(false)).ExecuteLater(milliSeconds);
    }
}
