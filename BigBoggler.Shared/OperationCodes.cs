namespace BigBoggler.Models
{
    public enum OperationCodes
    {
        // Sistema e Chat
        Dummy = 0,
        SendChatMessage = 1,
        ValidateResults = 2,

        // Timer e Sync
        ElapsedSecond = 99,

        // Gestione Sessione
        JoinRound = 100,
        LeaveRound = 101,
        PlayerReady = 102
    }
}