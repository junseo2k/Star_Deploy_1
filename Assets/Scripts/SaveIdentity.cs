using UnityEngine;

// 표시 이름을 바꿔도 유지되는 에셋 식별자. 빌드에서는 직렬화된 ID만 사용한다.
public abstract class SaveIdentity : ScriptableObject
{
    [SerializeField, HideInInspector] private string saveId;
    public string SaveId => saveId;

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        string path = UnityEditor.AssetDatabase.GetAssetPath(this);
        if (!string.IsNullOrEmpty(path))
            saveId = UnityEditor.AssetDatabase.AssetPathToGUID(path);
    }
#endif
}
