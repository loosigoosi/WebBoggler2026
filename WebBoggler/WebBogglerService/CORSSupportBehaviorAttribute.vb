' Simply apply this attribute to a DataService-derived class to get
' CORS support in that service
Imports System.ServiceModel.Channels
Imports System.ServiceModel.Description
Imports System.ServiceModel.Dispatcher

<AttributeUsage(AttributeTargets.Class)>
Public Class CORSSupportBehaviorAttribute
    Inherits Attribute
    Implements IServiceBehavior
#Region "IServiceBehavior Members"

    Private Sub AddBindingParameters(serviceDescription As ServiceDescription, serviceHostBase As ServiceHostBase, endpoints As System.Collections.ObjectModel.Collection(Of ServiceEndpoint), bindingParameters As BindingParameterCollection) Implements IServiceBehavior.AddBindingParameters
    End Sub

    Private Sub ApplyDispatchBehavior(serviceDescription As ServiceDescription, serviceHostBase As ServiceHostBase) Implements IServiceBehavior.ApplyDispatchBehavior
        Dim requiredHeaders = New Dictionary(Of String, String)()

        'Chrome doesn't accept wildcards when authorization flag is true
        'requiredHeaders.Add("Access-Control-Allow-Origin", "*")
        requiredHeaders.Add("Access-Control-Request-Method", "POST,GET,PUT,DELETE,OPTIONS")
        requiredHeaders.Add("Access-Control-Allow-Headers", "Accept, Origin, Authorization, X-Requested-With,Content-Type, SOAPAction")
        requiredHeaders.Add("Access-Control-Allow-Credentials", "true")
        requiredHeaders.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE")
        requiredHeaders.Add("Access-Control-Max-Age", "1728000")
        For Each cd As ChannelDispatcher In serviceHostBase.ChannelDispatchers
            For Each ed As EndpointDispatcher In cd.Endpoints
                ed.DispatchRuntime.MessageInspectors.Add(New CORSSupport(requiredHeaders))
            Next
        Next
    End Sub

    Private Sub Validate(serviceDescription As ServiceDescription, serviceHostBase As ServiceHostBase) Implements IServiceBehavior.Validate
    End Sub

#End Region
End Class