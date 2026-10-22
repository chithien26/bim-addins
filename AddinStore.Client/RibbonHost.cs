using Autodesk.Revit.UI;

namespace AddinStore;

public static class RibbonHost
{
    public const string TabName = "BIM Add-ins";

    public static void AddCommand(
        UIControlledApplication application,
        string panelTitle,
        string commandId,
        string buttonText,
        Type commandType)
    {
        try
        {
            application.CreateRibbonTab(TabName);
        }
        catch (Autodesk.Revit.Exceptions.ArgumentException)
        {
        }

        RibbonPanel panel;
        try
        {
            panel = application.CreateRibbonPanel(TabName, panelTitle);
        }
        catch (Autodesk.Revit.Exceptions.ArgumentException)
        {
            panel = application.GetRibbonPanels(TabName).FirstOrDefault(item => item.Name == panelTitle)
                ?? throw new InvalidOperationException($"Không tạo được panel {panelTitle}.");
        }

        var data = new PushButtonData(
            commandId,
            buttonText,
            commandType.Assembly.Location,
            commandType.FullName ?? throw new InvalidOperationException("Lệnh không có tên lớp."));
        panel.AddItem(data);
    }
}
