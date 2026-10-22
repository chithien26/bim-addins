using AddinStore;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using TaskDialog = Autodesk.Revit.UI.TaskDialog;

namespace SheetSync;

[Transaction(TransactionMode.Manual)]
public class SyncSheetsCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        if (!StoreClient.Open(SheetSyncApplication.Slug, commandData))
        {
            return Result.Cancelled;
        }

        TaskDialog.Show("Sheet Sync", "Lệnh nghiệp vụ để sau. Cửa version đã qua.");
        return Result.Succeeded;
    }
}
