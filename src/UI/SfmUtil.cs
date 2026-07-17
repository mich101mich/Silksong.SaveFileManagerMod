using UnityEngine;

namespace SaveFileManagerMod.UI;

public static class SfmUtil
{
    public static GameObject? GetChild(GameObject parent, string childPath)
    {
        var child = parent.transform.Find(childPath);
        if (child == null)
        {
            SfmLogger.LogError($"Could not find child '{childPath}' in '{parent.name}'");
            return null;
        }
        return child.gameObject;
    }

    public static T? GetChildComponent<T>(GameObject parent, string childPath) where T : class
    {
        var child = GetChild(parent, childPath);
        if (child == null)
        {
            return null;
        }
        var component = child.GetComponent<T>();
        if (component == null)
        {
            SfmLogger.LogError($"Could not find component '{typeof(T).Name}' in '{parent.name}/{childPath}'");
            return null;
        }
        return component;
    }

    public static void RemoveComponent<T>(GameObject gameObject) where T : Component
    {
        var component = gameObject.GetComponent<T>();
        if (component != null)
        {
            UnityEngine.Object.Destroy(component);
        }
    }

    public static void RemoveComponentImmediate<T>(GameObject gameObject) where T : Component
    {
        var component = gameObject.GetComponent<T>();
        if (component != null)
        {
            UnityEngine.Object.DestroyImmediate(component);
        }
    }

}
