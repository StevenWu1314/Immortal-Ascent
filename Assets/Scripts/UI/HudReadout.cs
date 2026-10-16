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
    [SerializeField] private TMP_Text ammoText;
    [SerializeField] private TMP_Text weaponText;
    [SerializeField] private TMP_Text turnText;
    [SerializeField] private TMP_Text realmText;

    [Header("Status")]
    [SerializeField] private GameObject rootIcon;
    [SerializeField] private TMP_Text rootText;

    [Header("Colours")]
    [SerializeField] private Color ammoNormal = Color.white;
    [SerializeField] private Color ammoEmpty = Color.red;

    private int shownArrows = -1;
    private string shownWeapon;
    private string shownTurn;
    private string shownRealm;
    private bool shownRooted;

    private void Update()
    {
        PlayerStats player = PlayerStats.Instance;
        Controls controls = player != null ? player.GetComponent<Controls>() : null;

        RefreshAmmo(player);
        RefreshWeapon();
        RefreshTurn(controls);
        RefreshRealm();
        RefreshRoot(player);
    }

    private void RefreshAmmo(PlayerStats player)
    {
        if (ammoText == null || player == null)
        {
            return;
        }

        int arrows = player.Arrows;
        if (arrows == shownArrows)
        {
            return;
        }
        shownArrows = arrows;
        ammoText.text = $"Arrows {arrows}";
        ammoText.color = arrows <= 0 ? ammoEmpty : ammoNormal;
    }

    private void RefreshWeapon()
    {
        if (weaponText == null)
        {
            return;
        }

        Equipments equipment = Equipments.Instance;
        string weapon = equipment == null || equipment.meleeWeapon == null ? "Melee: none"
            : $"Melee: {equipment.meleeWeapon.DisplayName}";
        if (equipment != null && equipment.rangeWeapon != null)
        {
            weapon += $" / Bow: {equipment.rangeWeapon.DisplayName}";
        }

        if (weapon == shownWeapon)
        {
            return;
        }
        shownWeapon = weapon;
        weaponText.text = weapon;
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
