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
    public Text m_defeatedNameLabel = null!;

    public void Initialize(SaveSlotButton button)
    {
        m_saveSlotButton = button;
        m_archiveMenu = SaveFileManagerPlugin.s_instance.m_archiveMenu;

        SfmLogger.LogInfo($"Creating SaveOptions for {button.name}");

        m_actionRow = new SaveSlotActionRow(m_saveSlotButton, OpenRenameEditor, OpenArchiveMenu);

        m_renameEditor = new RenameEditor(m_saveSlotButton, OnRenameEditorClosed, (value) =>
        {
            m_nameLabel.text = value;
            m_defeatedNameLabel.text = value;
        });

        CreateNameLabels();
    }

    public void OnDestroy()
    {
        SfmLogger.LogInfo($"Destroying SaveOptions for {m_saveSlotButton.name}");

        m_actionRow.Dispose();
        m_renameEditor.Dispose();

        UnityEngine.Object.Destroy(m_nameLabel.gameObject);
        UnityEngine.Object.Destroy(m_defeatedNameLabel.gameObject);
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
            m_defeatedNameLabel.text = name;
            m_defeatedNameLabel.gameObject.SetActive(hasName);
        }
    }

    public void OpenRenameEditor()
    {
        if (m_renameEditor.IsOpen || m_archiveMenu.IsOpen)
        {
            return;
        }

        m_actionRow.SetButtonVisibility(visible: false);

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

    public void CreateNameLabels()
    {
        // Normal saves: Display below the location text
        m_nameLabel = UnityEngine.Object.Instantiate(m_saveSlotButton.locationText, m_saveSlotButton.locationText.transform.parent);
        m_nameLabel.name = "SFM-SaveNameLabel";
        m_nameLabel.fontSize = Mathf.Max(16, m_saveSlotButton.locationText.fontSize - 6);
        m_nameLabel.color = new Color(1f, 0.86f, 0.58f, 1f);
        m_nameLabel.raycastTarget = false;
        m_nameLabel.gameObject.SetActive(value: false);

        // Defeated saves: Anchor bottom-right on the defeated background.
        // locationText is disabled for defeated saves, while defeatedBackground is only enabled for defeated saves
        // => The game automatically only shows one of the two.
        m_defeatedNameLabel = UnityEngine.Object.Instantiate(m_nameLabel, m_saveSlotButton.defeatedBackground.transform);
        m_defeatedNameLabel.name = "SFM-SaveNameLabel-Defeated";
        m_defeatedNameLabel.rectTransform.anchorMin = new Vector2(1f, 0f);
        m_defeatedNameLabel.rectTransform.anchorMax = new Vector2(1f, 0f);
        m_defeatedNameLabel.rectTransform.anchoredPosition = new Vector2(-30f, 53f);
        m_defeatedNameLabel.rectTransform.pivot = new Vector2(1f, 0f);
        m_defeatedNameLabel.alignment = TextAnchor.LowerRight;
    }
}
