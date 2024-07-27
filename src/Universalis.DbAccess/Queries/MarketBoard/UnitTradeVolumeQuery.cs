using System;

namespace Universalis.DbAccess.Queries.MarketBoard;

public record TradeVelocityQuery(
    string WorldIdDcRegion,
    int ItemId,
    DateOnly From,
    DateOnly To
);
