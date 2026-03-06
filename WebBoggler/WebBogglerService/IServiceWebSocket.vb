Imports System.ServiceModel.Channels
Imports System.Threading.Tasks

' NOTA: è possibile utilizzare il comando "Rinomina" del menu di scelta rapida per modificare il nome di interfaccia "IService1" nel codice e nel file di configurazione contemporaneamente.
'<XmlSerializerFormat>
<ServiceContract(CallbackContract:=GetType(IWSCallback))>
Public Interface IServiceWebSocket

    <OperationContract(IsOneWay:=True, Action:="*")>
    Function MessageListener(msg As Message) As Task

End Interface


'End Class
