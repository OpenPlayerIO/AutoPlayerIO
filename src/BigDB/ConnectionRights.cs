namespace AutoPlayerIO
{
    public class ConnectionRights
    {
        public bool CreateMultiplayerRoom { get; internal set; }
        public bool JoinMultiplayerRoom { get; internal set; }
        public bool ListMultiplayerRooms { get; internal set; }
        public bool AccessPayVault { get; internal set; }
        public bool CreditPayVault { get; internal set; }
        public bool DebitPayVault { get; internal set; }
        public bool CanGiveVaultItems { get; internal set; }
        public bool CanBuyVaultItems { get; internal set; }
        public bool CanConsumeVaultItems { get; internal set; }
        public bool CanReadPayVaultHistory { get; internal set; }
        public bool CanAwardAchievement { get; internal set; }
        public bool AccessAchievement { get; internal set; }
        public bool CanLoadAchievements { get; internal set; }
        public bool CanSendGameRequests { get; internal set; }
        public bool CanAccessGameRequests { get; internal set; }
        public bool CanDeleteGameRequests { get; internal set; }
        public bool CanSendNotifications { get; internal set; }
        public bool CanRegisterNotificationEndpoints { get; internal set; }
        public bool CanManageNotifications { get; internal set; }
        public bool CanPlayerInsightRefresh { get; internal set; }
        public bool CanPlayerInsightSetSegments { get; internal set; }
        public bool CanmarkwhoinvitedauserinPlayerInsight { get; internal set; }
        public bool CanPlayerInsightTrackEvents { get; internal set; }
        public bool CanPlayerInsightTrackExternalPayment { get; internal set; }
        public bool CanSetCustomPlayerInsightSegments { get; internal set; }
        public bool CanModifyOneScore { get; internal set; }
        public bool CanSetLeaderboardScore { get; internal set; }
        public bool CanLoadLeaderboardScores { get; internal set; }
    }
}
