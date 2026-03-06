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
        Dim ctx = HttpContext.Current
        If ctx Is Nothing Then Return

        ' Log every BeginRequest to temp file for diagnostics
        Try
            Dim req = ctx.Request
            Dim resp = ctx.Response
            Dim logPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "wbg_cors_log.txt")
            Dim origin As String = If(req.Headers("Origin"), "")
            Dim line As String = String.Format("{0:u} | {1} {2} | Origin:{3} | URL:{4}{5}", DateTime.Now, req.HttpMethod, req.RawUrl, origin, req.Url.ToString(), Environment.NewLine)
            System.IO.File.AppendAllText(logPath, line)

            ' Existing CORS handling: ensure response contains a single Access-Control-Allow-Origin
            If Not String.IsNullOrEmpty(origin) Then
                If Not String.IsNullOrEmpty(resp.Headers("Access-Control-Allow-Origin")) Then
                    resp.Headers.Remove("Access-Control-Allow-Origin")
                End If
                resp.AddHeader("Access-Control-Allow-Origin", origin)
            End If

            If String.Equals(req.HttpMethod, "OPTIONS", StringComparison.OrdinalIgnoreCase) Then
                If Not String.IsNullOrEmpty(resp.Headers("Access-Control-Allow-Methods")) Then resp.Headers.Remove("Access-Control-Allow-Methods")
                resp.AddHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS")

                If Not String.IsNullOrEmpty(resp.Headers("Access-Control-Allow-Headers")) Then resp.Headers.Remove("Access-Control-Allow-Headers")
                resp.AddHeader("Access-Control-Allow-Headers", "Content-Type, Accept, SOAPAction")

                If Not String.IsNullOrEmpty(resp.Headers("Access-Control-Max-Age")) Then resp.Headers.Remove("Access-Control-Max-Age")
                resp.AddHeader("Access-Control-Max-Age", "1728000")

                resp.StatusCode = 200
                ' Avoid ThreadAbortException caused by Response.End();
                ' CompleteRequest signals ASP.NET to skip remaining pipeline and finish the request cleanly.
                HttpContext.Current.ApplicationInstance.CompleteRequest()
            End If
        Catch ex As Exception
            ' Log exceptions related to BeginRequest diagnostics
            Try
                Dim errPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "wbg_cors_log_errors.txt")
                System.IO.File.AppendAllText(errPath, DateTime.Now.ToString("u") & " | BeginRequest error: " & ex.ToString() & Environment.NewLine)
            Catch
            End Try
        End Try
    End Sub</script>