using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Text;

namespace Greg.Xrm.Command
{
	public static class Extensions
	{
		public static void RegisterCommandExecutors(this IServiceCollection services, Assembly assembly)
		{
			var genericCommandExecutorType = typeof(ICommandExecutor<>);
#pragma warning disable S6605 // Collection-specific "Exists" method should be used instead of the "Any" extension
			assembly
				.GetTypes()
				.Where(t => !t.IsAbstract && !t.IsInterface)
				.Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == genericCommandExecutorType))
				.ToList()
				.ForEach(t =>{
					var specificCommandExecutorType = t.GetInterfaces().First(i => i.IsGenericType && i.GetGenericTypeDefinition() == genericCommandExecutorType);
					services.AddTransient(specificCommandExecutorType, t);
				});
#pragma warning restore S6605 // Collection-specific "Exists" method should be used instead of the "Any" extension
		}



        // Originally made by Mikescher at https://gist.github.com/Mikescher/a1450d13980f4363b47cdab5430b411a
        // Licensed under "CC BY-SA 4.0" (specified by Stackoverflow's rules)
        public static IEnumerable<string> SplitArgs(this string commandLine)
        {
            var result = new StringBuilder();

            var quoted = false;
            var escaped = false;
            var started = false;
            var allowcaret = false;
            for (int i = 0; i < commandLine.Length; i++)
            {
                var chr = commandLine[i];

                if (chr == '^' && !quoted)
                {
                    if (allowcaret)
                    {
                        result.Append(chr);
                        started = true;
                        escaped = false;
                        allowcaret = false;
                    }
                    else if (i + 1 < commandLine.Length && commandLine[i + 1] == '^')
                    {
                        allowcaret = true;
                    }
                    else if (i + 1 == commandLine.Length)
                    {
                        result.Append(chr);
                        started = true;
                        escaped = false;
                    }
                }
                else if (escaped)
                {
                    result.Append(chr);
                    started = true;
                    escaped = false;
                }
                else if (chr == '"')
                {
                    quoted = !quoted;
                    started = true;
                }
                else if (chr == '\\' && i + 1 < commandLine.Length && commandLine[i + 1] == '"')
                {
                    escaped = true;
                }
                else if (chr == ' ' && !quoted)
                {
                    if (started) yield return result.ToString();
                    result.Clear();
                    started = false;
                }
                else
                {
                    result.Append(chr);
                    started = true;
                }
            }

            if (started) yield return result.ToString();
        }
    }
}
