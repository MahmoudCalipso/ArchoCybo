using ArchoCybo.Domain.Common;

namespace ArchoCybo.Application.Common;

public static class RepositoryMessageBuilder
{
    public static string Success(RepositoryAction action, string entityName)
        => action switch
        {
            RepositoryAction.Create =>
                $"YOUR ACTION CREATE ({entityName}) IS SUCCESSFULLY DONE",

            RepositoryAction.Update =>
                $"YOUR ACTION UPDATE ({entityName}) IS SUCCESSFULLY DONE",

            RepositoryAction.Delete =>
                $"YOUR ACTION DELETE ({entityName}) IS SUCCESSFULLY DONE",

            RepositoryAction.Get =>
                $"YOUR ACTION GET ({entityName}) IS SUCCESSFULLY DONE",

            _ => "YOUR ACTION IS SUCCESSFULLY DONE"
        };

    public static string Failed(RepositoryAction action, string entityName)
        => action switch
        {
            RepositoryAction.Create =>
                $"YOUR ACTION CREATE ({entityName}) FAILED",

            RepositoryAction.Update =>
                $"YOUR ACTION UPDATE ({entityName}) FAILED",

            RepositoryAction.Delete =>
                $"YOUR ACTION DELETE ({entityName}) FAILED",

            RepositoryAction.Get =>
                $"YOUR ACTION GET ({entityName}) FAILED",

            _ => "YOUR ACTION FAILED"
        };

    public static string NotFound(string entityName)
        => $"ENTITY ({entityName}) NOT FOUND";
}
