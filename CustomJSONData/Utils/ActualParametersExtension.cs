using System;
using System.Reflection;
using HarmonyLib;

namespace CustomJSONData
{
    public static class ActualParametersExtension
    {
        // AccessTools.ActualParameters except this one throws exceptions.
        public static object[] ActualParameters(this MethodBase method, object[] inputs)
        {
            ParameterInfo[] parameters = method.GetParameters();
            object[] actualParameters = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo parameter = parameters[i];
                bool found = false;
                foreach (object input in inputs)
                {
                    if (input != null && parameter.ParameterType.IsAssignableFrom(input.GetType()))
                    {
                        actualParameters[i] = input;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    throw new InvalidOperationException($"[{method.FullDescription()}] requested [{parameter.ParameterType.FullName}] but was not available.");
                }
            }

            return actualParameters;
        }
    }
}
