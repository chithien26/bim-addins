using AddinStore;
using Autodesk.Revit.UI;

namespace RebarMark;

public class RebarMarkApplication : IExternalApplication
{
    public const string Slug = "rebar-mark";

    public Result OnStartup(UIControlledApplication application)
    {
        RibbonHost.AddCommand(application, "Rebar Mark", "RebarMarkCommand", "Đánh dấu\nthép", typeof(MarkRebarCommand));
        return Result.Succeeded;
    }

    public Result OnShutdown(UIControlledApplication application) => Result.Succeeded;
}
