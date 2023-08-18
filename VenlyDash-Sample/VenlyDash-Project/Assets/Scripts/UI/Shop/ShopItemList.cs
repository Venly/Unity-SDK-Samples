using Unity.VisualScripting;
using UnityEngine;

public class ShopItemList : ShopList
{
	static public Consumable.ConsumableType[] s_ConsumablesTypes = System.Enum.GetValues(typeof(Consumable.ConsumableType)) as Consumable.ConsumableType[];

	public override void Populate()
    {
		m_RefreshCallback = null;
        foreach (Transform t in listRoot)
        {
            Destroy(t.gameObject);
        }

        for(int i = 0; i < s_ConsumablesTypes.Length; ++i)
        {
            Consumable c = ConsumableDatabase.GetConsumbale(s_ConsumablesTypes[i]);
            if(c != null)
            {
                prefabItem.InstantiateAsync().Completed += (op) =>
                {
                    if (op.Result == null || !(op.Result is GameObject))
                    {
                        Debug.LogWarning(string.Format("Unable to load item shop list {0}.", prefabItem.RuntimeKey));
                        return;
                    }
                    GameObject newEntry = op.Result;
                    newEntry.transform.SetParent(listRoot, false);

                    ShopItemListItem itm = newEntry.GetComponent<ShopItemListItem>();

                    itm.buyButton.image.sprite = itm.buyButtonSprite;

                    itm.nameText.text = c.GetConsumableName();
                    itm.pricetext.text = c.GetPrice().ToString();

                    if (c.GetPremiumCost() > 0)
                    {
                        itm.premiumText.transform.parent.gameObject.SetActive(true);
                        itm.premiumText.text = c.GetPremiumCost().ToString();
                    }
                    else
                    {
                        itm.premiumText.transform.parent.gameObject.SetActive(false);
                    }

                    itm.icon.sprite = c.icon;

                    itm.countText.gameObject.SetActive(true);

                    itm.buyButton.onClick.AddListener(delegate() { Buy(c); });
                    m_RefreshCallback += delegate() { RefreshButton(itm, c); };
                    RefreshButton(itm, c);
                };
            }
        }
    }

	protected void RefreshButton(ShopItemListItem itemList, Consumable c)
	{
		int count = 0;
        PlayerDataWeb3.instance.consumables.TryGetValue(c.GetConsumableType(), out count);
		itemList.countText.text = count.ToString();

		if (c.GetPrice() > PlayerDataWeb3.instance.User.Coins)
		{
			itemList.buyButton.interactable = false;
			itemList.pricetext.color = Color.red;
		}
		else
		{
			itemList.pricetext.color = Color.black;
		}

		if (c.GetPremiumCost() > PlayerDataWeb3.instance.User.Coins)
		{
			itemList.buyButton.interactable = false;
			itemList.premiumText.color = Color.red;
		}
		else
		{
			itemList.premiumText.color = Color.black;
		}
	}

    public void Buy(Consumable c)
    {
        Web3Controller.ShowLoader("Claiming Consumable");
        Web3Controller.ClaimConsumable(c.GetConsumableType(), c.GetPrice())
            .OnFail(Web3Controller.ShowException)
            .Finally(() =>
            {
                Refresh();
                Web3Controller.HideLoader();
            });

        Refresh();
    }
}
