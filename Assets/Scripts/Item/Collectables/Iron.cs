using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Iron : Collectables
{
    public override void displaySelf()
    {
        Debug.Log("not Implemented");
    }

    public override void onUse()
    {
        Debug.Log("Not supposed to be used directly in inventory");
    }

}
