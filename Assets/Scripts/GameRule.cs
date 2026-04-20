using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/* Creating a new Rule workflow:
 * 
 * 1. Add a new enum under ruleTitle
 * 2. Go to "Assets" Folder -> Rules -> Create -> GameRules -> Rule
 * 3. Name that new rule the same as the enum added to avoid confusion
 * 4. Add this element into gameManager folder
 * 5. Add the element onto RuleManager.cs on Unity inspector
  
*/



//enum will be used for the dictionary/map in rule manager
public enum ruleTitle
{
    None,
    firstRule,
    secondRule,
    thirdRule,
    fourthRule,
    fifthRule,
    sixthRule,

}

[CreateAssetMenu(fileName = "NewRule", menuName = "Game Rules/Rule")]
public class GameRule : ScriptableObject
{
    public ruleTitle title;
    public string description;
    public bool isDiscovered = false;

    //when game resets
    public void resetRule()
    {
        isDiscovered = false;
    }
}
