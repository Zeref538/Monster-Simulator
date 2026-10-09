using System;
class Rules
{
    static int checks;
    static void Check(bool result, string rule) { checks++; if (!result) throw new Exception(rule); }
    static void Main()
    {
        var m = new MonsterState(); m.Tick(); Check(m.HappinessHundredths == 7499, "Exact passive decay");
        m = new MonsterState(); Check(m.Act(0) && m.HappinessHundredths == 7550 && m.Stamina == 90, "Play");
        Check(m.Act(1) && m.HappinessHundredths == 7500 && m.Stamina == 80, "Study");
        m = new MonsterState(9950); m.Act(0); Check(m.Mood == 1 && m.Happiness == 100, "Happy at 100");
        m.Tick(); Check(m.Mood == 0 && m.HappinessHundredths == 9999, "Happy ends at next tick");
        m = new MonsterState(5050); m.Act(1); Check(m.Mood == 2 && m.Happiness == 50, "Sad at 50");
        Check(new MonsterState(5001).Mood == 0 && new MonsterState(4999).Mood == 2, "Sad boundary");
        m = new MonsterState(7500,20); Check(m.CanWork && !m.CanSleep && !m.Act(3), "20 boundary");
        m.Act(1); Check(m.Stamina == 10 && !m.CanWork && m.CanSleep, "Below 20 lock");
        int value = m.HappinessHundredths; Check(!m.Act(0) && !m.Act(1) && m.HappinessHundredths == value, "Blocked actions preserve state");
        Check(m.Act(3) && m.Stamina == 100 && !m.CanSleep && m.CanWork, "Sleep restores and relocks");
        m = new MonsterState(9700,95); m.Act(2); Check(m.Happiness == 100 && m.Stamina == 100, "Eat adds and clamps");
        m = new MonsterState(1); m.Tick(); m.Tick(); Check(m.HappinessHundredths == 0, "Decay floor");
        Check(!m.Act(99), "Invalid action");
        Console.WriteLine("PASS: " + checks + " C# logic assertions.");
    }
}
