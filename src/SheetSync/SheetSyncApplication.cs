using AddinStore;
using Autodesk.Revit.UI;

namespace SheetSync;

public class SheetSyncApplication : IExternalApplication
{
    public const string Slug = "sheet-sync";

    public Result OnStartup(UIControlledApplication application)
    {
        RibbonHost.AddCommand(application, "Sheet Sync", "SheetSyncCommand", "Đồng bộ\nsheet", typeof(SyncSheetsCommand));
        return Result.Succeeded;
    }

    public Result OnShutdown(UIControlledApplication application) => Result.Succeeded;
}
