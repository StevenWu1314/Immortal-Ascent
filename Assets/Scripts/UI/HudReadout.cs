using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The HUD's text and icon readouts (T08, GDD 14.1): ammo, weapon, whose turn it is, realm and floor, and the
/// Root status. Every field is optional and every value is written only when it changes, so this costs
/// nothing per frame and a half-wired HUD still runs.
/// </summary>
public class HudReadout : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private TMP_Text turnText;
    [SerializeField] private TMP_Text realmText;

    [Header("Status")]
    [SerializeField] private GameObject rootIcon;
    [SerializeField] private TMP_Text rootText;

    [Header("Colours")]

    private int shownArrows = -1;
    private string shownWeapon;
    private string shownTurn;
    private string shownRealm;
    private bool shownRooted;

    [Tooltip("The HUD root to hide while a panel is open. Assigned by Tools/Content/Wire HUD References.")]
    [SerializeField] private GameObject hudRoot;

    /// <summary>
    /// Lay the rows out down the bottom-left corner, and cache their shared parent so the whole readout can be
    /// hidden in one go. Done in code because the scene references cannot be rearranged from script: the rows
    /// were spread along one bottom strip with fixed widths, so "Melee: Rusty Sword / Bow: Wooden Bow" wrapped
    /// onto two lines and pushed into the ammo count and the existing position readout.
    /// </summary>
    private void Awake()
    {

        if (hudRoot == null)
        {
            Transform parent = realmText != null ? realmText.transform.parent : (turnText != null ? turnText.transform.parent : null);
            hudRoot = parent != null ? parent.gameObject : null;
        }
    }


    private void Update()
    {
        PlayerStats player = PlayerStats.Instance;
        Controls controls = player != null ? player.GetComponent<Controls>() : null;

        // The readout belongs to the world, so it goes away while the inventory, crafting, level-up or
        // inscription panel is open. MenuIsOpen is the same gate the input code uses.
        bool hide = controls != null && controls.MenuIsOpen;
        if (hudRoot != null && hudRoot.activeSelf == hide)
        {
            hudRoot.SetActive(!hide);
        }
        if (hide)
        {
            return;
        }

        RefreshTurn(controls);
        RefreshRealm();
        RefreshRoot(player);
    }



    private void RefreshTurn(Controls controls)
    {
        if (turnText == null)
        {
            return;
        }

        string turn = controls == null ? "—"
            : controls.takingTurn ? "Enemies…"
            : controls.IsAiming ? "Your turn (aiming)"
            : "Your turn";

        if (turn == shownTurn)
        {
            return;
        }
        shownTurn = turn;
        turnText.text = turn;
    }

    /// <summary>
    /// A weapon's display name, with the "(empty equipment)" placeholder that the empty gear definitions carry
    /// rendered as "none" instead of leaking into the HUD. Equipments holds EquipmentInstance, not
    /// ItemDefinition - the instance carries the per-item inscriptions.
    /// </summary>
    private static string NameOf(EquipmentInstance equipment)
    {
        if (equipment == null)
        {
            return "none";
        }
        string name = equipment.DisplayName;
        return string.IsNullOrEmpty(name) || name.StartsWith("(") ? "none" : name;
    }

    private void RefreshRealm()
    {
        if (realmText == null)
        {
            return;
        }

        string realm = RunState.HasRun ? $"{RunState.RealmName} — Floor {RunState.DisplayFloor}" : "—";
        if (realm == shownRealm)
        {
            return;
        }
        shownRealm = realm;
        realmText.text = realm;
    }

    private void RefreshRoot(PlayerStats player)
    {
        StatusController statuses = player != null ? player.Statuses : null;
        bool rooted = statuses != null && statuses.IsRooted;
        if (rooted == shownRooted)
        {
            return;
        }
        shownRooted = rooted;

        if (rootIcon != null)
        {
            rootIcon.SetActive(rooted);
        }
        if (rootText != null)
        {
            rootText.text = rooted
                ? $"Rooted ({statuses.Set.RoundsRemaining(StatusKind.Root)})"
                : string.Empty;
        }
    }
}
