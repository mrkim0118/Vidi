namespace Vidi;

// Where the current playlist item ends up after the list changes. Pure, so it is tested.
public static class PlaylistIndex
{
    public static int AfterMove(int current, int from, int to)
    {
        if (current == from)
        {
            return to;
        }

        if (from < current && to >= current)
        {
            return current - 1;
        }

        if (from > current && to <= current)
        {
            return current + 1;
        }

        return current;
    }

    // Removing the current item selects the item that took its place (or the new last one).
    public static int AfterRemove(int current, int removed, int newCount)
    {
        if (newCount == 0)
        {
            return -1;
        }

        if (removed < current)
        {
            return current - 1;
        }

        return removed == current ? Math.Min(removed, newCount - 1) : current;
    }
}
