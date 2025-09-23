using BlazorWebGame.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Services
{
    /// <summary>
    /// ���������Ϸ�еĶ���ϵͳ
    /// </summary>
    public class PartyService
    {
        /// <summary>
        /// ��Ϸ�д��ڵ����ж����б�
        /// </summary>
        public List<Party> Parties { get; private set; } = new();

        /// <summary>
        /// ״̬����¼�
        /// </summary>
        public event Action? OnStateChanged;

        // ����������� - ��GameStateService�ṩ
        private readonly List<Player> _allCharacters;

        public PartyService(List<Player> allCharacters)
        {
            _allCharacters = allCharacters;
        }

        /// <summary>
        /// ���ݽ�ɫID���������ڵĶ��顣
        /// �����ɫ�����κζ����У��򷵻� null��
        /// </summary>
        /// <param name="characterId">Ҫ���ҵĽ�ɫID</param>
        /// <returns>��ɫ���ڵĶ�����󣬻� null</returns>
        public Party? GetPartyForCharacter(string characterId)
        {
            return Parties.FirstOrDefault(p => p.MemberIds.Contains(characterId));
        }

        /// <summary>
        /// ʹ��ָ���Ľ�ɫ����һ���¶��飬�ý�ɫ����Ϊ�ӳ���
        /// </summary>
        public void CreateParty(Player character)
        {
            // ��ȫ��飺ȷ����ɫ��ǰ�����κζ����С�
            if (character == null || GetPartyForCharacter(character.Id) != null)
            {
                return; // �����������������ִ���κβ���
            }

            // ����һ���µĶ���ʵ��
            var newParty = new Party
            {
                CaptainId = character.Id,
                MemberIds = new List<string> { character.Id } // �ӳ��Լ�Ҳ�Ƕ���ĵ�һ����Ա
            };

            // ���¶�����ӵ���Ϸ״̬�Ķ����б���
            Parties.Add(newParty);

            // ֪ͨUI����ˢ�£�����ʾ����״̬�ı仯
            NotifyStateChanged();
        }

        /// <summary>
        /// ��ָ����ɫ����һ��ָ���Ķ��顣
        /// </summary>
        /// <param name="character">Ҫ����Ľ�ɫ</param>
        /// <param name="partyId">Ҫ����Ķ����ID</param>
        public void JoinParty(Player character, Guid partyId)
        {
            // ��ȫ���
            if (character == null || GetPartyForCharacter(character.Id) != null)
            {
                return;
            }

            // ����Ŀ�����
            var partyToJoin = Parties.FirstOrDefault(p => p.Id == partyId);
            if (partyToJoin == null)
            {
                return; // ���鲻����
            }

            // �������Ƿ�����
            if (partyToJoin.MemberIds.Count >= Party.MaxMembers)
            {
                return; // �����������޷�����
            }

            // ִ�м������
            partyToJoin.MemberIds.Add(character.Id);

            // ֪ͨUI����
            NotifyStateChanged();
        }

        /// <summary>
        /// ��ָ����ɫ�뿪�����ڵĶ��顣
        /// </summary>
        public void LeaveParty(Player character)
        {
            // ��ȫ���
            if (character == null) return;
            var party = GetPartyForCharacter(character.Id);
            if (party == null)
            {
                return; // ��ɫ�����κζ�����
            }

            // �ж��Ƕӳ��뿪���ǳ�Ա�뿪
            if (party.CaptainId == character.Id)
            {
                // ����Ƕӳ��뿪�����ɢ��������
                Parties.Remove(party);
            }
            else
            {
                // ���ֻ����ͨ��Ա�뿪����ӳ�Ա�б����Ƴ��Լ�
                party.MemberIds.Remove(character.Id);
            }

            // ֪ͨUI����
            NotifyStateChanged();
        }
        
        /// <summary>
        /// ֹͣ���������г�Ա�ĵ�ǰս��״̬
        /// </summary>
        public void StopPartyAction(Party party)
        {
            if (party == null) return;
            
            // ��������ս��Ŀ��
            party.CurrentEnemy = null;
        }

        /// <summary>
        /// ����״̬����¼�
        /// </summary>
        private void NotifyStateChanged() => OnStateChanged?.Invoke();
    }
}