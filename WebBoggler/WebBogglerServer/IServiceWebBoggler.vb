
' NOTA: è possibile utilizzare il comando "Rinomina" del menu di scelta rapida per modificare il nome di interfaccia "IService1" nel codice e nel file di configurazione contemporaneamente.
Imports System.ServiceModel.Web

<XmlSerializerFormat>
<ServiceContract()>
Public Interface IServiceWebBoggler

    <OperationContract()>
    Function GetBoard(ByVal localeID As String) As Board

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
Public Class Board

    <DataMember>
    Public Property LocaleID As String

    <DataMember>
    Public ReadOnly Property Language As String

    <DataMember>
    Public Property DicesVector As List(Of Dice)

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


'End Class
