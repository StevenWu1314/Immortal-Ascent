using UnityEngine;
using System.Collections;

static class Manager {
    public static Grid grid;
    public static PlayerStats player;
 
    // when the program launches, UtilityManager will check that all the needed elements are in place
    // that's exactly what you do in the static constructor here:

    static Manager() {
        //grid = new Grid(100, 100, 1, new Vector3(0, 0, 0));
        //player = safeFind("Player").GetComponent<PlayerStats>(); // (some persistent object)

        // etc ..
        // (you could use just the one persistent game object, or many - irrelevant)
       
       
        // PS. annoying arcane technical note - remember that really, in c# static constructors do not run
        // until the first time you use them.  almost certainly in any large project like this, Manager
        // would be called zillions of times by all the Awake (etc etc) code everywhere, so it is
        // a non-issue. but if you're just testing or something, it may be confusing that (for example)
        // the wake-up alert only appears just before you happen to use Grid, rather than "when you hit play"
    }

   
    // this has no purpose other than for developers wondering HTF you use UtilityManager
    // just type UtilityManager.SayHello() anywhere in the project.
    // it is useful to add a similar routine to (example) PurchaseManager.cs
    // then from anywhere in the project, you can type UtilityManager.purchaseManager.SayHello()
    // to check everything is hooked-up properly.
    public static void SayHello() {
        Debug.Log("Confirming to developer that the UtilityManager is working fine.");
    }
   
    // just some convenience routines to save people copy pasting
   
    // when GameManager wakes up, it checks everything is in place...
    private static GameObject safeFind(string s) {
        GameObject g = GameObject.Find(s);
        if ( g == null ) bigProblem("The " +s+ " game object is not in this scene. You're stuffed.");
        // next .... see Vexe to check that there is strictly ONE of these fuckers. you never know.
        return g;
    }
    private static Component safeComponent(GameObject g, string s) {
        Component c = g.GetComponent(s);
        if ( c == null ) bigProblem("The " +s+ " component is not there. You're stuffed.");
        return c;
    }
    private static void bigProblem(string error) {
        for (int i=10;i>0;--i) Debug.LogError(" >>> Cannot proceed... " +error);
        for (int i=10;i>0;--i) Debug.LogError(" !!!!!  Is it possible you just forgot to launch from scene zero.");
        Debug.Break();
    }

}
