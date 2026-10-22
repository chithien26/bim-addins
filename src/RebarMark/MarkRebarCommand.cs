using AddinStore;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using TaskDialog = Autodesk.Revit.UI.TaskDialog;

namespace RebarMark;

[Transaction(TransactionMode.Manual)]
public class MarkRebarCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        if (!StoreClient.Open(RebarMarkApplication.Slug, commandData))
        {
            return Result.Cancelled;
        }

        TaskDialog.Show("Rebar Mark", "Lệnh nghiệp vụ để sau. Cửa version và license đã qua.");
        return Result.Succeeded;
    }
}
