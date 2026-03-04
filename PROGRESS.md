# WebBoggler - Progress Tracker

**Last Updated**: 2025-01-XX (aggiorna data)
**Current Branch**: Copilot-patched
**Status**: Server-side complete, Client-side pending

---

## 🎯 Current Milestone: Board Discard Feature

### ✅ Completed (Server-Side)

#### **RoomMaster.cs**
- [x] Costante `DISCARD_ALLOWED_TIME_MS = 15000` (15 secondi)
- [x] Timer `_discardAllowedTimer` per finestra temporale
- [x] Flag `_discardAllowed` per controllo tempo
- [x] Metodo `SetPlayerDiscardState(clientID, wantsDiscard)` - imposta voto giocatore
- [x] Metodo `CheckDiscard()` - verifica consenso unanime
- [x] Event `BoardDiscarded` per notifiche
- [x] Proprietà `DiscardAllowed` per query client
- [x] Integrazione in ciclo `CheckPlayersCycleTimer_Elapsed()`
- [x] Reset `WantsDiscard` in `OnPlayerJoined()`, `EndRound()`, `DiscardAllowedTimer_Elapsed()`

#### **Player.cs (DTO Model)**
- [x] Proprietà `WantsDiscard` aggiunta con `[DataMember]`

#### **GameHub.cs**
- [x] Metodo `ProposeDiscard(bool wantsDiscard)` - riceve richiesta client
- [x] Metodo `IsDiscardAllowed()` - query per verificare tempo disponibile
- [x] Mapping `ConnectionId → ClientID` applicato correttamente

#### **Program.cs**
- [x] Evento `BoardDiscarded` sottoscritto (log only)

#### **Build Status**
- [x] Server compila senza errori
- [x] Tutti i timer orchestrati correttamente

---

### 📋 Pending (Client-Side)

#### **ServiceConnector.cs** (DA FARE)
```csharp
// Aggiungere metodi:
public async Task ProposeDiscard(bool wantsDiscard)
{
    await _connection.InvokeAsync("ProposeDiscard", wantsDiscard);
}

public async Task<bool> IsDiscardAllowed()
{
    return await _connection.InvokeAsync<bool>("IsDiscardAllowed");
}
```

#### **MainPage.xaml** (DA FARE)
```xaml
<!-- Aggiungere checkbox dopo chkReady -->
<CheckBox x:Name="chkDiscard" 
          Content="Proposta di scarto" 
          Checked="chkDiscard_Checked" 
          Unchecked="chkDiscard_Unchecked"
          Visibility="Collapsed" />
```

#### **Desk.cs** (DA FARE)
1. **Gestione visibilità checkbox**:
   - Mostra `chkDiscard` solo durante `RunningRound`
   - Nascondi dopo 15 secondi o quando `IsDiscardAllowed == false`

2. **Event handlers**:
   ```csharp
   private async void chkDiscard_Checked(object sender, RoutedEventArgs e)
   {
       await _connector.ProposeDiscard(true);
   }
   
   private async void chkDiscard_Unchecked(object sender, RoutedEventArgs e)
   {
       await _connector.ProposeDiscard(false);
   }
   ```

3. **UPDATE_PLAYERS handler** (modificare esistente):
   ```csharp
   case "UPDATE_PLAYERS":
       _Players = await UpdatePlayers();
       
       // Sync chkReady (esistente)
       if (_chkReady != null && _Players != null) {
           var localPlayer = _Players.FirstOrDefault(p => p.ID == _localPlayer.ID);
           if (localPlayer != null) {
               _chkReady.IsChecked = localPlayer.IsReady;
               
               // NUOVO: Sync chkDiscard
               if (_chkDiscard != null) {
                   _chkDiscard.IsChecked = localPlayer.WantsDiscard;
               }
           }
       }
       break;
   ```

