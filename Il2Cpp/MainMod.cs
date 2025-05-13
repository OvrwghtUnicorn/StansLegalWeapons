using MelonLoader;
using HarmonyLib;
using UnityEngine.Events;
using UnityEngine;
using Il2Generic = Il2CppSystem.Collections.Generic;
using ScheduleOneGame = Il2CppScheduleOne;
using Il2CppScheduleOne.NPCs;
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
        public const string Version = "1.2.0";
        public const string DownloadLink = null;
    }

    public class ArmsDealerShop : MelonMod
    {
        public DialogueController.DialogueChoice dialogueChoice = null;
        public DialogueController_ArmsDealer stan = null;
        public static ShopInterface Shop = null;
        public static GameObject shopGo = null;
        public static Dictionary<string, HashSet<string>> allListings = null;

        public override void OnLateInitializeMelon() {
            ScheduleOneGame.Persistence.LoadManager.Instance.onLoadComplete.AddListener((UnityAction)Initialize);
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName) {
            if(sceneName.ToLower() == "main") {
                allListings = new Dictionary<string, HashSet<string>>();
            }
        }

        public void Initialize() {
            MelonLogger.Msg(System.ConsoleColor.Magenta, "Stan's 'Legal' Weapons is in business");

            stan = GameObject.FindObjectOfType<DialogueController_ArmsDealer>();
            if (stan != null) {
                GameObject stanGo = stan.gameObject;
                this.dialogueChoice = stan.Choices[0];
                this.dialogueChoice.ChoiceText = "What do you have for sale?";

                var shopInterfaces = GameObject.FindObjectsOfType<ShopInterface>();

                if (shopGo == null) {
                    foreach (ShopInterface shopInterface in shopInterfaces) {
                        if (shopInterface.gameObject.name == "DarkMarketInterface") {
                            shopGo = GameObject.Instantiate(shopInterface.gameObject);
                            shopGo.name = "WeaponsDealerShopInterface";
                            shopGo.transform.SetParent(shopInterface.transform.parent, false);
                            break;
                        }
                    }
                }

                if (shopGo == null) return;

                MelonLogger.Msg(System.ConsoleColor.Magenta, "ShopGo Instantiated");

                if (Shop == null) {                 
                    Shop = shopGo.GetComponent<ShopInterface>();
                    Shop.ShopName = "Stan's 'Legal' Weapons";
                    Shop.Listings.Clear();
                    Shop.listingUI.Clear();
                    Shop.categoryButtons.Clear();
                    AdjustStoreLabel();
                } else {
                    MelonLogger.Msg(System.ConsoleColor.Magenta, "Shop Exists");
                }

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

        public static void CreateListings(Il2Generic.List<DialogueController_ArmsDealer.WeaponOption> weaponOptions, string type, bool createUi = false) {
            HashSet<string> typeListings;
            if (allListings.ContainsKey(type)) {
                typeListings = allListings[type];
            } else {
                typeListings = new HashSet<string>();
            }

            foreach (DialogueController_ArmsDealer.WeaponOption option in weaponOptions) {
                if (!typeListings.Contains(option.Item.ID)) {
                    ShopListing newListing = WeaponOptionToShopListing(option, type);
                    typeListings.Add(newListing.Item.ID);
                    Shop.Listings.Add(newListing);
                    if (createUi) { 
                        Shop.CreateListingUI(newListing);
                    }
                }
            }

            allListings[type] = typeListings;
        }

        public static ShopListing WeaponOptionToShopListing(DialogueController_ArmsDealer.WeaponOption weaponOption, string type) {
            ShopListing newListing = new ShopListing();
            newListing.name = $"{weaponOption.Name} ({weaponOption.Price}) ({type}, ) [{weaponOption.Item.RequiredRank.ToString()}]";
            newListing.OverridePrice = true;
            newListing.OverriddenPrice = weaponOption.Price;
            newListing.Item = weaponOption.Item;
            newListing.MinimumGameCreationVersion = 27;
            newListing.CanBeDelivered = true;
            newListing.DefaultStock = 1000;
            return newListing;
        }

        [HarmonyPatch(typeof(DialogueController_ArmsDealer),nameof(DialogueController_ArmsDealer.ChoiceCallback))]
        public static class DialogueController_ArmsDealer_ChoiceCallback_Patch{
            public static bool Prefix(DialogueController_ArmsDealer __instance, ref string choiceLabel) {
                if (Shop != null) {
                    if (__instance.MeleeWeapons.Count != allListings["Melee"].Count) {
                        CreateListings(__instance.MeleeWeapons, "Melee",true);
                    }
                    if (__instance.RangedWeapons.Count != allListings["Ranged"].Count) {
                        CreateListings(__instance.RangedWeapons, "Ranged", true);
                    }
                    if (__instance.Ammo.Count != allListings["Ammo"].Count) {
                        CreateListings(__instance.Ammo, "Ammo", true);
                    }
                    Shop.SetIsOpen(true);
                    return false;
                }
                return true;
            }
        }
    }
}
