using MelonLoader;
using HarmonyLib;
using UnityEngine.Events;
using UnityEngine;
using Il2Generic = Il2CppSystem.Collections.Generic;
using ScheduleOneGame = Il2CppScheduleOne;
using Il2CppScheduleOne.Dialogue;
using Il2CppScheduleOne.UI.Shop;

[assembly: MelonInfo(typeof(StansLegalWeapons.ArmsDealerShop), StansLegalWeapons.BuildInfo.Name, StansLegalWeapons.BuildInfo.Version, StansLegalWeapons.BuildInfo.Author, StansLegalWeapons.BuildInfo.DownloadLink)]
[assembly: MelonColor(255, 191, 0, 255)]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace StansLegalWeapons {
    public static class BuildInfo
    {
        public const string Name = "Stan's 'Legal' Weapons";
        public const string Description = "Adds a shop interface for the arms dealer";
        public const string Author = "OverweightUnicorn";
        public const string Company = "UnicornsCanMod";
        public const string Version = "1.0.0";
        public const string DownloadLink = null;
    }

    public class ArmsDealerShop : MelonMod
    {
        public DialogueController.DialogueChoice dialogueChoice = null;
        public DialogueController_ArmsDealer stan = null;
        public static ShopInterface Shop = null;
        public static GameObject shopGo = null;

        public override void OnLateInitializeMelon() {
            ScheduleOneGame.Persistence.LoadManager.Instance.onLoadComplete.AddListener((UnityAction)Initialize);
        }

        public void Initialize() {
            stan = GameObject.FindObjectOfType<DialogueController_ArmsDealer>();
            if (stan != null) {
                GameObject stanGo = stan.gameObject;
                this.dialogueChoice = stan.Choices[0];
                this.dialogueChoice.ChoiceText = "What do you have for sale?";

                var shopInterfaces = GameObject.FindObjectsOfType<ShopInterface>();
                foreach (ShopInterface shopInterface in shopInterfaces) {
                    if (shopInterface.gameObject.name == "DarkMarketInterface") {
                        shopGo = GameObject.Instantiate(shopInterface.gameObject);
                        shopGo.name = "WeaponsDealerShopInterface";
                        shopGo.transform.SetParent(shopInterface.transform.parent, false);

                        Shop = shopGo.GetComponent<ShopInterface>();
                        Shop.ShopName = "Stan's 'Legal' Weapons";
                        Shop.Listings.Clear();
                        Shop.listingUI.Clear();
                        Shop.categoryButtons.Clear();
                        break;
                    }
                }

                if (Shop == null) return;

                AdjustStoreLabel();
                DestroyCategories();
                DestroyOldShopItems(Shop.ListingContainer);

                CreateListings(stan.MeleeWeapons, "Melee");
                CreateListings(stan.RangedWeapons, "Ranged");
                CreateListings(stan.Ammo,"Ammo");

                Shop.Awake();
                Shop.Start();
            }
        }

        public void AdjustStoreLabel() {
            RectTransform textLabel = Shop.StoreNameLabel.gameObject.GetComponent<RectTransform>();
            if (textLabel != null) {
                textLabel.anchoredPosition = new Vector2(10, -17);
                textLabel.pivot = new Vector2(0, 0.5f);
                textLabel.sizeDelta = new Vector2(600, 25);
            }
        }

        public void DestroyCategories() {
            Transform categoryContainer = shopGo.transform.Find("Container/Topbar/Categories");
            if (categoryContainer != null) {
                var Categories = categoryContainer.transform.GetComponentsInChildren<CategoryButton>();
                foreach (CategoryButton child in Categories) {
                    if (child.name != "All") {
                        GameObject.Destroy(child.gameObject);
                    }
                }
            }
        }

        public void DestroyOldShopItems(RectTransform listContainer) {
            var children = listContainer.transform.GetComponentsInChildren<Transform>();
            foreach (Transform child in children) {
                if (child.name.ToLower().Contains("clone")) {
                    GameObject.Destroy(child.gameObject);
                }
            }
        }

        public void CreateListings(Il2Generic.List<DialogueController_ArmsDealer.WeaponOption> weaponOptions, string type) {
            foreach (DialogueController_ArmsDealer.WeaponOption option in weaponOptions) {
                ShopListing newListing = new ShopListing();
                newListing.name = $"{option.Name} ({option.Price}) ({type}, ) [{option.Item.RequiredRank.ToString()}]";
                newListing.OverridePrice = true;
                newListing.OverriddenPrice = option.Price;
                newListing.Item = option.Item;
                newListing.MinimumGameCreationVersion = 27;
                newListing.CanBeDelivered = true;
                newListing.DefaultStock = 1000;
                Shop.Listings.Add(newListing);
            }
        }

        [HarmonyPatch(typeof(DialogueController_ArmsDealer),nameof(DialogueController_ArmsDealer.ChoiceCallback))]
        public static class DialogueController_ArmsDealer_ChoiceCallback_Patch{
            public static bool Prefix(ref string choiceLabel) {
                if (Shop != null) {
                    Shop.SetIsOpen(true);
                    return false;
                }
                return true;
            }
        }
    }
}
