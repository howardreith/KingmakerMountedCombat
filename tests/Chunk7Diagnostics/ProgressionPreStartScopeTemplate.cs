using System;
using System.IO;

class Unit { public object Descriptor = new object(), View = new object(); }
class WorldScope { public bool Refuse; public void RequireCurrent() { if (Refuse) throw new InvalidOperationException("world replaced"); } }
class Session
{
    public bool HasStarted, Refuse; public int Calls;
    public void AssertOwnedScope() { Calls++; if (Refuse) throw new InvalidOperationException("session binding or authority replaced"); }
}
class ActualProgressionPreStartScope
{
    private WorldScope scope = new WorldScope();
    private Session session;
    private bool captured = true, authorityRefused;
    private bool ownedProgression, ownedRosterRefused; private Unit[] ownedParty; private int ownedRosterCalls;
    // The complete actual owned-roster predicate is covered by OwnedProgressionRosterTemplate.
    private void RequireOwnedRoster(bool final) { ownedRosterCalls++; if (final || ownedRosterRefused) throw new InvalidOperationException("owned roster refused"); }
    private Unit rider = new Unit(), mount = new Unit();
    private object targetDescriptor, targetView, petDescriptor, petView;
    private string sourceEvidencePath, inventoryPath;
    private long sourceEvidenceLength, sourceEvidenceTicks, inventoryLength, inventoryTicks;
    private ActualProgressionPreStartScope()
    {
        targetDescriptor = rider.Descriptor; targetView = rider.View; petDescriptor = mount.Descriptor; petView = mount.View;
    }
    private void RequireAuthority(bool before) { if (authorityRefused) throw new InvalidOperationException("authority replaced"); }
    private static void Require(bool ok, string why) { if (!ok) throw new InvalidOperationException(why); }
/*REQUIRE_SCOPE*/
    private static int passes, failures;
    private static bool Refuses(Action action) { try { action(); return false; } catch (InvalidOperationException) { return true; } }
    private static void Check(string name, Func<ActualProgressionPreStartScope, bool> test)
    {
        var value = new ActualProgressionPreStartScope(); bool ok = false;
        try { ok = test(value); } catch (Exception e) { Console.WriteLine("EXCEPTION " + e); }
        if (ok) { passes++; Console.WriteLine("PASS " + name); } else { failures++; Console.WriteLine("FAIL " + name); }
    }
    public static int Main()
    {
        Check("unchanged target before child construction", a => { a.RequireScope(); return true; });
        Check("unchanged constructed unstarted child retains authority", a => { a.session = new Session(); a.RequireScope(); return a.session.Calls == 1; });
        Check("unstarted child cannot hide rider descriptor replacement", a => { a.session = new Session(); a.rider.Descriptor = new object(); return Refuses(a.RequireScope) && a.session.Calls == 1; });
        Check("unstarted child cannot hide rider view replacement", a => { a.session = new Session(); a.rider.View = new object(); return Refuses(a.RequireScope) && a.session.Calls == 1; });
        Check("unstarted child cannot hide pet descriptor replacement", a => { a.session = new Session(); a.mount.Descriptor = new object(); return Refuses(a.RequireScope) && a.session.Calls == 1; });
        Check("unstarted child cannot hide pet view replacement", a => { a.session = new Session(); a.mount.View = new object(); return Refuses(a.RequireScope) && a.session.Calls == 1; });
        Check("null child still refuses original binding replacement", a => { a.mount.View = new object(); return Refuses(a.RequireScope); });
        Check("started native child owns reviewed rebind admission", a => { a.session = new Session { HasStarted = true }; a.rider.Descriptor = new object(); a.rider.View = new object(); a.mount.View = null; a.RequireScope(); return a.session.Calls == 1; });
        Check("started child refusal remains authoritative", a => { a.session = new Session { HasStarted = true, Refuse = true }; return Refuses(a.RequireScope) && a.session.Calls == 1; });
        Check("unstarted child still checks own authority", a => { a.session = new Session { Refuse = true }; return Refuses(a.RequireScope) && a.session.Calls == 1; });
        Check("outer authority refusal precedes child", a => { a.session = new Session(); a.authorityRefused = true; return Refuses(a.RequireScope) && a.session.Calls == 0; });
        Check("changed world refusal precedes child", a => { a.session = new Session(); a.scope.Refuse = true; return Refuses(a.RequireScope) && a.session.Calls == 0; });
        Check("uncaptured admission has no target pin to release", a => { a.captured = false; a.rider.Descriptor = new object(); a.RequireScope(); return true; });
        Check("owned captured roster delegates nonfinal preservation", a => { a.ownedProgression = true; a.ownedParty = new Unit[0]; a.RequireScope(); return a.ownedRosterCalls == 1; });
        Check("owned source before roster binding has no synthetic membership", a => { a.ownedProgression = true; a.RequireScope(); return a.ownedRosterCalls == 0; });
        Check("owned roster refusal is retained after exact session authority", a => { a.ownedProgression = true; a.ownedParty = new Unit[0]; a.ownedRosterRefused = true; a.session = new Session(); return Refuses(a.RequireScope) && a.ownedRosterCalls == 1 && a.session.Calls == 1; });
        Console.WriteLine("ACTUAL PROGRESSION PRESTART SCOPE PASS " + passes + " FAIL " + failures); return failures == 0 ? 0 : 1;
    }
}
