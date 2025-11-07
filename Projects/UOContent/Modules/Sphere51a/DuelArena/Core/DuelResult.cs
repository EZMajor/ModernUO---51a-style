using System;
using System.Collections.Generic;

namespace Server.Engines.DuelArena;

public sealed record DuelResult(
    Serial ContextSerial,
    DateTime CompletedAt,
    DuelType Type,
    List<DuelParticipant> Winners,
    List<DuelParticipant> Losers,
    TimeSpan Duration,
    int GoldPot
);
