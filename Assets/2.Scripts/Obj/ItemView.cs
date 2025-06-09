using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemView : MonoBehaviour
{
    int Rarity;
    int ind;
    bool isweapon;

    int MyFloatInd = -1;
    private void Start()
    {
        MyFloatInd = GameManager.instance.FloatM.Register();
    }

    public void Init(int rarity, int ind, bool IsWeapon, Sprite sp)
    {
        Rarity = rarity; this.ind = ind; isweapon = IsWeapon;  GetComponent<UnityEngine.UI.Image>().sprite = sp;
    }

    public void PointerEnter()
    {
        GameManager.instance.FloatM.Init(GameManager.instance.Data.Weapon[ind].DescribeSelf(), GameManager.instance.Data.RarityColor[Rarity],MyFloatInd,25);
    }

    public void PointerOut()
    {
        GameManager.instance.FloatM.Close(MyFloatInd);
    }
}
