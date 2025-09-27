using BlazorIdleGame.Client.Models;
using System.Collections.Generic;

namespace BlazorIdleGame.Client.Store.PartyStore
{
    public class LoadAvailablePartiesAction { }

    public class LoadAvailablePartiesSuccessAction
    {
        public List<PartyInfo> Parties { get; }

        public LoadAvailablePartiesSuccessAction(List<PartyInfo> parties)
        {
            Parties = parties;
        }
    }

    public class CreatePartyAction
    {
        public string Name { get; }
        public int MaxMembers { get; }

        public CreatePartyAction(string name, int maxMembers = 5)
        {
            Name = name;
            MaxMembers = maxMembers;
        }
    }

    public class JoinPartyAction
    {
        public string PartyId { get; }

        public JoinPartyAction(string partyId)
        {
            PartyId = partyId;
        }
    }

    public class LeavePartyAction { }

    public class UpdatePartyAction
    {
        public PartyInfo Party { get; }

        public UpdatePartyAction(PartyInfo party)
        {
            Party = party;
        }
    }

    public class InvitePlayerAction
    {
        public string PlayerId { get; }

        public InvitePlayerAction(string playerId)
        {
            PlayerId = playerId;
        }
    }

    public class ReceiveInviteAction
    {
        public PartyInvite Invite { get; }

        public ReceiveInviteAction(PartyInvite invite)
        {
            Invite = invite;
        }
    }

    public class KickMemberAction
    {
        public string MemberId { get; }

        public KickMemberAction(string memberId)
        {
            MemberId = memberId;
        }
    }

    public class PromoteToLeaderAction
    {
        public string MemberId { get; }

        public PromoteToLeaderAction(string memberId)
        {
            MemberId = memberId;
        }
    }

    public class SetReadyStatusAction
    {
        public bool IsReady { get; }

        public SetReadyStatusAction(bool isReady)
        {
            IsReady = isReady;
        }
    }

    public class PartyOperationSuccessAction { }

    public class PartyOperationFailureAction
    {
        public string ErrorMessage { get; }

        public PartyOperationFailureAction(string errorMessage)
        {
            ErrorMessage = errorMessage;
        }
    }
}