4. **Gestione timer 15 secondi** (opzionale):
   ```csharp
   private DispatcherTimer _discardTimer;
   
   // In START_ROUND:
   _discardTimer = new DispatcherTimer();
   _discardTimer.Interval = TimeSpan.FromSeconds(15);
   _discardTimer.Tick += (s, e) => {
       _chkDiscard.Visibility = Visibility.Collapsed;
       _discardTimer.Stop();
   };
   _discardTimer.Start();
   chkDiscard.Visibility = Visibility.Visible;
   ```

---

## 🏗️ Architecture Overview

### **State Machine Flow con Discard**
```
Ready → SendingBoard → KeepReady → RunningRound
                                        ↓ (discard allowed: 15s)
                                    CheckDiscard()
                                        ↓ (all agree)
                                    KeepReady → RunningRound (new board)
                                        ↓ (timer expires)
                                    PauseAfterRound → ValidatingWordLists → ShowTime
```

### **Timer Orchestration**
- `_preStartDelayTimer` (3s) → Avvia `RunningRound` + `_discardAllowedTimer`
- `_discardAllowedTimer` (15s) → Scade permesso discard, reset `WantsDiscard`
- `_checkPlayersCycleTimer` (250ms) → Controlla `CheckPlayers()` + `CheckDiscard()`
- `_hourglassTimer` (60s debug/180s prod) → Fine round normale
- `_preValidationTimer` (3.5s) → Pausa prima validazione
- `_showTimeTimer` (60s) → Mostra risultati

### **Critical Patterns**

#### **Explicit WordList Conversion** (NON TOCCARE!)
```csharp
// In Desk.SendWordListAsync() - WORKING PATTERN
var proxyWordList = new WebBogglerServer.WordList {
    Items = _wordList.Select(w => new WebBogglerServer.Word {
        DicePath = w.DicePath?.Select(d => new WebBogglerServer.Dice {
            Index = d.Index, 
            Letter = d.Letter, 
            Rotation = d.Rotation
        }).ToArray()
    }).ToArray()
};
```

#### **ConnectionId Mapping** (SERVER AUTHORITY)
```csharp
// GameHub pattern - SEMPRE usare mapped ID
if (_connectionToClientId.TryGetValue(Context.ConnectionId, out var clientId))
{
    _roomMaster.SetPlayerDiscardState(clientId, wantsDiscard);
}
```

---

## 📝 Known Working Features

✅ Complete game cycle (Ready → Running → Validation → ShowTime)
✅ Player ready state synchronization (checkbox auto-sync)
✅ Second player join triggers full reset (scores=0, new board, serial++)
✅ Score accumulation in solo play
✅ Word submission with explicit type conversion
✅ Duplicate word detection (text-based)
✅ Scoring calculation (length-based: 3-4=1, 5=2, 6=3, 7=5, 8+=11)
✅ Results display
✅ Serial number persistence (never resets)

---

## 🐛 Optional Enhancements (Future)

- [ ] Implement `CheckWord()` with Lexicon validation (currently stubbed)
- [ ] Clean up diagnostic `Console.WriteLine` logging
- [ ] Test edge cases (disconnect, mid-round join, network failures)
- [ ] Add visual Dice XML controls for word lists

---

## 🔗 References

**Repository**: https://github.com/loosigoosi/WebBoggler2026
**Branch**: Copilot-patched
**Migration**: WCF → ASP.NET Core SignalR (.NET 8)

**Key Files**:
- Server: `WebBoggler.SignalRServer/Services/RoomMaster.cs`
- Server: `WebBoggler.SignalRServer/GameHub.cs`
- Client: `WebBoggler/Desk.cs`
- Client: `WebBoggler/ServiceConnector.cs`
- Client: `WebBoggler/MainPage.xaml`

---

## 💡 Resume Instructions for Copilot

Quando riprendi la sessione, usa questa frase:

> "Riprendo il lavoro su WebBoggler. Server-side della feature 'Proposta di scarto' è completo. 
> Devo implementare la parte client: checkbox in MainPage.xaml, metodi in ServiceConnector, 
> gestione visibilità e sync in Desk.cs. Vedi PROGRESS.md per dettagli."

Copilot riconoscerà il contesto dal summary della conversazione precedente e da questo documento.

---

**End of Progress Document**
