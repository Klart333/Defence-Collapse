using System;
using UnityEngine;

public class UIBuildingUpgrade : MonoBehaviour
{
    public void ShowUpgrades(TowerType type)
    {
        switch (type)
        {
            case TowerType.None:
                Debug.LogError("lmao bro thinks hes upgrades a house");
                break;
            case TowerType.Archer:
                break;
            default:
                break;
        }
    }

    public void Close()
    {
        
    }
}
