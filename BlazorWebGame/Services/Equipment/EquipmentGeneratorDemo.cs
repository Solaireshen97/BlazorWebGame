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
        /// ����һЩʾ��������չʾװ������ϵͳ�Ĺ���
        /// </summary>
        public static List<Equipment> GenerateWeaponExamples()
        {
            var weapons = new List<Equipment>();

            // 1. ����һ��1���İ�ɫƷ�ʽ�
            var basicSword = EquipmentGenerator.GenerateEquipment(
                name: "���ֳ���",
                level: 1,
                slot: EquipmentSlot.MainHand,
                quality: EquipmentGenerator.EquipmentQuality.Common,
                weaponType: WeaponType.Sword
            );
            weapons.Add(basicSword);

            // 2. ����һ��5������ɫƷ��ذ�ף������Եȼ�(T3)
            var uncommonDagger = EquipmentGenerator.GenerateEquipment(
                name: "����ذ��",
                level: 5,
                slot: EquipmentSlot.MainHand,
                quality: EquipmentGenerator.EquipmentQuality.Uncommon,
                attributeTier: EquipmentGenerator.AttributeTier.T3,
                weaponType: WeaponType.Dagger
            );
            weapons.Add(uncommonDagger);

            // 3. ����һ��10������ɫƷ��˫�ֽ�
            var rareTwoHandSword = EquipmentGenerator.GenerateEquipment(
                name: "�����޽�",
                level: 10,
                slot: EquipmentSlot.MainHand,
                quality: EquipmentGenerator.EquipmentQuality.Rare,
                weaponType: WeaponType.TwoHandSword,
                isTwoHanded: true
            );
            weapons.Add(rareTwoHandSword);

            // 4. ����һ��15������ɫƷ�ʷ��ȣ����Ʒ�ʦְҵʹ��
            var epicStaff = EquipmentGenerator.GenerateEquipment(
                name: "��������",
                level: 15,
                slot: EquipmentSlot.MainHand,
                quality: EquipmentGenerator.EquipmentQuality.Epic,
                weaponType: WeaponType.Staff,
                isTwoHanded: true,
                allowedProfessions: new List<BattleProfession> { BattleProfession.Mage },
                // �Զ��帱���Գ�
                secondaryAttributePool: new List<string> { 
                    "intellect", "spirit", "critical_chance", "fire_resistance" 
                }
            );
            weapons.Add(epicStaff);
            
            // 5. ����һ��8������ɫ����
            var uncommonShield = EquipmentGenerator.GenerateEquipment(
                name: "��̶���",
                level: 8,
                slot: EquipmentSlot.OffHand,
                quality: EquipmentGenerator.EquipmentQuality.Uncommon,
                weaponType: WeaponType.Shield
            );
            weapons.Add(uncommonShield);

            return weapons;
        }
        
        /// <summary>
        /// ���Է����������ɵ�������ӵ�EquipmentData��
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
                    ShopCategory = "��������",
                    Price = weapon.Value * 2
                };
                
                // �����ɵ�������ӵ�EquipmentData��_items�б���
                // ע�⣺���ַ��������������в��Ƽ��������ڲ���
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