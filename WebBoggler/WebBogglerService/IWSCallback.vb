
imports System.ServiceModel.Channels
imports System.Threading.Tasks

<ServiceContract>
Interface IWSCallback

    <OperationContract(IsOneWay:=True, Action:="*")>
    Function SendMessageToClient(msg As Message) As Task

End Interface



