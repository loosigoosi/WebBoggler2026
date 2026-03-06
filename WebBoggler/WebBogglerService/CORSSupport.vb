

Imports System.ServiceModel.Channels
Imports System.ServiceModel.Dispatcher

Public Class CORSSupport
    Implements IDispatchMessageInspector
    Private requiredHeaders As Dictionary(Of String, String)
    Public Sub New(headers As Dictionary(Of String, String))
        requiredHeaders = If(headers, New Dictionary(Of String, String)())
    End Sub

    Public Function AfterReceiveRequest(ByRef request As Message, channel As System.ServiceModel.IClientChannel, instanceContext As InstanceContext) As Object Implements IDispatchMessageInspector.AfterReceiveRequest
        If Not request.Properties.ContainsKey(HttpRequestMessageProperty.Name) Then

            request.Properties.Add(HttpRequestMessageProperty.Name, New HttpRequestMessageProperty())
        End If

        Dim httpRequest = TryCast(request.Properties("httpRequest"), HttpRequestMessageProperty)
        ' Do not abort the instance context for OPTIONS here - aborting can trigger thread aborts
        ' and interfere with the ASP.NET pipeline. Global.asax handles OPTIONS preflight responses.
        Return httpRequest
    End Function

    Public Sub BeforeSendReply(ByRef reply As System.ServiceModel.Channels.Message, correlationState As Object) Implements IDispatchMessageInspector.BeforeSendReply

        If Not reply.Properties.ContainsKey(HttpResponseMessageProperty.Name) Then

            reply.Properties.Add(HttpResponseMessageProperty.Name, New HttpResponseMessageProperty())
        End If

        Try
            Dim httpResponse = TryCast(reply.Properties("httpResponse"), HttpResponseMessageProperty)
            Dim httpRequest = TryCast(correlationState, HttpRequestMessageProperty)

            For Each item As Object In requiredHeaders
                httpResponse.Headers.Add(item.Key, item.Value)
            Next
            Dim origin = httpRequest.Headers("origin")
            If origin IsNot Nothing Then
                httpResponse.Headers.Add("Access-Control-Allow-Origin", origin)
            End If

            Dim method = httpRequest.Method
            If method.ToLower() = "options" Then
                httpResponse.StatusCode = System.Net.HttpStatusCode.NoContent
            End If
        Catch ex As Exception

        End Try
    End Sub
End Class