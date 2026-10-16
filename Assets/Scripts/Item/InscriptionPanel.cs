using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Three-slot inscription panel. Flow: pick an equipment target from the list, then pick which
/// of its three slots to write. When all three are taken the same slot buttons act as the
/// overwrite chooser, so there is no second dialog to keep in sync.
///
/// All rules live in <see cref="InscriptionService"/>; this class only gathers the player's
/// choice and renders state.
/// </summary>
public class InscriptionPanel : MonoBehaviour
{
    public static InscriptionPanel Instance;

    [Header("Root")]
    [SerializeField] private GameObject window;

    [Header("Inscription being applied")]
    [SerializeField] private TMP_Text inscriptionName;
    [SerializeField] private TMP_Text inscriptionEffect;

    [Header("Target list")]
    [SerializeField] private Transform targetList;
    [SerializeField] private Button targetButtonTemplate;

    [Header("Slots of the selected target")]
    [SerializeField] private GameObject slotSection;
    [SerializeField] private TMP_Text slotPrompt;
    [SerializeField] private Button[] slotButtons = new Button[EquipmentInstance.MaxInscriptions];
    [SerializeField] private TMP_Text[] slotLabels = new TMP_Text[EquipmentInstance.MaxInscriptions];

    [Header("Buttons")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button backButton;

    private ItemDefinition pendingInscription;
    private EquipmentInstance selectedTarget;
    private readonly List<GameObject> spawnedTargetButtons = new List<GameObject>();
    private Controls playerControls;

    private void Awake()
    {
        Instance = this;
        if (window != null)
        {
            window.SetActive(false);
        }
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Close);
        }
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(ShowTargetList);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>Open the panel for an inscription and the equipment it may go on.</summary>
    public void Open(ItemDefinition inscription, List<EquipmentInstance> targets)
    {
        pendingInscription = inscription;
        selectedTarget = null;

        if (inscriptionName != null)
        {
            inscriptionName.text = inscription.DisplayName;
        }
        if (inscriptionEffect != null)
        {
            inscriptionEffect.text = inscription.InscriptionDescription;
        }

        if (window != null)
        {
            window.SetActive(true);
        }
        BuildTargetList(targets);
        SetPlayerMenuOpen(true);
    }

    private void SetPlayerMenuOpen(bool open)
    {
        if (playerControls == null && PlayerStats.Instance != null)
        {
            playerControls = PlayerStats.Instance.GetComponent<Controls>();
        }
        if (playerControls != null)
        {
            playerControls.MenuIsOpen = open;
        }
        else
        {
            Debug.LogWarning("InscriptionPanel: could not find Controls; the player is not locked " +
                             "while the panel is open.");
        }
    }

    private void BuildTargetList(List<EquipmentInstance> targets)
    {
        ClearTargetList();
        ShowTargetList();

        if (targetList == null || targetButtonTemplate == null)
        {
            Debug.LogError("InscriptionPanel: target list or template is not wired.");
            return;
        }

        foreach (EquipmentInstance target in targets)
        {
            Button button = Instantiate(targetButtonTemplate, targetList);
            button.gameObject.SetActive(true);

            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                int used = target.Inscriptions.Count;
                label.text = $"{target.DisplayName}  ({used}/{EquipmentInstance.MaxInscriptions})";
            }

            // Capture a local copy: closing over the loop variable would bind every button to
            // the last target.
            EquipmentInstance captured = target;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => ShowSlotsFor(captured));
            spawnedTargetButtons.Add(button.gameObject);
        }
    }

    private void ClearTargetList()
    {
        foreach (GameObject button in spawnedTargetButtons)
        {
            if (button != null)
            {
                Destroy(button);
            }
        }
        spawnedTargetButtons.Clear();

        if (targetList != null && targetButtonTemplate != null)
        {
            targetButtonTemplate.gameObject.SetActive(false);
        }
    }

    private void ShowTargetList()
    {
        if (targetList != null)
        {
            targetList.gameObject.SetActive(true);
        }
        if (slotSection != null)
        {
            slotSection.SetActive(false);
        }
    }

    private void ShowSlotsFor(EquipmentInstance target)
    {
        selectedTarget = target;

        if (targetList != null)
        {
            targetList.gameObject.SetActive(false);
        }
        if (slotSection != null)
        {
            slotSection.SetActive(true);
        }

        int free = InscriptionService.NextFreeSlot(target);
        if (slotPrompt != null)
        {
            slotPrompt.text = free >= 0
                ? $"{target.DisplayName}: choose a slot"
                : $"{target.DisplayName} is full ({EquipmentInstance.MaxInscriptions}/{EquipmentInstance.MaxInscriptions}) " +
                  "-- choose a slot to overwrite";
        }

        for (int i = 0; i < slotButtons.Length; i++)
        {
            int slot = i;
            Button button = slotButtons[i];

            if (slotLabels != null && i < slotLabels.Length && slotLabels[i] != null)
            {
                ItemDefinition existing = target.GetInscriptionAt(slot);
                slotLabels[i].text = existing != null
                    ? $"{slot + 1}. {existing.DisplayName}"
                    : $"{slot + 1}. (empty)";
            }

            if (button == null)
            {
                continue;
            }

            bool canTake = InscriptionService.CanApply(pendingInscription, target, out _);
            button.interactable = canTake;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => Commit(slot));
        }
    }

    private void Commit(int slot)
    {
        if (selectedTarget == null)
        {
            return;
        }

        if (InscriptionService.Apply(pendingInscription, selectedTarget, slot, Inventory.Instance))
        {
            Close();
        }
        else if (slotPrompt != null)
        {
            slotPrompt.text = "That inscription could not be applied.";
        }
    }

    public void Close()
    {
        ClearTargetList();
        pendingInscription = null;
        selectedTarget = null;

        if (window != null)
        {
            window.SetActive(false);
        }
        SetPlayerMenuOpen(false);
    }
}
