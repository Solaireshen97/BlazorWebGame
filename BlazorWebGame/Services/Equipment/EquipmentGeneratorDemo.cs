using BlazorWebGame.Models;
using BlazorWebGame.Models.Items;
using BlazorWebGame.Models.Monsters;
using BlazorWebGame.Services.Equipments;
using System.Collections.Generic;

namespace BlazorWebGame.Services
{
    public static class EquipmentGeneratorDemo
    {
        /// <summary>
        /// 生成一些示例武器，展示装备生成系统的功能
        /// </summary>
        public static List<Equipment> GenerateWeaponExamples()
        {
            var weapons = new List<Equipment>();

            // 1. 生成一把1级的白色品质剑
            var basicSword = EquipmentGenerator.GenerateEquipment(
                name: "新手长剑",
                level: 1,
                slot: EquipmentSlot.MainHand,
                quality: EquipmentGenerator.EquipmentQuality.Common,
                weaponType: WeaponType.Sword
            );
            weapons.Add(basicSword);

            // 2. 生成一把5级的绿色品质匕首，高属性等级(T3)
            var uncommonDagger = EquipmentGenerator.GenerateEquipment(
                name: "锋锐匕首",
                level: 5,
                slot: EquipmentSlot.MainHand,
                quality: EquipmentGenerator.EquipmentQuality.Uncommon,
                attributeTier: EquipmentGenerator.AttributeTier.T3,
                weaponType: WeaponType.Dagger
            );
            weapons.Add(uncommonDagger);

            // 3. 生成一把10级的蓝色品质双手剑
            var rareTwoHandSword = EquipmentGenerator.GenerateEquipment(
                name: "钢铁巨剑",
                level: 10,
                slot: EquipmentSlot.MainHand,
                quality: EquipmentGenerator.EquipmentQuality.Rare,
                weaponType: WeaponType.TwoHandSword,
                isTwoHanded: true
            );
            weapons.Add(rareTwoHandSword);

            // 4. 生成一把15级的紫色品质法杖，限制法师职业使用
            var epicStaff = EquipmentGenerator.GenerateEquipment(
                name: "奥术法杖",
                level: 15,
                slot: EquipmentSlot.MainHand,
                quality: EquipmentGenerator.EquipmentQuality.Epic,
                weaponType: WeaponType.Staff,
                isTwoHanded: true,
                allowedProfessions: new List<BattleProfession> { BattleProfession.Mage },
                // 自定义副属性池
                secondaryAttributePool: new List<string> { 
                    "intellect", "spirit", "critical_chance", "fire_resistance" 
                }
            );
            weapons.Add(epicStaff);
            
            // 5. 生成一个8级的绿色盾牌
            var uncommonShield = EquipmentGenerator.GenerateEquipment(
                name: "坚固盾牌",
                level: 8,
                slot: EquipmentSlot.OffHand,
                quality: EquipmentGenerator.EquipmentQuality.Uncommon,
                weaponType: WeaponType.Shield
            );
            weapons.Add(uncommonShield);

            return weapons;
        }
        
        /// <summary>
        /// 测试方法：将生成的武器添加到EquipmentData中
        /// </summary>
        public static void AddGeneratedWeaponsToDatabase()
        {
            int counter = 1;
            foreach (var weapon in GenerateWeaponExamples())
            {
                weapon.Id = $"EQ_WEP_GEN_{counter:D3}";
                weapon.Value = EquipmentGenerator.CalculateEquipmentValue(weapon);
                weapon.ShopPurchaseInfo = new PurchaseInfo 
                { 
                    ShopCategory = "生成武器",
                    Price = weapon.Value * 2
                };
                
                // 将生成的武器添加到EquipmentData的_items列表中
                // 注意：这种方法在生产环境中不推荐，仅用于测试
                typeof(EquipmentData).GetField("_items", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                    ?.GetValue(null)
                    ?.GetType()
                    .GetMethod("Add")
                    ?.Invoke(typeof(EquipmentData).GetField("_items", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)?.GetValue(null), new object[] { weapon });
                
                counter++;
            }
        }
    }
}