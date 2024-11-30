using UnityEngine;


public class PlayerStats : MonoBehaviour{
    [SerializeField] int Health;
    [SerializeField] int strength;
    [SerializeField] int dexterity;
    [SerializeField] Weapon weapon;
    [SerializeField] Weapon rangedWeapon;
    
    public void attack(string form, Enemy target)
    {
        Debug.Log(target);
        switch (form){
            case "melee":
                target.takeDamage(strength + weapon.getDamage());
                target.attack(this);
                break;
            case "range":
                target.takeDamage(dexterity + weapon.getDamage());
                break;        
           }
    }

    public void takeDamage(int damage)
    {
        Health -= damage;
    }

    public void heal(int amount) 
    {
        Health += amount;
    }
    private void OnDrawGizmos() {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 1);
    }
}
