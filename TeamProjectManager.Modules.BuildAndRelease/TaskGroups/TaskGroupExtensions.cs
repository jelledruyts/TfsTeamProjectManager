using Microsoft.TeamFoundation.DistributedTask.WebApi;

namespace TeamProjectManager.Modules.BuildAndRelease.TaskGroups
{
    public static class TaskGroupExtensions
    {
        public static TaskGroupCreateParameter ConvertToTaskGroupCreateParameter(this TaskGroup taskGroup)
        {
            if (taskGroup == null)
            {
                return null;
            }

            var create = new TaskGroupCreateParameter()
            {
                Author = taskGroup.Author,
                Category = taskGroup.Category,
                Description = taskGroup.Description,
                FriendlyName = taskGroup.FriendlyName,
                IconUrl = taskGroup.IconUrl,
                InstanceNameFormat = taskGroup.InstanceNameFormat,
                Name = taskGroup.Name,
                ParentDefinitionId = taskGroup.ParentDefinitionId,
                Version = taskGroup.Version,
            };

            foreach (var item in taskGroup.Inputs)
            {
                create.Inputs.Add(item);
            }

            foreach (var item in taskGroup.Tasks)
            {
                create.Tasks.Add(item);
            }

            return create;
        }
    }
}