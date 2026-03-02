<%@ Application Language="VB" Debug="True" %>

<script runat="server">

    Sub Application_Start(ByVal sender As Object, ByVal e As EventArgs)
        ' Codice eseguito all\'avvio dell\'applicazione
    End Sub

    Sub Application_End(ByVal sender As Object, ByVal e As EventArgs)
        ' Codice eseguito all\'arresto dell\'applicazione
    End Sub

    Sub Application_Error(ByVal sender As Object, ByVal e As EventArgs)
        ' Codice eseguito in caso di errore non gestito
    End Sub

    Sub Session_Start(ByVal sender As Object, ByVal e As EventArgs)
        ' Codice eseguito all\'avvio di una nuova sessione
    End Sub

    Sub Session_End(ByVal sender As Object, ByVal e As EventArgs)
        ' Codice eseguito al termine di una sessione. 
        ' Nota: l\'evento Session_End viene generato solo quando la modalità sessionstate
        ' è impostata su InProc nel file Web.config. Se la modalità è impostata su StateServer 
        ' o SQLServer, l\'evento non viene generato.
    End Sub

    Sub Application_BeginRequest(sender As Object, e As EventArgs)
        HttpContext.Current.Response.AddHeader("Access-Control-Allow-Origin", "*")

        If HttpContext.Current.Request.HttpMethod = "OPTIONS" Then
            'These headers are handling the "pre-flight" OPTIONS call sent by the browser
            HttpContext.Current.Response.AddHeader("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE")
            HttpContext.Current.Response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Accept, SOAPAction")
            HttpContext.Current.Response.AddHeader("Access-Control-Max-Age", "1728000")
            HttpContext.Current.Response.End()
        End If
    End Sub</script>