using UnityEngine;
using UnityEngine.UI;

namespace SaveFileManagerMod.UI;

public class SaveOptions : MonoBehaviour
{
    public SaveSlotButton m_saveSlotButton = null!;

    public SaveSlotActionRow m_actionRow = null!;
    public ArchiveSlotSelectionMenu m_archiveMenu = null!;
    public RenameEditor m_renameEditor = null!;

    public Text m_nameLabel = null!;

    public void Initialize(SaveSlotButton button)
    {
        m_saveSlotButton = button;
        m_archiveMenu = SaveFileManagerPlugin.s_instance.m_archiveMenu;

        SfmLogger.LogInfo($"Creating SaveOptions for {button.name}");

        m_actionRow = new SaveSlotActionRow(m_saveSlotButton, OpenRenameEditor, OpenArchiveMenu);

        m_renameEditor = new RenameEditor(m_saveSlotButton, OnRenameEditorClosed, (value) => m_nameLabel.text = value);

        m_nameLabel = CreateNameLabel();
    }

    public void OnDestroy()
    {
        SfmLogger.LogInfo($"Destroying SaveOptions for {m_saveSlotButton.name}");

        m_actionRow.Dispose();

        if (m_nameLabel != null)
        {
            UnityEngine.Object.Destroy(m_nameLabel.gameObject);
        }
    }

    public void SetupNavigation(SaveOptions? previousOptions, SaveOptions? nextOptions)
    {
        m_actionRow.SetupNavigation(previousOptions?.m_actionRow, nextOptions?.m_actionRow);
    }

    public void Update()
    {
        SyncFromSlotState();
    }

    public void SyncFromSlotState()
    {
        if (m_renameEditor.IsOpen)
        {
            m_renameEditor.Refresh();
        }
        else
        {
            m_actionRow.Refresh();

            string name;
            bool hasName = SaveName.TryGetSlotName(m_saveSlotButton.SaveSlotIndex, out name);
            m_nameLabel.text = name;
            m_nameLabel.gameObject.SetActive(hasName);
        }
    }

    public void OpenRenameEditor()
    {
        if (m_renameEditor.IsOpen || m_archiveMenu.IsOpen)
        {
            return;
        }

        m_actionRow.SetButtonVisibility(visible: false);
        // m_nameLabel.gameObject.SetActive(false);

        m_renameEditor.Open();
    }

    public void OnRenameEditorClosed()
    {
        m_actionRow.SetButtonVisibility(visible: true, focusedElement: m_actionRow.m_rename);
        SyncFromSlotState();
    }

    public void OpenArchiveMenu()
    {
        if (m_renameEditor.IsOpen || m_archiveMenu.IsOpen)
        {
            return;
        }

        m_archiveMenu.OpenForSlot(m_saveSlotButton);
    }

    public Text CreateNameLabel()
    {
        Text label = UnityEngine.Object.Instantiate(m_saveSlotButton.locationText, m_saveSlotButton.locationText.transform.parent);
        label.name = "SFM-SaveNameLabel";
        label.fontSize = Mathf.Max(16, m_saveSlotButton.locationText.fontSize - 6);
        label.color = new Color(1f, 0.86f, 0.58f, 1f);
        label.raycastTarget = false;
        label.rectTransform.anchoredPosition = m_saveSlotButton.locationText.rectTransform.anchoredPosition + new Vector2(0f, 58f);
        label.gameObject.SetActive(value: false);

        // TODO: Have this also be displayed on "Defeated" files

        return label;
    }
}
