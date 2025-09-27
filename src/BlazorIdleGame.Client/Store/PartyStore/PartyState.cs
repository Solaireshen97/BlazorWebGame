using BlazorIdleGame.Client.Models;
using Fluxor;
using System.Collections.Generic;

namespace BlazorIdleGame.Client.Store.PartyStore
{
    [FeatureState]
    public class PartyState
    {
        public PartyInfo? CurrentParty { get; }
        public List<PartyInfo> AvailableParties { get; }
        public List<PartyInvite> PendingInvites { get; }
        public bool IsLoading { get; }
        public string? ErrorMessage { get; }

        public PartyState()
        {
            AvailableParties = new List<PartyInfo>();
            PendingInvites = new List<PartyInvite>();
        }

        public PartyState(PartyInfo? currentParty, List<PartyInfo> availableParties, 
            List<PartyInvite> pendingInvites, bool isLoading, string? errorMessage)
        {
            CurrentParty = currentParty;
            AvailableParties = availableParties ?? new List<PartyInfo>();
            PendingInvites = pendingInvites ?? new List<PartyInvite>();
            IsLoading = isLoading;
            ErrorMessage = errorMessage;
        }
    }
}