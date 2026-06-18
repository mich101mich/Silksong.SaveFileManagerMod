using UnityEngine;

namespace SaveFileManagerMod.UI;

public sealed class SaveOptionsUpdater : MonoBehaviour
{
    private SaveOptions? m_owner;

    public void Initialize(SaveOptions owner)
    {
        m_owner = owner;
    }

    public void ClearOwner()
    {
        m_owner = null;
    }

    private void Update()
    {
        m_owner?.Tick();
    }
}
