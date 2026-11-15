namespace Vidi;

internal sealed class VideoHostPanel : Panel
{
    private const int WsClipChildren = 0x02000000;
    private const int WsClipSiblings = 0x04000000;

    public VideoHostPanel()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.ResizeRedraw
            | ControlStyles.UserPaint,
            true);
        DoubleBuffered = true;
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var createParams = base.CreateParams;
            createParams.Style |= WsClipChildren | WsClipSiblings;
            return createParams;
        }
    }
}
