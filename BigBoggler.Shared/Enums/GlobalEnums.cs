namespace BigBoggler.Enums
{
	public enum OperationCodes { Dummy = 0, SendChatMessage = 1, ValidateResults = 2, ElapsedSecond = 99, JoinRound = 100, LeaveRound = 101, PlayerReady = 102 }
	public enum ParameterKeys { Board = 1, WordList = 2, OpponentScore = 3, SecondsElapsed = 10, WordListMetadata = 11, PlayersScore = 12 }
	public enum PropertyCodes { GameScore = 1, AbsoluteScore = 2, Language = 10 }
}