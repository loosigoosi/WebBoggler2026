Imports System.Runtime.InteropServices
Imports System.Text

' NOTA: è possibile utilizzare il comando "Rinomina" del menu di scelta rapida per modificare il nome di interfaccia "IService1" nel codice e nel file di configurazione contemporaneamente.
<XmlSerializerFormat>
<ServiceContract()>
Public Interface IServiceWebBoggler

    <OperationContract()>
    Function GetBoard(ByVal localeID As String) As Board

    <OperationContract()>
    Function CheckWord(word As String) As Boolean

    <OperationContract()>
    Sub SendWordList(wordList As WordList, ClientID As String)

    <OperationContract()>
    Function Join(clientID As String, name As String) As Boolean

    <OperationContract()>
    Function Leave(clientID As String) As Boolean

    <OperationContract()>
    Function GetPlayers(clientID As String) As Players

    <OperationContract()>
    Function GetSolution() As WordList

    <OperationContract()>
    Function Observe() As GameInfo

    <OperationContract()>
    Function IsServerAlive() As Boolean

End Interface


' Per aggiungere tipi compositi alle operazioni del servizio utilizzare un contratto di dati come descritto nell'esempio seguente.
' È possibile aggiungere file XSD nel progetto. Dopo la compilazione del progetto è possibile utilizzare direttamente i tipi di dati definiti qui con lo spazio dei nomi "BigBogglerWebService.ContractType".

'<DataContract()>
'Public Class CompositeType

'    <DataMember()>
'    Public Property BoolValue() As Boolean

'    <DataMember()>
'    Public Property StringValue() As String

<DataContract>
Public Class GameInfo

    <DataMember>
    Public Property ServerTimeUTC As String

    <DataMember>
    Public Property RoundElapsedTimeMS As Integer

    <DataMember>
    Public Property RoundDurationMS As Integer

    <DataMember>
    Public Property DeadTimeAmountMS As Integer

    <DataMember>
    Public Property RoomState As String 'passare il membro enum provoca errore nel browser C#XAML4HTML5

End Class

<DataContract>
Public Class Board

    <DataMember>
    Public Property LocaleID As String

    <DataMember>
    Public ReadOnly Property Language As String

    <DataMember>
    Public Property DicesVector As List(Of Dice)

    <DataMember>
    Public Property WordCount As Integer

    <DataMember>
    Public Property GameSerial As Long

End Class

<DataContract>
Public Class Dice

    <DataMember>
    Public Property Index As Integer

    <DataMember>
    Public Property Rotation As Integer

    <DataMember>
    Public Property Letter As String

End Class

<DataContract>
Public Class Dices
    Inherits List(Of Dice)

End Class

<DataContract>
Public Class Word

    <DataMember>
    Public Property DicePath As List(Of Dice)


End Class

<DataContract>
Public Class WordList

    <DataMember>
    Public Property Items As List(Of Word)

End Class

<DataContract>
Public Class Player


    <DataMember>
    Public Property ID As String

    <DataMember>
    Public Property NickName As String

    <DataMember>
    Public Property Rank As Integer

    <DataMember>
    Public Property Score As Integer

    <DataMember>
    Public Property Record As Integer

    <DataMember>
    Public Property TotalRoundPlayed As Integer

    <DataMember>
    Public Property TotalWinningRound As Integer

    <DataMember>
    Public Property WinPercent As Double

    <DataMember>
    Public Property IsLocal As Boolean

    <DataMember>
    Public Property IsGuest As Boolean

    <DataMember>
    Public Property WordList As WordList

    <DataMember>
    Public Property TotalWordsCount As Integer

    <DataMember>
    Public Property IsReady As Boolean

End Class

<DataContract>
Public Class Players

    Public Items As List(Of Player)

End Class

