using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Models.Items
{
    /// <summary>
    /// �������в�������Ʒ������
    /// </summary>
    public static class MaterialData
    {
        private static readonly List<Item> _items = new()
        {
            // --- ��ҩ ---
            new Item
            {
                Id = "HERB_PEACEBLOOM", Name = "����",
                Description = "һ�ֳ����İ�ɫС����ɢ���Ű�����������",
                Type = ItemType.Material, IsStackable = true, Value = 2,
                ShopPurchaseInfo = new PurchaseInfo
                {
                    ShopCategory = "�ز�",
                    Currency = CurrencyType.Gold,
                    Price = 10
                }
            },
            new Item
            {
                Id = "HERB_SILVERLEAF", Name = "��Ҷ��",
                Description = "ҶƬ�ϴ�����ɫ��·��ֲ����¹��»�΢΢���⡣",
                Type = ItemType.Material, IsStackable = true, Value = 5,
                ShopPurchaseInfo = new PurchaseInfo
                {
                    ShopCategory = "�ز�",
                    Currency = CurrencyType.Gold,
                    Price = 10
                }
            },
            new Item
            {
                Id = "HERB_MAGEROYAL", Name = "ħ�ʲ�",
                Description = "����Ϊ�̺���ħ��������ϡ��ֲ����ܷ�ʦ�ǵ�ϲ����",
                Type = ItemType.Material, IsStackable = true, Value = 15
            },
            
            // --- ��ʯ�ͽ��� ---
            new Item
            {
                Id = "ORE_COPPER", Name = "ͭ��ʯ",
                Description = "һ�ֻ����Ľ�����ʯ���������ڶ��졣",
                Type = ItemType.Material, IsStackable = true, Value = 4,
                ShopPurchaseInfo = new PurchaseInfo { ShopCategory = "�ز�", Price = 10 }
            },
            new Item
            {
                Id = "ORE_IRON", Name = "����ʯ",
                Description = "��ͭ����̵Ľ�����ʯ��",
                Type = ItemType.Material, IsStackable = true, Value = 10,
                ShopPurchaseInfo = new PurchaseInfo { ShopCategory = "�ز�", Price = 15 }
            },
            new Item
            {
                Id = "BAR_COPPER",
                Name = "ͭ��",
                Description = "��ͭ��ʯ�������ɵĽ��������Ƕ���Ļ������ϡ�",
                Type = ItemType.Material,
                Value = 8,
                IsStackable = true,
                ShopPurchaseInfo = new PurchaseInfo { ShopCategory = "�ز�", Price = 50 }
            },
            
            // --- ��ʯ ---
            new Item
            {
                Id = "GEM_ROUGH_TIGERSEYE",
                Name = "���ʵĻ���ʯ",
                Description = "һ��δ����ĥ�ı�ʯ���ڲ��ƺ���΢��������",
                Type = ItemType.Material,
                Value = 10,
                IsStackable = true,
                ShopPurchaseInfo = new PurchaseInfo { ShopCategory = "�ز�", Price = 50 }
            },
            new Item
            {
                Id = "GEM_TIGERSEYE",
                Name = "����ʯ",
                Description = "������ϸ�и�Ļ���ʯ��������Ƕ�������ϡ�",
                Type = ItemType.Material,
                Value = 25,
                IsStackable = true,
                ShopPurchaseInfo = new PurchaseInfo { ShopCategory = "�ز�", Price = 50 }
            },
            
            // --- �������� ---
            new Item
            {
                Id = "MAT_COPPER_WIRE",
                Name = "ͭ˿",
                Description = "��ͭ�����ɵ�ϸ˿�����������鱦�Ļ�����",
                Type = ItemType.Material,
                Value = 12,
                IsStackable = true,
                ShopPurchaseInfo = new PurchaseInfo { ShopCategory = "�ز�", Price = 50 }
            },
            new Item
            {
                Id = "MAT_RUINED_LEATHER_SCRAPS",
                Name = "�����Ƥ����Ƭ",
                Description = "���Ժϳɴ���Ƥ�",
                Type = ItemType.Material,
                Value = 1,
                IsStackable = true,
                ShopPurchaseInfo = new PurchaseInfo { ShopCategory = "�ز�", Price = 50 }
            },
            new Item
            {
                Id = "MAT_COARSE_THREAD",
                Name = "����",
                Description = "���ڷ���Ƥ����Ʒ��",
                Type = ItemType.Material,
                Value = 2,
                IsStackable = true,
                ShopPurchaseInfo = new PurchaseInfo { ShopCategory = "�ز�", Price = 50 }
            },
            new Item
            {
                Id = "MAT_LINEN_CLOTH",
                Name = "���鲼",
                Description = "��������ά֯�ɵĲ��ϣ��ǲ÷�Ļ������ϡ�",
                Type = ItemType.Material,
                Value = 3,
                IsStackable = true,
                ShopPurchaseInfo = new PurchaseInfo { ShopCategory = "�ز�", Price = 50 }
            },
            new Item
            {
                Id = "MAT_ROUGH_STONE",
                Name = "���ʵ�ʯͷ",
                Description = "���Ա�ĥ�ɷ�ĩ������������ը�",
                Type = ItemType.Material,
                Value = 1,
                IsStackable = true
            },
            
            // --- ���� ---
            new Item
            {
                Id = "FISH_TROUT", Name = "������",
                Description = "һ����ͨ�ĺ��㣬����������⿡�",
                Type = ItemType.Material, IsStackable = true, Value = 6
            },
            new Item
            {
                Id = "FISH_BASS", Name = "��������",
                Description = "����������Ƭ�������⣬�ƺ���ֵǮ��",
                Type = ItemType.Material, IsStackable = true, Value = 18
            },
            
            // --- ������� ---
            new Item
            {
                Id = "MAT_DEMON_ESSENCE", Name = "��ħ����",
                Description = "��ǿ���ħ�����ռ������������ģ�������������ϡ����Ʒ��",
                Type = ItemType.Material,
                IsStackable = true,
                Value = 100
            }
        };

        /// <summary>
        /// ��ȡ���в�����Ʒ
        /// </summary>
        public static List<Item> Items => _items;

        /// <summary>
        /// ����ID���Ҳ�����Ʒ
        /// </summary>
        public static Item? GetById(string id) => _items.FirstOrDefault(i => i.Id == id);
    }
}