using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The two weapon slots in the HUD's bottom-left corner (T08): melee and bow.
///
/// Each shows the equipped weapon's icon, an empty slot background when nothing is equipped, the weapon
/// currently in use highlighted, and the arrow count on the bow's top-right corner.
///
/// It builds its own widgets when the references are unassigned, so it needs no scene wiring - HudReadout
/// attaches it once at runtime.
/// </summary>
public class WeaponSlots : MonoBehaviour
{
    [Header("Slots (built at runtime when unassigned)")]
    [SerializeField] private Image meleeSlot;
    [SerializeField] private Image meleeIcon;
    [SerializeField] private Image rangedSlot;
    [SerializeField] private Image rangedIcon;
    [SerializeField] private TMP_Text ammoBadge;

    [Header("Look")]
    [SerializeField] private Color emptySlot = new Color(0.12f, 0.13f, 0.15f, 0.65f);
    [SerializeField] private Color activeTint = Color.white;
    [SerializeField] private Color inactiveTint = new Color(0.55f, 0.55f, 0.55f, 1f);
    [SerializeField] private Color ammoNormal = Color.white;
    [SerializeField] private Color ammoEmpty = Color.red;

    private EquipmentInstance shownMelee;
    private EquipmentInstance shownRanged;
    private bool hadMelee;
    private bool hadRanged;
    private bool shownRangedActive;
    private int shownArrows = -1;

    private void Awake()
    {
        EnsureWidgets();
    }

    private void Update()
    {
        Equipments equipment = Equipments.Instance;
        Controls controls = PlayerStats.Instance != null ? PlayerStats.Instance.GetComponent<Controls>() : null;

        bool bowInUse = controls != null && controls.IsAiming;

        RefreshWeapons(equipment, bowInUse);
        RefreshAmmo(equipment);
    }

    /// <summary>Melee is the default weapon, so the bow is only highlighted while aiming.</summary>
    private void RefreshWeapons(Equipments equipment, bool bowInUse)
    {
        EquipmentInstance melee = equipment != null ? equipment.meleeWeapon : null;
        EquipmentInstance ranged = equipment != null ? equipment.rangeWeapon : null;
        bool hasMelee = HasIcon(melee);
        bool hasRanged = HasIcon(ranged);

        if (melee == shownMelee && ranged == shownRanged && hasMelee == hadMelee && hasRanged == hadRanged &&
            bowInUse == shownRangedActive)
        {
            return;
        }

        shownMelee = melee;
        shownRanged = ranged;
        hadMelee = hasMelee;
        hadRanged = hasRanged;
        shownRangedActive = bowInUse;

        ApplySlot(meleeSlot, meleeIcon, melee, hasMelee, active: !bowInUse || !hasRanged);
        ApplySlot(rangedSlot, rangedIcon, ranged, hasRanged, active: bowInUse);
    }

    private void ApplySlot(Image slot, Image icon, EquipmentInstance weapon, bool hasIcon, bool active)
    {
        if (slot != null)
        {
            // A lit background marks the weapon in use; an empty slot stays dark either way.
            slot.color = active && hasIcon ? Color.Lerp(emptySlot, Color.white, 0.35f) : emptySlot;
        }

        if (icon == null)
        {
            return;
        }

        icon.enabled = hasIcon;
        if (!hasIcon)
        {
            return;
        }

        icon.sprite = weapon.definition.Icon;
        icon.color = active ? activeTint : inactiveTint;
    }

    /// <summary>Arrow count sits on the bow slot's top-right corner, and only while a bow is equipped.</summary>
    private void RefreshAmmo(Equipments equipment)
    {
        if (ammoBadge == null)
        {
            return;
        }

        bool hasBow = equipment != null && equipment.rangeWeapon != null;
        if (ammoBadge.gameObject.activeSelf != hasBow)
        {
            ammoBadge.gameObject.SetActive(hasBow);
        }
        if (!hasBow)
        {
            return;
        }

        int arrows = PlayerStats.Instance != null ? PlayerStats.Instance.Arrows : 0;
        if (arrows == shownArrows)
        {
            return;
        }
        shownArrows = arrows;
        ammoBadge.text = arrows.ToString();
        ammoBadge.color = arrows <= 0 ? ammoEmpty : ammoNormal;
    }

    private static bool HasIcon(EquipmentInstance weapon)
    {
        return weapon != null && weapon.definition != null && weapon.definition.Icon != null;
    }

    private void EnsureWidgets()
    {
        if (meleeSlot == null || meleeIcon == null || rangedSlot == null || rangedIcon == null)
        {
            Debug.LogWarning("[HUD] WeaponSlots is missing its slot references. " +
                             "Run Tools/Content/Wire HUD References to build them in the scene.");
        }
    }
}
