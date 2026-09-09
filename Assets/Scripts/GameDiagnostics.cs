using UnityEngine;

public static class GameDiagnostics
{
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void Log(object message, Object context = null) => Debug.Log(message, context);
}
