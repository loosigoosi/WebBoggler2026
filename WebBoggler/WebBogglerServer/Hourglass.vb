Imports System.Threading

Public Class Hourglass

    Public Class HourglassTickEventArgs : Inherits EventArgs
        Public ReadOnly ElapsedTime As TimeSpan

        Public Sub New(ByVal elapsedTime As TimeSpan)
            Me.ElapsedTime = elapsedTime
        End Sub
    End Class 'TickEventArgs


    Private ReadOnly _asyncTimer As New System.Threading.Timer(AddressOf AsyncTimer_Callback, Nothing, Timeout.Infinite, Timeout.Infinite)

    Private Const DefaultIntervalMs = 100 'default di 100 ms
    Private _startTime As DateTime = Nothing
    Private _pauseTime As DateTime = Nothing
    Private _lastSecondElapsed As DateTime = Nothing
    Private ReadOnly _oneSecondTimeSpan As New TimeSpan(0, 0, 1)
    Private _perpetual As Boolean
    Private _duration As TimeSpan
    Private _isRunning As Boolean


    Public Sub New()
        _asyncTimer.Change(System.Threading.Timeout.Infinite, DefaultIntervalMs)
        _duration = New TimeSpan(0, 0, 1, 0, 0) 'default di 1'00"

    End Sub

    Public Property Perpetual As Boolean
        Get
            Return _perpetual
        End Get
        Set(value As Boolean)
            _perpetual = value
        End Set
    End Property

    Public ReadOnly Property ElapsedTime() As TimeSpan
        Get
            If IsRunning Then
                Return Now - _startTime

            Else
                If _pauseTime <> Nothing Then
                    Return _pauseTime - _startTime
                Else
                    Return Nothing
                End If
            End If
        End Get
    End Property

    Public ReadOnly Property ElapsedPercent() As Byte
        Get
            If _startTime <> Nothing Then
                Return Int((ElapsedTime).Ticks / _duration.Ticks) * 100

            Else
                Return 0
            End If
        End Get
    End Property

    Public Property Duration() As TimeSpan
        Get
            Return _duration
        End Get
        Set(ByVal value As TimeSpan)
            _duration = value
        End Set
    End Property

    Public ReadOnly Property RemainingTime() As TimeSpan
        Get
            Return (Duration - ElapsedTime)
        End Get
    End Property


    Public Property IsRunning() As Boolean
        Get
            Return _isRunning
        End Get
        Set(ByVal value As Boolean)
            _isRunning = value
        End Set
    End Property

    Public Sub Run()
        If _pauseTime = Nothing Then
            _startTime = Now
        Else
            _startTime = Now - (_pauseTime - _startTime)
            _pauseTime = Nothing
        End If
        _asyncTimer.Change(0, DefaultIntervalMs)
        _isRunning = True
    End Sub

    Public Sub Pause()

        _isRunning = False
        _pauseTime = Now
        _asyncTimer.Change(Timeout.Infinite, Timeout.Infinite)
    End Sub

    Public Sub Reset()
        _isRunning = False
        _pauseTime = Nothing
        _asyncTimer.Change(Timeout.Infinite, Timeout.Infinite)
    End Sub


    Public Delegate Sub ElapsedTenthDelegate(ByVal sender As Object, ByVal e As HourglassTickEventArgs)
    Public Event ElapsedTenth As EventHandler(Of HourglassTickEventArgs)
    Protected Overridable Sub OnElapsedTenth(ByVal sender As Object, ByVal e As HourglassTickEventArgs)
        RaiseEvent ElapsedTenth(Me, e)
    End Sub

    Public Delegate Sub ElapsedSecondDelegate(ByVal sender As Object, ByVal e As HourglassTickEventArgs)
    Public Event ElapsedSecond As EventHandler(Of HourglassTickEventArgs)
    Protected Overridable Sub OnElapsedSecond(ByVal sender As Object, ByVal e As HourglassTickEventArgs)
        RaiseEvent ElapsedSecond(Me, e)
    End Sub

    Public Event TimeExpired As EventHandler
    Protected Overridable Sub OnTimeExpired()
        RaiseEvent TimeExpired(Me, EventArgs.Empty)
    End Sub

    Private Sub AsyncTimer_Callback(ByVal state As Object)
        'to try a thread-unsafe call, comment out the line before, 
        ' and uncomment the line after
        'OnTick(New TickEventArgs(_Counter))
        If ((Now - _startTime) >= _duration) And (Not _perpetual) Then
            Reset()
            RaiseEvent TimeExpired(Me, Nothing)
            'InvokeAction(AddressOf OnTimeExpired, System.EventArgs.Empty)					 'raise TimeExpired
        Else
            'RaiseEvent ElapsedTenth(Me, ElapsedTime, New EventArgs)
            If (Now - _lastSecondElapsed) < _oneSecondTimeSpan Then
                RaiseEvent ElapsedTenth(Me, New HourglassTickEventArgs(ElapsedTime))
                'InvokeAction(AddressOf OnElapsedTenth, New HourglassTickEventArgs(ElapsedTime))	 'raise Tick
            Else
                _lastSecondElapsed = Now
                RaiseEvent ElapsedSecond(Me, New HourglassTickEventArgs(ElapsedTime))
                'InvokeAction(AddressOf OnElapsedSecond, New HourglassTickEventArgs(ElapsedTime))	  'raise Tick	
            End If
        End If
    End Sub

    Protected Overrides Sub Finalize()
        _asyncTimer.Dispose()
        MyBase.Finalize()
    End Sub
End Class
