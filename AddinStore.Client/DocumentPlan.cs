using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace AddinStore;

internal sealed class DocumentPlan
{
    public List<Document> ToSave { get; } = [];
    public List<string> Unsaved { get; } = [];
    public string? Reopen { get; set; }

    public static DocumentPlan Capture(UIApplication application)
    {
        var plan = new DocumentPlan();
        var active = application.ActiveUIDocument?.Document;
        foreach (Document document in application.Application.Documents)
        {
            if (!document.IsValidObject || document.IsLinked)
            {
                continue;
            }

            var path = document.PathName;
            if (document.IsModified && (string.IsNullOrWhiteSpace(path) || document.IsReadOnly))
            {
                plan.Unsaved.Add(string.IsNullOrWhiteSpace(document.Title) ? "Tài liệu chưa đặt tên" : document.Title);
                continue;
            }

            if (document.IsModified)
            {
                plan.ToSave.Add(document);
            }
        }

        if (active != null && !string.IsNullOrWhiteSpace(active.PathName))
        {
            plan.Reopen = active.PathName;
        }

        return plan;
    }
}
