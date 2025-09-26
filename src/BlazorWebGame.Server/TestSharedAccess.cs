using BlazorWebGame.Shared.Enums;
using BlazorWebGame.Shared.Models.Items;
using BlazorWebGame.Shared.Models.Skills;
using BlazorWebGame.Shared.Models.Base;
using BlazorWebGame.Shared.Models.Combat;

namespace BlazorWebGame.Server
{
    /// <summary>
    /// Test class demonstrating that the server can now access shared models and enums
    /// </summary>
    public class TestSharedAccess
    {
        public void TestSharedEnumsAndModels()
        {
            // Test enums access
            var faction = Faction.StormwindGuard;
            var factionName = FactionData.GetName(faction);
            
            var itemType = ItemType.Equipment;
            var profession = BattleProfession.Warrior;
            var weaponType = WeaponType.Sword;
            var armorType = ArmorType.Plate;
            var slot = EquipmentSlot.MainHand;
            
            // Test model access
            var item = new Item
            {
                Name = "Test Item",
                Type = itemType,
                Value = 100
            };
            
            var equipment = new Equipment
            {
                Name = "Test Sword",
                Type = ItemType.Equipment,
                Slot = slot,
                WeaponType = weaponType,
                WeaponDamage = 50
            };
            
            var skill = new Skill
            {
                Name = "Test Skill",
                Type = SkillType.Profession,
                RequiredProfession = profession
            };
            
            var attributeSet = new AttributeSet
            {
                Strength = 10,
                Agility = 8,
                Intellect = 5
            };
            
            var consumable = new Consumable
            {
                Name = "Health Potion",
                Type = ItemType.Consumable,
                Category = ConsumableCategory.Potion,
                Effect = ConsumableEffectType.Heal,
                EffectValue = 50
            };
            
            var buff = new Buff
            {
                SourceItemId = "test-buff",
                BuffType = StatBuffType.AttackPower,
                BuffValue = 10,
                TimeRemainingSeconds = 300
            };
            
            // All models and enums are accessible from the server now!
        }
    }
}