# Звіт по StyleCop

Перш за все, хочеться наголосити на тому, що статичні аналізатори варто
додавати у проекти ще до того, як був написаний хоч якийсь код.

Адже набагато простіше виправляти 1-2 warning-и з кожним комітом, ніж потім
виправляти таке:

`Build succeeded with 771 warning(s) in 6.5s`

## Вимкнення деяких warning-ів
У нашому проекті вимкнено два warning-и:

```csharp
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("StyleCop.CSharp.NamingRules", "SA1309:FieldNamesMustNotBeginWithUnderscore", Justification = "Reviewed.")]
[assembly: SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:FieldNamesMustNotContainUnderscore", Justification = "Reviewed.")]
```

Обидва забороняють використання нижнього підкреслення у назвах полів класу.
Перше - перед іменем, друге - загалом. Враховуючи, що зараз у C#
прийнято ставити на початку назв приватних імен нижнє підкреслення,
ми вирішили заглушити ці warning-и.

## Автоформатування

Дуже корисна можливіть, яка виправила відразу кілька десятків warning-ів:

`dotnet format --diagnostics SA1101,SA1200,SA1413`

Де номери warning-ів можна задавати через кому.

## Детальніше про warning-и, що нам зустрілись

### SA1503 | Фігурні дужки після if/for/while
Після `if/for/while` має бути блок у фігурних дужках, навіть якщо він з одного рядка.

```csharp
// Bogar.BLL.Networking.GameServer.TryKickClient
// Погано
if (this._clientsInGame.Contains(clientId))
    this._clientsInGame.Remove(clientId);

// Добре
if (this._clientsInGame.Contains(clientId))
{
    this._clientsInGame.Remove(clientId);
}
```

### SA1519 | Кілька операторів без фігурних дужок
Без дужок тільки перший рядок входить у блок, решта виконуються завжди.

```csharp
// Bogar.UI.WindowNavigationHelper.Replace
// Погано
if (currentWindow == null)
    throw new ArgumentNullException(nameof(currentWindow));
nextWindow.Show();
currentWindow.Close();

// Добре
if (currentWindow == null)
{
    throw new ArgumentNullException(nameof(currentWindow));
}

nextWindow.Show();
currentWindow.Close();
```

### SA1501 | Конструкція з блоком в одному рядку

```csharp
// Bogar.BLL.Networking.GameServer.StopMatch
// Погано
if (this._matchControllers.TryGetValue((whiteId, blackId), out var controller)) { controller.MatchCancellation.Cancel(); return true; }

// Добре
if (this._matchControllers.TryGetValue((whiteId, blackId), out var controller))
{
    controller.MatchCancellation.Cancel();
    return true;
}
```

### SA1101 | Використання this для полів і методів екземпляра

```csharp
// Bogar.BLL.Networking.GameServer.TryKickClient
// Погано
if (!_clients.TryRemove(clientId, out var client))
{
    return false;
}

// Добре
if (!this._clients.TryRemove(clientId, out var client))
{
    return false;
}
```

### SA1407 | Явний порядок у арифметичних виразах

```csharp
// Bogar.BLL.Core.SquareExtensions.Parse
// Погано
return (Square)(rank * 8 + file);

// Добре
return (Square)((rank * 8) + file);
```

### SA1116 / SA1117 / SA1111 | Форматування списку параметрів

```csharp
// Bogar.UI.AdminWaitingRoomWindow.TryKickClient
// Погано
MessageBox.Show("Unable to kick the selected player.",
    "Kick player",
    MessageBoxButton.OK,
    MessageBoxImage.Information
    );

// Добре
MessageBox.Show(
    "Unable to kick the selected player.",
    "Kick player",
    MessageBoxButton.OK,
    MessageBoxImage.Information);
```

### SA1202 / SA1201 / SA1204 / SA1214 | Порядок членів класу

Порядок оголошень:
1. Статичні члени
2. Поля `readonly`
3. Інші поля
4. Конструктори
5. Публічні члени
6. Приватні допоміжні члени

### SA1200 / SA1208 / SA1210 | Порядок і розміщення using

- `using` всередині `namespace`.  
- Спочатку простори імен `System.*`.  
- У кожній групі — алфавітне сортування.

### SA1136 | Окремий рядок для кожного enum-значення

```csharp
// Bogar.BLL.Core.Square
// Добре
public enum Square
{
    SQ_NONE = -1,
    A1 = 0,
    B1,
    C1,
    D1,
    // ...
    H8,
    SQ_COUNT,
}
```

### SA1413 | Кома після останнього елемента в багаторядкових ініціалізаторах

```csharp
// Bogar.BLL.Networking.GameServer.RunGameAsync
var result = new MatchResult
{
    WhiteClientId = white.Id,
    WhiteNickname = white.Nickname,
    BlackClientId = black.Id,
    BlackNickname = black.Nickname,
    Moves = moves,
    WhiteScore = whiteScore,
    BlackScore = blackScore,
};
```

### SA1513 / SA1516 | Порожній рядок після блоків і між оголошеннями

```csharp
// Bogar.UI.WaitingRoomWindow
// Погано
public string LobbyName { get; set; }
public void AddStatus(string message)
{
    StatusText.Text = message;
}

// Добре
public string LobbyName { get; set; }

public void AddStatus(string message)
{
    this.StatusText.Text = message;
}

```

### SA1009 | Пробіл перед закриваючою дужкою

```csharp
// Bogar.BLL.Networking.GameClient
// Погано
if (tcpClient.Connected )
{
    Logger.Warning("Socket connection lost");
}

// Добре
if (tcpClient.Connected)
{
    Logger.Warning("Socket connection lost");
}
```

### SA1011 | Пробіл після закриваючої квадратної дужки

```csharp
// Погано
[Obsolete]public ...
// Добре
[Obsolete] public ...
```

### SA1000 | Пробіл після new

```csharp
// Bogar.BLL.Networking.GameServer
// Погано
private readonly ConcurrentDictionary<Guid, ConnectedClient> _clients = new();

// Добре
private readonly ConcurrentDictionary<Guid, ConnectedClient> _clients = new ();
```

### SA1119 | Зайві круглі дужки у виразах

```csharp
// Bogar.BLL.Core.Position.CalculateScore
// Погано
if (((occupied & sqBitboard) == 0))
{
    continue;
}

// Добре
if ((occupied & sqBitboard) == 0)
{
    continue;
}
```

### SA1028 | Пробіли в кінці рядка

Рядок не має закінчуватись пробілами або табами.
