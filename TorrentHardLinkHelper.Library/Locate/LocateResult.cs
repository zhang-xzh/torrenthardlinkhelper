using System.Collections.Generic;
using System.Linq;

namespace TorrentHardLinkHelper.Locate;

public class LocateResult
{
    public LocateResult(IList<TorrentFileLink> torrentFileLinks)
    {
        TorrentFileLinks = torrentFileLinks;
        LocateState = TorrentFileLinks.Any(c => c.State != LinkState.Located)
            ? LocateState.Fail
            : LocateState.Succeed;
        if (LocateState == LocateState.Succeed)
        {
            LocatedCount = TorrentFileLinks.Count;
            UnlocatedCount = 0;
        }
        else
        {
            LocatedCount = TorrentFileLinks.Count(c => c.State == LinkState.Located);
            UnlocatedCount = TorrentFileLinks.Count - LocatedCount;
        }
    }

    public IList<TorrentFileLink> TorrentFileLinks { get; }

    public LocateState LocateState { get; }

    public int LocatedCount { get; }

    public int UnlocatedCount { get; }
}