using UnityEngine.EventSystems;

public class CheckGameplayInteractionPolicy
{
    public bool IsAllowed()
    {
        return !(EventSystem.current != null && EventSystem.current.IsPointerOverGameObject());
    }
}