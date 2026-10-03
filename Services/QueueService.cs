using RallyQueue.Models;

namespace RallyQueue.Services;

public class QueueService
{
    int _id, _order;

    public List<Player> Players { get; } = new();
    public List<Match> Queue { get; } = new();
    public List<Court> Courts { get; } = new() { new Court { Number = 1 }, new Court { Number = 2 } };

    public string Sport { get; private set; } = "Pickleball";
    public int PerGame { get; private set; } = 4;     // 4 = doubles, 2 = singles
    public int QueueTarget { get; set; } = 2;         // matches kept lined up
    public bool AutoQueue { get; private set; } = true;

    // Raised after every change so every open browser/tab can refresh.
    public event Action? Changed;
    void Notify() => Changed?.Invoke();

    public void SetSport(string sport) { Sport = sport; Notify(); }
    public void SetAutoQueue(bool on) { AutoQueue = on; Fill(); }

    public bool IsQueued(Player p) => Queue.Any(m => m.Players.Contains(p));
    public bool IsPlaying(Player p) => Courts.Any(c => c.Current?.Players.Contains(p) == true);

    public string StatusOf(Player p) =>
        IsPlaying(p) ? "Playing" : IsQueued(p) ? "Queued" : p.Resting ? "Resting" : "Waiting";

    // Fairness rule: fewest games first, then whoever has waited longest.
    public List<Player> Waiting => Players
        .Where(p => !p.Resting && !IsQueued(p) && !IsPlaying(p))
        .OrderBy(p => p.Games).ThenBy(p => p.Order).ToList();

    public void AddPlayer(string name)
    {
        name = name.Trim();
        if (name.Length == 0) return;
        // A latecomer joins at the current minimum so they don't monopolise the queue.
        var start = Players.Count == 0 ? 0 : Players.Min(p => p.Games);
        Players.Add(new Player { Id = ++_id, Name = name, Games = start, Order = ++_order });
        Fill();
    }

    public bool RemovePlayer(Player p)
    {
        if (IsPlaying(p)) return false;
        DissolveMatchOf(p);
        Players.Remove(p);
        Fill();
        return true;
    }

    public void SetResting(Player p, bool resting)
    {
        if (IsPlaying(p)) return;
        p.Resting = resting;
        if (resting) DissolveMatchOf(p);
        p.Order = ++_order;
        Fill();
    }

    public void AdjustGames(Player p, int delta)
    {
        p.Games = Math.Max(0, p.Games + delta);
        Notify();
    }

    void DissolveMatchOf(Player p)
    {
        var m = Queue.FirstOrDefault(m => m.Players.Contains(p));
        if (m != null) Queue.Remove(m);
    }

    public bool QueueMatch() { var ok = TryBuildMatch(); Notify(); return ok; }

    bool TryBuildMatch()
    {
        var w = Waiting;
        if (w.Count < PerGame) return false;
        Queue.Add(new Match { Players = w.Take(PerGame).ToList() });
        return true;
    }

    public void Fill()
    {
        if (AutoQueue)
            while (Queue.Count < QueueTarget && TryBuildMatch()) { }
        Notify();
    }

    public void Swap(Match m, Player oldP, Player newP)
    {
        var i = m.Players.IndexOf(oldP);
        if (i < 0 || IsQueued(newP) || IsPlaying(newP)) return;
        m.Players[i] = newP;
        oldP.Order = 0;     // bumped out: first in line for the next match at their game count
        Fill();
    }

    public void RemoveMatch(Match m) { Queue.Remove(m); Fill(); }

    public void Start(Court c)
    {
        if (c.Current != null || Queue.Count == 0) return;
        c.Current = Queue[0];
        Queue.RemoveAt(0);
        Fill();
    }

    public void Finish(Court c)
    {
        if (c.Current == null) return;
        foreach (var p in c.Current.Players) { p.Games++; p.Order = ++_order; }
        c.Current = null;
        Fill();
    }

    public void SetFormat(int perGame)
    {
        PerGame = perGame;
        Queue.Clear();
        Fill();
    }

    public void SetCourts(int n)
    {
        while (Courts.Count < n) Courts.Add(new Court { Number = Courts.Count + 1 });
        while (Courts.Count > n && Courts[^1].Current == null) Courts.RemoveAt(Courts.Count - 1);
        Notify();
    }
}
